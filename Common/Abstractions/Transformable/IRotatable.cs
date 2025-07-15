using Common.Mathematics;

namespace Common.Abstractions
{
    /// <summary>
    /// 회전 기능을 제공한다.
    /// </summary>
    public interface IRotatable
    {
        Pose Pose { get; } // 자세

        /// <summary>
        /// 직접 각도를 설정한다.
        /// </summary>
        /// <param name="pitch">피치 각도</param>
        /// <param name="yaw">요 각도</param>
        /// <param name="roll">롤 각도</param>
        void SetRollPitchAngle(float pitch, float yaw, float roll);

        /// <summary>
        /// 상대적 요 회전을 적용한다.
        /// </summary>
        /// <param name="deltaDegree">회전 각도</param>
        void Yaw(float deltaDegree);

        /// <summary>
        /// 상대적 롤 회전을 적용한다.
        /// </summary>
        /// <param name="deltaDegree">회전 각도</param>
        void Roll(float deltaDegree);

        /// <summary>
        /// 상대적 피치 회전을 적용한다.
        /// </summary>
        /// <param name="deltaDegree">회전 각도</param>
        void Pitch(float deltaDegree);
    }
}
