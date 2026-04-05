using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZetaExt;

namespace Occlusion
{
    public static class HierarchyZBufferDebug
    {
        /// <summary>
        /// CPU 버퍼(_zbuffer)를 이미지로 저장합니다.
        /// GPU에서 읽어온 결과와 다르면 TransferDepthDataToCPU에 문제가 있는 것입니다.
        /// </summary>
        public static void DebugSaveCpuBuffer(List<float[]> zbuffer, int width, int height, string debugDir, int maxLevel)
        {
            for (int level = 0; level <= maxLevel; level++)
            {
                if (level >= zbuffer.Count) break;
                int w = width >> level;
                int h = height >> level;
                float[] data = zbuffer[level];

                float maxVal = 0f;
                float minVal = float.MaxValue;
                for (int i = 0; i < data.Length; i++)
                {
                    maxVal = Math.Max(maxVal, data[i]);
                    minVal = Math.Min(minVal, data[i]);
                }
                Console.WriteLine($"[CPU Buffer] Level {level} ({w}x{h}): min={minVal:F4} max={maxVal:F4}");

                // ✅ C# 7.3 호환: using 블록 방식
                using (var bitmap = new System.Drawing.Bitmap(w, h,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int flippedY = h - 1 - y;
                            float value = data[flippedY * w + x];
                            float normalized = maxVal > 0 ? value / maxVal : 0f;
                            System.Drawing.Color color = DepthToJetColor(normalized);
                            bitmap.SetPixel(x, y, color);
                        }
                    }

                    string path = Path.Combine(debugDir, $"hzb_cpu_level_{level}.png");
                    bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"[CPU Buffer] 저장: {path}");
                }
            }
        }

        /// <summary>
        /// Jet 컬러맵 변환 (파랑=가까움, 빨강=멈)
        /// </summary>
        private static System.Drawing.Color DepthToJetColor(float t)
        {
            t = t.Clamp(0f, 1f);
            if (t < 0.25f)
            {
                float s = t * 4f;
                return System.Drawing.Color.FromArgb(0, (int)(s * 255), 255);
            }
            else if (t < 0.5f)
            {
                float s = (t - 0.25f) * 4f;
                return System.Drawing.Color.FromArgb(0, 255, (int)((1f - s) * 255));
            }
            else if (t < 0.75f)
            {
                float s = (t - 0.5f) * 4f;
                return System.Drawing.Color.FromArgb((int)(s * 255), 255, 0);
            }
            else
            {
                float s = (t - 0.75f) * 4f;
                return System.Drawing.Color.FromArgb(255, (int)((1f - s) * 255), 0);
            }
        }
    }
}
