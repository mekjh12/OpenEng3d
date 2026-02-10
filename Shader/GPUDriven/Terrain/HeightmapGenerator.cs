using Common;
using OpenGL;
using StbImageWriteSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZetaExt;

namespace Shader
{
    public class HeightmapGenerator
    {
        private HeightmapGeneratorComputeShader _compShader;
        private DisplayShader _displayShader;

        private uint _basemapTexture;
        private int _size = 1024;

        uint _quadVAO;
        uint _quadVBO;

        bool _useBuffer0 = true;
        uint _mapBuffer0;
        uint _mapBuffer1;

        float _time = 0.0f;

        public uint ReadBuffer => _useBuffer0 ? _mapBuffer1 : _mapBuffer0;
        public uint WriteBuffer => _useBuffer0 ? _mapBuffer0 : _mapBuffer1;

        public HeightmapGenerator()
        {
        }

        public void Initialize(string projectPath, int size)
        {
            // 사이즈 설정
            _size = size;

            // 셰이더 초기화
            _compShader = new HeightmapGeneratorComputeShader(projectPath);
            _displayShader = new DisplayShader(StrRes.PROJECT_PATH);

            // 텍스처 생성 (RGBA32F)
            _mapBuffer0 = CreateBuffer(_size, _size, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
            _mapBuffer1 = CreateBuffer(_size, _size, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);

            // 전체 화면 쿼드 생성
            CreateFullscreenQuad();
        }

        private uint CreateBuffer(int width, int height, InternalFormat internalFormat, OpenGL.PixelFormat pixelFormat)
        {
            uint buffer = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, buffer);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                internalFormat,
                width, height, 0,
                pixelFormat,
                PixelType.Float,
                IntPtr.Zero
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

            Console.WriteLine($"[HeightmapGenerator {buffer}] 버퍼 생성됨");
            return buffer;
        }

        private void CreateFullscreenQuad()
        {
            float[] vertices = {
                -1.0f,  1.0f,   0.0f, 1.0f,
                -1.0f, -1.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,

                -1.0f,  1.0f,   0.0f, 1.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,
                 1.0f,  1.0f,   1.0f, 1.0f
            };

            _quadVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_quadVAO);

            _quadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length),
                          vertices, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), new IntPtr(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 버퍼 스왑 헬퍼 메서드
        /// </summary>
        private void SwapBuffers()
        {
            _useBuffer0 = !_useBuffer0;
        }

        /// <summary>
        /// 단일 컴퓨트 패스 실행
        /// </summary>
        private void ExecutePass(int mode, int blurPass = -1)
        {
            _compShader.BindBuffers(ReadBuffer, WriteBuffer);
            _compShader.SetMode(mode);

            if (blurPass >= 0)
            {
                _compShader.SetBlurPass(blurPass);
            }

            _compShader.Dispatch(_size, _size);

            // GPU 쓰기 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            SwapBuffers();
        }

        /// <summary>
        /// 하이트맵 생성 파이프라인
        /// </summary>
        /// <param name="baseHeigthmapTextureId">기본 하이트맵 텍스처 ID</param>
        /// <param name="useBicubic">Bicubic 보간 사용 여부</param>
        /// <param name="useGaussianBlur">Gaussian 블러 사용 여부</param>
        public void Generate(uint baseHeigthmapTextureId, bool useBicubic = true, bool useGaussianBlur = true)
        {
            _compShader.Bind();

            // 기본 하이트맵 로드
            _compShader.LoadBaseHeightmap(baseHeigthmapTextureId);

            // Pass 0: Bilinear 업스케일 (항상 실행)
            // ReadBuffer(이전 결과) -> WriteBuffer(새 결과)
            ExecutePass(mode: 0);

            // Pass 1: Bicubic 보간 (선택적)
            if (useBicubic)
            {
                ExecutePass(mode: 1);
            }

            // Pass 2-3: Separable Gaussian Blur (선택적)
            if (useGaussianBlur)
            {
                // 수평 블러
                ExecutePass(mode: 2, blurPass: 0);

                // 수직 블러
                ExecutePass(mode: 2, blurPass: 1);
            }

            _compShader.Unbind();

            // 최종 동기화 (텍스처 읽기용)
            Gl.MemoryBarrier(
                MemoryBarrierMask.ShaderImageAccessBarrierBit |
                MemoryBarrierMask.TextureFetchBarrierBit
            );

            Console.WriteLine($"[HeightmapGenerator] 생성 완료 - 최종 버퍼: {ReadBuffer}");
        }

        public void Render(float deltaTime)
        {
            // 시간 누적
            _time += deltaTime;

            // Depth test 비활성화 (fullscreen quad이므로)
            Gl.Disable(EnableCap.DepthTest);

            // 쉐이더 바인딩 및 Uniform 설정
            _displayShader.Bind();
            _displayShader.LoadHeightMapTexture(TextureUnit.Texture0, ReadBuffer);
            _displayShader.LoadUseGrayMode(false);

            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);

            _displayShader.Unbind();

            // 상태 복원
            Gl.Enable(EnableCap.DepthTest);
        }

        public void Cleanup()
        {
            if (_mapBuffer0 != 0) Gl.DeleteTextures(_mapBuffer0);
            if (_mapBuffer1 != 0) Gl.DeleteTextures(_mapBuffer1);
            if (_quadVAO != 0) Gl.DeleteVertexArrays(_quadVAO);
            if (_quadVBO != 0) Gl.DeleteBuffers(_quadVBO);

            _compShader?.CleanUp();

            Console.WriteLine("[HeightmapGenerator] 리소스 정리 완료");
        }

        /// <summary>
        /// Heightmap을 16bit PNG로 저장
        /// </summary>
        public void SaveHeightmapToPng(string filePath, bool saveMeta = true)
        {
            // GPU에서 데이터 읽기
            float[] heightData = ReadHeightmapFromGPU(flipY: false);

            BmpHeightmapSaver.SaveAsRaw16(heightData, _size, _size, filePath, saveMeta: false);

            Console.WriteLine($"[Heightmap] PNG 저장 완료: {filePath}");
        }

        /// <summary>
        /// GPU 텍스처에서 float 데이터 읽기 (Y축 반전 최적화)
        /// </summary>
        private float[] ReadHeightmapFromGPU(bool flipY = false)
        {
            Gl.BindTexture(TextureTarget.Texture2d, ReadBuffer);

            float[] data = new float[_size * _size * 4]; // RGBA

            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.Float,
                data
            );

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // Y축 반전 (인라인 최적화)
            if (flipY)
            {
                int halfHeight = _size / 2;
                int rowSize = _size * 4; // RGBA

                for (int y = 0; y < halfHeight; y++)
                {
                    int topIdx = y * rowSize;
                    int bottomIdx = (_size - 1 - y) * rowSize;

                    for (int i = 0; i < rowSize; i++)
                    {
                        float temp = data[topIdx + i];
                        data[topIdx + i] = data[bottomIdx + i];
                        data[bottomIdx + i] = temp;
                    }
                }

                Console.WriteLine($"[HeightmapGenerator] Y축 반전 완료: {_size}x{_size}");
            }

            return data;
        }

    }
}