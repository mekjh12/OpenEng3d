using OpenGL;
using ZetaExt;

namespace Common.Mathematics
{
    /// <summary>
    /// 3D 공간에서 객체의 위치와 방향을 나타내는 포즈(Pose) 클래스
    /// <code>
    /// 쿼터니온(Quaternion)을 사용하여 회전을 표현하고, 
    /// 3차원 벡터로 위치를 표현한다.
    /// </code>
    /// </summary>
    public struct Pose
    {
        // 회전을 표현하는 쿼터니온
        Quaternion4 _q;
        // 위치를 표현하는 3차원 벡터
        Vertex3f _pos;

        /// <summary>
        /// 포즈의 회전을 표현하는 쿼터니온
        /// 쿼터니온은 3D 회전을 표현하는 데 사용되며, 짐벌락(Gimbal Lock)을 방지할 수 있다.
        /// </summary>
        public Quaternion4 Quaternion
        {
            get => _q;
            set => _q = value;
        }


        /// <summary>
        /// Pose를 4x4 변환 행렬로 명시적 변환
        /// 회전축이 주축(X, Y, Z)일 경우 해당하는 회전 행렬을 반환하고,
        /// 임의의 축일 경우 쿼터니온을 행렬로 변환하여 반환한다.
        /// </summary>
        /// <param name="pose">변환할 Pose 객체</param>
        /// <returns>
        /// 포즈의 변환을 표현하는 4x4 행렬
        /// - 주축 회전의 경우: RotatedX, RotatedY, RotatedZ 행렬
        /// - 임의 축 회전의 경우: 쿼터니온으로부터 계산된 회전 행렬
        /// </returns>
        public static explicit operator Matrix4x4f(Pose pose)
        {
            float angle = pose._q.RotationAngle;

            // X축 회전 검사
            if (pose._q.RotationVector.Normalized == Vertex3f.UnitX)
            {
                return Matrix4x4f.RotatedX(angle);
            }
            // Y축 회전 검사
            else if (pose._q.RotationVector.Normalized == Vertex3f.UnitY)
            {
                return Matrix4x4f.RotatedY(angle);
            }
            // Z축 회전 검사
            else if (pose._q.RotationVector.Normalized == Vertex3f.UnitZ)
            {
                return Matrix4x4f.RotatedZ(angle);
            }
            // 임의의 축 회전
            else
            {
                return (Matrix4x4f)pose._q;
            }
        }

        /// <summary>
        /// 3D 공간에서의 포즈의 위치를 표현하는 3차원 벡터
        /// X, Y, Z 좌표로 구성된다.
        /// </summary>
        public Vertex3f Position
        {
            get => _pos;
            set => _pos = value;
        }

        /// <summary>
        /// Pose 클래스의 생성자
        /// </summary>
        /// <param name="q">초기 회전을 나타내는 쿼터니온</param>
        /// <param name="pos">초기 위치를 나타내는 3차원 벡터</param>
        /// <remarks>
        /// 새로운 포즈 객체를 생성하고 초기 회전과 위치를 설정한다.
        /// - q: 회전을 나타내는 Quaternion4 객체
        /// - pos: 위치를 나타내는 Vertex3f 객체
        /// </remarks>
        public Pose(Quaternion4 q, Vertex3f pos)
        {
            _q = q;
            _pos = pos;
        }
    }
}