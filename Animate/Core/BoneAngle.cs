using OpenGL;

namespace Animate
{
    /// <summary>
    /// 본(뼈대)의 회전 각도 제한을 정의하는 클래스<br/>
    /// - 3D 애니메이션에서 본의 회전 범위를 제한하여 자연스러운 움직임 구현<br/>
    /// - X축/Z축 회전(구부리기/젖히기)과 Y축 회전(비틀기) 각도를 별도 관리
    /// </summary>
    public struct BoneAngle
    {
        Vertex4f _angle; // 제약 각도 (x축 min/max, z축 min/max)
        Vertex2f _twist; // 비틀기 각도 (y축 min/max)

        /// <summary>
        /// 구부리기/젖히기 제약 각도 (X축, Z축 회전)<br/>
        /// x,y = X축 회전 범위(min, max), z,w = Z축 회전 범위(min, max)
        /// </summary>
        public Vertex4f ConstraintAngle
        {
            get => _angle;
            set => _angle = value;
        }

        /// <summary>
        /// 비틀기 제약 각도 (Y축 회전)<br/>
        /// x = 최소 비틀기 각도, y = 최대 비틀기 각도
        /// </summary>
        public Vertex2f TwistAngle
        {
            get => _twist;
            set => _twist = value;
        }

        /// <summary>
        /// 본의 회전 제한 각도를 설정하는 생성자
        /// </summary>
        /// <param name="theta1">X축 회전 최소 각도 (구부리기 한계)</param>
        /// <param name="theta2">X축 회전 최대 각도 (젖히기 한계)</param>
        /// <param name="theta3">Z축 회전 최소 각도 (좌측 굽힘 한계)</param>
        /// <param name="theta4">Z축 회전 최대 각도 (우측 굽힘 한계)</param>
        /// <param name="twistThetaMin">Y축 회전 최소 각도 (왼쪽 비틀기 한계)</param>
        /// <param name="twistThetaMax">Y축 회전 최대 각도 (오른쪽 비틀기 한계)</param>
        public BoneAngle(float theta1 = -180.0f, float theta2 = 180.0f, float theta3 = -180.0f,
                        float theta4 = 180.0f, float twistThetaMin = -90.0f, float twistThetaMax = 90.0f)
        {
            _angle = new Vertex4f(theta1, theta2, theta3, theta4);
            _twist = new Vertex2f(twistThetaMin, twistThetaMax);
        }

        /// <summary>지정된 비틀기 각도가 제한 범위 내에 있는지 확인</summary>
        public bool IsInboundTwist(float theta) => (_twist.x <= theta && theta <= _twist.y);

        /// <summary>지정된 X축 회전 각도가 제한 범위 내에 있는지 확인</summary>
        public bool IsInboundXRotation(float theta) => (_angle.x <= theta && theta <= _angle.y);

        /// <summary>지정된 Z축 회전 각도가 제한 범위 내에 있는지 확인</summary>
        public bool IsInboundZRotation(float theta) => (_angle.z <= theta && theta <= _angle.w);

        /// <summary>
        /// 주어진 각도를 제한 범위 내로 클램핑
        /// </summary>
        /// <param name="xRotation">X축 회전 각도</param>
        /// <param name="yRotation">Y축 회전 각도 (비틀기)</param>
        /// <param name="zRotation">Z축 회전 각도</param>
        /// <returns>제한된 회전 각도 (x, y, z)</returns>
        public Vertex3f ClampRotation(float xRotation, float yRotation, float zRotation)
        {
            float clampedX = System.Math.Max(_angle.x, System.Math.Min(_angle.y, xRotation));
            float clampedY = System.Math.Max(_twist.x, System.Math.Min(_twist.y, yRotation));
            float clampedZ = System.Math.Max(_angle.z, System.Math.Min(_angle.w, zRotation));

            return new Vertex3f(clampedX, clampedY, clampedZ);
        }

        public override string ToString()
        {
            return $"BoneAngle(X: [{_angle.x:F1}°, {_angle.y:F1}°], " +
                   $"Y: [{_twist.x:F1}°, {_twist.y:F1}°], " +
                   $"Z: [{_angle.z:F1}°, {_angle.w:F1}°])";
        }
    }
}