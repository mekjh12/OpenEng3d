using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Occlusion
{
    /// <summary>
    /// Z-버퍼를 비트맵으로 시각화하기 위한 디버깅 클래스
    /// </summary>
    public static class DebugZBufferVisualizer
    {
        /// <summary>
        /// 깊이 데이터(float 배열)를 비트맵으로 저장합니다.
        /// </summary>
        /// <param name="depthData">깊이값 배열 (float)</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="filePath">저장할 파일 경로</param>
        /// <param name="normalize">true: 0~1 범위로 정규화, false: 그대로 사용</param>
        public static void SaveDepthMapAsGrayscale(float[] depthData, int width, int height, string filePath, bool normalize = true)
        {
            if (depthData == null || depthData.Length != width * height)
                throw new ArgumentException($"깊이 데이터 크기가 일치하지 않습니다. 예상: {width * height}, 실제: {depthData?.Length ?? 0}");

            // 최소/최대값 찾기
            float minDepth = float.MaxValue;
            float maxDepth = float.MinValue;

            if (normalize)
            {
                for (int i = 0; i < depthData.Length; i++)
                {
                    minDepth = Math.Min(minDepth, depthData[i]);
                    maxDepth = Math.Max(maxDepth, depthData[i]);
                }

                // 모든 값이 같은 경우 처리
                if (minDepth == maxDepth)
                    maxDepth = minDepth + 1.0f;
            }

            // 비트맵 생성
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float depthValue = depthData[index];

                    // 정규화
                    float normalizedValue;
                    if (normalize)
                    {
                        normalizedValue = (depthValue - minDepth) / (maxDepth - minDepth);
                    }
                    else
                    {
                        normalizedValue = Math.Max(0.0f, Math.Min(1.0f, depthValue));
                    }

                    // 0~255로 변환
                    byte grayValue = (byte)(normalizedValue * 255.0f);

                    // 픽셀 설정 (Y축 반전 - 이미지 좌표계 맞춤)
                    Color pixelColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    bitmap.SetPixel(x, height - 1 - y, pixelColor);
                }
            }

            // 디렉토리 생성 (필요한 경우)
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 파일 저장
            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();

            Console.WriteLine($"# 깊이맵 저장 완료: {filePath}");
            Console.WriteLine($"  - 해상도: {width}x{height}");
            Console.WriteLine($"  - 깊이 범위: {minDepth:F4} ~ {maxDepth:F4}");
        }

        /// <summary>
        /// HierarchicalZBuffer의 모든 레벨을 비트맵으로 저장합니다.
        /// </summary>
        /// <param name="zbuffer">Z-버퍼 데이터 (List&lt;float[]&gt;)</param>
        /// <param name="width">원본 너비</param>
        /// <param name="height">원본 높이</param>
        /// <param name="outputDirectory">저장할 디렉토리</param>
        public static void SaveAllLevelsAsImages(System.Collections.Generic.List<float[]> zbuffer, int width, int height, string outputDirectory, bool isHeatmap = false)
        {
            if (zbuffer == null || zbuffer.Count == 0)
                throw new ArgumentException("Z-버퍼가 비어있습니다.");

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            for (int level = 0; level < zbuffer.Count; level++)
            {
                int levelWidth = width >> level;
                int levelHeight = height >> level;
                string filePath = Path.Combine(outputDirectory, $"zbuffer_level_{level:D2}_{levelWidth}x{levelHeight}.png");

                if (isHeatmap)
                {
                    SaveDepthMapAsHeatmap(zbuffer[level], levelWidth, levelHeight, filePath);
                }
                else
                {
                    SaveDepthMapAsGrayscale(zbuffer[level], levelWidth, levelHeight, filePath, normalize: true);
                }
            }

            Console.WriteLine($"\n✓ 모든 레벨 저장 완료: {outputDirectory}");
        }

        /// <summary>
        /// 깊이맵을 히트맵 색상으로 저장합니다 (빨강=깊음, 파랑=얕음).
        /// </summary>
        /// <param name="depthData">깊이값 배열</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public static void SaveDepthMapAsHeatmap(float[] depthData, int width, int height, string filePath)
        {
            if (depthData == null || depthData.Length != width * height)
                throw new ArgumentException($"깊이 데이터 크기가 일치하지 않습니다.");

            // 최소/최대값 찾기
            float minDepth = float.MaxValue;
            float maxDepth = float.MinValue;

            for (int i = 0; i < depthData.Length; i++)
            {
                minDepth = Math.Min(minDepth, depthData[i]);
                maxDepth = Math.Max(maxDepth, depthData[i]);
            }

            if (minDepth == maxDepth)
                maxDepth = minDepth + 1.0f;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float depthValue = depthData[index];
                    float normalized = (depthValue - minDepth) / (maxDepth - minDepth);

                    // 히트맵 색상 생성 (파랑 → 초록 → 노랑 → 빨강)
                    Color color = GetHeatmapColor(normalized);

                    bitmap.SetPixel(x, height - 1 - y, color);
                }
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();

            Console.WriteLine($"✓ 히트맵 저장 완료: {filePath}");
            Console.WriteLine($"  - 해상도: {width}x{height}");
            Console.WriteLine($"  - 깊이 범위: {minDepth:F4} ~ {maxDepth:F4}");
        }

        /// <summary>
        /// 정규화된 값(0~1)을 히트맵 색상으로 변환합니다.
        /// </summary>
        private static Color GetHeatmapColor(float value)
        {
            byte r, g, b;

            if (value < 0.25f)
            {
                // 파랑 → 초록
                float t = value / 0.25f;
                r = 0;
                g = (byte)(t * 255);
                b = 255;
            }
            else if (value < 0.5f)
            {
                // 초록 → 노랑
                float t = (value - 0.25f) / 0.25f;
                r = (byte)(t * 255);
                g = 255;
                b = 0;
            }
            else if (value < 0.75f)
            {
                // 노랑 → 주황
                float t = (value - 0.5f) / 0.25f;
                r = 255;
                g = (byte)(255 - t * 100);
                b = 0;
            }
            else
            {
                // 주황 → 빨강
                float t = (value - 0.75f) / 0.25f;
                r = 255;
                g = (byte)(155 - t * 100);
                b = 0;
            }

            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// 두 깊이맵을 비교하여 차이를 시각화합니다.
        /// </summary>
        /// <param name="depthData1">첫 번째 깊이맵</param>
        /// <param name="depthData2">두 번째 깊이맵</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public static void CompareDepthMaps(float[] depthData1, float[] depthData2, int width, int height, string filePath)
        {
            if (depthData1 == null || depthData2 == null)
                throw new ArgumentNullException("깊이 데이터가 null입니다.");

            if (depthData1.Length != depthData2.Length || depthData1.Length != width * height)
                throw new ArgumentException("깊이 데이터 크기가 일치하지 않습니다.");

            // 차이값 계산
            float[] diffData = new float[depthData1.Length];
            float maxDiff = 0.0f;

            for (int i = 0; i < depthData1.Length; i++)
            {
                diffData[i] = Math.Abs(depthData1[i] - depthData2[i]);
                maxDiff = Math.Max(maxDiff, diffData[i]);
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float diff = diffData[index];
                    float normalized = maxDiff > 0 ? diff / maxDiff : 0.0f;

                    // 차이가 있으면 빨강, 없으면 검정
                    byte redValue = (byte)(normalized * 255);
                    Color pixelColor = Color.FromArgb(redValue, 0, 0);

                    bitmap.SetPixel(x, height - 1 - y, pixelColor);
                }
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();

            Console.WriteLine($"✓ 비교 맵 저장 완료: {filePath}");
            Console.WriteLine($"  - 최대 차이: {maxDiff:F6}");
        }
    }
}