using System;

namespace ZetaExt
{
    /// <summary>
    /// ZYx 오일러 각도 구조체
    /// <para>
    /// 3D 공간에서의 회전을 세 개의 각도로 표현한다.
    /// </para>
    /// <remarks>
    /// 오일러 각도는 짐벌 락(Gimbal Lock) 문제가 있을 수 있으므로
    /// 복잡한 회전 보간에는 쿼터니언 사용을 권장한다.
    /// </remarks>
    /// </summary>
    public struct EulerAngle
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private float _pitch;  // x축 회전 (피치)
        private float _roll;   // Y축 회전 (롤)
        private float _yaw;    // Z축 회전 (요)

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>
        /// x축 회전 각도 (Pitch)
        /// <para>범위: -180° ~ 180° (라디안: -π ~ π)</para>
        /// <para>고개를 끄덕이는 동작 (위/아래)</para>
        /// </summary>
        public float Pitch
        {
            get => _pitch;
            set => _pitch = value;
        }

        /// <summary>
        /// Y축 회전 각도 (Roll)
        /// <para>범위: -90° ~ 90° (라디안: -π/2 ~ π/2)</para>
        /// <para>몸을 좌우로 기울이는 동작</para>
        /// </summary>
        public float Roll
        {
            get => _roll;
            set => _roll = value;
        }

        /// <summary>
        /// Z축 회전 각도 (Yaw)
        /// <para>범위: -180° ~ 180° (라디안: -π ~ π)</para>
        /// <para>고개를 좌우로 돌리는 동작 (회전)</para>
        /// </summary>
        public float Yaw
        {
            get => _yaw;
            set => _yaw = value;
        }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 지정된 오일러 각도로 초기화한다
        /// </summary>
        /// <param name="pitch">x축 회전 (Pitch, 도 단위)</param>
        /// <param name="roll">Y축 회전 (Roll, 도 단위)</param>
        /// <param name="yaw">Z축 회전 (Yaw, 도 단위)</param>
        public EulerAngle(float pitch, float roll, float yaw)
        {
            _pitch = pitch;
            _roll = roll;
            _yaw = yaw;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 오일러 각도를 문자열로 변환한다
        /// </summary>
        /// <returns>각도 정보 문자열 (소수점 0자리)</returns>
        public override string ToString()
        {
            return $"Pitch(x){_pitch:F0}, Roll(Y){_roll:F0}, Yaw(Z){_yaw:F0}";
        }

        /// <summary>
        /// 두 오일러 각도가 같은지 비교한다
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is EulerAngle other)
            {
                return Math.Abs(_pitch - other._pitch) < 0.0001f &&
                       Math.Abs(_roll - other._roll) < 0.0001f &&
                       Math.Abs(_yaw - other._yaw) < 0.0001f;
            }
            return false;
        }

        /// <summary>
        /// 해시 코드를 반환한다
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _pitch.GetHashCode();
                hash = hash * 23 + _roll.GetHashCode();
                hash = hash * 23 + _yaw.GetHashCode();
                return hash;
            }
        }

        // -----------------------------------------------------------------------
        // 정적 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 제로 회전 (모든 각도가 0인 상태)
        /// </summary>
        public static EulerAngle Zero => new EulerAngle(0, 0, 0);

        /// <summary>
        /// 각도를 정규화한다
        /// </summary>
        public EulerAngle Normalized()
        {
            return new EulerAngle(
                NormalizeAngle(_pitch, -180f, 180f),
                NormalizeAngle(_roll, -90f, 90f),     // Roll은 -90~90 범위
                NormalizeAngle(_yaw, -180f, 180f)
            );
        }

        /// <summary>
        /// 각도를 지정된 범위로 정규화한다
        /// </summary>
        private static float NormalizeAngle(float angle, float min, float max)
        {
            float range = max - min;
            while (angle > max) angle -= range;
            while (angle < min) angle += range;
            return angle;
        }

        // -----------------------------------------------------------------------
        // 연산자 오버로딩
        // -----------------------------------------------------------------------

        public static bool operator ==(EulerAngle left, EulerAngle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EulerAngle left, EulerAngle right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// 두 오일러 각도를 더한다
        /// </summary>
        public static EulerAngle operator +(EulerAngle a, EulerAngle b)
        {
            return new EulerAngle(a._pitch + b._pitch, a._roll + b._roll, a._yaw + b._yaw);
        }

        /// <summary>
        /// 두 오일러 각도를 뺀다
        /// </summary>
        public static EulerAngle operator -(EulerAngle a, EulerAngle b)
        {
            return new EulerAngle(a._pitch - b._pitch, a._roll - b._roll, a._yaw - b._yaw);
        }

        /// <summary>
        /// 오일러 각도에 스칼라를 곱한다
        /// </summary>
        public static EulerAngle operator *(EulerAngle angle, float scalar)
        {
            return new EulerAngle(angle._pitch * scalar, angle._roll * scalar, angle._yaw * scalar);
        }
    }
}