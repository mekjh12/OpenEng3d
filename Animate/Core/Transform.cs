using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 3D 공간에서 객체의 변환(위치, 회전)을 관리하는 클래스<br/>
    /// - 4x4 변환 행렬 기반으로 위치 이동과 회전 제어<br/>
    /// - 로컬 좌표계: Right(-X), Forward(-Y), Up(Z), Position(W)
    /// </summary>
    /// <remarks>
    /// <para>3D 좌표계 및 회전:</para>
    /// <code>
    ///           Up(+Z)
    ///             |
    ///             |
    ///             |
    ///   Left(+X)  *----------- Right(-X)
    ///            /|
    ///           / |
    ///          /  |
    ///    Forward(-Y)
    ///    
    ///    Yaw   : Y축 중심 회전 (← →)
    ///    Pitch : X축 중심 회전 (↑ ↓)  
    ///    Roll  : Z축 중심 회전 (⟲ ⟳)
    /// </code>
    /// 
    /// <para>행렬 구조 (컬럼 기준):</para>
    /// <code>
    ///        X     Y     Z     W
    ///    [Right Back   Up   Pos ]  ← 각 컬럼의 의미
    ///    [ -X    Y     Z     X  ]  ← 실제 방향
    ///    [ lx   by    uz    px  ]
    ///    [ ly   by    uy    py  ]
    ///    [ lz   bz    uz    pz  ]
    ///    [  0    0     0     1  ]
    /// </code>
    /// </remarks>
    public class Transform
    {
        protected Matrix4x4f _transform; // 4x4 변환 행렬

        public Matrix4x4f Matrix4x4f => _transform;
        public Vertex3f Up => _transform.Column2.xyz();
        public Vertex3f Forward => -_transform.Column1.xyz();
        public Vertex3f Right => -_transform.Column0.xyz();
        public Vertex3f Left => _transform.Column0.xyz();
        public Vertex3f Position => _transform.Column3.xyz();
        public Matrix4x4f InverseMatrix => _transform.Inversed();

        /// <summary>
        /// 생성자
        /// </summary>
        public Transform()
        {
            _transform = Matrix4x4f.Identity;
        }

        /// <summary>
        /// 바닥면에 맞춰 정렬된 전진 방향<br/>
        /// Z값을 최소 0.1로 제한하여 수직 하향을 방지
        /// </summary>
        public Vertex3f ForwardAlignFloor
        {
            get
            {
                Vertex3f goForward = Forward;
                goForward.z = Math.Max(0.1f, goForward.z); // 최소 기울기 보장
                goForward.Normalize();
                return goForward;
            }
        }

        public void GoFoward(float delta)
        {
            SetPosition(Position + Forward.Normalized * delta);
        }

        /// <summary>전진 방향을 설정하여 로컬 좌표계 재구성</summary>
        public void SetForward(Vertex3f forward)
        {
            Vertex3f z = _transform.Column2.xyz(); // 현재 Up 벡터 유지
            Vertex3f x = forward.Cross(z).Normalized; // 새로운 Right 벡터 계산
            Vertex3f y = z.Cross(x).Normalized; // 새로운 Back 벡터 계산
            Vertex3f p = _transform.Column3.xyz(); // 현재 위치 유지

            Matrix4x4f mat = Matrix4x4f.Identity.Frame(x, y, z, p) * Matrix4x4f.RotatedZ(180);
            _transform = mat;
        }

        private void SetPosition(Vertex3f pos)
        {
            _transform[3, 0] = pos.x;
            _transform[3, 1] = pos.y;
            _transform[3, 2] = pos.z;
        }

        public void IncreasePosition(float dx, float dy, float dz)
        {
            SetPosition(_transform.Position + new Vertex3f(dx, dy, dz));
        }

        /// <summary>Y축(수직) 중심 회전 (좌우 회전)</summary>
        public void Yaw(float deltaDegree)
        {
            Vertex3f up = -_transform.Column1.Vertex3f(); // 오른손 법칙으로
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(up, deltaDegree);
            _transform = ((Matrix4x4f)(_transform.ToQuaternion() * q));
        }

        /// <summary>Z축(전진) 중심 회전 (롤링)</summary>
        public virtual void Roll(float deltaDegree)
        {
            Vertex3f forward = _transform.Column2.Vertex3f();
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(forward, deltaDegree);
            _transform = ((Matrix4x4f)(_transform.ToQuaternion() * q));
        }

        /// <summary>X축(오른쪽) 중심 회전 (위아래 회전)</summary>
        public virtual void Pitch(float deltaDegree)
        {
            Vertex3f right = _transform.Column0.Vertex3f();
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(right, deltaDegree);
            _transform = ((Matrix4x4f)(_transform.ToQuaternion() * q));
        }

        /// <summary>로컬 좌표계 기준으로 이동</summary>
        public void MoveLocal(float forward, float right, float up)
        {
            Vertex3f movement = Forward * forward + Right * right + Up * up;
            IncreasePosition(movement.x, movement.y, movement.z);
        }

        /// <summary>지정된 방향을 바라보도록 회전 설정</summary>
        public void LookAt(Vertex3f target)
        {
            Vertex3f direction = (target - Position).Normalized;
            SetForward(direction);
        }

        /// <summary>변환 행렬을 항등 행렬로 초기화</summary>
        public void Reset()
        {
            _transform = Matrix4x4f.Identity;
        }

        public override string ToString()
        {
            return $"Transform(Pos: {Position}, Forward: {Forward}, Up: {Up})";
        }
    }
}