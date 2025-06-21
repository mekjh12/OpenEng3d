using OpenGL;
using System;

namespace ZetaExt
{
    public static class Float1F
    {
        /// <summary>
        /// start이상 end미만
        /// </summary>
        /// <param name="value"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool Bwtween(this float value, float start, float end)
        {
            return (start <= value && value < end);
        }


        public static float Sgn(this float value)
        {
            if (value > 0.0f) return 1.0f;
            if (value < 0.0f) return -1.0f;
            return 0.0f;
        }

        /// <summary>
        /// 소숫점 아래의 숫자를 버린다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalLength"></param>
        /// <returns></returns>
        public static float Round(this float value, uint decimalLength)
        {
            float exp = (float)Math.Pow(10, decimalLength);
            return (float)((int)(value * exp) * (1.0f / exp));
        }

        public static float ClampCycle(this float value, float min, float max)
        {
            if (value < min) return max;
            if (value > max) return min;
            return value;
        }

        public static float Clamp(this float value, float min, float max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }


    }
}
