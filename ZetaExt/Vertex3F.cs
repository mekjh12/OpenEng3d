using OpenGL;
using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ZetaExt
{
    public static class Vertex3F
    {
        //public static Vertex3f ToVertex3f(this Assimp.Vector3D v) => new Vertex3f(v.X, v.Y, v.Z);

        /// <summary>
        /// 벡터의 성분들의 최댓값
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float MaxComponent(this Vertex3f a)
        {
            return Math.Max(a.x, Math.Max(a.y, a.z));
        }

        /// <summary>
        /// 벡터의 성분들의 최솟값
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float MinComponent(this Vertex3f a)
        {
            return Math.Min(a.x, Math.Min(a.y, a.z));
        }


        public static bool IsEqual(this Vertex3f a, Vertex3f b, float kEpsilon = 0.0001f)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) + Math.Abs(a.z - b.z) < kEpsilon;
        }

        public static uint ColorUInt(this Vertex3f color)
        {
            byte rr = (byte)(1 * color.x);
            byte gg = (byte)(1 * color.y);
            byte bb = (byte)(1 * color.z);
            uint red = (uint)(rr << 16);
            uint green = (uint)(gg << 8);
            uint blue = (uint)(bb);
            return red + green + blue;
        }

        public static Matrix4x4f RotateBetween(this Vertex3f a, Vertex3f b)
        {
            // 월드공간의 좌표로 변환을 해야 함.
            float af = a.Length();
            float bf = b.Length();
            if (af == 0 || bf == 0) return Matrix4x4f.Identity;

            Vertex3f R = a.Cross(b).Normalized;
            float cf = (a - b).Length();
            float cos = (af * af + bf * bf - cf * cf) / (2 * af * bf);
            float cosTheta = (-1 <= cos && cos <= 1) ? ((float)Math.Acos(cos)).ToDegree() : 0.0f;
            return (Matrix4x4f)new Quaternion4(R, cosTheta);
        }

        public static Vertex4f Vertex4f(this Vertex3f a)
        {
            return new Vertex4f(a.x, a.y, a.z, 1.0f);
        }

        public static Vertex4f Vertex4f(this Vertex3f a, float w)
        {
            return new Vertex4f(a.x, a.y, a.z, w);
        }

        public static float Distance(this Vertex3f a)
        {
            return (float)Math.Sqrt(a.Dot(a));
        }

        public static float DistanceSquare(this Vertex3f a)
        {
            return a.Dot(a);
        }

        public static float Distance(this Vertex3f a, Vertex3f b)
        {
            Vertex3f d = a - b;
            return (float)Math.Sqrt(d.Dot(d));
        }

        public static Vertex3f Reject(this Vertex3f a, Vertex3f b)
        {
            float k = a.Dot(b) / b.Dot(b);
            return a - b * k;
        }

        /// <summary>
        /// dot(a, a)
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float Dot(this Vertex3f a)
        {
            return a.Dot(a);
        }

        /// <summary>
        /// 벡터의 길이를 계산합니다 (최적화된 버전)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Length(this Vertex3f a)
        {
            float dot = a.Dot(a);

            // 0에 가까운 값 체크 (정확도 향상)
            if (dot < float.Epsilon)
                return 0.0f;

            // MathF.Sqrt는 Math.Sqrt보다 빠름 (float 전용)
            return MathF.Sqrt(dot);
        }

        /// <summary>
        /// 벡터의 크기
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float Magnitude(this Vertex3f a)
        {
            return (float)Math.Sqrt(a.Dot(a));
        }

        /// <summary>
        /// 벡터의 크기의 제곱
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float SquareMagnitude(this Vertex3f a)
        {
            return (float)a.Dot(a);
        }

        public static float Dot(this Vertex3f a, Vertex3f b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        /// <summary>
        /// 두 벡터의 성분곱의 벡터를 반환한다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vertex3f ComponentProduct(this Vertex3f a, Vertex3f b)
        {
            return new Vertex3f(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static float Dot(this Vertex3f a, Vertex2f b)
        {
            return a.x * b.x + a.y * b.y + a.z;
        }

        /// <summary>
        /// 외적의 방향은 왼손으로 감는다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vertex3f Cross(this Vertex3f a, Vertex3f b)
        {
            return new Vertex3f(a.y * b.z - a.z * b.y, -a.x * b.z + a.z * b.x, a.x * b.y - a.y * b.x);
        }

        public static Vertex3f xyz(this Vertex4f a)
        {
            if (a.w == 0)
            {
                return new Vertex3f(a.x, a.y, a.z);
            }
            else
            {
                return new Vertex3f(a.x / a.w, a.y / a.w, a.z / a.w);
            }
        }

        public static Vertex2f xy(this Vertex4f a)
        {
            return new Vertex2f(a.x, a.y);
        }

        public static Vertex2f xy(this Vertex3f a)
        {
            return new Vertex2f(a.x, a.y);
        }

        public static float[] ToArray(this Vertex3f a, float w)
        {
            return new float[] { a.x, a.y, a.z, w };
        }

        public static Vertex4f xyzw(this Vertex3f a, float w = 1.0f)
        {
            return new Vertex4f(a.x, a.y, a.z, w);
        }

        public static Vertex3f SetValue(this Vertex3f a, int index, float value)
        {
            if (index == 0) a.x = value;
            else if (index == 1) a.y = value;
            else if (index == 2) a.z = value;
            return new Vertex3f(a.x, a.y, a.z);
        }

        public static float GetValue(this Vertex3f a, int index)
        {
            if (index == 0) return a.x;
            else if (index == 1) return a.y;
            else if (index == 2) return a.z;
            else return 0;
        }

        /// <summary>
        /// 쿼터니온을 이용하여 점을 임의의 축으로 theta만큼 회전한 위치벡터를 반환한다.
        /// 방향은 왼손으로 감는 방향이다.
        /// </summary>
        /// <param name="point">회전할 점</param>
        /// <param name="theta">라디안</param>
        /// <param name="axis">회전축</param>
        /// <returns></returns>
        public static Vertex3f RotateAxis(this Vertex3f point, float theta, Vertex3f axis)
        {
            axis.Normalize();
            float s = (float)Math.Cos(theta / 2);
            float x = (float)Math.Sin(theta / 2) * axis.x;
            float y = (float)Math.Sin(theta / 2) * axis.y;
            float z = (float)Math.Sin(theta / 2) * axis.z;

            Quaternion4 q = new Quaternion4(x, y, z, s);
            Quaternion4 p = new Quaternion4(point.x, point.y, point.z, 0);
            Quaternion4 qi = q.Conjugated;

            Quaternion4 P = q * p * qi;
            Vertex3f pVec = new Vertex3f(P.X, P.Y, P.Z);

            return pVec;
        }


        public static Vertex3f MultiplyComponents(Vertex3f a, Vertex3f b)
        {
            return new Vertex3f(a.x * b.x, a.y * b.y, a.z * b.z);
        }

    }
}
