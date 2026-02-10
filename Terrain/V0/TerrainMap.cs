using Common.Abstractions;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Terrain
{
    public class TerrainMap
    {
        bool _isLoaded = false;              // 높이맵 로드 상태
        float[] _maps;                       // 높이맵 데이터 배열
        int _width;                          // 높이맵의 너비
        int _height;                         // 높이맵의 높이
        float _size;

        Texture[] _texture = new Texture[5];
        Texture _heightMapTexture;
        Texture _detailMap;



        /// <summary>
        /// TerrainMap의 새 인스턴스를 초기화합니다.
        /// </summary>
        public TerrainMap()
        {

        }

        /// <summary>
        /// 높이맵의 로드 상태를 가져오거나 설정합니다.
        /// </summary>
        public bool IsLoaded
        {
            get => _isLoaded;
            //set => _isLoaded = value;
        }

        public Texture DetailMap
        {
            get => _detailMap; 
            set => _detailMap = value;
        }

        public Texture[] GroundTextures
        {
            get => _texture; 
            set => _texture = value;
        }

        public Texture HeightMapTexture
        {
            get => _heightMapTexture; 
            set => _heightMapTexture = value;
        }

        /// <summary>
        /// 주어진 월드 좌표에서의 높이값을 계산하여 3D 점을 반환합니다.
        /// </summary>
        /// <param name="x">월드 공간의 X 좌표</param>
        /// <param name="y">월드 공간의 Y 좌표</param>
        /// <returns>계산된 높이값을 포함한 3D 점</returns>
        /// <remarks>
        /// 입력 좌표는 지형 크기(_size-2) 범위 내로 제한됩니다.
        /// 반환되는 점의 z값은 GetHeight(Vertex3f)를 통해 계산됩니다.
        /// </remarks>
        public Vertex3f GetHeight(float x, float y)
        {
            y = Math.Min(y, _height - 2);
            x = Math.Min(x, _width - 2);
            return new Vertex3f(x, y, GetHeight(new Vertex3f(x, y, 0)));
        }

        /// <summary>
        /// 이미지 파일로부터 높이맵을 로드
        /// </summary>
        /// <param name="fileName">높이맵 이미지 파일 경로</param>
        public void LoadHeightMap(string fileName)
        {
            // 변수 설명
            // width, height: 높이맵의 크기
            // size: 지형의 절반 크기 (높이의 절반으로 설정)
            // maps: 높이값을 저장할 배열 (0-1 사이값)

            Bitmap bitmap = null;
            try
            {
                // 이미지 파일 로드
                bitmap = (Bitmap)Image.FromFile(fileName);
                _width = bitmap.Width;
                _height = bitmap.Height;

                // 비트맵 데이터 잠금 및 접근
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.ReadOnly,
                     System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // 비트맵 바이트 데이터 준비
                int stride = Math.Abs(bmpData.Stride);
                byte[] pixelData = new byte[stride * _height];
                _maps = new float[_width * _height];

                // 비트맵 데이터 복사
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                // RGB 값을 높이값으로 변환 (빨간색 채널 사용)
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int pixelIndex = y * _width + x;                  // 높이맵 배열 인덱스
                        int byteIndex = (y * stride) + (x * 4);          // 픽셀 데이터 인덱스
                        _maps[pixelIndex] = pixelData[byteIndex + 2] / 255.0f;  // R값 추출 및 정규화
                    }
                }

                bitmap.UnlockBits(bmpData);
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"높이맵 로드 실패: {ex.Message}");
            }
            finally
            {
                bitmap?.Dispose();  // 비트맵 리소스 해제
            }
        }

        /// <summary>
        /// 주어진 위치에서의 보간된 높이값을 계산
        /// </summary>
        /// <param name="position">높이를 계산할 3D 위치</param>
        /// <returns>보간된 높이값 (200배 스케일링 적용)</returns>
        public float GetHeight(Vertex3f position)
        {
            // 변수 설명
            // wx, wy: 월드 좌표계에서의 x, y 위치
            // mapW, mapH: 높이맵의 너비와 높이
            // gx, gy: 높이맵 그리드에서의 정수 인덱스
            // lx, ly: 그리드 셀 내에서의 로컬 좌표 (0-1 사이 값)
            // ix, iy: 보간 계수 (interpolation factor)
            // h00~h22: 3x3 그리드의 높이값들 (행렬 형식으로 인덱싱)

            // 입력된 월드 좌표
            float wx = position.x;
            float wy = position.y;

            // 높이맵 크기
            int mapW = _width;
            int mapH = _height;

            // 높이맵에서의 그리드 좌표 계산 (_size를 더해 음수 좌표를 양수로 변환)
            int gx = (int)Math.Floor(wx + _width);
            int gy = (int)Math.Floor(wy + _height);

            // 그리드 셀 내에서의 상대 위치 계산
            float lx = wx + _width - gx;
            float ly = wy + _height - gy;

            // 높이맵 범위 벗어나면 0 반환
            if (gx < 1 || gy < 1 || gx >= mapW - 1 || gy >= mapH - 1)
                return 0.0f;

            // 3x3 주변 영역의 높이값 샘플링
            float h00 = _maps[(gy - 1) * mapW + (gx - 1)];  // 좌상단
            float h10 = _maps[(gy - 1) * mapW + gx];        // 상단
            float h20 = _maps[(gy - 1) * mapW + (gx + 1)];  // 우상단
            float h01 = _maps[gy * mapW + (gx - 1)];        // 좌측
            float h11 = _maps[gy * mapW + gx];              // 중앙
            float h21 = _maps[gy * mapW + (gx + 1)];        // 우측
            float h02 = _maps[(gy + 1) * mapW + (gx - 1)];  // 좌하단
            float h12 = _maps[(gy + 1) * mapW + gx];        // 하단
            float h22 = _maps[(gy + 1) * mapW + (gx + 1)];  // 우하단

            float ix, iy;  // 보간 계수

            // 4개의 서브그리드에 대한 보간 처리
            if (lx < 0.5f && ly < 0.5f)           // 좌상단 영역
            {
                ix = lx + 0.5f; iy = ly + 0.5f;
                return 200.0f * ((1 - ix) * (1 - iy) * h00 + ix * (1 - iy) * h10 +
                                (1 - ix) * iy * h01 + ix * iy * h11);
            }
            if (lx < 0.5f && ly >= 0.5f)          // 좌하단 영역
            {
                ix = lx + 0.5f; iy = ly - 0.5f;
                return 200.0f * ((1 - ix) * (1 - iy) * h01 + ix * (1 - iy) * h11 +
                                (1 - ix) * iy * h02 + ix * iy * h12);
            }
            if (lx >= 0.5f && ly >= 0.5f)         // 우하단 영역
            {
                ix = lx - 0.5f; iy = ly - 0.5f;
                return 200.0f * ((1 - ix) * (1 - iy) * h11 + ix * (1 - iy) * h21 +
                                (1 - ix) * iy * h12 + ix * iy * h22);
            }

            // 우상단 영역
            ix = lx - 0.5f; iy = ly + 0.5f;
            return 200.0f * ((1 - ix) * (1 - iy) * h10 + ix * (1 - iy) * h20 +
                            (1 - ix) * iy * h11 + ix * iy * h21);
        }



        public void Bake()
        {
            /*
            RawModel3d planedRawModel3d = Loader3d.LoadPlaneNxN(1, unitSize);
            TexturedModel texturedPlaneModel = new TexturedModel(planedRawModel3d, _heightMapTexture);
            texturedPlaneModel.AABB = new AABB(-Vertex3f.One * (1 * unitSize), Vertex3f.One * (1 * unitSize));
            texturedPlaneModel.OBB = OBB.ZeroSizeOBB; // 수정필요

            string patchName = $"patch_{x}x{y}";
            RawModel3d patch = BakeRawModel3dFromPatchIndex(x, y, n, unitSize);

            List<Vertex3f> vertices = GetVertices(x, y, unitSize);
            patch.GenerateBoundingBox(vertices.ToArray());

            TexturedModel texturedPatch = new TexturedModel(patch, _heightMapTexture);

            _patchEntity = new Entity(patchName, "", texturedPatch);
            _patchEntity.Position = Vertex3f.Zero;
            _patchEntity.UpdateBoundingBox();
            _patchEntity.AABB.BaseEntity = _patchEntity;
            */
        }

        public Vertex3f GetTerrainHeight(Vertex3f position)
        {
            float height = GetHeight(position);
            Vertex3f cameraPostion = position;
            cameraPostion.z = height;
            return cameraPostion;
        }

        /// <summary>
        /// 주어진 위치(position)에서의 높이 값을 보간하여 반환합니다.
        /// </summary>
        /// <param name="position">높이를 계산할 위치 (Vertex3f 타입).</param>
        /// <param name="verticalScaled">높이 값을 스케일링할 때 사용할 배율 (기본값: 200.0f).</param>
        /// <returns>보간된 높이 값 (float 타입).</returns>
        /// <remarks>
        /// 이 함수는 높이 맵(_maps)을 기반으로 주어진 위치에서의 높이 값을 계산합니다.
        /// 위치가 높이 맵의 경계를 벗어나면 0.0f를 반환합니다.
        /// 높이 값은 이중 선형 보간을 사용하여 계산됩니다.
        /// </remarks>
        public float GetHeight(Vertex3f position, float verticalScaled = 200.0f)
        {
            // 입력된 위치(position)의 x, y 좌표를 가져옴
            float px = position.x;
            float py = position.y;

            // 높이 맵의 너비(W)와 높이(H)를 가져옴
            int W = _width;
            int H = _height;

            // 위치를 정수로 변환하여 높이 맵의 인덱스 계산
            int ix = (int)Math.Floor(px + _width);
            int iy = (int)Math.Floor(py + _height);

            // 소수 부분을 계산하여 보간에 사용
            float s = px + _width - ix;
            float t = py + _height - iy;

            // 경계 조건 검사: 인덱스가 높이 맵 범위를 벗어나면 0.0f 반환
            if (ix < 1 || iy < 1) return 0.0f;
            if (iy >= W - 1 || ix >= H - 1) return 0.0f;

            // 주변 9개의 높이 값을 가져옴 (3x3 그리드)
            float a = _maps[(iy - 1) * W + (ix - 1)];
            float b = _maps[(iy - 1) * W + (ix + 0)];
            float c = _maps[(iy - 1) * W + (ix + 1)];
            float d = _maps[(iy + 0) * W + (ix - 1)];
            float e = _maps[(iy + 0) * W + (ix + 0)];
            float f = _maps[(iy + 0) * W + (ix + 1)];
            float g = _maps[(iy + 1) * W + (ix - 1)];
            float h = _maps[(iy + 1) * W + (ix + 0)];
            float i = _maps[(iy + 1) * W + (ix + 1)];

            // 보간된 높이 값을 저장할 변수
            float height = 0.0f;

            // 보간에 사용할 가중치 변수
            float u = 0.0f;
            float v = 0.0f;

            // 위치(s, t)에 따라 적절한 보간 방식을 선택
            if (s < 0.5f && t < 0.5f)
            {
                u = s + 0.5f; v = t + 0.5f;
                height = (1 - u) * (1 - v) * a + u * (1 - v) * b + (1 - u) * v * d + u * v * e;
            }
            if (s < 0.5f && t >= 0.5f)
            {
                u = s + 0.5f; v = t - 0.5f;
                height = (1 - u) * (1 - v) * d + u * (1 - v) * e + (1 - u) * v * g + u * v * h;
            }
            if (s >= 0.5f && t >= 0.5f)
            {
                u = s - 0.5f; v = t - 0.5f;
                height = (1 - u) * (1 - v) * e + u * (1 - v) * f + (1 - u) * v * h + u * v * i;
            }
            if (s >= 0.5f && t < 0.5f)
            {
                u = s - 0.5f; v = t + 0.5f;
                height = (1 - u) * (1 - v) * b + u * (1 - v) * c + (1 - u) * v * e + u * v * f;
            }

            // 최종 높이 값을 verticalScaled로 스케일링하여 반환
            return verticalScaled * height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="unitSize"></param>
        /// <returns></returns>
        private List<Vertex3f> GetVertices(int x, int y, int unitSize)
        {
            List<Vertex3f> vertices = new List<Vertex3f>();

            for (int i = unitSize * y; i < unitSize * (y + 1); i += 2)
            {
                for (int j = unitSize * x; j < unitSize * (x + 1); j += 2)
                {
                    Vertex3f vertex = GetHeight(j, i);
                    vertices.Add(vertex);
                }
            }

            return vertices;
        }

        public RawModel3d BakeRawModel3dFromPatchIndex(int x, int y, int n = 10, float unitSize = 1.0f)
        {
            // 정점 데이터를 저장할 리스트 초기화
            List<float> pos = new List<float>();      // 위치 데이터
            List<float> tex = new List<float>();      // 텍스처 좌표
            List<float> nor = new List<float>();      // 법선 벡터
            List<uint> indices = new List<uint>();    // 인덱스 데이터

            // 정점 위치 데이터 추가 (4개의 코너 정점)
            pos.Add(x * unitSize); pos.Add(y * unitSize); pos.Add(0.0f);                    // 좌하단
            pos.Add((x + 1) * unitSize); pos.Add(y * unitSize); pos.Add(0.0f);             // 우하단
            pos.Add(x * unitSize); pos.Add((y + 1) * unitSize); pos.Add(0.0f);             // 좌상단
            pos.Add((x + 1) * unitSize); pos.Add((y + 1) * unitSize); pos.Add(0.0f);       // 우상단

            // 텍스처 좌표 계산 및 추가
            tex.Add(1.0f * (unitSize * (x + 0) + _size) / _width);    // 좌하단 U
            tex.Add(1.0f * (unitSize * (y + 0) + _size) / _height);   // 좌하단 V
            tex.Add(1.0f * (unitSize * (x + 1) + _size) / _width);    // 우하단 U
            tex.Add(1.0f * (unitSize * (y + 0) + _size) / _height);   // 우하단 V
            tex.Add(1.0f * (unitSize * (x + 0) + _size) / _width);    // 좌상단 U
            tex.Add(1.0f * (unitSize * (y + 1) + _size) / _height);   // 좌상단 V
            tex.Add(1.0f * (unitSize * (x + 1) + _size) / _width);    // 우상단 U
            tex.Add(1.0f * (unitSize * (y + 1) + _size) / _height);   // 우상단 V

            // 법선 벡터 추가 (모든 정점이 위쪽을 향함)
            nor.Add(0.0f); nor.Add(0.0f); nor.Add(1.0f);    // 정점1 법선
            nor.Add(0.0f); nor.Add(0.0f); nor.Add(1.0f);    // 정점2 법선
            nor.Add(0.0f); nor.Add(0.0f); nor.Add(1.0f);    // 정점3 법선
            nor.Add(0.0f); nor.Add(0.0f); nor.Add(1.0f);    // 정점4 법선

            // 인덱스 데이터 추가 (쿼드를 구성하는 정점 순서)
            indices.Add(0); indices.Add(1); indices.Add(2); indices.Add(3);

            // GPU에 데이터 업로드
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            Loader3d.StoreDataInAttributeList(0, 3, pos.ToArray());    // 위치 데이터 업로드
            Loader3d.StoreDataInAttributeList(1, 2, tex.ToArray());    // 텍스처 좌표 업로드
            Loader3d.StoreDataInAttributeList(2, 3, nor.ToArray());    // 법선 데이터 업로드
            uint ibo = Loader3d.StoreDataInAttributeList(indices.ToArray());    // 인덱스 데이터 업로드
            Gl.BindVertexArray(0);

            // RawModel3d 객체 생성 및 설정
            RawModel3d rawModel = new RawModel3d(vao, pos.ToArray());
            rawModel.IBO = ibo;
            rawModel.VertexCount = indices.Count;
            rawModel.IsDrawElement = true; // IBO 이용하여

            return rawModel;
        }

        public void LoadMap(string heightmapFileName, string[] textures, string detailMap, int n, int unitSize)
        {
            // 높이맵 텍스처를 가져온다.
            _heightMapTexture = new Texture(heightmapFileName);

            _width = unitSize * n * 2;
            _height = unitSize * n * 2;
            _size = _width * 0.5f;

            // 지형 텍스처를 가져온다.
            if (textures != null)
            {
                _texture[0] = new Texture(textures[0]);
                _texture[1] = new Texture(textures[1]);
                _texture[2] = new Texture(textures[2]);
                _texture[3] = new Texture(textures[3]);
                _texture[4] = new Texture(textures[4]);
            }

            // 디테일 텍스처를 가져온다.
            if (detailMap != null)
            {
                _detailMap = new Texture(detailMap);
            }

            // 높이맵 텍스처로부터 비트맵을 읽어와 맵배열에 값을 로딩한다.
            LoadHeightMap(heightmapFileName);
        }
    }
}
