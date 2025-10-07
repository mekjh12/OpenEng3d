using OpenGL;
using System;

namespace ZetaExt
{
    /// <summary>
    /// -----------------------------------------
    /// * Animation Dll만 사용한다.
    /// -----------------------------------------
    /// </summary>
    public struct Quaternion
    {
        public static readonly Quaternion Identity = new Quaternion(0.0, 0.0, 0.0, 1.0);

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

        public Quaternion Conjugated
        {
            get
            {
                Quaternion result = new Quaternion(this);
                result._Vector = -result._Vector;
                return result;
            }
        }

        public Quaternion(double q1, double q2, double q3, double q4)
        {
            _DefaultVector = Vertex3d.UnitY;
            _Vector.x = q1;
            _Vector.y = q2;
            _Vector.z = q3;
            _CosAngle = q4;
            _DefaultVector = RotationVector;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rVector"></param>
        /// <param name="rAngle">Degree</param>
        public Quaternion(Vertex3f rVector, float rAngle)
        {
            _DefaultVector = rVector;
            _Vector = default(Vertex3d);
            _CosAngle = 0.0;
            SetEuler(rVector, rAngle);
        }

        public static Matrix4x4f CreateRotationMatrix(Vertex3f rVector, float rAngle)
        {
            Quaternion q = new Quaternion(rVector, rAngle);
            q.Normalize();
            return (Matrix4x4f)q;
        }


        public Quaternion(Quaternion other)
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
            if (magnitude >= 1.4012984643248171E-45)
            {
                double num = 1.0 / magnitude;
                _Vector *= num;
                _CosAngle *= num;
                return;
            }

            //throw new InvalidOperationException("zero magnitude quaternion");
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
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            //순서는 q2.Concatenate(q1)의 의미는 q1을 적용한 후에 q2를 적용한다.
            float s1 = q1.W;
            float s2 = q2.W;
            Vertex3f v1 = new Vertex3f(q1.X, q1.Y, q1.Z);
            Vertex3f v2 = new Vertex3f(q2.X, q2.Y, q2.Z);
            float s = s1 * s2 - v1.Dot(v2);
            Vertex3f v = v1 * s2 + v2 * s1 + v1.Cross(v2);
            return new Quaternion(v.x, v.y, v.z, s);
        }

        public static Vertex3f operator *(Quaternion q, Vertex3f v)
        {
            return (Matrix3x3f)q * v;
        }

        public static Vertex3d operator *(Quaternion q, Vertex3d v)
        {
            return (Matrix3x3d)q * v;
        }

        public static Vertex4f operator *(Quaternion q, Vertex4f v)
        {
            return (Matrix4x4f)q * v;
        }

        public static Vertex4d operator *(Quaternion q, Vertex4d v)
        {
            return (Matrix4x4d)q * v;
        }

        public static explicit operator Matrix3x3f(Quaternion q)
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

        public static explicit operator Matrix3x3d(Quaternion q)
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

        public static explicit operator Matrix4x4f(Quaternion q)
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

        public static explicit operator Matrix4x4d(Quaternion q)
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

        /// <summary>
        /// 두 쿼터니온 간의 구면 선형 보간(Slerp)을 수행합니다.
        /// </summary>
        /// <param name="q2">목표 쿼터니온</param>
        /// <param name="progression">보간 진행도 (0.0 ~ 1.0)</param>
        /// <returns>보간된 쿼터니온</returns>
        public Quaternion Interpolate(Quaternion q2, float progression)
        {
            // 진행도를 0~1 사이로 제한
            progression = Math.Max(0.0f, Math.Min(1.0f, progression));

            // 시작점이면 현재 쿼터니온 반환
            if (progression <= float.Epsilon)
                return new Quaternion(this);

            // 끝점이면 목표 쿼터니온 반환
            if (progression >= 1.0f - float.Epsilon)
                return new Quaternion(q2);

            // 현재 쿼터니온을 q1으로 복사
            Quaternion q1 = new Quaternion(this);

            // 두 쿼터니온의 내적 계산
            double dot = q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z + q1.W * q2.W;

            // 쿼터니온 플리핑 방지 - 내적이 음수면 한 쪽을 뒤집음
            // (같은 회전을 나타내는 두 쿼터니온 중 더 가까운 경로 선택)
            if (dot < 0.0)
            {
                q2 = -q2;
                dot = -dot;
            }

            // 내적을 [-1, 1] 범위로 제한 (부동소수점 오차 방지)
            dot = Math.Max(-1.0, Math.Min(1.0, dot));

            double theta0, sinTheta0, theta, sinTheta;
            double s0, s1; // 보간 계수

            // 두 쿼터니온이 거의 같은 경우 (선형 보간 사용)
            if (dot > 0.9995)
            {
                // 선형 보간 (Linear interpolation)
                s0 = 1.0 - progression;
                s1 = progression;
            }
            else
            {
                // 구면 선형 보간 (Spherical linear interpolation)
                theta0 = Math.Acos(dot);        // 두 쿼터니온 사이의 각도
                sinTheta0 = Math.Sin(theta0);   // sin(theta0)
                theta = theta0 * progression;   // 보간된 각도
                sinTheta = Math.Sin(theta);     // sin(theta)

                s0 = Math.Sin(theta0 - theta) / sinTheta0;  // 첫 번째 쿼터니온의 계수
                s1 = sinTheta / sinTheta0;                  // 두 번째 쿼터니온의 계수
            }

            // 보간된 쿼터니온 계산
            double x = s0 * q1.X + s1 * q2.X;
            double y = s0 * q1.Y + s1 * q2.Y;
            double z = s0 * q1.Z + s1 * q2.Z;
            double w = s0 * q1.W + s1 * q2.W;

            // 결과 쿼터니온 생성 및 정규화
            Quaternion result = new Quaternion(x, y, z, w);
            result.Normalize();

            return result;
        }




        /// <param name="q2">두 번째 쿼터니언</param>
        /// <returns>두 쿼터니언의 내적 값</returns>
        public static float Dot(Quaternion q1, Quaternion q2)
        {
            return q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z + q1.W * q2.W;
        }

        /// <summary>
        /// 쿼터니언의 단항 마이너스 연산자 (모든 성분에 -1을 곱함)
        /// 쿼터니언 플리핑 방지를 위해 사용됩니다.
        /// </summary>
        /// <param name="q">대상 쿼터니언</param>
        /// <returns>모든 성분이 음수로 변환된 쿼터니언</returns>
        public static Quaternion operator -(Quaternion q)
        {
            return new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
        }

        /// <summary>
        /// 두 쿼터니언의 빼기 연산자
        /// </summary>
        /// <param name="q1">첫 번째 쿼터니언</param>
        /// <param name="q2">두 번째 쿼터니언</param>
        /// <returns>두 쿼터니언의 차</returns>
        public static Quaternion operator -(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X - q2.X, q1.Y - q2.Y, q1.Z - q2.Z, q1.W - q2.W);
        }

        /// <summary>
        /// 두 쿼터니언의 더하기 연산자 (보완용)
        /// </summary>
        /// <param name="q1">첫 번째 쿼터니언</param>
        /// <param name="q2">두 번째 쿼터니언</param>
        /// <returns>두 쿼터니언의 합</returns>
        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
        }

        public override string ToString()
        {
            float x = (float)((_Vector.x < 0.000001f) ? 0.0f : _Vector.x);
            float y = (float)((_Vector.y < 0.000001f) ? 0.0f : _Vector.y);
            float z = (float)((_Vector.z < 0.000001f) ? 0.0f : _Vector.z);
            if (x.ToString() == "NaN") throw new Exception("");
            return $"Angle: {RotationAngle} Axis:({x},{y},{z},{_CosAngle})";
        }


    }
}
