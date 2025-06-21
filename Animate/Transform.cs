using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    ///                           up
    ///    lx  bx  ux  px         |
    ///    ly  by  uy  py         |______ back
    ///    lz  bz  uz  pz        /
    ///     0   0   0   1       /
    ///                        left
    /// </summary>
    public class Transform
    {
        protected Matrix4x4f _transform;

        public Matrix4x4f Matrix4x4f => _transform;

        public Vertex3f Up => _transform.Column2.xyz();

        public Vertex3f Forward => -_transform.Column1.xyz();

        public Vertex3f Right => -_transform.Column0.xyz();

        public Vertex3f Left => _transform.Column0.xyz();

        public Vertex3f Position => _transform.Column3.xyz();

        public Vertex3f ForwardAlignFloor
        {
            get
            {
                Vertex3f goForward = Forward;
                goForward.z = Math.Max(0.1f, goForward.z);
                goForward.Normalize();
                return goForward;
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public Transform()
        {
            _transform = Matrix4x4f.Identity;
        }

        public void SetForward(Vertex3f forward)
        {
            Vertex3f z = _transform.Column2.xyz();
            Vertex3f x = forward.Cross(z).Normalized;
            Vertex3f y = z.Cross(x).Normalized;
            Vertex3f p = _transform.Column3.xyz();
            Matrix4x4f mat = Matrix4x4f.Identity.Frame(x, y, z, p) * Matrix4x4f.RotatedZ(180);
            _transform = mat;
        }

        public void SetPosition(Vertex3f pos)
        {
            _transform[3, 0] = pos.x;
            _transform[3, 1] = pos.y;
            _transform[3, 2] = pos.z;
        }

        public void IncreasePosition(float dx, float dy, float dz)
        {
            SetPosition(_transform.Position + new Vertex3f(dx, dy, dz));
        }

        public void Yaw(float deltaDegree)
        {
            Vertex3f up = -_transform.Column1.Vertex3f(); // 오른손 법칙으로
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(up, deltaDegree);
            _transform = ((Matrix4x4f)( _transform.ToQuaternion() * q));
        }

        public virtual void Roll(float deltaDegree)
        {
            Vertex3f forward = _transform.Column2.Vertex3f();
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(forward, deltaDegree);
            _transform = ((Matrix4x4f)(_transform.ToQuaternion() * q));
        }

        public virtual void Pitch(float deltaDegree)
        {
            Vertex3f right = _transform.Column0.Vertex3f();
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(right, deltaDegree);
            _transform = ((Matrix4x4f)(_transform.ToQuaternion() * q));
        }

    }
}
