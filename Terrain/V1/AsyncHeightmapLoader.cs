using OpenGL;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Terrain
{
    /// <summary>
    /// 비동기 하이트맵 로더 (프레임 락 없음)
    /// </summary>
    public class AsyncHeightmapLoader
    {
        private readonly TileFormat _format;
        private readonly int _maxCacheSize;
        private readonly int _maxUploadsPerFrame;

        private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
        private readonly ArrayPool<float> _floatPool = ArrayPool<float>.Shared;

        // 로딩 큐
        private ConcurrentQueue<LoadRequest> _loadQueue = new ConcurrentQueue<LoadRequest>();
        private ConcurrentQueue<UploadRequest> _uploadQueue = new ConcurrentQueue<UploadRequest>();

        // 캐시 (LRU)
        private Dictionary<(int, int), CachedTile> _tileCache = new Dictionary<(int, int), CachedTile>();
        private LinkedList<(int, int)> _lruList = new LinkedList<(int, int)>();

        // 워커 스레드
        private Thread _loaderThread;
        private bool _isRunning = false;

        // 도움 변수
        private bool _wasLoading = false;
        private bool _uploadJustCompleted = false;
        private readonly bool _keepCpuData;

        // 통계
        private LoaderStatistics _stats = new LoaderStatistics();

        #region Nested Types

        private class LoadRequest
        {
            public string FilePath;
            public int RegionX;
            public int RegionY;
            public int Priority;
        }

        private class UploadRequest
        {
            public float[] Data;
            public int RegionX;
            public int RegionY;
        }

        private class CachedTile
        {
            public uint TextureId;
            public int RegionX;
            public int RegionY;
            public DateTime LastAccess;
            public float[] CpuData;
        }

        public class LoaderStatistics
        {
            public int LoadedCount;
            public int CacheHits;
            public int CacheMisses;

            public void Reset()
            {
                LoadedCount = 0;
                CacheHits = 0;
                CacheMisses = 0;
            }
        }

        #endregion

        /// <summary>
        /// 생성자
        /// </summary>
        public AsyncHeightmapLoader(TileFormat format, int maxCacheSize = 128, int maxUploadsPerFrame = 1, bool keepCpuData = false)
        {
            _format = format;
            _maxCacheSize = maxCacheSize;
            _maxUploadsPerFrame = maxUploadsPerFrame;
            _keepCpuData = keepCpuData;
        }

        /// <summary>
        /// 로더 시작
        /// </summary>
        public void Start(string typeName = "")
        {
            if (_isRunning) return;

            _isRunning = true;
            _loaderThread = new Thread(LoaderThreadFunc)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _loaderThread.Start();

            Console.WriteLine($"[AsyncLoader {typeName}] 시작됨");
        }

        /// <summary>
        /// 로더 중지
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _loaderThread?.Join();
            Console.WriteLine("[AsyncLoader] 중지됨");
        }

        public float? SampleHeightByUV(int regionX, int regionY, float u, float v)
        {
            lock (_tileCache)
            {
                if (!_tileCache.TryGetValue((regionX, regionY), out CachedTile tile)) return null;
                if (tile.CpuData == null) return null;

                int size = (int)_format.TileSize;

                // 셰이더: texture(heightMapHighRes, texCoord).r
                // OpenGL 텍셀 중심 보정 (-0.5)
                float texelX = u * size - 0.5f;
                float texelY = v * size - 0.5f;

                int ix = (int)Math.Floor(texelX);
                int iy = (int)Math.Floor(texelY);
                float s = texelX - ix;
                float t = texelY - iy;

                // 경계 클램프
                ix = Math.Max(0, Math.Min(ix, size - 2));
                iy = Math.Max(0, Math.Min(iy, size - 2));

                // 셰이더 texture() bilinear와 동일
                float h00 = tile.CpuData[iy * size + ix];
                float h10 = tile.CpuData[iy * size + (ix + 1)];
                float h01 = tile.CpuData[(iy + 1) * size + ix];
                float h11 = tile.CpuData[(iy + 1) * size + (ix + 1)];

                float h0 = h00 * (1f - s) + h10 * s;
                float h1 = h01 * (1f - s) + h11 * s;
                return h0 * (1f - t) + h1 * t;
            }
        }

        /// <summary>
        /// 타일 내 로컬 좌표로 높이 샘플링 (bilinear 보간, OpenGL 텍셀 중심 규칙)
        /// localX, localY: 0.0 ~ TileSize 범위의 실수 좌표
        /// </summary>
        public float? SampleHeight(int regionX, int regionY, float localX, float localY)
        {
            lock (_tileCache)
            {
                if (!_tileCache.TryGetValue((regionX, regionY), out CachedTile tile)) return null;
                if (tile.CpuData == null) return null;

                int size = (int)_format.TileSize;

                // OpenGL 텍셀 중심 보정 (-0.5)
                float texelX = localX - 0.5f;
                float texelY = localY - 0.5f;

                int ix = (int)Math.Floor(texelX);
                int iy = (int)Math.Floor(texelY);
                float s = texelX - ix;
                float t = texelY - iy;

                // 경계 체크
                if (ix < 0 || iy < 0 || ix >= size - 1 || iy >= size - 1)
                    return null;

                // Bilinear interpolation (TerrainData.InterpolateHeightOpenGLStyle와 동일)
                float h00 = tile.CpuData[iy * size + ix];
                float h10 = tile.CpuData[iy * size + (ix + 1)];
                float h01 = tile.CpuData[(iy + 1) * size + ix];
                float h11 = tile.CpuData[(iy + 1) * size + (ix + 1)];

                float h0 = h00 * (1.0f - s) + h10 * s;
                float h1 = h01 * (1.0f - s) + h11 * s;

                return h0 * (1.0f - t) + h1 * t;  // 0~1 정규화값
            }
        }

        /// <summary>
        /// 업로드 완료 체크
        /// </summary>
        public bool CheckUploadCompleted()
        {
            bool result = _uploadJustCompleted;
            if (result)
            {
                _uploadJustCompleted = false;
            }
            return result;
        }

        /// <summary>
        /// 타일 로드 요청 (비동기)
        /// </summary>
        public void RequestLoad(string filePath, int regionX, int regionY, int priority = 0)
        {
            var key = (regionX, regionY);

            // 캐시 확인
            lock (_tileCache)
            {
                if (_tileCache.ContainsKey(key))
                {
                    _stats.CacheHits++;
                    _tileCache[key].LastAccess = DateTime.Now;
                    UpdateLRU(key);
                    return;
                }
            }

            _stats.CacheMisses++;

            // 로딩 큐에 추가
            _loadQueue.Enqueue(new LoadRequest
            {
                FilePath = filePath,
                RegionX = regionX,
                RegionY = regionY,
                Priority = priority
            });
        }

        /// <summary>
        /// 타일 언로드
        /// </summary>
        public void UnloadTile(int regionX, int regionY)
        {
            var key = (regionX, regionY);

            lock (_tileCache)
            {
                if (_tileCache.TryGetValue(key, out CachedTile tile))
                {
                    Gl.DeleteTextures(tile.TextureId);
                    tile.CpuData = null;  // GC 해제
                    _tileCache.Remove(key);
                    _lruList.Remove(key);
                }
            }
        }

        /// <summary>
        /// 타일 텍스처 ID 가져오기
        /// </summary>
        public uint? GetTileTexture(int regionX, int regionY)
        {
            var key = (regionX, regionY);

            lock (_tileCache)
            {
                if (_tileCache.TryGetValue(key, out CachedTile tile))
                {
                    tile.LastAccess = DateTime.Now;
                    UpdateLRU(key);
                    return tile.TextureId;
                }
            }

            return null;
        }

        /// <summary>
        /// 메인 스레드에서 호출 (GPU 업로드 처리)
        /// </summary>
        public void ProcessUploads()
        {
            int uploadCount = 0;

            while (uploadCount < _maxUploadsPerFrame && _uploadQueue.TryDequeue(out UploadRequest req))
            {
                var key = (req.RegionX, req.RegionY);

                // 1. 텍스처 생성 전에 캐시 확인
                lock (_tileCache)
                {
                    if (_tileCache.ContainsKey(key))
                    {
                        // 이미 있으면 GPU 텍스처 생성 없이 그냥 skip
                        uploadCount++;
                        continue;
                    }
                }

                // 2. 없을 때만 GPU 텍스처 생성
                uint textureId = CreateTexture(req.Data);

                lock (_tileCache)
                {
                    // 3. lock 사이에 다른 곳에서 추가됐을 수도 있으니 한 번 더 확인
                    if (_tileCache.ContainsKey(key))
                    {
                        Gl.DeleteTextures(textureId);  // TOCTOU 방어
                        uploadCount++;
                        continue;
                    }

                    if (_tileCache.Count >= _maxCacheSize)
                        EvictOldestTile();

                    _tileCache[key] = new CachedTile
                    {
                        TextureId = textureId,
                        RegionX = req.RegionX,
                        RegionY = req.RegionY,
                        LastAccess = DateTime.Now,
                        CpuData = _keepCpuData ? req.Data : null
                    };

                    _lruList.AddFirst(key);
                }

                _stats.LoadedCount++;
                uploadCount++;
            }

            // 로딩 완료 감지
            bool isLoading = !_loadQueue.IsEmpty || !_uploadQueue.IsEmpty;

            if (_wasLoading && !isLoading)
            {
                _uploadJustCompleted = true;
            }

            _wasLoading = isLoading;
        }

        /// <summary>
        /// 워커 스레드 (파일 읽기)
        /// </summary>
        private void LoaderThreadFunc()
        {
            while (_isRunning)
            {
                if (_loadQueue.TryDequeue(out LoadRequest req))
                {
                    try
                    {
                        float[] data = LoadRawFile(req.FilePath);

                        if (data != null)
                        {
                            _uploadQueue.Enqueue(new UploadRequest
                            {
                                Data = data,
                                RegionX = req.RegionX,
                                RegionY = req.RegionY
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AsyncLoader] 로드 실패: {ex.Message}");
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// RAW 파일 읽기 (형식에 따라 처리)
        /// </summary>
        private float[] LoadRawFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            int expectedSize = _format.GetExpectedFileSize();

            // Pool에서 빌림
            byte[] rawBytes = _bytePool.Rent(expectedSize);

            try
            {
                // 직접 읽기
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead = fs.Read(rawBytes, 0, expectedSize);
                    if (bytesRead != expectedSize)
                    {
                        Console.WriteLine($"[AsyncLoader] 크기 오류: {bytesRead} != {expectedSize}");
                        return null;
                    }
                }

                // 처리
                if (_format.ChannelCount == 1)
                    return LoadSingleChannel(rawBytes, expectedSize);
                else if (_format.ChannelCount == 3)
                    return LoadTripleChannel(rawBytes);

                return null;
            }
            finally
            {
                // Pool에 반환 (중요!)
                _bytePool.Return(rawBytes);
            }
        }

        /// <summary>
        /// 단일 채널 로드 (Heightmap)
        /// </summary>
        private float[] LoadSingleChannel(byte[] rawBytes, int length)
        {
            uint size = _format.TileSize;

            // float 배열도 풀링 가능
            float[] normalizedData = _floatPool.Rent((int)(size * size));

            if (_format.BytesPerChannel == 2) // ushort
            {
                // Span 사용으로 추가 할당 방지
                Span<ushort> heightData = MemoryMarshal.Cast<byte, ushort>(
                    rawBytes.AsSpan(0, length));

                for (uint y = 0; y < size; y++)
                {
                    for (uint x = 0; x < size; x++)
                    {
                        uint srcIdx = y * size + x;
                        uint dstIdx = y * size + x;
                        normalizedData[dstIdx] = heightData[(int)srcIdx] / _format.NormalizeValue;
                    }
                }
            }

            // 정확한 크기로 복사 (Pool은 더 큰 배열을 줄 수 있음)
            float[] result = new float[size * size];
            Array.Copy(normalizedData, result, result.Length);

            _floatPool.Return(normalizedData);

            return result;
        }

        /// <summary>
        /// 3채널 로드 (Normal map)
        /// </summary>
        private float[] LoadTripleChannel(byte[] rawBytes)
        {
            uint size = _format.TileSize;
            float[] normalData = new float[size * size * 3];

            for (uint y = 0; y < size; y++)
            {
                for (uint x = 0; x < size; x++)
                {
                    uint srcIdx = (y * size + x) * 3;
                    uint dstIdx = (y * size + x) * 3;

                    normalData[dstIdx + 0] = rawBytes[srcIdx + 0] / _format.NormalizeValue;
                    normalData[dstIdx + 1] = rawBytes[srcIdx + 1] / _format.NormalizeValue;
                    normalData[dstIdx + 2] = rawBytes[srcIdx + 2] / _format.NormalizeValue;
                }
            }

            return normalData;
        }

        /// <summary>
        /// OpenGL 텍스처 생성
        /// </summary>
        private uint CreateTexture(float[] data)
        {
            uint texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, texture);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                _format.InternalFormat,
                (int)_format.TileSize,
                (int)_format.TileSize,
                0,
                _format.PixelFormat,
                PixelType.Float,
                data
            );

            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return texture;
        }

        /// <summary>
        /// LRU 캐시 업데이트
        /// </summary>
        private void UpdateLRU((int, int) key)
        {
            _lruList.Remove(key);
            _lruList.AddFirst(key);
        }

        /// <summary>
        /// 가장 오래된 타일 제거
        /// </summary>
        private void EvictOldestTile()
        {
            if (_lruList.Count == 0) return;

            var oldestKey = _lruList.Last.Value;
            _lruList.RemoveLast();

            if (_tileCache.TryGetValue(oldestKey, out CachedTile tile))
            {
                Gl.DeleteTextures(tile.TextureId);
                tile.CpuData = null;  // GC 해제
                _tileCache.Remove(oldestKey);
                //Console.WriteLine($"[AsyncLoader] LRU 제거: Region({tile.RegionX}, {tile.RegionY})");
            }
        }

        /// <summary>
        /// 통계 조회
        /// </summary>
        public LoaderStatistics GetStatistics() => _stats;

        /// <summary>
        /// 통계 출력
        /// </summary>
        public void PrintStats()
        {
            Console.WriteLine("═══════════════════════════════════");
            Console.WriteLine($"로드된 타일: {_stats.LoadedCount}");
            Console.WriteLine($"캐시 크기: {_tileCache.Count}/{_maxCacheSize}");
            Console.WriteLine($"캐시 히트: {_stats.CacheHits}");
            Console.WriteLine($"캐시 미스: {_stats.CacheMisses}");
            Console.WriteLine($"대기 중 로드: {_loadQueue.Count}");
            Console.WriteLine($"대기 중 업로드: {_uploadQueue.Count}");
            Console.WriteLine("═══════════════════════════════════");
        }

        /// <summary>
        /// 정리
        /// </summary>
        public void Cleanup()
        {
            Stop();

            lock (_tileCache)
            {
                foreach (var tile in _tileCache.Values)
                {
                    Gl.DeleteTextures(tile.TextureId);
                }
                _tileCache.Clear();
                _lruList.Clear();
            }

            Console.WriteLine("[AsyncLoader] 정리 완료");
        }
    }
}