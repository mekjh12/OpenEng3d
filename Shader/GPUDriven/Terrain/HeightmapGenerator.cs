using Common;
using OpenGL;
using StbImageWriteSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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
        /// 하이트맵 생성 파이프라인 (Bilinear -> Bicubic -> Gaussian Blur)
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

        public void SaveHeightmapToPng(string filePath, bool saveMeta = false, bool generateNormalMap = true)
        {
            float[] heightData = ReadHeightmapFromGPU(flipY: false);
            //SaveFloatArrayToBmp(heightData, 1025, @"C:\Users\mekjh\OneDrive\바탕 화면\a.bmp");

            BmpHeightmapSaver.SaveAsRaw16(heightData, _size, _size, filePath, saveMeta: saveMeta, saveWithLowRes: true);
            Console.WriteLine($"[Heightmap] RAW 저장 완료: {filePath}");

            if (generateNormalMap)
            {
                float[] normalData = GenerateNormalMapFromHeight(heightData, _size, _size);

                string normalPath = Path.Combine(
                    Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath) + "_normal.raw"
                );

                BmpHeightmapSaver.SaveNormalMapAsRaw8(normalData, _size, _size, normalPath, saveMeta: false);
            }
        }

        public void SaveFloatArrayToBmp(float[] data, int size, string filePath)
        {
            // 1. 비트맵 객체 생성 (32bpp ARGB 권장)
            using (Bitmap bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // 2. 비트맵 데이터 잠금 (포인터 접근을 위해)
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, size, size),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                try
                {
                    int pixelCount = size * size;
                    byte[] pixelBytes = new byte[pixelCount * 4]; // BGRA 순서

                    for (int i = 0; i < pixelCount; i++)
                    {
                        // float[] data는 RGBA 순서라고 가정 (ReadHeightmapFromGPU 기준)
                        int floatIdx = i * 4;
                        int byteIdx = i * 4;

                        // 0.0 ~ 1.0 범위를 0 ~ 255로 클램핑 및 변환
                        // GDI+의 Format32bppArgb는 실제 메모리에 [B, G, R, A] 순서로 저장됩니다.
                        pixelBytes[byteIdx + 0] = (byte)(data[floatIdx + 2] * 255).Clamp(0, 255); // Blue
                        pixelBytes[byteIdx + 1] = (byte)(data[floatIdx + 1] * 255).Clamp(0, 255); // Green
                        pixelBytes[byteIdx + 2] = (byte)(data[floatIdx + 0] * 255).Clamp(0, 255); // Red
                        pixelBytes[byteIdx + 3] = (byte)(data[floatIdx + 3] * 255).Clamp(0, 255); // Alpha
                    }

                    // 3. 변환된 바이트 배열을 비트맵 메모리로 복사
                    Marshal.Copy(pixelBytes, 0, bmpData.Scan0, pixelBytes.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                // 4. 파일 저장 (확장자에 따라 포맷 자동 결정)
                bitmap.Save(filePath, ImageFormat.Bmp);
            }

            Console.WriteLine($"[Success] BMP 저장 완료: {filePath}");
        }
        /// <summary>
        /// RGB 3채널 float[] 다운샘플링 (Bilinear)
        /// </summary>
        private static float[] DownsampleRgb(float[] src, int srcW, int srcH, int dstW, int dstH)
        {
            float[] dst = new float[dstW * dstH * 3];

            float scaleX = (float)srcW / dstW;
            float scaleY = (float)srcH / dstH;

            for (int y = 0; y < dstH; y++)
            {
                float srcY = y * scaleY;
                int y0 = Math.Min((int)srcY, srcH - 1);
                int y1 = Math.Min(y0 + 1, srcH - 1);
                float fy = srcY - y0;

                for (int x = 0; x < dstW; x++)
                {
                    float srcX = x * scaleX;
                    int x0 = Math.Min((int)srcX, srcW - 1);
                    int x1 = Math.Min(x0 + 1, srcW - 1);
                    float fx = srcX - x0;

                    int i00 = (y0 * srcW + x0) * 3;
                    int i10 = (y0 * srcW + x1) * 3;
                    int i01 = (y1 * srcW + x0) * 3;
                    int i11 = (y1 * srcW + x1) * 3;

                    int dstIdx = (y * dstW + x) * 3;
                    for (int c = 0; c < 3; c++)
                    {
                        float top = src[i00 + c] * (1f - fx) + src[i10 + c] * fx;
                        float bot = src[i01 + c] * (1f - fx) + src[i11 + c] * fx;
                        dst[dstIdx + c] = top * (1f - fy) + bot * fy;
                    }
                }
            }

            return dst;
        }

        private float[] GenerateNormalMapFromHeight(float[] heightRGBA, int width, int height)
        {
            float[] normals = new float[width * height * 3];
            float strengthScale = width * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int xL = Math.Max(x - 1, 0);
                    int xR = Math.Min(x + 1, width - 1);
                    int yD = Math.Max(y - 1, 0);
                    int yU = Math.Min(y + 1, height - 1);

                    float hL = heightRGBA[y * width * 4 + xL * 4];
                    float hR = heightRGBA[y * width * 4 + xR * 4];
                    float hD = heightRGBA[yD * width * 4 + x * 4];
                    float hU = heightRGBA[yU * width * 4 + x * 4];

                    float dx = (hR - hL) * strengthScale;
                    float dy = (hU - hD) * strengthScale;

                    float nx = -dx;
                    float ny = -dy;
                    float nz = 1.0f;
                    float len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);

                    int idx = (y * width + x) * 3;
                    normals[idx + 0] = (nx / len) * 0.5f + 0.5f;
                    normals[idx + 1] = (ny / len) * 0.5f + 0.5f;
                    normals[idx + 2] = (nz / len) * 0.5f + 0.5f;
                }
            }
            return normals;
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