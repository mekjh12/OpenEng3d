using OpenGL;
using System;

namespace ZetaExt
{
    /// <summary>
    /// GC allocation 없이 Vertex3f 연산을 수행하는 고성능 유틸리티 클래스.
    /// <br/>
    /// 모든 메서드는 ref 매개변수를 사용하여 힙 할당을 방지하고,
    /// 매 프레임 호출되는 연산에서 GC 압박을 제거합니다.
    /// </summary>
    public static class Vertex3NoGC
    {
        public static void Subtract(in this Vertex3f a, in Vertex3f b, ref Vertex3f result)
        {
            result.x = a.x - b.x;
            result.y = a.y - b.y;
            result.z = a.z - b.z;
        }

        public static void Copy(in Vertex3f source, ref Vertex3f destination)
        {
            destination.x = source.x;
            destination.y = source.y;
            destination.z = source.z;
        }

        public static void Cross(in Vertex3f a, in Vertex3f b, ref Vertex3f result)
        {
            result.x = a.y * b.z - a.z * b.y;
            result.y = a.z * b.x - a.x * b.z;
            result.z = a.x * b.y - a.y * b.x;
        }

        public static void Normalize(ref Vertex3f v)
        {
            float length = MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            if (length > 0.0001f)
            {
                float invLength = 1f / length;
                v.x *= invLength;
                v.y *= invLength;
                v.z *= invLength;
            }
            else
            {
                v.x = 0;
                v.y = 0;
                v.z = 0;
            }
        }

        public static void Transform(in Matrix4x4f mat, in Vertex3f v, ref Vertex3f result)
        {
            result.x = mat[0, 0] * v.x + mat[1, 0] * v.y + mat[2, 0] * v.z + mat[3, 0];
            result.y = mat[0, 1] * v.x + mat[1, 1] * v.y + mat[2, 1] * v.z + mat[3, 1];
            result.z = mat[0, 2] * v.x + mat[1, 2] * v.y + mat[2, 2] * v.z + mat[3, 2];
        }

        /// <summary>
        /// 두 벡터의 사이 각도를 도 단위로 계산합니다.
        /// </summary>
        /// <param name="a">정규화된 벡터</param>
        /// <param name="b">정규화된 벡터</param>
        /// <returns></returns>
        public static float AngleBetween(in Vertex3f a, in Vertex3f b)
        {
            float dot = a.x * b.x + a.y * b.y + a.z * b.z;
            dot = Math.Max(-1f, Math.Min(1f, dot)); // 클램핑
            float angleRad = (float)Math.Acos(dot);
            float angleDeg = angleRad * 180f / MathF.Pi;
            return angleDeg;
        }

    }
}
