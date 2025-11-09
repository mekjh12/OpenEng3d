using OpenGL;
using System;
using ZetaExt;

namespace Rot3dMath
{
    public struct EulerAngle
    {
        float _theta;
        float _phi;
        float _psi;

        /// <summary>
        /// Y axis rotation
        /// </summary>
        public float Theta
        {
            get => _theta;
            set => _theta = value;
        }

        /// <summary>
        /// Z axis rotation
        /// </summary>
        public float Psi
        {
            get => _psi;
            set => _psi = value;
        }

        /// <summary>
        /// X axis rotation
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
        }

        public EulerAngle(Matrix4x4f mat)
        {
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

        public EulerAngle(Vertex3f v)
        {
            v.Normalize();
            Vertex3f x = v;
            Vertex3f y = Vertex3f.UnitZ.Cross(x).Normalized;
            Vertex3f z = x.Cross(y).Normalized;

            Matrix3x3f rot = Matrix3x3f.Identity;
            rot = rot.Frame(x, y, z);

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

        public override string ToString()
        {
            return $"phi={_psi}, theta={_theta}, phi={_phi}";
        }
    }
}
