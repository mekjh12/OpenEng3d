using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// 지형의 높이 데이터를 관리하는 클래스입니다.
    /// 높이맵 이미지를 로드하고 지형의 높이값을 계산하는 기능을 제공합니다.
    /// </summary>
    public class TerrainData
    {
        /// <summary>
        /// 대기 중인 텍스처 업데이트 정보를 저장할 구조체
        /// </summary>
        private struct TextureUpdateInfo
        {
            public byte[] Data;    // 텍스처 업데이트에 사용될 바이트 데이터
            public int TileX;      // 타일의 X 좌표
            public int TileY;      // 타일의 Y 좌표
            public int TileSize;   // 타일의 크기
        }

        private float[] _heightmapLowRes;         // 125x125 크기의 저해상도 높이맵 데이터
        private float[] _heightmapHighRes;        // 2000x2000 크기의 고해상도 높이맵 데이터
        private bool _isLowResLoaded;             // 저해상도 높이맵이 로드되었는지 여부
        private bool _isHighResLoaded;            // 고해상도 높이맵이 로드되었는지 여부
        private int _heightmapWidth;              // 높이맵의 너비
        private int _heightmapHeight;             // 높이맵의 높이
        private int _regionOriginalSize;          // 리전의 원본 크기(미터 단위)
        private float _regionHalfSize;            // 리전 크기의 절반 값(계산 최적화용)
        private Texture _heightMapTextureLowRes;  // GPU에 업로드된 저해상도 높이맵 텍스처 ID
        private Texture _heightMapTextureHighRes; // GPU에 업로드된 고해상도 높이맵 텍스처 ID
        private float[] _maxChunks;               // 청크별 최대 높이값 배열
        private float[] _minChunks;               // 청크별 최소 높이값 배열
        private float _maxHeight;                 // 리전 전체의 최대 높이값
        private float _minHeight;                 // 리전 전체의 최소 높이값

        private Queue<string> _pendingTileFiles;  // 로드 대기 중인 타일 파일 경로 큐(pending 계류중인)
        private uint _pendingTextureID;           // 임시로 저장할 텍스처 ID(타일 업데이트용)
        private List<TextureUpdateInfo> _pendingTextureUpdates = new List<TextureUpdateInfo>(); 
                                                  // 텍스처 업데이트를 대기 중인 정보 목록
        private bool _needTextureInitFlag;        // 텍스처 초기화가 필요한지 여부의 플래그

        /// <summary>높이맵의 너비를 반환합니다.</summary>
        public int Width => _heightmapWidth;
        /// <summary>높이맵의 높이를 반환합니다.</summary>
        public int Height => _heightmapHeight;
        /// <summary>저해상도 맵 로드 여부</summary>
        public bool IsLowResLoaded => _isLowResLoaded;
        /// <summary>고해상도 맵 로드 여부</summary>
        public bool IsHighResLoaded { get => _isHighResLoaded; set => _isHighResLoaded = value; }
        /// <summary>지형의 전체 크기(미터 단위)를 반환합니다.</summary>
        public float Size => _regionOriginalSize;
        /// <summary>리전의 최대 높이값</summary>
        public float MaxHeight => _maxHeight;
        /// <summary>리전의 최소 높이값</summary>
        public float MinHeight => _minHeight;

        public Texture HeightMapTextureLowRes { get => _heightMapTextureLowRes; set => _heightMapTextureLowRes = value; }
        public Texture HeightMapTextureHighRes { get => _heightMapTextureHighRes; set => _heightMapTextureHighRes = value; }

        /// <summary>
        /// 생성자
        /// </summary>
        public TerrainData()
        {
            _isLowResLoaded = false;
            _isHighResLoaded = false;
        }

        /// <summary>
        /// 높이맵 버퍼를 고해상도로 교체합니다.
        /// 모든 고해상도 타일이 로드되면 호출하여 저해상도에서 고해상도로 전환합니다.
        /// </summary>
        public void SwapMapTextureBuffer()
        {
            // 고해상도 텍스처로 바꾸기(해상도, 너비, 높이 등)
            _heightmapWidth = _regionOriginalSize;
            _heightmapHeight = _regionOriginalSize;
            _regionHalfSize = _regionOriginalSize * 0.5f;
        }

        /// <summary>
        /// 리전 데이터를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            _isLowResLoaded = false;
            _isHighResLoaded = false;
        }

        /// <summary>
        /// 저해상도맵으로부터 생성한 청크의 최소, 최대 높이를 가져온다.
        /// </summary>
        /// <param name="cx">청크의 X 인덱스</param>
        /// <param name="cy">청크의 Y 인덱스</param>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <returns>청크의 최소/최대 높이값</returns>
        public Vertex2f GetHeightBound(int cx, int cy, int n)
        {
            // 저해상도맵을 로딩하지 않았으면 0.0를 반환한다.
            if (_minChunks == null) return Vertex2f.Zero;
            if (_maxChunks == null) return Vertex2f.Zero;

            int n2 = 2 * n;
            int cIndex = n2 * cy + cx;
            return new Vertex2f(_minChunks[cIndex], _maxChunks[cIndex]);
        }

        /// <summary>
        /// 고해상도 타일 로딩을 초기화합니다. <br/>
        /// 지정된 리전에 대한 모든 타일 파일을 큐에 추가합니다.
        /// </summary>
        /// <param name="coord">리전 좌표</param>
        /// <param name="basePath">높이맵 파일 기본 경로</param>
        public void InitializeHighResLoading(RegionCoord coord, string basePath)
        {
            _pendingTileFiles = new Queue<string>();

            // 8x8 타일의 파일명들을 큐에 추가
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    string highResFileName = basePath +
                        $"region_{coord.X}x{coord.Y}_tiles\\tile_{x}_{y}.png";

                    if (File.Exists(highResFileName))
                    {
                        _pendingTileFiles.Enqueue(highResFileName);
                    }
                }
            }
        }

        /// <summary>
        /// 큐에서 다음 고해상도 타일을 로드합니다.
        /// 비동기적으로 타일을 로드하고 CPU와 GPU 메모리에 업데이트합니다.
        /// </summary>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <param name="chunkSize">청크 크기</param>
        /// <returns>비동기 작업</returns>
        public async Task LoadNextTile(int n, int chunkSize)
        {
            if (_pendingTileFiles.Count == 0)
                return;

            string fileName = _pendingTileFiles.Dequeue();
            string baseFileName = Path.GetFileNameWithoutExtension(fileName); // "tile_3_0"
            string[] parts = baseFileName.Split('_');  // ["tile", "3", "0"]

            // 타일 좌표 추출 ("tile_3_0.raw" -> x=3, y=0)
            int tileX = int.Parse(parts[1]);
            int tileY = int.Parse(parts[2]);

            await LoadHighResPartialFile(fileName, tileX, tileY, 250, n, chunkSize);
        }

        /// <summary>
        /// 모든 고해상도 타일이 로드되었는지 확인합니다.
        /// </summary>
        /// <returns>모든 타일이 로드되었으면 true, 아니면 false</returns>
        public bool IsAllTilesLoaded()
        {
            if (_pendingTileFiles == null) return false;
            return _pendingTileFiles.Count == 0;
        }

        /// <summary>
        /// 높이맵 이미지 파일로부터 지형 데이터를 로드합니다.
        /// 이미지의 빨간색 채널을 높이값으로 사용하며, 0-1 사이로 정규화됩니다.
        /// </summary>
        /// <param name="fileName">로드할 높이맵 이미지의 파일 경로</param>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <param name="chunkSize">청크 크기</param>
        /// <returns>로드된 비트맵 객체</returns>
        /// <exception cref="FileNotFoundException">이미지 파일을 찾을 수 없는 경우</exception>
        /// <exception cref="ArgumentException">올바르지 않은 이미지 형식인 경우</exception>
        public async Task<Bitmap> LoadFromFile(string fileName, int n, int chunkSize)
        {
            if (!File.Exists(fileName)) return null;

            Bitmap bitmap = await Task.Run(() => (Bitmap)Image.FromFile(fileName));

            _heightmapWidth = bitmap.Width;
            _heightmapHeight = bitmap.Height;
            _regionHalfSize = _heightmapWidth / 2;
            _regionOriginalSize = 2 * n * chunkSize;

            if (!_isLowResLoaded)
            {
                await LoadHeightMapFromLowBitmap(fileName, bitmap, n, chunkSize);
            }

            return bitmap;
        }

        /// <summary>
        /// 저해상도 비트맵에서 높이맵 데이터를 로드합니다.
        /// </summary>
        /// <param name="fileName">로드할 높이맵 이미지의 파일 경로</param>
        /// <param name="bitmap">로드된 비트맵 객체</param>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <param name="chunkSize">청크 크기</param>
        /// <returns>비동기 작업</returns>
        public async Task LoadHeightMapFromLowBitmap(string fileName, Bitmap bitmap, int n, int chunkSize)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int w = bmpData.Width;
            int h = bmpData.Height;
            int regionSize = 2 * n * chunkSize;
            float ratio = (float)regionSize / (float)w;
            int dividedChunkCount = 2 * n;
            try
            {
                await Task.Run(() =>
                {
                    const int BytesPerPixel = 4;
                    int width = bitmap.Width;
                    int height = bitmap.Height;
                    int stride = Math.Abs(bmpData.Stride);
                    byte[] pixels = new byte[stride * height];

                    // 높이맵 배열 초기화
                    _heightmapLowRes = new float[width * height];

                    // 청크별 최대, 최소 높이를 위한 배열 초기화
                    int totalChunkCount = dividedChunkCount * dividedChunkCount;
                    _maxChunks = new float[totalChunkCount];
                    _minChunks = new float[totalChunkCount];
                    for (int i = 0; i < totalChunkCount; i++)
                    {
                        _maxChunks[i] = float.MinValue;
                        _minChunks[i] = float.MaxValue;
                    }

                    // 청크별 락 객체 생성
                    object[] locks = new object[totalChunkCount];
                    for (int i = 0; i < totalChunkCount; i++)
                    {
                        locks[i] = new object();
                    }
                    Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);

                    // 처리할 청크의 크기 정의
                    const int PARALLEL_BATCH_SIZE = 16;

                    string fn = Path.GetFileName(fileName);
                    //LogProfile.WriteLine($"[{DateTime.Now}] {fn} 처리를 시작합니다.");

                    // y축 방향 병렬 처리
                    Parallel.For(0, height, y =>
                    {
                        int rowOffset = y * stride;
                        int heightMapOffset = y * width;
                        int cy = (int)((ratio * y) / chunkSize);

                        // 경계 검사 추가
                        if (cy >= dividedChunkCount) cy = dividedChunkCount - 1;
                        int cyOffset = dividedChunkCount * cy;

                        // x축 방향을 청크 단위로 병렬 처리
                        Parallel.For(0, (width + PARALLEL_BATCH_SIZE - 1) / PARALLEL_BATCH_SIZE, batchIndex =>
                        {
                            int startX = batchIndex * PARALLEL_BATCH_SIZE;
                            int endX = Math.Min(startX + PARALLEL_BATCH_SIZE, width);
                            for (int x = startX; x < endX; x++)
                            {
                                int pixelOffset = rowOffset + (x * BytesPerPixel);

                                // 픽셀 값 - 그레이스케일로 변환 (RGB 평균 사용)
                                float value = (pixels[pixelOffset] + pixels[pixelOffset + 1] + pixels[pixelOffset + 2]) / (3.0f * 255.0f);
                                //float value = pixels[pixelOffset + 2] / 255.0f;

                                // 높이맵 배열에 값 저장 
                                _heightmapLowRes[heightMapOffset + x] = value;

                                int cx = (int)((ratio * x) / chunkSize);

                                // 경계 검사 추가
                                if (cx >= dividedChunkCount) cx = dividedChunkCount - 1;
                                int cIndex = cyOffset + cx;

                                // 경계 체크
                                if (cIndex >= 0 && cIndex < totalChunkCount)
                                {
                                    lock (locks[cIndex])
                                    {
                                        _maxChunks[cIndex] = Math.Max(_maxChunks[cIndex], value);
                                        _minChunks[cIndex] = Math.Min(_minChunks[cIndex], value);
                                    }
                                }
                            }
                        });
                    });

                    // 청크를 모두 살펴본 후 리전의 최대, 최소 높이
                    _maxHeight = float.MinValue;
                    _minHeight = float.MaxValue;
                    for (int i = 0; i < _maxChunks.Length; i++)
                    {
                        _maxHeight = Math.Max(_maxHeight, _maxChunks[i]);
                        _minHeight = Math.Min(_minHeight, _minChunks[i]);
                    }

                    // 높이맵 데이터가 적절히 로드되었는지 확인 (디버깅용)
                    Console.WriteLine($"HeightMap loaded: {_heightmapLowRes.Length} values, Min: {_minHeight}, Max: {_maxHeight}");
                });
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
                _isLowResLoaded = true;
            }
        }

        /// <summary>
        /// 주어진 3D 위치에서의 지형 높이를 계산하여 반환합니다.
        /// </summary>
        /// <param name="positionInRegionSpace">리전좌표공간의 위치</param>
        /// <returns>해당 위치의 높이가 적용된 새로운 3D 좌표</returns>
        /// <remarks>
        /// 입력된 position의 x, y 좌표는 그대로 사용되며,
        /// z 좌표만 계산된 높이값으로 대체됩니다.
        /// </remarks>
        public void GetTerrainHeightVertex3f(ref Vertex3f positionInRegionSpace)
        {
            float height = GetTerrainHeight(ref positionInRegionSpace, TerrainConstants.DEFAULT_VERTICAL_SCALE);
            positionInRegionSpace.z = height;
        }

        /// <summary>
        /// 지형의 특정 위치에서 보간된 높이값을 계산합니다.
        /// 두 맵이 모두 로딩된 경우 블렌딩 계수를 사용해 부드럽게 전환합니다.
        /// </summary>
        public float GetTerrainHeight(ref Vertex3f positionInRegionSpace, float verticalScale, float blendFactor = 1.0f)
        {
            // 아무 높이맵도 로드되지 않았다면 0 반환
            if (!_isLowResLoaded && !_isHighResLoaded) return 0.0f;

            // 먼저 두 결과값을 저장할 변수를 초기화합니다
            float heightLow = 0.0f;
            float heightHigh = 0.0f;

            // 저해상도 맵이 로드되어 있고 완전히 고해상도로 전환되지 않았다면(blendFactor < 1) 저해상도 높이 계산
            if (_isLowResLoaded && (blendFactor < 1.0f || !_isHighResLoaded))
            {
                // 해상도에 따른 픽셀 조회 비율을 계산한다.
                float ratioLow = (float)_heightmapWidth / _regionOriginalSize;
                float px = positionInRegionSpace.x * ratioLow;
                float py = positionInRegionSpace.y * ratioLow;
                int ix = (int)Math.Floor(px + _regionHalfSize);
                int iy = (int)Math.Floor(py + _regionHalfSize);
                float s = px + _regionHalfSize - ix;
                float t = py + _regionHalfSize - iy;

                // 경계 체크
                if (ix < 1 || iy < 1 || iy >= _heightmapWidth - 1 || ix >= _heightmapHeight - 1)
                    heightLow = 0.0f;
                else
                    heightLow = InterpolateHeight(_heightmapLowRes, ix, iy, s, t, _heightmapWidth);
            }

            // 고해상도 맵이 로드되어 있다면 고해상도 높이 계산
            if (_isHighResLoaded && blendFactor > 0.0f)
            {
                float ratioHigh = 1.0f; // 고해상도는 비율이 1:1
                float px = positionInRegionSpace.x * ratioHigh;
                float py = positionInRegionSpace.y * ratioHigh;
                int ix = (int)Math.Floor(px + _regionHalfSize);
                int iy = (int)Math.Floor(py + _regionHalfSize);
                float s = px + _regionHalfSize - ix;
                float t = py + _regionHalfSize - iy;

                // 경계 체크
                if (ix < 1 || iy < 1 || iy >= _heightmapWidth - 1 || ix >= _heightmapHeight - 1)
                    heightHigh = 0.0f;
                else
                    heightHigh = InterpolateHeight(_heightmapHighRes, ix, iy, s, t, _heightmapWidth);
            }

            // 최종 높이 계산
            float finalHeight;

            // 두 맵이 모두 로드되어 있다면 블렌딩 계수에 따라 보간
            if (_isLowResLoaded && _isHighResLoaded)
            {
                finalHeight = (1.0f - blendFactor) * heightLow + blendFactor * heightHigh;
            }
            // 고해상도 맵만 로드된 경우
            else if (_isHighResLoaded)
            {
                finalHeight = heightHigh;
            }
            // 저해상도 맵만 로드된 경우
            else
            {
                finalHeight = heightLow;
            }

            return verticalScale * finalHeight;
        }

        /// <summary>
        /// 높이맵에서 이중 선형 보간을 통해 높이값을 계산합니다.
        /// </summary>
        private float InterpolateHeight(float[] heightmap, int ix, int iy, float s, float t, int width)
        {
            if (heightmap == null) return 0.0f;

            // 3x3 그리드의 높이값 가져오기
            float a = heightmap[(iy - 1) * width + (ix - 1)];
            float b = heightmap[(iy - 1) * width + ix];
            float c = heightmap[(iy - 1) * width + (ix + 1)];
            float d = heightmap[iy * width + (ix - 1)];
            float e = heightmap[iy * width + ix];
            float f = heightmap[iy * width + (ix + 1)];
            float g = heightmap[(iy + 1) * width + (ix - 1)];
            float h = heightmap[(iy + 1) * width + ix];
            float i = heightmap[(iy + 1) * width + (ix + 1)];

            float height;
            float u, v;

            // 이중 선형 보간 수행
            if (s < 0.5f && t < 0.5f)
            {
                u = s + 0.5f; v = t + 0.5f;
                height = (1 - u) * (1 - v) * a + u * (1 - v) * b + (1 - u) * v * d + u * v * e;
            }
            else if (s < 0.5f && t >= 0.5f)
            {
                u = s + 0.5f; v = t - 0.5f;
                height = (1 - u) * (1 - v) * d + u * (1 - v) * e + (1 - u) * v * g + u * v * h;
            }
            else if (s >= 0.5f && t >= 0.5f)
            {
                u = s - 0.5f; v = t - 0.5f;
                height = (1 - u) * (1 - v) * e + u * (1 - v) * f + (1 - u) * v * h + u * v * i;
            }
            else
            {
                u = s - 0.5f; v = t + 0.5f;
                height = (1 - u) * (1 - v) * b + u * (1 - v) * c + (1 - u) * v * e + u * v * f;
            }

            return height;
        }

        private float InterpolateHeight(float[] heightmap, int ix, int iy, float s, float t, int width, int height)
        {
            if (heightmap == null) return 0.0f;

            // 경계 오프셋 설정 (픽셀 단위)
            const int borderOffset = 0;

            // 안전한 인덱스 계산 함수 - 경계로부터 오프셋만큼 안쪽으로 조정
            Func<int, int, int> safeIndex = (x, y) => {
                // 경계를 벗어날 경우 안쪽으로 오프셋 적용
                int safeX = x < borderOffset ? borderOffset :
                            x >= width - borderOffset ? width - borderOffset - 1 : x;
                int safeY = y < borderOffset ? borderOffset :
                            y >= height - borderOffset ? height - borderOffset - 1 : y;
                return safeY * width + safeX;
            };

            // 3x3 그리드의 높이값 가져오기 (안전한 인덱스 사용)
            float a = heightmap[safeIndex(ix - 1, iy - 1)];
            float b = heightmap[safeIndex(ix, iy - 1)];
            float c = heightmap[safeIndex(ix + 1, iy - 1)];
            float d = heightmap[safeIndex(ix - 1, iy)];
            float e = heightmap[safeIndex(ix, iy)];
            float f = heightmap[safeIndex(ix + 1, iy)];
            float g = heightmap[safeIndex(ix - 1, iy + 1)];
            float h = heightmap[safeIndex(ix, iy + 1)];
            float i = heightmap[safeIndex(ix + 1, iy + 1)];

            float finalHeight;
            float u, v;

            // 이중 선형 보간 수행
            if (s < 0.5f && t < 0.5f)
            {
                u = s + 0.5f; v = t + 0.5f;
                finalHeight = (1 - u) * (1 - v) * a + u * (1 - v) * b + (1 - u) * v * d + u * v * e;
            }
            else if (s < 0.5f && t >= 0.5f)
            {
                u = s + 0.5f; v = t - 0.5f;
                finalHeight = (1 - u) * (1 - v) * d + u * (1 - v) * e + (1 - u) * v * g + u * v * h;
            }
            else if (s >= 0.5f && t >= 0.5f)
            {
                u = s - 0.5f; v = t - 0.5f;
                finalHeight = (1 - u) * (1 - v) * e + u * (1 - v) * f + (1 - u) * v * h + u * v * i;
            }
            else
            {
                u = s - 0.5f; v = t + 0.5f;
                finalHeight = (1 - u) * (1 - v) * b + u * (1 - v) * c + (1 - u) * v * e + u * v * f;
            }

            return finalHeight;
        }


        /// <summary>
        /// 메인 렌더링 스레드에서 호출될 메서드로, 고해상도 텍스처를 초기화하고 타일 데이터를 GPU로 업로드합니다.
        /// </summary>
        /// <param name="regionSize">리전의 크기(픽셀 단위)</param>
        public void UpdateTexturesOnMainThread(int regionSize)
        {
            // 고해상도맵 텍스처가 아직 초기화되지 않은 경우 실행 (초기에 한번만 실행된다.)
            if (_needTextureInitFlag)
            {
                // 기존 텍스처가 있으면 정리
                if (_heightMapTextureHighRes != null)
                {
                    _heightMapTextureHighRes.Clear();
                }

                // 단일 채널(Red) 텍스처 생성
                uint textureId = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, textureId);

                // 1바이트 정렬 설정 - 단일 채널 텍스처의 행이 4바이트 경계에 맞지 않을 때 발생하는 
                // 대각선 왜곡 현상을 방지하기 위한 설정
                Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                // 빈 텍스처 초기화 (regionSize x regionSize 크기)
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R8,
                    regionSize, regionSize, 0,
                    OpenGL.PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);

                // 텍스처 필터링 설정
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // 텍스처 객체 생성 및 초기화
                _heightMapTextureHighRes = new Texture(regionSize, regionSize)
                {
                    TextureID = textureId,
                };

                // 초기화 완료 플래그 설정
                _needTextureInitFlag = false;
            }

            // 업데이트할 타일텍스처가 없으면 리턴한다.
            if (_pendingTextureUpdates.Count == 0) return;

            // 대기 중인 타일 업데이트 정보를 새 리스트로 복사 (스레드 안전성을 위해)
            List<TextureUpdateInfo> updates = new List<TextureUpdateInfo>(_pendingTextureUpdates);
            _pendingTextureUpdates.Clear();

            // 각 타일 데이터를 GPU 텍스처에 업데이트
            foreach (TextureUpdateInfo update in updates)
            {
                // 디버깅용 타일 정보 출력
                //Console.WriteLine($"Updating texture region: ({update.TileX}, {update.TileY}), " + $"Size: {update.TileSize}, First value: {update.Data[0]}");

                // 텍스처 바인딩
                Gl.BindTexture(TextureTarget.Texture2d, _heightMapTextureHighRes.TextureID);

                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // 텍스처의 특정 영역(타일) 업데이트
                Gl.TexSubImage2D(
                    TextureTarget.Texture2d,
                    0,                          // mipmap 레벨
                    update.TileX * update.TileSize,  // x 오프셋 - 타일 위치 기반
                    update.TileY * update.TileSize,  // y 오프셋 - 타일 위치 기반
                    update.TileSize,            // 업데이트할 너비
                    update.TileSize,            // 업데이트할 높이
                    OpenGL.PixelFormat.Red,     // 데이터 포맷 (단일 채널)
                    PixelType.UnsignedByte,     // 데이터 타입 (8비트)
                    update.Data);               // 픽셀 데이터
            }

            // 모든 타일이 로드되고 업데이트가 있었다면 추가 작업 수행
            if (IsAllTilesLoaded() && updates.Count > 0)
            {
                Gl.BindTexture(TextureTarget.Texture2d, _heightMapTextureHighRes.TextureID);
                // Mipmap 생성은 현재 비활성화
                //Gl.GenerateMipmap(TextureTarget.Texture2d);
            }
        }

        /// <summary>
        /// 고해상도 높이맵의 부분(타일)을 로드합니다.
        /// </summary>
        /// <param name="fileName">로드할 타일 이미지의 파일 경로</param>
        /// <param name="tileX">타일의 X 좌표(0-7)</param>
        /// <param name="tileY">타일의 Y 좌표(0-7)</param>
        /// <param name="tileSize">타일의 크기(픽셀 단위, 보통 250)</param>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <param name="chunkSize">청크 크기</param>
        public async Task LoadHighResPartialFile(string fileName, int tileX, int tileY, int tileSize, int n, int chunkSize)
        {
            if (!File.Exists(fileName)) return;
            int regionSize = 2 * n * chunkSize;

            if (_heightmapHighRes == null)
            {
                _heightmapHighRes = new float[regionSize * regionSize];
            }

            if (_heightMapTextureHighRes == null)
            {
                _needTextureInitFlag = true;
            }

            Bitmap bitmap = await Task.Run(() => (Bitmap)Image.FromFile(fileName));
            try
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    int stride = Math.Abs(bmpData.Stride);
                    byte[] pixels = new byte[stride * bitmap.Height];
                    Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);

                    // CPU 메모리의 높이맵 데이터 업데이트
                    await Task.Run(() =>
                    {
                        for (int y = 0; y < tileSize; y++)
                        {
                            for (int x = 0; x < tileSize; x++)
                            {
                                int pixelOffset = y * stride + x * 4;
                                float value = pixels[pixelOffset + 2] / 255.0f;  // R 채널

                                int globalX = tileX * tileSize + x;
                                int globalY = tileY * tileSize + y;
                                _heightmapHighRes[globalY * regionSize + globalX] = value;

                                // 저해상도맵의 청크바운딩으로 충분하여 속도를 위하여 생략 가능
                                //UpdateChunkHeightBounds(globalX, globalY, value, n, chunkSize);
                            }
                        }
                    });

                    // GPU 텍스처 업데이트용 데이터 준비 - R 채널만 사용
                    byte[] textureData = new byte[tileSize * tileSize];
                    for (int y = 0; y < tileSize; y++)
                    {
                        // 한 줄씩 연속적으로 복사
                        for (int x = 0; x < tileSize; x++)
                        {
                            int srcOffset = y * stride + x * 4 + 2;  // BGRA에서 R 채널
                            int dstOffset = y * tileSize + x;        // 연속된 메모리로 저장
                            textureData[dstOffset] = pixels[srcOffset];
                        }
                    }

                    // 메인쓰레드에서 텍스처타일을 업데이트 하기 위하여 타일업데이트 대기열에 추가한다.
                    _pendingTextureUpdates.Add(new TextureUpdateInfo
                    {
                        Data = textureData,
                        TileX = tileX,
                        TileY = tileY,
                        TileSize = tileSize
                    });
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }
        /// <summary>
        /// 지정된 위치의 높이값으로 청크의 최대/최소 높이를 업데이트합니다.
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="height">높이값</param>
        /// <param name="n">리전의 청크 반 개수</param>
        /// <param name="chunkSize">청크 크기</param>
        private void UpdateChunkHeightBounds(int x, int y, float height, int n, int chunkSize)
        {
            // 해당 좌표가 속한 청크 인덱스 계산
            int chunkX = x / chunkSize;
            int chunkY = y / chunkSize;
            int chunkIndex = chunkY * (2 * n) + chunkX;

            // 청크의 최대/최소 높이 업데이트
            if (chunkIndex >= 0 && chunkIndex < _maxChunks.Length)
            {
                _maxChunks[chunkIndex] = Math.Max(_maxChunks[chunkIndex], height);
                _minChunks[chunkIndex] = Math.Min(_minChunks[chunkIndex], height);
            }
        }

        /// <summary>
        /// 리소스를 정리한다
        /// </summary>
        public void Dispose()
        {
            if (_heightMapTextureHighRes != null && _heightMapTextureHighRes.TextureID != 0)
            {
                Gl.DeleteTextures(_heightMapTextureHighRes.TextureID);
                _heightmapHighRes = null;
            }

            if (_heightMapTextureLowRes != null && _heightMapTextureLowRes.TextureID != 0)
            {
                Gl.DeleteTextures(_heightMapTextureLowRes.TextureID);
                _heightmapHighRes = null;
            }

            _heightmapHighRes = null;
            _heightmapLowRes = null;
            _minChunks = null;
            _maxChunks = null;
        }

        /// <summary>
        /// 디버깅용: GPU에 업로드된 고해상도 텍스처 데이터를 비트맵으로 가져옵니다.
        /// </summary>
        /// <returns>고해상도 텍스처의 비트맵 이미지</returns>
        public Bitmap _DEBUG_GetHighResTexture()
        {
            if (_heightMapTextureHighRes == null || _heightMapTextureHighRes.TextureID == 0)
                return null;

            int texSize = 2000; // 전체 텍스처 크기
            Bitmap result = new Bitmap(texSize, texSize, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            // 그레이스케일 팔레트 설정
            ColorPalette palette = result.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            result.Palette = palette;

            // 텍스처 데이터 읽기
            byte[] pixels = new byte[texSize * texSize];
            Gl.BindTexture(TextureTarget.Texture2d, _heightMapTextureHighRes.TextureID);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.UnsignedByte, pixels);

            // 비트맵에 데이터 복사
            BitmapData bmpData = result.LockBits(
                new Rectangle(0, 0, texSize, texSize),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            try
            {
                for (int y = 0; y < texSize; y++)
                {
                    IntPtr row = bmpData.Scan0 + (y * bmpData.Stride);
                    Marshal.Copy(pixels, y * texSize, row, texSize);
                }
            }
            finally
            {
                result.UnlockBits(bmpData);
            }

            return result;
        }

        /// <summary>
        /// 디버깅용: _heightmapHighRes 배열의 데이터를 비트맵으로 생성합니다.
        /// 고해상도 타일이 CPU 메모리에 올바르게 로드되었는지 확인하는 데 사용됩니다.
        /// </summary>
        /// <returns>고해상도 높이맵의 비트맵 이미지</returns>
        public Bitmap _DEBUG_GetHighResHeightmapBitmap()
        {
            if (_heightmapHighRes == null)
                return null;

            int regionSize = (int)Math.Sqrt(_heightmapHighRes.Length);
            Bitmap result = new Bitmap(regionSize, regionSize, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            // 그레이스케일 팔레트 설정
            ColorPalette palette = result.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            result.Palette = palette;

            // 높이맵 데이터를 비트맵에 복사
            BitmapData bmpData = result.LockBits(
                new Rectangle(0, 0, regionSize, regionSize),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            try
            {
                byte[] pixels = new byte[regionSize * regionSize];

                // 높이맵 값(0-1)을 바이트값(0-255)으로 변환
                for (int i = 0; i < _heightmapHighRes.Length; i++)
                {
                    pixels[i] = (byte)(_heightmapHighRes[i] * 255.0f);
                }

                for (int y = 0; y < regionSize; y++)
                {
                    IntPtr row = bmpData.Scan0 + (y * bmpData.Stride);
                    Marshal.Copy(pixels, y * regionSize, row, regionSize);
                }
            }
            finally
            {
                result.UnlockBits(bmpData);
            }

            return result;
        }
    }
}