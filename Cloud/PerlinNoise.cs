using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud
{
    public static class PerlinNoise
    {
        /// <summary>
        /// -1부터 1사이의 랜덤한 실수값을 반환한다.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float Noise(int x)
        {
            x = (x << 13) ^ x;
            return (1.0f - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 1073741824.0f);
        }

        public static float Noise(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 1073741824.0f);
        }

        public static float SmoothedNoise(int x)
        {
            return Noise(x) * 0.5f + Noise(x - 1) * 0.25f + Noise(x + 1) * 0.25f;
        }

        private static float InterpolateNoise1(float x)
        {
            int ix = (int)x;
            float fx = x - ix;
            float v1 = SmoothedNoise(ix);
            float v2 = SmoothedNoise(ix + 1);
            return v1 * (1.0f - fx) + v2 * fx;
        }

        public static float PerinNoise1d(float x, float repeat = 0.3f, int octaves = 10, float persistence = 0.5f)
        {
            float total = 0.0f;
            float frequency = 1.0f;
            float amplitude = 1.0f;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += InterpolateNoise1(x * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }
    }

}
