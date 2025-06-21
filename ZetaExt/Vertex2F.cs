using OpenGL;
using System;
using System.Numerics;

namespace ZetaExt
{
    public static class Vertex2F
    {
        /// <summary>
        /// 정규화된 2D 벡터의 X축 기준 반시계 방향 각도를 계산합니다. (0도 ~ 360도)
        /// </summary>
        public static float CalculateAngle(this Vertex2f vector)
        {
            // Atan2는 -π ~ π (-180도 ~ 180도) 를 반환
            float radians = (float)Math.Atan2(vector.y, vector.x);

            // 라디안을 도(degree)로 변환
            float degrees = radians * (180.0f / (float)Math.PI);

            // 음수 각도를 양수로 변환 (예: -90도 → 270도)
            if (degrees < 0)
                degrees += 360.0f;

            return degrees;
        }

        public static bool IsEqual(this Vertex2f a, Vertex2f b, float kEpsilon = 0.0001f)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) < kEpsilon;
        }


        /// <summary>
        /// 피봇점을 중심으로 일정한 각도로 회전한 점을 반환한다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="pivot"></param>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static Vertex2f Rotation(this Vertex2f a, Vertex2f pivot, float degree)
        {
            float cos = (float)Math.Cos(degree.ToRadian());
            float sin = (float)Math.Sin(degree.ToRadian());

            Vertex2f d = a - pivot;
            Vertex2f r = new Vertex2f(cos * d.x - sin * d.y, sin * d.x + cos * d.y);
            return pivot + r;
        }

        public static float Dot(this Vertex2f a, Vertex2f b)
        {
            return a.x * b.x + a.y * b.y;
        }

        public static float Norm(this Vertex2f a)
        {
            return (float)Math.Sqrt(a.Dot(a));
        }

    }
}
