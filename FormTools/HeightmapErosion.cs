using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace FormTools
{
    public class HeightmapErosion
    {
        public static void ApplyThermalAndHydraulicErosion(string inputPath, string outputPath, int iterations, float erosionStrength, float depositionStrength)
        {
            using (Bitmap originalImage = new Bitmap(inputPath))
            {
                int width = originalImage.Width;
                int height = originalImage.Height;

                Console.WriteLine($"이미지 크기: {width}x{height}");

                // 높이맵 데이터 추출
                float[,] heightmap = ExtractHeightmap(originalImage);

                // 침식 알고리즘 적용
                float[,] erodedHeightmap = ApplyErosion(heightmap, iterations, erosionStrength, depositionStrength);

                // 결과를 이미지로 변환
                using (Bitmap resultImage = HeightmapToBitmap(erodedHeightmap))
                {
                    resultImage.Save(outputPath, ImageFormat.Png);
                }
            }
        }

        static float[,] ExtractHeightmap(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            float[,] heightmap = new float[width, height];

            // 이미지 데이터 잠금
            BitmapData bitmapData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4; // ARGB
            int stride = bitmapData.Stride;
            IntPtr scan0 = bitmapData.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0;

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * stride + x * bytesPerPixel;

                        // 그레이스케일 값 추출 (R, G, B 채널 평균)
                        byte r = ptr[offset + 2]; // R
                        byte g = ptr[offset + 1]; // G
                        byte b = ptr[offset + 0]; // B

                        // 0-1 범위로 정규화
                        heightmap[x, y] = (r + g + b) / (3.0f * 255.0f);
                    }
                });
            }

            image.UnlockBits(bitmapData);
            return heightmap;
        }

        static Bitmap HeightmapToBitmap(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // 이미지 데이터 잠금
            BitmapData bitmapData = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4; // ARGB
            int stride = bitmapData.Stride;
            IntPtr scan0 = bitmapData.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0;

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * stride + x * bytesPerPixel;

                        // 0-1 값을 0-255 범위로 변환
                        byte value = (byte)(Math.Min(Math.Max(heightmap[x, y], 0), 1) * 255);

                        // BGRA 형식으로 저장
                        ptr[offset + 0] = value; // B
                        ptr[offset + 1] = value; // G
                        ptr[offset + 2] = value; // R
                        ptr[offset + 3] = 255;   // A (불투명)
                    }
                });
            }

            result.UnlockBits(bitmapData);
            return result;
        }

        static float[,] ApplyErosion(float[,] heightmap, int iterations, float erosionStrength, float depositionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            float[,] result = (float[,])heightmap.Clone();

            // 주변 8방향의 오프셋
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            Random random = new Random();

            Console.WriteLine("침식 처리 중...");

            // 열 침식 (Thermal Erosion) - 경사가 높은 지점에서 흙이 미끄러지는 현상
            for (int iter = 0; iter < iterations; iter++)
            {
                Console.Write($"\r진행 중: {iter + 1}/{iterations}");

                float[,] tempMap = (float[,])result.Clone();

                // 열 침식 단계
                Parallel.For(0, width, x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        // 현재 위치의 높이
                        float currentHeight = result[x, y];

                        // 주변 셀 중 가장 낮은 높이 찾기
                        float lowestHeight = currentHeight;
                        int lowestX = x;
                        int lowestY = y;

                        for (int d = 0; d < 8; d++)
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];

                            // 경계 확인
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (result[nx, ny] < lowestHeight)
                                {
                                    lowestHeight = result[nx, ny];
                                    lowestX = nx;
                                    lowestY = ny;
                                }
                            }
                        }

                        // 높이 차이가 있으면 침식 적용
                        if (lowestHeight < currentHeight)
                        {
                            float heightDiff = currentHeight - lowestHeight;
                            float erosionAmount = heightDiff * erosionStrength;

                            tempMap[x, y] -= erosionAmount;              // 현재 위치에서 높이 감소
                            tempMap[lowestX, lowestY] += erosionAmount;  // 가장 낮은 이웃 위치에 퇴적
                        }
                    }
                });

                result = tempMap;

                // 수문 침식 (Hydraulic Erosion) - 물이 높은 곳에서 낮은 곳으로 흐르며 침식하는 현상
                // 간소화된 버전 - 실제 물 시뮬레이션은 훨씬 복잡함
                tempMap = (float[,])result.Clone();

                // 무작위 시작점 선정 (빗방울)
                int droplets = Math.Min(width * height / 100, 10000); // 너무 많은 방울은 처리 시간이 오래 걸림

                for (int d = 0; d < droplets; d++)
                {
                    int x = random.Next(width);
                    int y = random.Next(height);
                    float sediment = 0; // 방울이 운반하는 침전물

                    // 최대 경로 길이 제한
                    for (int step = 0; step < 30; step++)
                    {
                        // 현재 위치의 높이
                        float currentHeight = result[x, y];

                        // 주변에서 가장 낮은 지점 찾기
                        int lowestX = x;
                        int lowestY = y;
                        float lowestHeight = currentHeight;

                        for (int dir = 0; dir < 8; dir++)
                        {
                            int nx = x + dx[dir];
                            int ny = y + dy[dir];

                            // 경계 확인
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (result[nx, ny] < lowestHeight)
                                {
                                    lowestHeight = result[nx, ny];
                                    lowestX = nx;
                                    lowestY = ny;
                                }
                            }
                        }

                        // 더 낮은 지점이 없으면 퇴적하고 중단
                        if (lowestX == x && lowestY == y)
                        {
                            tempMap[x, y] += sediment;
                            break;
                        }

                        // 경사에 따라 침식량 결정
                        float slope = currentHeight - lowestHeight;
                        float erosionAmount = slope * erosionStrength * 0.5f; // 수문 침식은 열 침식보다 약함

                        // 침식 및 퇴적
                        erosionAmount = Math.Min(erosionAmount, 0.005f); // 단일 셀에서의 과도한 침식 방지
                        tempMap[x, y] -= erosionAmount;
                        sediment += erosionAmount;

                        // 침전물의 일부 퇴적
                        float depositAmount = sediment * depositionStrength;
                        tempMap[lowestX, lowestY] += depositAmount;
                        sediment -= depositAmount;

                        // 다음 위치로 이동
                        x = lowestX;
                        y = lowestY;
                    }
                }

                // 침식 결과 업데이트
                result = tempMap;

                // 높이맵 정규화 (선택적)
                if ((iter + 1) % 5 == 0)
                {
                    NormalizeHeightmap(result);
                }
            }

            Console.WriteLine("\n침식 완료");

            // 마지막 정규화
            NormalizeHeightmap(result);

            // 최종 부드럽게 처리 (선택적)
            result = SmoothHeightmap(result, 1);

            return result;
        }

        static void NormalizeHeightmap(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            // 최소/최대 높이 찾기
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    minHeight = Math.Min(minHeight, heightmap[x, y]);
                    maxHeight = Math.Max(maxHeight, heightmap[x, y]);
                }
            }

            // 범위가 너무 작으면 정규화 건너뛰기
            if (maxHeight - minHeight < 0.001f)
                return;

            // 0-1 범위로 정규화
            float range = maxHeight - minHeight;

            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] = (heightmap[x, y] - minHeight) / range;
                }
            });
        }

        static float[,] SmoothHeightmap(float[,] heightmap, int radius)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            float[,] smoothed = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0;
                    int count = 0;

                    // 평균 필터 적용
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += heightmap[nx, ny];
                                count++;
                            }
                        }
                    }

                    smoothed[x, y] = sum / count;
                }
            });

            return smoothed;
        }

        public static void CreateComparisonImage(string originalPath, string processedPath, string outputPath)
        {
            using (Bitmap original = new Bitmap(originalPath))
            using (Bitmap processed = new Bitmap(processedPath))
            {
                int width = original.Width;
                int height = original.Height;

                using (Bitmap comparison = new Bitmap(width * 2, height))
                using (Graphics g = Graphics.FromImage(comparison))
                {
                    // 왼쪽에 원본 그리기
                    g.DrawImage(original, 0, 0, width, height);

                    // 오른쪽에 처리된 이미지 그리기
                    g.DrawImage(processed, width, 0, width, height);

                    // 구분선 그리기
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        g.DrawLine(pen, width, 0, width, height);
                    }

                    // 레이블 추가
                    using (Font font = new Font("Arial", 20, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    using (SolidBrush shadowBrush = new SolidBrush(Color.Black))
                    {
                        // 가독성을 위한 배경 직사각형 추가
                        g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), 10, 10, 150, 40);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), width + 10, 10, 150, 40);

                        // 가시성을 높이기 위해 그림자와 텍스트 그리기
                        g.DrawString("원본", font, shadowBrush, 12, 12);
                        g.DrawString("원본", font, brush, 10, 10);

                        g.DrawString("침식 적용", font, shadowBrush, width + 12, 12);
                        g.DrawString("침식 적용", font, brush, width + 10, 10);
                    }

                    // 비교 이미지 저장
                    comparison.Save(outputPath, ImageFormat.Png);
                }
            }
        }
    }
}
