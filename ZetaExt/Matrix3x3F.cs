using OpenGL;
using System;
using ZetaExt;

namespace ZetaExt
{
    public static class Matrix3x3F
    {
        /// <summary>
        /// 행렬의 0, 1, 2번째 행렬에 벡터를 순서대로 설정한다.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void Framed(ref this Matrix3x3f mat, Vertex3f x, Vertex3f y, Vertex3f z)
        {
            mat[0, 0] = x.x;
            mat[0, 1] = x.y;
            mat[0, 2] = x.z;
            mat[1, 0] = y.x;
            mat[1, 1] = y.y;
            mat[1, 2] = y.z;
            mat[2, 0] = z.x;
            mat[2, 1] = z.y;
            mat[2, 2] = z.z;
        }

        public static Matrix3x3f Frame(this Matrix3x3f mat, Vertex3f x, Vertex3f y, Vertex3f z)
        {
            mat[0, 0] = x.x;
            mat[0, 1] = x.y;
            mat[0, 2] = x.z;
            mat[1, 0] = y.x;
            mat[1, 1] = y.y;
            mat[1, 2] = y.z;
            mat[2, 0] = z.x;
            mat[2, 1] = z.y;
            mat[2, 2] = z.z;
            return mat;
        }

        public static Matrix4x4f ToMat4x4f(this Matrix3x3f mat, Vertex3f translated)
        {
            Matrix4x4f m = Matrix4x4f.Identity;
            m[0, 0] = mat[0, 0];
            m[0, 1] = mat[0, 1];
            m[0, 2] = mat[0, 2];
            m[1, 0] = mat[1, 0];
            m[1, 1] = mat[1, 1];
            m[1, 2] = mat[1, 2];
            m[2, 0] = mat[2, 0];
            m[2, 1] = mat[2, 1];
            m[2, 2] = mat[2, 2];
            m[3, 0] = translated.x;
            m[3, 1] = translated.y;
            m[3, 2] = translated.z;
            return m;
        }

    }
}
