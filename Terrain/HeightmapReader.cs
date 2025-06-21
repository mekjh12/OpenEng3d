using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Terrain
{
    public class HeightmapReader
    {
        // 하이트맵을 PNG 이미지로 저장하는 함수
        public static void SaveHeightmapToPng(float[,] heightmap, string filePath)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            using (Bitmap bitmap = new Bitmap(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // 높이 값을 0-255 범위의 그레이스케일로 변환
                        int grayValue = (int)(heightmap[x, y] * 255);
                        grayValue = Math.Max(0, Math.Min(255, grayValue)); // 값 범위 제한

                        Color pixelColor = Color.FromArgb(grayValue, grayValue, grayValue);
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }

                // 파일로 저장
                bitmap.Save(filePath, ImageFormat.Png);
                Console.WriteLine($"하이트맵 저장 완료: {filePath}");
            }
        }

        public static float[,] ReadHeightmapFromPng(string filePath)
        {
            try
            {
                // 이미지 파일 로드
                using (Bitmap bitmap = new Bitmap(filePath))
                {
                    // 이미지 크기 확인
                    if (bitmap.Width != 2000 || bitmap.Height != 2000)
                    {
                        Console.WriteLine($"경고: 이미지 크기가 2000x2000이 아닙니다. 실제 크기: {bitmap.Width}x{bitmap.Height}");
                    }

                    int width = bitmap.Width;
                    int height = bitmap.Height;
                    float[,] heightmap = new float[width, height];

                    // 비트맵 데이터를 메모리에 잠금
                    BitmapData bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                    // 픽셀 데이터 추출
                    int bytesPerPixel = 4; // ARGB 형식은 픽셀당 4바이트
                    byte[] pixels = new byte[bitmapData.Stride * height];
                    Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);

                    // 비트맵 잠금 해제
                    bitmap.UnlockBits(bitmapData);

                    // 픽셀 데이터를 높이 값으로 변환
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int offset = y * bitmapData.Stride + x * bytesPerPixel;

                            // 그레이스케일 이미지로 가정, RGB 채널의 평균값 사용
                            byte b = pixels[offset];
                            byte g = pixels[offset + 1];
                            byte r = pixels[offset + 2];

                            // RGB 평균을 0-1 사이의 값으로 정규화
                            heightmap[x, y] = (r + g + b) / (3.0f * 255.0f);

                            // 대안: 그레이스케일 이미지라면 하나의 채널만 사용할 수도 있음
                            // heightmap[x, y] = r / 255.0f;
                        }
                    }

                    return heightmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"하이트맵 로딩 오류: {ex.Message}");
                return null;
            }
        }

        // 5x5 평균 필터를 사용하여 하이트맵을 부드럽게 만드는 함수
        public static float[,] ApplySmoothingFilter(float[,] heightmap, int filterSize = 5)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float[,] smoothedHeightmap = new float[width, height];

            // 각 픽셀에 대해 5x5 필터 적용
            int halfFilter = filterSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;
                    int count = 0;

                    // 주변 픽셀 합산
                    for (int fy = -halfFilter; fy <= halfFilter; fy++)
                    {
                        for (int fx = -halfFilter; fx <= halfFilter; fx++)
                        {
                            int nx = x + fx;
                            int ny = y + fy;

                            // 이미지 경계 확인
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += heightmap[nx, ny];
                                count++;
                            }
                        }
                    }

                    // 평균 계산
                    smoothedHeightmap[x, y] = sum / count;
                }
            }

            return smoothedHeightmap;
        }

        // 가우시안 필터를 사용하여 하이트맵을 부드럽게 만드는 함수 (더 자연스러운 결과)
        public static float[,] ApplyGaussianFilter(float[,] heightmap, int filterSize = 5)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float[,] smoothedHeightmap = new float[width, height];

            // 가우시안 커널 생성 (5x5)
            float sigma = filterSize / 3.0f; // 시그마 값 (필터 크기에 따라 조정)
            float[,] kernel = new float[filterSize, filterSize];
            float kernelSum = 0;

            int halfFilter = filterSize / 2;
            for (int y = -halfFilter; y <= halfFilter; y++)
            {
                for (int x = -halfFilter; x <= halfFilter; x++)
                {
                    // 가우시안 함수: exp(-(x^2 + y^2) / (2 * sigma^2))
                    float value = (float)Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[x + halfFilter, y + halfFilter] = value;
                    kernelSum += value;
                }
            }

            // 커널 정규화
            for (int y = 0; y < filterSize; y++)
            {
                for (int x = 0; x < filterSize; x++)
                {
                    kernel[x, y] /= kernelSum;
                }
            }

            // 필터 적용
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;

                    for (int fy = -halfFilter; fy <= halfFilter; fy++)
                    {
                        for (int fx = -halfFilter; fx <= halfFilter; fx++)
                        {
                            int nx = x + fx;
                            int ny = y + fy;

                            // 이미지 경계 확인
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += heightmap[nx, ny] * kernel[fx + halfFilter, fy + halfFilter];
                            }
                        }
                    }

                    smoothedHeightmap[x, y] = sum;
                }
            }

            return smoothedHeightmap;
        }
    }
}
