using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZetaExt;

namespace Terrain
{
    /// <summary>
    /// 높이맵 노이즈 처리기
    /// </summary>
    /// <remarks>
    /// 기존 높이맵에 노이즈를 추가하여 보다 자연스러운 지형을 생성합니다.
    /// </remarks>
    public class HeightmapNoiseProcessor
    {
        private static Random _random = new Random();

        /// <summary>
        /// 기존 높이맵에 노이즈를 추가하여 새로운 높이맵 생성
        /// </summary>
        /// <param name="inputHeightmapPath">입력 높이맵 경로</param>
        /// <param name="outputHeightmapPath">출력 높이맵 경로</param>
        /// <param name="noiseScale">노이즈 스케일 (값이 클수록 노이즈 세부 사항이 커짐)</param>
        /// <param name="noiseStrength">노이즈 강도 (0.0-1.0)</param>
        /// <param name="octaves">노이즈 옥타브 수 (세부 디테일 수준)</param>
        /// <param name="persistence">각 옥타브의 영향력 감소 비율</param>
        /// <returns>처리 성공 여부</returns>
        public static bool AddNoiseToHeightmap(string inputHeightmapPath, string outputHeightmapPath,
                                            float noiseScale = 0.01f, float noiseStrength = 0.05f,
                                            int octaves = 4, float persistence = 0.5f)
        {
            try
            {
                // 입력 높이맵 로드
                using (Bitmap inputBitmap = new Bitmap(inputHeightmapPath))
                {
                    int width = inputBitmap.Width;
                    int height = inputBitmap.Height;

                    // 결과 비트맵 생성
                    using (Bitmap outputBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        // 퍼린 노이즈 생성
                        float[,] perlinNoise = GeneratePerlinNoise(width, height, octaves, persistence, noiseScale);

                        // 각 픽셀에 노이즈 적용
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                // 원본 높이맵 픽셀 값 (그레이스케일)
                                Color originalColor = inputBitmap.GetPixel(x, y);
                                int originalHeight = originalColor.R; // 그레이스케일이므로 R, G, B 값이 동일함

                                // 노이즈 값 계산 (-1.0 ~ 1.0 사이)
                                float noise = (perlinNoise[x, y] * 2.0f - 1.0f) * noiseStrength;

                                // 원본 높이에 노이즈 추가
                                int newHeight = (int)(originalHeight + noise * 255);
                                newHeight = newHeight.Clamp(0, 255);

                                // 결과 픽셀 설정 (그레이스케일 유지)
                                outputBitmap.SetPixel(x, y, Color.FromArgb(255, newHeight, newHeight, newHeight));
                            }
                        }

                        // 결과 저장
                        outputBitmap.Save(outputHeightmapPath, ImageFormat.Png);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"높이맵 노이즈 처리 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 더 빠른 처리를 위해 BitmapData를 직접 사용하는 노이즈 추가 버전
        /// </summary>
        public static bool AddNoiseToHeightmapFast(string inputHeightmapPath, string outputHeightmapPath,
                                                 float noiseScale = 0.01f, float noiseStrength = 0.05f,
                                                 int octaves = 4, float persistence = 0.5f)
        {
            try
            {
                // 입력 높이맵 로드
                using (Bitmap inputBitmap = new Bitmap(inputHeightmapPath))
                {
                    int width = inputBitmap.Width;
                    int height = inputBitmap.Height;

                    // 결과 비트맵 생성
                    using (Bitmap outputBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        // 퍼린 노이즈 생성
                        float[,] perlinNoise = GeneratePerlinNoise(width, height, octaves, persistence, noiseScale);

                        // 입력 비트맵 데이터 잠금
                        BitmapData inputData = inputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        // 출력 비트맵 데이터 잠금
                        BitmapData outputData = outputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppArgb);

                        // 바이트 배열 준비
                        int inputStride = inputData.Stride;
                        int outputStride = outputData.Stride;
                        int bytes = Math.Abs(inputStride) * height;
                        byte[] inputPixels = new byte[bytes];
                        byte[] outputPixels = new byte[bytes];

                        // 입력 비트맵 데이터 복사
                        Marshal.Copy(inputData.Scan0, inputPixels, 0, bytes);

                        // 각 픽셀에 노이즈 적용 (BGRA 형식)
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int i = y * inputStride + x * 4;

                                // 원본 높이맵 픽셀 값 (그레이스케일이므로 R, G, B 값 중 하나만 사용)
                                int originalHeight = inputPixels[i + 2]; // R 값 (BGRA에서 인덱스 2)

                                // 노이즈 값 계산 (-1.0 ~ 1.0 사이)
                                float noise = (perlinNoise[x, y] * 2.0f - 1.0f) * noiseStrength;

                                // 원본 높이에 노이즈 추가
                                int newHeight = (int)(originalHeight + noise * 255);
                                newHeight = newHeight.Clamp(0, 255);

                                // 결과 픽셀 설정 (그레이스케일 유지)
                                outputPixels[i] = (byte)newHeight;     // B
                                outputPixels[i + 1] = (byte)newHeight; // G
                                outputPixels[i + 2] = (byte)newHeight; // R
                                outputPixels[i + 3] = 255;             // A (완전 불투명)
                            }
                        }

                        // 출력 비트맵에 데이터 복사
                        Marshal.Copy(outputPixels, 0, outputData.Scan0, bytes);

                        // 비트맵 데이터 잠금 해제
                        inputBitmap.UnlockBits(inputData);
                        outputBitmap.UnlockBits(outputData);

                        // 결과 저장
                        outputBitmap.Save(outputHeightmapPath, ImageFormat.Png);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"빠른 높이맵 노이즈 처리 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지형의 등고선을 개선한 노이즈 추가 버전
        /// (높이 값에 따라 노이즈 강도 조절)
        /// </summary>
        public static bool AddAdaptiveNoiseToHeightmap(string inputHeightmapPath, string outputHeightmapPath,
                                                    float noiseScale = 0.01f, float noiseStrength = 0.05f,
                                                    int octaves = 4, float persistence = 0.5f)
        {
            try
            {
                using (Bitmap inputBitmap = new Bitmap(inputHeightmapPath))
                {
                    int width = inputBitmap.Width;
                    int height = inputBitmap.Height;

                    using (Bitmap outputBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        // 노이즈 생성
                        float[,] perlinNoise = GeneratePerlinNoise(width, height, octaves, persistence, noiseScale);
                        float[,] detailNoise = GeneratePerlinNoise(width, height, octaves + 2, persistence, noiseScale * 2.5f);

                        // 입력 비트맵 데이터 잠금
                        BitmapData inputData = inputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        // 출력 비트맵 데이터 잠금
                        BitmapData outputData = outputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppArgb);

                        // 바이트 배열 준비
                        int inputStride = inputData.Stride;
                        int outputStride = outputData.Stride;
                        int bytes = Math.Abs(inputStride) * height;
                        byte[] inputPixels = new byte[bytes];
                        byte[] outputPixels = new byte[bytes];

                        // 입력 비트맵 데이터 복사
                        Marshal.Copy(inputData.Scan0, inputPixels, 0, bytes);

                        // 각 픽셀에 노이즈 적용
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int i = y * inputStride + x * 4;

                                // 원본 높이맵 픽셀 값
                                int originalHeight = inputPixels[i + 2]; // R 값

                                // 정규화된 높이 값 (0.0-1.0)
                                float normalizedHeight = originalHeight / 255.0f;

                                // 높이 값에 따른 노이즈 강도 조절
                                // 중간 높이(평평한 지역)에 더 많은 노이즈 적용, 산봉우리나 계곡에는 적은 노이즈 적용
                                float heightFactor = 1.0f - Math.Abs(normalizedHeight - 0.5f) * 2.0f;
                                heightFactor = heightFactor * 0.7f + 0.3f; // 너무 차이나지 않게 조정

                                // 두 노이즈 값 조합
                                float noise1 = (perlinNoise[x, y] * 2.0f - 1.0f) * noiseStrength;
                                float noise2 = (detailNoise[x, y] * 2.0f - 1.0f) * (noiseStrength * 0.4f);
                                float combinedNoise = (noise1 + noise2) * heightFactor;

                                // 원본 높이에 노이즈 추가
                                int newHeight = (int)(originalHeight + combinedNoise * 255);
                                newHeight = newHeight.Clamp(0, 255);

                                // 결과 픽셀 설정
                                outputPixels[i] = (byte)newHeight;     // B
                                outputPixels[i + 1] = (byte)newHeight; // G
                                outputPixels[i + 2] = (byte)newHeight; // R
                                outputPixels[i + 3] = 255;             // A
                            }
                        }

                        // 출력 비트맵에 데이터 복사
                        Marshal.Copy(outputPixels, 0, outputData.Scan0, bytes);

                        // 비트맵 데이터 잠금 해제
                        inputBitmap.UnlockBits(inputData);
                        outputBitmap.UnlockBits(outputData);

                        // 결과 저장
                        outputBitmap.Save(outputHeightmapPath, ImageFormat.Png);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"적응형 높이맵 노이즈 처리 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        #region 노이즈 생성 헬퍼 메서드

        /// <summary>
        /// 퍼린 노이즈 생성
        /// </summary>
        private static float[,] GeneratePerlinNoise(int width, int height, int octaves, float persistence, float scale)
        {
            float[,] perlinNoise = new float[width, height];
            float maxValue = 0; // 정규화에 사용될 최대값

            // 각 옥타브마다 노이즈 생성 및 합산
            for (int octave = 0; octave < octaves; octave++)
            {
                float frequency = (float)Math.Pow(2, octave);
                float amplitude = (float)Math.Pow(persistence, octave);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float nx = x * scale * frequency / width;
                        float ny = y * scale * frequency / height;

                        float noise = PerlinNoise2D(nx, ny);
                        perlinNoise[x, y] += noise * amplitude;
                    }
                }

                maxValue += amplitude;
            }

            // 노이즈 값 정규화 (0.0-1.0 범위로)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    perlinNoise[x, y] /= maxValue;
                    perlinNoise[x, y] = (perlinNoise[x, y] + 1) * 0.5f; // -1~1 범위를 0~1 범위로 변환
                }
            }

            return perlinNoise;
        }

        /// <summary>
        /// 2D 퍼린 노이즈 함수
        /// </summary>
        private static float PerlinNoise2D(float x, float y)
        {
            // 격자 좌표 계산
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            // 격자 내 상대 위치 (0.0-1.0)
            float sx = x - x0;
            float sy = y - y0;

            // 각 격자점에서의 그레디언트 벡터와 상대 벡터의 내적 계산
            float n00 = DotGridGradient(x0, y0, x, y);
            float n10 = DotGridGradient(x1, y0, x, y);
            float n01 = DotGridGradient(x0, y1, x, y);
            float n11 = DotGridGradient(x1, y1, x, y);

            // 부드러운 보간을 위한 함수
            float sx2 = SmoothStep(sx);
            float sy2 = SmoothStep(sy);

            // 보간
            float nx0 = Lerp(n00, n10, sx2);
            float nx1 = Lerp(n01, n11, sx2);
            float nxy = Lerp(nx0, nx1, sy2);

            return nxy;
        }

        /// <summary>
        /// 격자점 그레디언트와 상대 벡터의 내적 계산
        /// </summary>
        private static float DotGridGradient(int ix, int iy, float x, float y)
        {
            // 의사 난수 그레디언트 벡터 생성
            float angle = PseudoRandom(ix, iy) * 2 * (float)Math.PI;
            float gx = (float)Math.Cos(angle);
            float gy = (float)Math.Sin(angle);

            // 상대 벡터 계산
            float dx = x - ix;
            float dy = y - iy;

            // 내적 계산
            return dx * gx + dy * gy;
        }

        /// <summary>
        /// 패턴이 반복되지 않는 의사 난수 생성
        /// </summary>
        private static float PseudoRandom(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
        }

        /// <summary>
        /// 부드러운 보간을 위한 함수 (3차 에르미트 스플라인)
        /// </summary>
        private static float SmoothStep(float t)
        {
            return t * t * (3 - 2 * t);
        }

        /// <summary>
        /// 선형 보간
        /// </summary>
        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        #endregion
    }
}