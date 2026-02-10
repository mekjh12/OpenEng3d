using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Terrain
{
    public class SimpleTerrainRegion
    {
        private RegionCoord _regionCoord;   // 리전 좌표
        private float _size;                // 지형의 크기
        private RawModel3d _rawModel;       // 지형 모델 (-size, -size, 0) -- (size, size, 0)
        private uint _textureId;            // 높이맵 Id
        private int _heightmapSize;         // 높이맵 크기
        private AABB _aabb;                 // 지형 바운딩
        private float _heightScale;         // 지형 높이 스케일
        private Matrix4x4f _model;          // 지형의 월드위치 (지형 모델을 오프셋만큼 이동 행렬)

        public AABB AABB { get => _aabb; }
        public RegionCoord RegionCoord { get => _regionCoord; }
        public uint TextureId { get => _textureId; }

        public SimpleTerrainRegion(RegionCoord regionCoord, RawModel3d rawModel3d, float size)
        {
            _regionCoord = regionCoord;
            _size = size;
            _rawModel = rawModel3d;
        }

        private void CalculateAABB(float minHeight, float maxHeight)
        {
            if (_aabb == null)
            {
                _aabb = new AABB();
            }

            float halfSize = (float)_size * 0.5f;
            float offsetX = _size * _regionCoord.X;
            float offsetY = _size * _regionCoord.Y;

            _model = Matrix4x4f.Translated(offsetX, offsetY, 0.0f);

            _aabb.LowerBound = new Vertex3f(-halfSize + offsetX, -halfSize + offsetY, minHeight);
            _aabb.UpperBound = new Vertex3f(halfSize + offsetX, halfSize + offsetY, maxHeight);
        }

        public async Task<bool> LoadTerrainFromFile(string fileName, float heightScale)
        {
            // 높이맵 파일 존재 검증
            if (!File.Exists(fileName)) return false;

            try
            {
                Bitmap bitmap = await Task.Run(() => (Bitmap)Image.FromFile(fileName));

                int width = bitmap.Width;
                int height = bitmap.Height;

                // 가로, 세로가 동일한 높이맵인지 검증
                if (width != height) return false;

                _heightScale = heightScale;

                // 높이맵 크기를 설정
                _heightmapSize = width;

                // 새 텍스처 ID 생성
                uint textureId = Gl.GenTexture();

                // 텍스처 바인딩
                Gl.BindTexture(TextureTarget.Texture2d, textureId);

                // 비트맵 데이터 잠금 및 접근
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0,width ,height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;

                // 비트맵에서 직접 높이 값을 추출
                unsafe
                {
                    byte* ptr = (byte*)bitmapData.Scan0.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int offset = y * bitmapData.Stride + x * 4; // 4 bytes per pixel (BGRA)
                            byte blue = ptr[offset];
                            byte green = ptr[offset + 1];
                            byte red = ptr[offset + 2];

                            // 그레이스케일 이미지라면 R, G, B 값이 동일하므로 하나만 사용
                            float heightValue = (float)red / 255.0f;  // 0-255 사이의 값

                            minHeight = Math.Min(minHeight, heightValue);
                            maxHeight = Math.Max(maxHeight, heightValue);
                        }
                    }
                }

                // 텍스처 데이터를 GPU에 업로드
                Gl.TexImage2D(
                    TextureTarget.Texture2d,
                    0,                          // 밉맵 레벨
                    InternalFormat.Rgba,        // 내부 형식
                    bitmap.Width,
                    bitmap.Height,
                    0,                          // 테두리
                    OpenGL.PixelFormat.Bgra,    // 입력 형식
                    PixelType.UnsignedByte,     // 데이터 타입
                    bitmapData.Scan0);          // 데이터 포인터

                // 비트맵 잠금 해제
                bitmap.UnlockBits(bitmapData);

                // 텍스처 파라미터 설정
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                // 텍스처 ID를 클래스 필드에 저장 
                _textureId = textureId;

                // 리소스 정리
                bitmap.Dispose();

                // 바운딩박스를 계산한다.
                CalculateAABB(heightScale * minHeight, heightScale * maxHeight);

                Console.WriteLine($"simple terrain load {_aabb} {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"텍스처 로딩 오류: {ex.Message}");
                return false;
            }
        }

        public void Update(Camera camera, float duration)
        {

        }

        public void Render(SimpleTerrainShader shader, Camera camera, Texture[] ground, 
            Vertex3f lightDirection,
            uint[] adjacentHeightMap, float heightScale)
        {
            shader.Bind();

            Gl.BindVertexArray(_rawModel.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);

            shader.LoadTexture(TextureUnit.Texture0, _textureId);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            for (int i = 0; i < 5; i++)
            {
                shader.SetInt($"gTextureHeight{i}", i + 2);
                Gl.ActiveTexture(TextureUnit.Texture2 + i);
                Gl.BindTexture(TextureTarget.Texture2d, ground[i] == null ? 0 : ground[i].TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
            }

            // 인접한 높이맵을 베어링한다.
            for (int i = 0; i < 8; i++)
            {
                shader.SetInt($"adjacentHeightMap{i}", i + 10);
                Gl.ActiveTexture(TextureUnit.Texture0 + i + 10);
                Gl.BindTexture(TextureTarget.Texture2d, adjacentHeightMap[i]);
            }

            shader.LoadHeightScale(heightScale);
            shader.LoadProjectionMatrix(camera.ProjectiveMatrix);
            shader.LoadViewMatrix(camera.ViewMatrix);
            shader.LoadModelMatrix(_model);
            shader.LoadCameraPosition(camera.Position);
            shader.LoadReversedLightDirection(-lightDirection);

            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _rawModel.IBO);
            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
            Gl.DrawElements(PrimitiveType.Patches, _rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            shader.Unbind();

        }
    }
}
