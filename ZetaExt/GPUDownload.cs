using OpenGL;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace ZetaExt
{
    public static class GPUDownload
    {
        /// <summary>
        /// GPU 텍스처를 BMP 이미지로 저장
        /// </summary>
        /// <param name="textureId"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Bitmap SaveAsBMP(uint textureId, int width, int height, string filePath)
        {
            // 텍스처 데이터 다운로드
            float[] textureData = DownloadTextureData(textureId, width, height);

            // BMP로 저장
            SaveAsBMP(textureData, width, height, filePath);

            // BMP 파일을 Bitmap으로 로드
            return new Bitmap(filePath);
        }

        /// <summary>
        /// OpenGL 텍스처에서 CPU로 데이터 다운로드
        /// </summary>
        private static float[] DownloadTextureData(uint textureId, int width, int height)
        {
            // 텍스처 데이터를 저장할 버퍼 생성 (RGBA 32비트 부동소수점)
            float[] buffer = new float[width * height * 4];

            try
            {
                // 텍스처 바인딩
                Gl.BindTexture(TextureTarget.Texture2d, textureId);

                // 픽셀 버퍼 객체(PBO) 생성
                uint pbo = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.PixelPackBuffer, pbo);

                // 버퍼 크기 계산 (RGBA, 32비트 부동소수점)
                int bufferSize = width * height * 4 * sizeof(float);
                Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)bufferSize, IntPtr.Zero, BufferUsage.StreamRead);

                // 텍스처 데이터를 PBO로 복사
                Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

                // PBO에서 CPU 메모리로 데이터 복사
                IntPtr mappedBuffer = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
                if (mappedBuffer != IntPtr.Zero)
                {
                    Marshal.Copy(mappedBuffer, buffer, 0, buffer.Length);
                    Gl.UnmapBuffer(BufferTarget.PixelPackBuffer);
                }
                else
                {
                    throw new Exception("텍스처 데이터 매핑 실패");
                }

                // 리소스 정리
                Gl.BindBuffer(BufferTarget.PixelPackBuffer, 0);
                Gl.DeleteBuffers(pbo);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"텍스처 데이터 다운로드 중 오류: {ex.Message}");
                throw;
            }

            return buffer;
        }

        /// <summary>
        /// 부동소수점 텍스처 데이터를 8비트 RGBA BMP로 저장
        /// </summary>
        private static void SaveAsBMP(float[] textureData, int width, int height, string filePath)
        {
            try
            {
                Console.WriteLine("BMP 이미지 생성 중...");

                // 확장자가 .png인 경우 .bmp로 변경
                if (Path.GetExtension(filePath).ToLower() == ".png")
                {
                    filePath = Path.ChangeExtension(filePath, ".bmp");
                }

                // Bitmap 생성
                using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    // 비트맵 데이터 잠금
                    BitmapData bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    // 픽셀당 바이트 수
                    int bytesPerPixel = 4;
                    int stride = bitmapData.Stride;

                    // 데이터 버퍼 생성
                    byte[] pixelBuffer = new byte[stride * height];

                    // 부동소수점 데이터를 8비트로 변환
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sourceIndex = (y * width + x) * 4;  // RGBA 부동소수점 데이터 인덱스
                            int destIndex = y * stride + x * bytesPerPixel;  // 비트맵 버퍼 인덱스

                            // 부동소수점 값을 0-255 범위로 변환
                            float r = Math.Max(0, Math.Min(1, textureData[sourceIndex])) * 255.0f;
                            float g = Math.Max(0, Math.Min(1, textureData[sourceIndex + 1])) * 255.0f;
                            float b = Math.Max(0, Math.Min(1, textureData[sourceIndex + 2])) * 255.0f;
                            float a = Math.Max(0, Math.Min(1, textureData[sourceIndex + 3])) * 255.0f;

                            // BGRA 형식으로 변환 (GDI+ Bitmap 형식)
                            pixelBuffer[destIndex] = (byte)b;     // Blue
                            pixelBuffer[destIndex + 1] = (byte)g; // Green
                            pixelBuffer[destIndex + 2] = (byte)r; // Red
                            pixelBuffer[destIndex + 3] = (byte)a; // Alpha
                        }
                    }

                    // 버퍼 데이터를 비트맵에 복사
                    Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);

                    // 비트맵 잠금 해제
                    bitmap.UnlockBits(bitmapData);

                    // 파일이 이미 존재하는 경우 삭제
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // BMP로 저장
                    bitmap.Save(filePath, ImageFormat.Bmp);
                    Console.WriteLine($"LUT 이미지가 {filePath}에 저장되었습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BMP 저장 중 오류: {ex.Message}");
                throw;
            }
        }
    }
}
