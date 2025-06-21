using OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace ZetaExt
{
    public class Quaternion4
    {
        public static readonly Quaternion4 Identity = new Quaternion4(0.0, 0.0, 0.0, 1.0);

        private Vertex3d _DefaultVector;
        private Vertex3d _Vector;
        private double _CosAngle;

        public Vertex3f RotationVector
        {
            get
            {
                if (_Vector.ModuleSquared() >= float.Epsilon)
                {
                    _DefaultVector = _Vector.Normalized;
                }

                return (Vertex3f)_DefaultVector;
            }
            set => SetEuler(value, RotationAngle);
        }

        public float RotationAngle
        {
            get => (float)Angle.ToDegrees(2.0 * Math.Acos(_CosAngle));
            set => SetEuler(RotationVector, value);
        }

        public float X
        {
            get => (float)_Vector.x;
            set => _Vector.x = value;
        }

        public float Y
        {
            get => (float)_Vector.y;
            set => _Vector.y = value;
        }

        public float Z
        {
            get => (float)_Vector.z;
            set => _Vector.z = value;
        }

        public float W
        {
            get => (float)_CosAngle;
            set => _CosAngle = value;
        }

        public double Magnitude
        {
            get
            {
                double num = _Vector.x * _Vector.x;
                double num2 = _Vector.y * _Vector.y;
                double num3 = _Vector.z * _Vector.z;
                double num4 = _CosAngle * _CosAngle;
                return Math.Sqrt(num + num2 + num3 + num4);
            }
        }

        public bool IsIdentity
        {
            get
            {
                if (Math.Abs(_Vector.Module()) >= float.Epsilon)
                {
                    return false;
                }

                if (Math.Abs(_CosAngle - 1.0) >= 1.4012984643248171E-45)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsNormalized => Math.Abs(Magnitude - 1.0) < 1.4012984643248171E-45;

        public Quaternion4 Conjugated
        {
            get
            {
                Quaternion4 result = new Quaternion4(this);
                result._Vector = -result._Vector;
                return result;
            }
        }

        /// <summary>
        /// ai+bj+ck+d = (a,b,c,d)
        /// </summary>
        /// <param name="q1">i</param>
        /// <param name="q2">j</param>
        /// <param name="q3">k</param>
        /// <param name="q4">real part</param>
        public Quaternion4(double q1, double q2, double q3, double q4)
        {
            _DefaultVector = Vertex3d.UnitY;
            _Vector.x = q1;
            _Vector.y = q2;
            _Vector.z = q3;
            _CosAngle = q4;
            _DefaultVector = RotationVector;
        }

        public Quaternion4(Vertex3f rVector)
        {
            _Vector = default(Vertex3d);
            _Vector.x = rVector.x;
            _Vector.y = rVector.y;
            _Vector.z = rVector.z;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rVector"></param>
        /// <param name="rAngle">Degree</param>
        public Quaternion4(Vertex3f rVector, float rAngle)
        {
            _DefaultVector = rVector;
            _Vector = default(Vertex3d);
            _CosAngle = 0.0;
            SetEuler(rVector, rAngle);
        }

        public Quaternion4(Quaternion4 other)
        {
            _DefaultVector = other._DefaultVector;
            _Vector = other._Vector;
            _CosAngle = other._CosAngle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rVector"></param>
        /// <param name="rAngle">Degree</param>
        public void SetEuler(Vertex3f rVector, float rAngle)
        {
            double num = Angle.ToRadians(rAngle / 2f);
            double num2 = Math.Sin(num);
            _Vector.x = num2 * (double)rVector.x;
            _Vector.y = num2 * (double)rVector.y;
            _Vector.z = num2 * (double)rVector.z;
            _CosAngle = Math.Cos(num);
            Normalize();
        }

        public void Normalize()
        {
            double magnitude = Magnitude;
            if (magnitude >= 0.00000000001f)
            {
                double num = 1.0 / magnitude;
                _Vector *= num;
                _CosAngle *= num;
                return;
            }

            throw new InvalidOperationException("zero magnitude Quaternion4");
        }

        public void Conjugate()
        {
            _Vector = -_Vector;
        }

        /// <summary>
        /// 쿼터니온의 곱을 반환한다. 
        /// OpenGL.Quaternion의 곱의 연산 오류로 인하여 새롭게 구현하였다.
        /// 순서는 q2.Concatenate(q1)의 의미는 q1을 적용한 후에 q2를 적용한다.
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static Quaternion4 operator *(Quaternion4 q1, Quaternion4 q2)
        {
            //순서는 q2.Concatenate(q1)의 의미는 q1을 적용한 후에 q2를 적용한다.
            float s1 = q1.W;
            float s2 = q2.W;
            Vertex3f v1 = new Vertex3f(q1.X, q1.Y, q1.Z);
            Vertex3f v2 = new Vertex3f(q2.X, q2.Y, q2.Z);
            float s = s1 * s2 - v1.Dot(v2);
            Vertex3f v = v1 * s2 + v2 * s1 + v1.Cross(v2);
            return new Quaternion4(v.x, v.y, v.z, s);
        }

        public static Quaternion4 operator +(Quaternion4 q1, Quaternion4 q2)
        {
            return new Quaternion4(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
        }

        public static Quaternion4 operator *(Quaternion4 q, float s)
        {
            return new Quaternion4(q.X * s, q.Y * s, q.Z * s, q.W * s);
        }

        public static Vertex3f operator *(Quaternion4 q, Vertex3f v)
        {
            return (Matrix3x3f)q * v;
        }

        public static Vertex3d operator *(Quaternion4 q, Vertex3d v)
        {
            return (Matrix3x3d)q * v;
        }

        public static Vertex4f operator *(Quaternion4 q, Vertex4f v)
        {
            return (Matrix4x4f)q * v;
        }

        public static Vertex4d operator *(Quaternion4 q, Vertex4d v)
        {
            return (Matrix4x4d)q * v;
        }

        public static explicit operator Matrix3x3f(Quaternion4 q)
        {
            Matrix3x3f result = default(Matrix3x3f);
            double x = q._Vector.x;
            double y = q._Vector.y;
            double z = q._Vector.z;
            double cosAngle = q._CosAngle;
            double num = x * x;
            double num2 = y * y;
            double num3 = z * z;
            result[0u, 0u] = (float)(1.0 - 2.0 * (num2 + num3));
            result[1u, 0u] = (float)(2.0 * (x * y - z * cosAngle));
            result[2u, 0u] = (float)(2.0 * (x * z + y * cosAngle));
            result[0u, 1u] = (float)(2.0 * (x * y + z * cosAngle));
            result[1u, 1u] = (float)(1.0 - 2.0 * (num + num3));
            result[2u, 1u] = (float)(2.0 * (y * z - x * cosAngle));
            result[0u, 2u] = (float)(2.0 * (x * z - y * cosAngle));
            result[1u, 2u] = (float)(2.0 * (x * cosAngle + y * z));
            result[2u, 2u] = (float)(1.0 - 2.0 * (num + num2));
            return result;
        }

        public static explicit operator Matrix3x3d(Quaternion4 q)
        {
            Matrix3x3d result = default(Matrix3x3d);
            double x = q._Vector.x;
            double y = q._Vector.y;
            double z = q._Vector.z;
            double cosAngle = q._CosAngle;
            double num = x * x;
            double num2 = y * y;
            double num3 = z * z;
            result[0u, 0u] = 1.0 - 2.0 * (num2 + num3);
            result[1u, 0u] = 2.0 * (x * y - z * cosAngle);
            result[2u, 0u] = 2.0 * (x * z + y * cosAngle);
            result[0u, 1u] = 2.0 * (x * y + z * cosAngle);
            result[1u, 1u] = 1.0 - 2.0 * (num + num3);
            result[2u, 1u] = 2.0 * (y * z - x * cosAngle);
            result[0u, 2u] = 2.0 * (x * z - y * cosAngle);
            result[1u, 2u] = 2.0 * (x * cosAngle + y * z);
            result[2u, 2u] = 1.0 - 2.0 * (num + num2);
            return result;
        }

        public static explicit operator Matrix4x4f(Quaternion4 q)
        {
            Matrix4x4f result = default(Matrix4x4f);
            double x = q._Vector.x;
            double y = q._Vector.y;
            double z = q._Vector.z;
            double cosAngle = q._CosAngle;
            double num = x * x;
            double num2 = y * y;
            double num3 = z * z;
            result[0u, 0u] = (float)(1.0 - 2.0 * (num2 + num3));
            result[1u, 0u] = (float)(2.0 * (x * y - z * cosAngle));
            result[2u, 0u] = (float)(2.0 * (x * z + y * cosAngle));
            result[3u, 0u] = 0f;
            result[0u, 1u] = (float)(2.0 * (x * y + z * cosAngle));
            result[1u, 1u] = (float)(1.0 - 2.0 * (num + num3));
            result[2u, 1u] = (float)(2.0 * (y * z - x * cosAngle));
            result[3u, 1u] = 0f;
            result[0u, 2u] = (float)(2.0 * (x * z - y * cosAngle));
            result[1u, 2u] = (float)(2.0 * (x * cosAngle + y * z));
            result[2u, 2u] = (float)(1.0 - 2.0 * (num + num2));
            result[3u, 2u] = 0f;
            result[0u, 3u] = 0f;
            result[1u, 3u] = 0f;
            result[2u, 3u] = 0f;
            result[3u, 3u] = 1f;
            return result;
        }

        /// <summary>
        /// 쿼터니온 방향에 위치를 지정한 변환행렬을 가져온다.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Matrix4x4f ToMatrixt4x4f(Vertex3f position)
        {
            Matrix4x4f transform = Matrix4x4f.Identity;

            float r = (float)_CosAngle;
            float i = (float)_Vector.x;
            float j = (float)_Vector.y;
            float k = (float)_Vector.z;

            transform[0, 0] = 1 - 2 * j * j - 2 * k * k;
            transform[1, 0] = 2 * i * j - 2 * r * k;
            transform[2, 0] = 2 * i * k + 2 * r * j;
            transform[3, 0] = position.x;

            transform[0, 1] = 2 * i * j + 2 * r * k;
            transform[1, 1] = 1 - 2 * i * i - 2 * k * k;
            transform[2, 1] = 2 * j * k - 2 * r * i;
            transform[3, 1] = position.y;

            transform[0, 2] = 2 * i * k - 2 * r * j;
            transform[1, 2] = 2 * j * k + 2 * r * i;
            transform[2, 2] = 1 - 2 * i * i - 2 * j * j;
            transform[3, 2] = position.z;

            return transform;
        }

        public static explicit operator Matrix4x4d(Quaternion4 q)
        {
            Matrix4x4d result = default(Matrix4x4d);
            double x = q._Vector.x;
            double y = q._Vector.y;
            double z = q._Vector.z;
            double cosAngle = q._CosAngle;
            double num = x * x;
            double num2 = y * y;
            double num3 = z * z;
            result[0u, 0u] = 1.0 - 2.0 * (num2 + num3);
            result[1u, 0u] = 2.0 * (x * y - z * cosAngle);
            result[2u, 0u] = 2.0 * (x * z + y * cosAngle);
            result[3u, 0u] = 0.0;
            result[0u, 1u] = 2.0 * (x * y + z * cosAngle);
            result[1u, 1u] = 1.0 - 2.0 * (num + num3);
            result[2u, 1u] = 2.0 * (y * z - x * cosAngle);
            result[3u, 1u] = 0.0;
            result[0u, 2u] = 2.0 * (x * z - y * cosAngle);
            result[1u, 2u] = 2.0 * (x * cosAngle + y * z);
            result[2u, 2u] = 1.0 - 2.0 * (num + num2);
            result[3u, 2u] = 0.0;
            result[0u, 3u] = 0.0;
            result[1u, 3u] = 0.0;
            result[2u, 3u] = 0.0;
            result[3u, 3u] = 1.0;
            return result;
        }

        public Quaternion4 Interpolate(Quaternion4 q2, float progression)
        {
            // 클래스를 정비할 필요가 있음.
            System.Numerics.Quaternion p
                = new System.Numerics.Quaternion((float)_Vector.x, (float)_Vector.y, (float)_Vector.z, (float)_CosAngle);
            System.Numerics.Quaternion q = new System.Numerics.Quaternion(q2.X, q2.Y, q2.Z, q2.W);
            System.Numerics.Quaternion r = System.Numerics.Quaternion.Slerp(p, q, progression);
            return new Quaternion4(r.X, r.Y, r.Z, r.W);
        }

        public override string ToString()
        {
            float x = (float)((_Vector.x < 0.000001f) ? 0.0f : _Vector.x);
            float y = (float)((_Vector.y < 0.000001f) ? 0.0f : _Vector.y);
            float z = (float)((_Vector.z < 0.000001f) ? 0.0f : _Vector.z);
            if (x.ToString() == "NaN") throw new Exception("");
            return $"Angle: {RotationAngle} Axis:({x},{y},{z},{_CosAngle})";
        }


        /// <summary>
        /// Quaternions for Computer Graphics by John Vince. p199 참고
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Quaternion4 ToQuaternion(Matrix3x3f mat)
        {
            Quaternion4 q = Quaternion4.Identity;

            float a11 = mat[0, 0];
            float a12 = mat[1, 0];
            float a13 = mat[2, 0];

            float a21 = mat[0, 1];
            float a22 = mat[1, 1];
            float a23 = mat[2, 1];

            float a31 = mat[0, 2];
            float a32 = mat[1, 2];
            float a33 = mat[2, 2];

            float trace = a11 + a22 + a33;
            if (trace >= -1)
            {
                // I changed M_EPSILON to 0
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                q.W = 0.25f / s;
                q.X = (a32 - a23) * s;
                q.Y = (a13 - a31) * s;
                q.Z = (a21 - a12) * s;
            }
            else
            {
                if (1 + a11 - a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + a11 - a22 - a33);
                    q.X = 0.25f * s;
                    q.Y = (a12 + a21) / s;
                    q.Z = (a13 + a31) / s;
                    q.W = (a32 - a23) / s;
                }
                else if (1 - a11 + a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 + a22 - a33);
                    q.Y = 0.25f * s;
                    q.X = (a12 + a21) / s;
                    q.Z = (a23 + a32) / s;
                    q.W = (a13 - a31) / s;
                }
                else
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 - a22 + a33);
                    q.Z = 0.25f * s;
                    q.X = (a13 + a31) / s;
                    q.Y = (a23 + a32) / s;
                    q.W = (a21 - a12) / s;
                }
            }
            return q;
        }

        /// <summary>
        /// 벡터로 향하는 쿼터니온을 가져온다.
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Quaternion4 ToQuaternion(Vertex3f to) => new Quaternion4(to.Normalized);

        public static Quaternion4 ToQuaternion(Matrix4x4f mat) => ToQuaternion(mat.Rot3x3f());

        public static Quaternion4 SlerpUnclamped(Quaternion4 a, Quaternion4 b, float t)
        {
            Quaternion4 quaternion;
            Quaternion4.INTERNAL_CALL_SlerpUnclamped(ref a, ref b, t, out quaternion);
            return quaternion;
        }

        private static void INTERNAL_CALL_SlerpUnclamped(ref Quaternion4 a, ref Quaternion4 b, float t, out Quaternion4 value)
        {
            float cosAngle = Quaternion4.Dot(a, b);
            if (cosAngle < 0)
            {
                cosAngle = -cosAngle;
                b = new Quaternion4(-b.X, -b.Y, -b.Z, -b.W);
            }
            float t1, t2;
            if (cosAngle < 0.95)
            {
                float angle = (float)Math.Acos(cosAngle);
                float sinAgle = (float)Math.Sin(angle);
                float invSinAngle = 1 / sinAgle;
                t1 = (float)(Math.Sin((1 - t) * angle) * invSinAngle);
                t2 = (float)(Math.Sin(t * angle) * invSinAngle);
                Quaternion4 quat = new Quaternion4(a.X * t1 + b.X * t2, a.Y * t1 + b.Y * t2, a.Z * t1 + a.Z * t2, a.W * t1 + b.W * t2);
                value = quat;
            }
            else
            {
                value = Quaternion4.Lerp(a, b, t);
            }
        }


        public static Quaternion4 Lerp(Quaternion4 a, Quaternion4 b, float t)
        {
            Quaternion4 quaternion;
            Quaternion4.INTERNAL_CALL_Lerp(ref a, ref b, t, out quaternion);
            return quaternion;
        }

        private static float Dot(Quaternion4 a, Quaternion4 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

        private static void INTERNAL_CALL_Lerp(ref Quaternion4 a, ref Quaternion4 b, float t, out Quaternion4 value)
        {
            t = Clamp(t, 0, 1);
            Quaternion4 q = new Quaternion4(0, 0, 0, 0);
            if (Quaternion4.Dot(a, b) < 0)
            {
                q.X = a.X + t * (-b.X - a.X);
                q.Y = a.Y + t * (-b.Y - a.Y);
                q.Z = a.Z + t * (-b.Z - a.Z);
                q.W = a.W + t * (-b.W - b.W);
            }
            else
            {
                q.X = a.X + t * (b.X - a.X);
                q.Y = a.Y + t * (b.Y - a.Y);
                q.Z = a.Z + t * (b.Z - a.Z);
                q.W = a.W + t * (b.W - b.W);
            }
            q.Normalize();
            value = q;
        }


        private static float Clamp(float t, float min, float max)
        {
            if (t < min) return min;
            else if (t > max) return max;
            return t;
        }


    }
}
