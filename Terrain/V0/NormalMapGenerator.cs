using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using ZetaExt;

namespace Terrain
{
    /// <summary>
    /// 높이맵으로부터 Normal Map을 생성합니다. (Zero-allocation)
    /// Sobel 필터를 사용하여 고품질 법선을 계산합니다.
    /// </summary>
    public static class NormalMapGenerator
    {
        /// <summary>
        /// 높이맵 텍스처로부터 Normal Map 생성
        /// </summary>
        /// <param name="heightMapPath">높이맵 이미지 파일 경로</param>
        /// <param name="heightScale">높이 스케일 (지형의 수직 배율)</param>
        /// <param name="wrapMode">텍스처 래핑 모드</param>
        /// <returns>생성된 Normal Map 텍스처 ID</returns>
        public static uint GenerateNormalMap(string heightMapPath, float heightScale = 1.0f, bool wrapMode = true)
        {
            // Normal Map PNG 저장 경로 생성
            string filenameWithoutExt = Path.Combine(
                Path.GetDirectoryName(heightMapPath),
                Path.GetFileNameWithoutExtension(heightMapPath)
            );

            string normalPngPath = filenameWithoutExt + "_normal.png";

            // 이미 존재하면 PNG 파일에서 직접 로드
            if (File.Exists(normalPngPath))
            {
                uint textureId = LoadNormalMapFromPng(normalPngPath);
                return textureId;
            }
            else
            {
                // 노말맵이 없으면 생성
                using (var heightImage = System.Drawing.Image.FromFile(heightMapPath))
                using (var heightBitmap = new System.Drawing.Bitmap(heightImage))
                {
                    int width = heightBitmap.Width;
                    int height = heightBitmap.Height;

                    // 높이 데이터 추출
                    float[] heightData = ExtractHeightData(heightBitmap);

                    // Normal Map 데이터 생성
                    byte[] normalData = new byte[width * height * 4];
                    GenerateNormalData(heightData, normalData, width, height, heightScale, wrapMode);

                    // PNG 파일로 저장
                    SaveNormalMapToPng(normalPngPath, heightData, width, height, heightScale);

                    // OpenGL 텍스처 생성 및 반환
                    return CreateNormalTexture(normalData, width, height);
                }
            }            
        }

        /// <summary>
        /// PNG 파일에서 Normal Map 텍스처 로드
        /// </summary>
        private static uint LoadNormalMapFromPng(string pngPath)
        {
            using (var image = System.Drawing.Image.FromFile(pngPath))
            using (var bitmap = new System.Drawing.Bitmap(image))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;

                // RGBA 데이터 추출
                byte[] normalData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        int idx = (y * width + x) * 4;

                        normalData[idx + 0] = pixel.R;
                        normalData[idx + 1] = pixel.G;
                        normalData[idx + 2] = pixel.B;
                        normalData[idx + 3] = pixel.A;
                    }
                }

                // OpenGL 텍스처 생성
                return CreateNormalTexture(normalData, width, height);
            }
        }

        /// <summary>
        /// 비트맵에서 높이 데이터 추출 (그레이스케일)
        /// </summary>
        private static float[] ExtractHeightData(System.Drawing.Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            float[] heightData = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    // RGB 평균으로 그레이스케일 변환
                    float gray = (pixel.R + pixel.G + pixel.B) / (3f * 255f);
                    heightData[y * width + x] = gray;
                }
            }

            return heightData;
        }


        /// <summary>
        /// Sobel 필터로 법선 데이터 생성 (Y축 반전 버전)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateNormalData(float[] heightData, byte[] normalData,
            int width, int height, float heightScale, bool wrapMode)
        {
            float strength = heightScale;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 주변 8개 픽셀 샘플링
                    float hTL = SampleHeight(heightData, width, height, x - 1, y + 1, wrapMode);
                    float hT = SampleHeight(heightData, width, height, x, y + 1, wrapMode);
                    float hTR = SampleHeight(heightData, width, height, x + 1, y + 1, wrapMode);

                    float hL = SampleHeight(heightData, width, height, x - 1, y, wrapMode);
                    float hR = SampleHeight(heightData, width, height, x + 1, y, wrapMode);

                    float hBL = SampleHeight(heightData, width, height, x - 1, y - 1, wrapMode);
                    float hB = SampleHeight(heightData, width, height, x, y - 1, wrapMode);
                    float hBR = SampleHeight(heightData, width, height, x + 1, y - 1, wrapMode);

                    // Sobel 연산자
                    float Gx = (hTR + 2.0f * hR + hBR) - (hTL + 2.0f * hL + hBL);
                    float Gy = (hBL + 2.0f * hB + hBR) - (hTL + 2.0f * hT + hTR);

                    // 법선 벡터 계산
                    float nx = -Gx * strength;
                    float ny = Gy * strength;  // ⭐ Y축 반전: 부호를 양수로 (원래는 음수였음)
                    float nz = 4.0f;

                    // 정규화
                    float len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= len;
                    ny /= len;
                    nz /= len;

                    // [-1, 1] → [0, 255] 범위로 인코딩
                    int idx = (y * width + x) * 4;
                    normalData[idx + 0] = (byte)((nx * 0.5f + 0.5f) * 255f);  // R
                    normalData[idx + 1] = (byte)((ny * 0.5f + 0.5f) * 255f);  // G
                    normalData[idx + 2] = (byte)((nz * 0.5f + 0.5f) * 255f);  // B
                    normalData[idx + 3] = 255;                                  // A
                }
            }
        }

        /// <summary>
        /// 높이 샘플링 (경계 처리)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SampleHeight(float[] heightData, int width, int height,
            int x, int y, bool wrapMode)
        {
            if (wrapMode)
            {
                // Wrap 모드: 타일링
                x = (x + width) % width;
                y = (y + height) % height;
            }
            else
            {
                // Clamp 모드: 경계 고정
                x = x.Clamp(0, width - 1);
                y = y.Clamp(0, height - 1);
            }

            return heightData[y * width + x];
        }

        // OpenGL 텍스처 생성 부분
        private static uint CreateNormalTexture(byte[] normalData, int width, int height)
        {
            uint textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            // 텍스처 데이터 업로드
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba8,
                width,
                height,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                normalData
            );

            // 밉맵 생성
            Gl.GenerateMipmap(TextureTarget.Texture2d);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.Repeat);

            Gl.BindTexture(TextureTarget.Texture2d, 0);
            return textureId;
        }


        /// <summary>
        /// Normal Map을 PNG 파일로 저장 (최고속 버전 - unsafe)
        /// </summary>
        public static unsafe void SaveNormalMapToPng(string outputPath, float[] heightData,
            int width, int height, float heightScale = 1.0f)
        {
            byte[] normalData = new byte[width * height * 4];
            GenerateNormalData(heightData, normalData, width, height, heightScale, true);

            using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

                try
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = (y * width + x) * 4;
                            int dstIdx = x * 4;

                            // RGBA → BGRA 변환
                            row[dstIdx + 0] = normalData[srcIdx + 2];  // B
                            row[dstIdx + 1] = normalData[srcIdx + 1];  // G
                            row[dstIdx + 2] = normalData[srcIdx + 0];  // R
                            row[dstIdx + 3] = normalData[srcIdx + 3];  // A
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }
    }
}