using OpenGL;
using System;

namespace ZetaExt
{
    /// <summary>
    /// ZYX 오일러 각도 구조체
    /// 회전 순서: Z축(psi) → Y축(theta) → X축(phi)
    /// </summary>
    public struct EulerAngle
    {
        float _theta;
        float _phi;
        float _psi;
        Matrix4x4f _transform;

        /// <summary>
        /// 변환 행렬
        /// </summary>
        public Matrix4x4f Transform => _transform;

        /// <summary>
        /// Y axis rotation, -90<deg<90
        /// </summary>
        public float Theta
        {
            get => _theta;
            set => _theta = value;
        }

        /// <summary>
        /// Z axis rotation, -180<deg<180
        /// </summary>
        public float Psi
        {
            get => _psi;
            set => _psi = value;
        }

        /// <summary>
        /// X axis rotation, -180<deg<180
        /// </summary>
        public float Phi
        {
            get => _phi;
            set => _phi = value;
        }

        public EulerAngle(float theta, float phi, float psi)
        {
            _theta = theta;
            _phi = phi;
            _psi = psi;
            _transform = Matrix4x4f.Identity;
        }

        public EulerAngle(Vertex3f v, Vertex3f up)
        {
            Vertex3f x = v.Normalized;
            Vertex3f y = up.Cross(v).Normalized;
            Vertex3f z = x.Cross(y).Normalized;

            Matrix3x3f rot = Matrix3x3f.Identity;
            rot.Framed(x, y, z);

            _transform = Matrix4x4f.Identity;
            _transform = _transform.Frame(x, y, z, Vertex3f.Zero);

            float R11 = rot[0, 0];
            float R12 = rot[1, 0];
            float R13 = rot[2, 0];
            float R21 = rot[0, 1];
            float R22 = rot[1, 1];
            float R23 = rot[2, 1];
            float R31 = rot[0, 2];
            float R32 = rot[1, 2];
            float R33 = rot[2, 2];

            _phi = ((float)Math.Atan2(R32, R33)).ToDegree();
            _theta = ((float)Math.Atan2(-R31, Math.Sqrt(R32 * R32 + R33 * R33))).ToDegree();
            _psi = ((float)Math.Atan2(R21, R11)).ToDegree();
        }

        public EulerAngle(Matrix4x4f mat)
        {
            _transform = mat;

            float R11 = mat[0, 0];
            float R12 = mat[1, 0];
            float R13 = mat[2, 0];
            float R21 = mat[0, 1];
            float R22 = mat[1, 1];
            float R23 = mat[2, 1];
            float R31 = mat[0, 2];
            float R32 = mat[1, 2];
            float R33 = mat[2, 2];

            _phi = ((float)Math.Atan2(R32, R33)).ToDegree();
            _theta = ((float)Math.Atan2(-R31, Math.Sqrt(R32 * R32 + R33 * R33))).ToDegree();
            _psi = ((float)Math.Atan2(R21, R11)).ToDegree();
        }

        public override string ToString()
        {
            return $"psi={_psi.ToString("F3")}, theta={_theta.ToString("F3")}, phi={_phi.ToString("F3")}";
        }
    }
}
