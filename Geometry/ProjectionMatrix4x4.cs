using Common;
using FastMath;
using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    public class ProjectionMatrix4x4
    {
        public static Matrix4x4f MakeReflection(Plane plane)
        {
            float nx = plane.X;
            float ny = plane.Y;
            float nz = plane.Z;
            float d = plane.W;

            Matrix4x4f mat = Matrix4x4f.Identity;
            mat[0, 0] = 1.0f - 2.0f * nx * nx;
            mat[1, 0] = -2.0f * nx * ny;
            mat[2, 0] = -2.0f * nx * nz;
            mat[3, 0] = -2.0f * nx * d;

            mat[0, 1] = -2.0f * nx * ny;
            mat[1, 1] = 1.0f - 2.0f * ny * ny;
            mat[2, 1] = -2.0f * ny * nz;
            mat[3, 1] = -2.0f * ny * d;

            mat[0, 2] = -2.0f * nx * nz;
            mat[1, 2] = -2.0f * ny * nz;
            mat[2, 2] = 1.0f - 2.0f * nz * nz;
            mat[3, 2] = -2.0f * nz * d;

            return mat;
        }

        public static Matrix4x4f ModifyProjectionNearPlane(Matrix4x4f matrix, Plane k, Matrix4x4f viewMatrix)
        {
            Matrix4x4f mat = matrix.Clone();
            Plane Pcam = k * viewMatrix;

            Vertex3f normal = Pcam.Normal;
            Vertex4f vcam = new Vertex4f(
                (normal.x.Sgn() - mat[2, 0]) / mat[0, 0],
                (normal.y.Sgn() - mat[2, 1]) / mat[1, 1],
                1.0f,
                (1.0f - mat[2, 2]) / mat[3, 2]);

            float m = 1.0f / (Pcam * vcam);
            mat[0, 2] = m * normal.x;
            mat[1, 2] = m * normal.y;
            mat[2, 2] = m * normal.z;
            mat[3, 2] = m * Pcam.W;

            return mat;
        }

        public static Matrix4x4f ModifyRevProjectionNearPlane(Matrix4x4f RevMatrix, Plane k, Matrix4x4f viewMatrix)
        {
            Matrix4x4f mat = RevMatrix.Clone();
            Plane Pcam = k * viewMatrix;

            Vertex3f normal = Pcam.Normal;
            Vertex4f vcam = new Vertex4f(
                (normal.x.Sgn() - mat[2, 0]) / mat[0, 0],
                (normal.y.Sgn() - mat[2, 1]) / mat[1, 1],
                1.0f,
                mat[2, 2] / mat[3, 2]);

            float m = -1.0f / (Pcam * vcam);
            mat[0, 2] = m * normal.x;
            mat[1, 2] = m * normal.y;
            mat[2, 2] = m * normal.z + 1.0f;
            mat[3, 2] = m * Pcam.W;

            return mat;
        }

        /// <summary>
        /// the perspective projection P_frustum matrix for a view frustum.
        /// </summary>
        /// <param name="fovy">vertical field of view</param>
        /// <param name="s">aspect ratio of the viewport</param>
        /// <param name="n">near plane</param>
        /// <param name="f">far plane</param>
        /// <returns></returns>
        public static Matrix4x4f MakeFrustumProjection(float fovy, float s, float n, float f)
        {
            float g = 1.0f / (float)MathFast.Tan(fovy.ToRadian() * 0.5f);
            float k = f / (f - n);
            //column 0, column 1, column 2, column 3
            return new Matrix4x4f(g / s, 0.0f, 0.0f, 0.0f,
                                0.0f, g, 0.0f, 0.0f,
                                0.0f, 0.0f, k, 1.0f,
                                0.0f, 0.0f, -n * k, 0.0f);
        }

        public static Matrix4x4f MakeFrustumProjectionInverse(float fovy, float s, float n, float f)
        {
            float g = 1.0f / (float)MathFast.Tan(fovy.ToRadian() * 0.5f);
            float k = f / (f - n);
            //column 0, column 1, column 2, column 3
            return new Matrix4x4f(s / g, 0.0f, 0.0f, 0.0f,
                                0.0f, 1 / g, 0.0f, 0.0f,
                                0.0f, 0.0f, 0.0f, -1.0f / n * k,
                                0.0f, 0.0f, 1.0f, 1 / n);
        }

        public static Matrix4x4f MakeInfiniteProjection(float fovy, float s, float n, float e = 0.000001f)
        {
            float g = 1.0f / (float)MathFast.Tan(fovy.ToRadian() * 0.5f);
            e = 1.0f - e;
            //column 0, column 1, column 2, column 3
            return new Matrix4x4f(g / s, 0.0f, 0.0f, 0.0f,
                                0.0f, g, 0.0f, 0.0f,
                                0.0f, 0.0f, 1 - e, 1.0f,
                                0.0f, 0.0f, -n * (1 - e), 0.0f);
        }

        /// <summary>
        /// the reversing perspective projection P_frustum matrix for a view frustum.
        /// </summary>
        /// <param name="fovy"></param>
        /// <param name="s"></param>
        /// <param name="n"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Matrix4x4f MakeRevFrustumProjection(float fovy, float s, float n, float f)
        {
            float g = 1.0f / (float)MathFast.Tan(fovy.ToRadian() * 0.5f);
            float k = n / (n - f);
            //column 0, column 1, column 2, column 3
            return new Matrix4x4f(g / s, 0.0f, 0.0f, 0.0f,
                                0.0f, g, 0.0f, 0.0f,
                                0.0f, 0.0f, k, 1.0f,
                                0.0f, 0.0f, -f * k, 0.0f);
        }

        /// <summary>
        /// orthographic projection matrix. z in from 0(near) to 1(far).
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="t"></param>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Matrix4x4f MakeOrthoProjection(float l, float r, float b, float t, float n, float f)
        {
            float w_inv = 1.0f / (r - l);
            float h_inv = 1.0f / (b - t);
            float d_inv = 1.0f / (f - n);
            return new Matrix4x4f(2.0f * w_inv, 0, 0, 0,
                0, 2.0f * h_inv, 0, 0,
                0, 0, d_inv, 0,
                -(r + l) * w_inv, -(b + t) * h_inv, -n * d_inv, 1.0f);
        }
    }
}
