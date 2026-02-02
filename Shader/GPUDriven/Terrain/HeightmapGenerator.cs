using Common;
using OpenGL;
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

        private uint _heightmapTexture;
        private int _size = 1024;
        
        uint _quadVAO;
        uint _quadVBO;

        float _time = 0.0f;

        public uint HeightMapTexture => _heightmapTexture;

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
            _heightmapTexture = CreateBuffer(_size, _size, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);

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
                internalFormat,      // ✅ Rgba32f 변경 (4채널), InternalFormat.Rgba32f
                width, height, 0,
                pixelFormat,     // ✅ 일치 OpenGL.PixelFormat.Rgba
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

            Console.WriteLine($"[WaterFlow {buffer}] 버퍼 생성됨");
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

        public void Generate(float scale, int octaves)
        {
            // 셰이더 바인딩
            _compShader.Bind();

            // 유니폼 설정
            _compShader.LoadUniforms(
                scale: scale,
                octaves: octaves,
                seed: new Random(10).Next()
            );

            // 버퍼 바인딩
            _compShader.BindBuffers(_heightmapTexture);

            // 실행
            _compShader.Dispatch(_size, _size);

            // 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _compShader.Unbind();
        }

        public void Render(float deltaTime)
        {
            // 시간 누적
            _time += deltaTime;

            // Depth test 비활성화 (fullscreen quad이므로)
            Gl.Disable(EnableCap.DepthTest);

            // 쉐이더 바인딩 및 Uniform 설정
            _displayShader.Bind();
            _displayShader.LoadHeightMapTexture(TextureUnit.Texture0, _heightmapTexture);
            _displayShader.LoadUseGrayMode(true);

            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);
            _displayShader.Unbind();

            // 상태 복원
            Gl.Enable(EnableCap.DepthTest);
        }

        public void Cleanup()
        {
            Gl.DeleteTextures(_heightmapTexture);
            _compShader.CleanUp();
        }

        /// <summary>
        /// Heightmap을 16bit PNG로 저장
        /// </summary>
        public void SaveHeightmapToPng(string filePath)
        {
            // GPU에서 데이터 읽기
            float[] heightData = ReadHeightmapFromGPU();

            // 16bit 변환 및 저장
            SaveAs16BitPng(heightData, filePath);

            Console.WriteLine($"[Heightmap] PNG 저장 완료: {filePath}");
        }

        /// <summary>
        /// GPU 텍스처에서 float 데이터 읽기
        /// </summary>
        private float[] ReadHeightmapFromGPU()
        {
            Gl.BindTexture(TextureTarget.Texture2d, _heightmapTexture);

            float[] data = new float[_size * _size * 4]; // RGBA

            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.Float,
                data
            );

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return data;
        }

        /// <summary>
        /// float 배열을 16bit PNG로 저장
        /// </summary>
        private void SaveAs16BitPng(float[] heightData, string filePath)
        {
            // 48bit RGB로 저장 (각 채널 16bit)
            Bitmap bitmap = new Bitmap(_size, _size, System.Drawing.Imaging.PixelFormat.Format48bppRgb);

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, _size, _size),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat
            );

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < _size; y++)
                {
                    ushort* row = (ushort*)(ptr + y * stride);

                    for (int x = 0; x < _size; x++)
                    {
                        int index = (y * _size + x) * 4;
                        float height = heightData[index];

                        ushort value = (ushort)(height.Clamp(0.0f, 1.0f) * 65535);

                        // RGB 모두 같은 값 (grayscale)
                        row[x * 3 + 0] = value; // R
                        row[x * 3 + 1] = value; // G
                        row[x * 3 + 2] = value; // B
                    }
                }
            }

            bitmap.UnlockBits(bmpData);

            // 파일 경로 확인
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();
        }

    }
}