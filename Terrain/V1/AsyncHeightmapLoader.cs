using Common;
using OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Terrain
{
    /// <summary>
    /// 비동기 하이트맵 로더 (프레임 락 없음)
    /// </summary>
    public class AsyncHeightmapLoader
    {
        private readonly uint TILE_SIZE = Constants.TERRAIN_TILE_SIZE;

        // 로딩 큐
        private ConcurrentQueue<LoadRequest> _loadQueue = new ConcurrentQueue<LoadRequest>();
        private ConcurrentQueue<UploadRequest> _uploadQueue = new ConcurrentQueue<UploadRequest>();

        // 캐시 (LRU)
        private Dictionary<string, CachedTile> _tileCache = new Dictionary<string, CachedTile>();
        private LinkedList<string> _lruList = new LinkedList<string>();
        private int _maxCacheSize = 128; // 최대 32개 타일 캐싱

        // 워커 스레드
        private Thread _loaderThread;
        private bool _isRunning = false;

        // 도움 변수
        private bool _wasLoading = false;
        private bool _uploadJustCompleted = false;

        // 통계
        private int _loadedCount = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;

        private class LoadRequest
        {
            public string FilePath;
            public int RegionX;
            public int RegionY;
            public int Priority; // 낮을수록 우선순위 높음
        }

        private class UploadRequest
        {
            public string Key;
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
        }

        public AsyncHeightmapLoader(uint tileSize = 129)
        {
            TILE_SIZE = tileSize;
        }

        /// <summary>
        /// 로더 시작
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _loaderThread = new Thread(LoaderThreadFunc)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _loaderThread.Start();

            Console.WriteLine("[AsyncLoader] 시작됨");
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CheckUploadCompleted()
        {
            bool result = _uploadJustCompleted;
            if (result)
            {
                _uploadJustCompleted = false; // 읽은 후 리셋
            }
            return result;
        }


        /// <summary>
        /// 타일 로드 요청 (비동기)
        /// </summary>
        public void RequestLoad(string filePath, int regionX, int regionY, int priority = 0)
        {
            string key = GetCacheKey(regionX, regionY);

            // 캐시 확인
            lock (_tileCache)
            {
                if (_tileCache.ContainsKey(key))
                {
                    _cacheHits++;
                    _tileCache[key].LastAccess = DateTime.Now;
                    UpdateLRU(key);
                    return;
                }
            }

            _cacheMisses++;

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
            string key = GetCacheKey(regionX, regionY);

            lock (_tileCache)
            {
                if (_tileCache.TryGetValue(key, out CachedTile tile))
                {
                    Gl.DeleteTextures(tile.TextureId);
                    _tileCache.Remove(key);
                    _lruList.Remove(key);
                    //Console.WriteLine($"[AsyncLoader] 언로드: Region({regionX}, {regionY})");
                }
            }
        }

        /// <summary>
        /// 타일 텍스처 ID 가져오기
        /// </summary>
        public uint? GetTileTexture(int regionX, int regionY)
        {
            string key = GetCacheKey(regionX, regionY);

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
        /// 매 프레임마다 호출
        /// </summary>
        public void ProcessUploads(int maxUploadsPerFrame = 1)
        {
            int uploadCount = 0;

            while (uploadCount < maxUploadsPerFrame && _uploadQueue.TryDequeue(out UploadRequest req))
            {
                // GPU 텍스처 생성
                uint textureId = CreateTexture(req.Data);

                // 캐시에 추가
                lock (_tileCache)
                {
                    // 캐시 크기 체크
                    if (_tileCache.Count >= _maxCacheSize)
                    {
                        EvictOldestTile();
                    }

                    _tileCache[req.Key] = new CachedTile
                    {
                        TextureId = textureId,
                        RegionX = req.RegionX,
                        RegionY = req.RegionY,
                        LastAccess = DateTime.Now
                    };

                    _lruList.AddFirst(req.Key);
                }

                _loadedCount++;
                //Console.WriteLine($"[AsyncLoader] 업로드 완료: Region({req.RegionX}, {req.RegionY}), ID: {textureId}");

                uploadCount++;
            }

            // 로딩 완료 감지
            bool isLoading = !_loadQueue.IsEmpty || !_uploadQueue.IsEmpty;

            if (_wasLoading && !isLoading)
            {
                _uploadJustCompleted = true; // 로딩 완료된 바로 그 순간
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
                        // 파일 읽기 (I/O 블로킹, 별도 스레드)
                        float[] data = LoadRawFile(req.FilePath);

                        if (data != null)
                        {
                            string key = GetCacheKey(req.RegionX, req.RegionY);

                            // 업로드 큐에 추가 (메인 스레드에서 처리)
                            _uploadQueue.Enqueue(new UploadRequest
                            {
                                Key = key,
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
                    Thread.Sleep(10); // 큐가 비었으면 대기
                }
            }
        }

        /// <summary>
        /// RAW 파일 읽기 (백그라운드 스레드)
        /// </summary>
        private float[] LoadRawFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                string filename = Path.GetFileName(filePath);
                //Console.WriteLine($"[AsyncLoader] 파일 없음: {filename}");
                return null;
            }
            byte[] rawBytes = File.ReadAllBytes(filePath);
            int expectedSize = (int)(TILE_SIZE * TILE_SIZE * 2);
            if (rawBytes.Length != expectedSize)
            {
                Console.WriteLine($"[AsyncLoader] 크기 오류: {rawBytes.Length}");
                return null;
            }

            // ushort -> float 변환
            ushort[] heightData = new ushort[TILE_SIZE * TILE_SIZE];
            System.Buffer.BlockCopy(rawBytes, 0, heightData, 0, rawBytes.Length);

            float[] normalizedData = new float[TILE_SIZE * TILE_SIZE];

            // X, Y 스왑하면서 변환 (transpose)
            for (uint y = 0; y < TILE_SIZE; y++)
            {
                for (uint x = 0; x < TILE_SIZE; x++)
                {
                    uint srcIdx = y * TILE_SIZE + x;
                    uint dstIdx = x * TILE_SIZE + y;  // transpose
                    normalizedData[dstIdx] = heightData[srcIdx] / 65535.0f;
                }
            }

            return normalizedData;
        }

        /// <summary>
        /// OpenGL 텍스처 생성 (메인 스레드)
        /// </summary>
        private uint CreateTexture(float[] data)
        {
            uint texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, texture);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32f,
                (int)TILE_SIZE, (int)TILE_SIZE, 0,
                OpenGL.PixelFormat.Red,
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
        private void UpdateLRU(string key)
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

            string oldestKey = _lruList.Last.Value;
            _lruList.RemoveLast();

            if (_tileCache.TryGetValue(oldestKey, out CachedTile tile))
            {
                Gl.DeleteTextures(tile.TextureId);
                _tileCache.Remove(oldestKey);
                Console.WriteLine($"[AsyncLoader] LRU 제거: Region({tile.RegionX}, {tile.RegionY})");
            }
        }

        private string GetCacheKey(int x, int y) => $"{x}_{y}";

        /// <summary>
        /// 통계 출력
        /// </summary>
        public void PrintStats()
        {
            Console.WriteLine("═══════════════════════════════════");
            Console.WriteLine($"로드된 타일: {_loadedCount}");
            Console.WriteLine($"캐시 크기: {_tileCache.Count}/{_maxCacheSize}");
            Console.WriteLine($"캐시 히트: {_cacheHits}");
            Console.WriteLine($"캐시 미스: {_cacheMisses}");
            Console.WriteLine($"대기 중 로드: {_loadQueue.Count}");
            Console.WriteLine($"대기 중 업로드: {_uploadQueue.Count}");
            Console.WriteLine("═══════════════════════════════════");
        }

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