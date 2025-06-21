using OpenGL;
using System;
using ZetaExt;

namespace ZetaExt
{
    public static class Matrix4x4F
    {
        public const float PI = (float)Math.PI;
        public const float RAD_90 = (float)Math.PI * 0.5f;
        public const float RAD_180 = (float)Math.PI;
        public const float RAD_270 = 3.0f * RAD_90;

        /// <summary>
        /// 점을 변환행렬에 의하여 변환한다.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vertex3f Transform(this Matrix4x4f mat, Vertex3f p)
        {
            return (mat * p.Vertex4f()).Vertex3f();
        }

        /// <summary>
        /// 점을 변환행렬에 의하여 변환한다.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vertex3f[] Transform(this Matrix4x4f mat, Vertex3f[] p)
        {
            Vertex3f[] result = new Vertex3f[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                result[i] = mat.Transform(p[i]);
            }
            return result;
        }

        /// <summary>
        /// 점을 변환행렬에 의하여 변환한다.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vertex3f Transform(this Matrix4x4f mat, Vertex4f p)
        {
            return (mat * p).Vertex3f();
        }

        public static Matrix4x4f Frame(this Matrix4x4f mat, Vertex3f x, Vertex3f y, Vertex3f z, Vertex3f p)
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
            mat[3, 0] = p.x;
            mat[3, 1] = p.y;
            mat[3, 2] = p.z;
            return mat;
        }

        /// <summary>
        /// 벡터를 행렬변환한다.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vertex3f Multiply(this Matrix4x4f mat, Vertex3f p)
        {
            return (mat * p.Vertex4f()).Vertex3f();
        }

        public static Matrix4x4f Column(this Matrix4x4f mat, uint col, Vertex3f column)
        {
            mat[col, 0] = column.x;
            mat[col, 1] = column.y;
            mat[col, 2] = column.z;
            return mat;
        }

        public static Matrix4x4f ScaleTransRot(Vertex3f position, float size, Quaternion4 quaternion)
        {
            Matrix4x4f S = Matrix4x4f.Scaled(size, size, size);
            Matrix4x4f T = Matrix4x4f.Translated(position.x, position.y, position.z);
            Matrix4x4f R = ((Matrix4x4f)quaternion);
            return S * R * T;
        }

        public static Matrix4x4f ScaledTranslated(Vertex3f position, float size)
        {
            Matrix4x4f S = Matrix4x4f.Scaled(size, size, size);
            Matrix4x4f T = Matrix4x4f.Translated(position.x, position.y, position.z);
            return S * T;
        }

        public static Matrix4x4f ScaledTranslated(Vertex3f position, float sx, float sy, float sz)
        {
            Matrix4x4f T = Matrix4x4f.Identity;
            T[0, 0] = sx;
            T[1, 1] = sy;
            T[2, 2] = sz;
            T[3, 0] = position.x;
            T[3, 1] = position.y;
            T[3, 2] = position.z;
            return T;
        }

        /// <summary>
        /// 3x3행렬의 값만 남기고 나머지는 모두 0이고 (4,4)=1이다.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix4x4f ToMatrix3x3f(this Matrix4x4f mat)
        {
            Matrix4x4f view3 = Matrix4x4f.Identity;
            for (uint i = 0; i < 3; i++)
            {
                for (uint j = 0; j < 3; j++)
                {
                    view3[j, i] = mat[j, i];
                }
            }
            return view3;
        }

        /// <summary>
        /// 4x4행렬에서 3x3행렬의 회전행렬만 가져온다.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix3x3f Rot3x3f(this Matrix4x4f mat)
        {
            Matrix3x3f m = Matrix3x3f.Identity;
            m[0, 0] = mat[0, 0];
            m[0, 1] = mat[0, 1];
            m[0, 2] = mat[0, 2];
            m[1, 0] = mat[1, 0];
            m[1, 1] = mat[1, 1];
            m[1, 2] = mat[1, 2];
            m[2, 0] = mat[2, 0];
            m[2, 1] = mat[2, 1];
            m[2, 2] = mat[2, 2];
            return m;
        }


        public static Vertex4f DiagonalVector(this ref Matrix4x4f mat)
        {
            return new Vertex4f(mat[0, 0], mat[1, 1], mat[2, 2], mat[3, 3]);
        }

        public static Vertex4f GetTranslation(this ref Matrix4x4f mat)
        {
            return mat.Column3.xyz().xyzw(1.0f);
        }

        public static void Column(ref Matrix4x4f mat, uint col, Vertex3f column)
        {
            mat[col, 0] = column.x;
            mat[col, 1] = column.y;
            mat[col, 2] = column.z;
        }

        public static Matrix4x4f Scaled(Vertex3f scale)
        {
            Matrix4x4f mat = Matrix4x4f.Identity;
            mat[0, 0] = scale.x;
            mat[1, 1] = scale.y;
            mat[2, 2] = scale.z;
            return mat;
        }

        public static Matrix4x4f ToMatrix4x4f(Vertex3f row0, Vertex3f row1, Vertex3f row2)
        {
            Matrix4x4f mat = new Matrix4x4f();
            mat[0, 0] = row0.x;
            mat[1, 0] = row0.y;
            mat[2, 0] = row0.z;
            mat[0, 1] = row1.x;
            mat[1, 1] = row1.y;
            mat[2, 1] = row1.z;
            mat[0, 2] = row2.x;
            mat[1, 2] = row2.y;
            mat[2, 2] = row2.z;
            return mat;
        }

        /*
        public static Matrix4x4f ToMatrix4x4f(this Assimp.Matrix4x4 matrix4)
        {
            Matrix4x4f mat = new Matrix4x4f();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    mat[(uint)j, (uint)i] = matrix4[i + 1, j + 1];
                }
            }
            return mat;
        }
        */

        public static Matrix4x4f Clone(this Matrix4x4f mat)
        {
            Matrix4x4f m = new Matrix4x4f();
            for (uint i = 0; i < 4; i++)
            {
                for (uint j = 0; j < 4; j++)
                {
                    m[j, i] = mat[j, i];
                }
            }
            return m;
        }

        public static Matrix4x4f CreateWorldMatrix(Vertex3f translation,
            float rx, float ry, float rz,
            float scaleX, float scaleY, float scaleZ)
        {
            // SRT순서 중요
            Matrix4x4f matrix = Matrix4x4f.Identity;
            matrix.Scale(scaleX, scaleY, scaleZ);
            matrix.RotateX((float)(rx.ToDegree()));
            matrix.RotateY((float)(ry.ToDegree()));
            matrix.RotateZ((float)(rz.ToDegree()));
            matrix.Translate(translation.x, translation.y, translation.z);
            return matrix;
        }

        public static Matrix4x4f CreateWorldMatrix(Vertex3f pos, Vertex3f right, Vertex3f up, Vertex3f forward)
        {
            Matrix4x4f view = Matrix4x4f.Identity;
            view[0, 0] = right.x;
            view[0, 1] = right.y;
            view[0, 2] = right.z;
            view[1, 0] = up.x;
            view[1, 1] = up.y;
            view[1, 2] = up.z;
            view[2, 0] = forward.x;
            view[2, 1] = forward.y;
            view[2, 2] = forward.z;
            view[3, 0] = pos.x;
            view[3, 1] = pos.y;
            view[3, 2] = pos.z;
            return view;
        }

        public static Matrix4x4f CreateViewMatrix(Vertex3f pos, Vertex3f right, Vertex3f up, Vertex3f forward)
        {
            Matrix4x4f view = Matrix4x4f.Identity;
            view[0, 0] = right.x;
            view[1, 0] = right.y;
            view[2, 0] = right.z;
            view[0, 1] = up.x;
            view[1, 1] = up.y;
            view[2, 1] = up.z;
            view[0, 2] = forward.x;
            view[1, 2] = forward.y;
            view[2, 2] = forward.z;
            view[3, 0] = -right.Dot(pos);
            view[3, 1] = -up.Dot(pos);
            view[3, 2] = -forward.Dot(pos);
            return view;
        }

        /// <summary>
        /// [0, 1] 0:near 1:far
        /// </summary>
        /// <param name="fovy"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public static Matrix4x4f CreateProjectionMatrix(float fovy, float aspectRatio, float near, float far)
        {
            //   --------------------------
            //   g/s  0      0       0
            //   0    g      0       0
            //   0    0   f/(f-n)  -nf/(f-n)
            //   0    0      1       0
            //   --------------------------
            float s = aspectRatio;// (float)_width / (float)_height;
            float g = 1.0f / (float)Math.Tan(fovy.ToRadian() * 0.5f); // g = 1/tan(fovy/2)
            float f = far;
            float n = near;
            Matrix4x4f m = new Matrix4x4f();
            m[0, 0] = g / s;
            m[1, 1] = g;
            m[2, 2] = f / (f - n);
            m[3, 2] = -(n * f) / (f - n);
            m[2, 3] = 1;
            return m;
        }

        public static Matrix4x4f CreateProjectionMatrix(float far, float near, float fov_radian, int w, int h)
        {
            float theta = (fov_radian / 2.0f);
            float aspectRatio = (float)w / (float)h;
            float top = near * (float)Math.Tan(theta);
            float bottom = -near * (float)Math.Tan(theta);
            float right = aspectRatio * top;
            float left = -aspectRatio * top;

            Matrix4x4f myProjection1 = new Matrix4x4f();
            myProjection1[0, 0] = (2 * near) / (right - left);
            myProjection1[1, 1] = (2 * near) / (top - bottom);
            myProjection1[2, 2] = -(far + near) / (far - near);
            myProjection1[3, 2] = -(2 * far * near) / (far - near);
            myProjection1[2, 3] = -1;

            return myProjection1;
        }

    }
}
