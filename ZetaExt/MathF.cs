using OpenGL;
using System;
using System.Collections.Generic;

namespace ZetaExt
{
    public static class MathF
    {
        public static float PiOver2 => (float)(Math.PI * 0.5f);

        public static float Pi => (float)(Math.PI);


        private static float DegreeToRadian = (float)Math.PI / 180.0f;
        private static float RadianToDegree = 180.0f / (float)Math.PI;

        public static float Max(float x, float y, float z)
        {
            return Math.Max(Math.Max(x, y), z);
        }

        public static float Log2(float value)
        {
            return (float)Math.Log(value) / (float)Math.Log(2);
        }

        public static float Sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }

        public static float Cos(float radian)
        {
            return (float)Math.Cos(radian);
        }

        public static float Sin(float radian)
        {
            return (float)Math.Sin(radian);
        }

        public static float Tan(float radian)
        {
            return (float)Math.Tan(radian);
        }

        public static float Abs(this float value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        /// 소숫점 아래에서 자른다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static float Round(this float value, int num)
        {
            float step = 1;
            for (int i = 0; i < num; i++)
            {
                step *= 10;
            }
            value *= step;
            value = (int)value;
            value = value / step;
            return value;
        }

        /// <summary>
        /// 제곱근
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float Sqr(this float value)
        {
            return (float)Math.Sqrt(value);
        }

        public static byte ToByte(this bool boolean)
        {
            return (boolean) ? (byte)0x00 : (byte)0x01;
        }

        public static byte ToByte(this ushort value)
        {
            return Convert.ToByte(value);
        }

        public static float ToRadian(this int degree)
        {
            return (float)degree * DegreeToRadian;
        }

        public static float ToRadian(this float degree)
        {
            return degree * DegreeToRadian;
        }

        public static float ToDegree(this int radian)
        {
            return (float)radian * RadianToDegree;
        }

        public static float ToDegree(this float radian)
        {
            return radian * RadianToDegree;
        }

        public static List<float> ToList(this Vertex3f vector)
        {
            List<float> list = new List<float>();
            list.Add(vector.x);
            list.Add(vector.y);
            list.Add(vector.z);
            return list;
        }

        public static Vertex3f Color3f(this uint color)
        {
            byte red = (byte)(color >> 16 & 0xFF);
            byte green = (byte)(color >> 8 & 0xFF);
            byte blue = (byte)(color & 0xFF);
            return new Vertex3f(red / 255.0f, green / 255.0f, blue / 255.0f);
        }

        public static string Color3fString(this uint color)
        {
            byte red = (byte)(color >> 16 & 0xFF);
            byte green = (byte)(color >> 8 & 0xFF);
            byte blue = (byte)(color & 0xFF);
            return $"{red},{green},{blue}";
        }

        public static uint ColorUInt(this byte[] color)
        {
            return new Vertex3f(color[0], color[1], color[2]).ColorUInt();
        }

        /// <summary>
        /// min <= value <= max
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        /// <summary>
        /// value가 0~1일 때 start~end 사이의 값을 선형 보간하여 반환
        /// </summary>
        public static float Lerp(float start, float end, float value)
        {
            return start + (end - start) * value;
        }
    }
}
