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
        /// 행렬이 유효한지 검사한다. 
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static bool IsValidMatrix(this Matrix4x4f matrix)
        {
            float determinant = matrix.Determinant;
            return !float.IsNaN(determinant) && !float.IsInfinity(determinant);
        }

        /// <summary>
        /// 문자열을 식별문자를 이용하여 Matrix4x4f로 변환한다.
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="splitChar"></param>
        /// <param name="transposed"></param>
        /// <returns></returns>
        public static Matrix4x4f ParseToMatrix4x4f(this string txt, char splitChar = ' ',  bool transposed = false)
        {
            string[] value = txt.Trim().Split(splitChar);
            float[] items = new float[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                items[i] = float.Parse(value[i].Trim());
            }

            Matrix4x4f mat = new Matrix4x4f(items);
            return transposed ? mat.Transposed : mat;
        }

        /// <summary>
        /// Quaternions for Computer Graphics by John Vince. p199 참고
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static ZetaExt.Quaternion ToQuaternion(this Matrix4x4f mat)
        {
            ZetaExt.Quaternion q = ZetaExt.Quaternion.Identity;
            float a11 = mat[0, 0];
            float a12 = mat[1, 0];
            float a13 = mat[2, 0];

            float a21 = mat[0, 1];
            float a22 = mat[1, 1];
            float a23 = mat[2, 1];

            float a31 = mat[0, 2];
            float a32 = mat[1, 2];
            float a33 = mat[2, 2];

            float trace = a11 + a22 + a33;
            if (trace >= -1)
            {
                // I changed M_EPSILON to 0
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                q.W = 0.25f / s;
                q.X = (a32 - a23) * s;
                q.Y = (a13 - a31) * s;
                q.Z = (a21 - a12) * s;
            }
            else
            {
                if (1 + a11 - a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + a11 - a22 - a33);
                    q.X = 0.25f * s;
                    q.Y = (a12 + a21) / s;
                    q.Z = (a13 + a31) / s;
                    q.W = (a32 - a23) / s;
                }
                else if (1 - a11 + a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 + a22 - a33);
                    q.Y = 0.25f * s;
                    q.X = (a12 + a21) / s;
                    q.Z = (a23 + a32) / s;
                    q.W = (a13 - a31) / s;
                }
                else
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 - a22 + a33);
                    q.Z = 0.25f * s;
                    q.X = (a13 + a31) / s;
                    q.Y = (a23 + a32) / s;
                    q.W = (a21 - a12) / s;
                }
            }
            return q;
        }

        /// <summary>
        /// 역행렬을 구한다.
        /// <para>행렬의 역행렬을 구할 때, 소수점 오차를 줄이기 위해 1000.0f로 곱한 후 역행렬을 구하고 다시 1000.0f으로 나눈다.</para>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix4x4f Inversed(this Matrix4x4f mat)
        {
            return (mat * 1000.0f).Inverse * 1000.0f;
        }

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

        /// <summary>
        /// 행렬의 회전 부분을 정규화하여 직교 행렬(Orthonormal Matrix)로 만든다.
        /// 이는 행렬에 누적된 스케일이나 비틀림(shear) 오차를 제거하여 순수 회전 행렬로 만듭니다.
        /// </summary>
        /// <param name="mat">원본 Matrix4x4f</param>
        /// <returns>회전 부분이 정규화된 새로운 Matrix4x4f</returns>
        public static Matrix4x4f Orthonormalize(this Matrix4x4f mat)
        {
            // 행렬의 열 벡터(축)를 추출
            Vertex3f x_axis = mat.Column0.Vertex3f();
            Vertex3f y_axis = mat.Column1.Vertex3f();
            Vertex3f z_axis = mat.Column2.Vertex3f();

            // Gram-Schmidt 과정을 사용하여 축을 직교정규화(Orthonormalize)합니다.
            // 1. 첫 번째 축(x_axis)을 정규화합니다.
            x_axis = x_axis.Normalized;

            // 2. 두 번째 축(y_axis)을 x_axis에 수직이 되도록 만듭니다.
            //    y_axis에서 x_axis 방향 성분을 뺀 후, 정규화합니다.
            Vertex3f projection_y_on_x = x_axis * y_axis.Dot(x_axis);
            y_axis = (y_axis - projection_y_on_x).Normalized;

            // 3. 세 번째 축(z_axis)은 x_axis와 y_axis에 모두 수직이 되도록
            //    외적(cross product)을 사용하여 계산합니다.
            //    이렇게 하면 z_axis는 x, y 축에 자동으로 수직이 됩니다.
            z_axis = x_axis.Cross(y_axis);
            // 외적 결과는 이미 정규화되어 있을 가능성이 높지만,
            // 안정성을 위해 다시 한번 정규화하는 것이 좋습니다.
            z_axis = z_axis.Normalized;

            // 보간에 사용될 최종 행렬을 생성
            Matrix4x4f orthoMat = mat.Clone();

            // 정규화된 축으로 회전 부분을 재구성
            // Column 함수를 사용하여 새로운 축 벡터로 행렬의 열을 채웁니다.
            orthoMat.Column(0, x_axis);
            orthoMat.Column(1, y_axis);
            orthoMat.Column(2, z_axis);

            return orthoMat;
        }
    }
}
