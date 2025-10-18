using OpenGL;
using System;
using System.Security.Policy;

namespace ZetaExt
{
    public static class Matrix4x4NoGC
    {
        /// <summary>
        /// 0번째 열을 정규화하여 result에 저장
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="result"></param>
        public static void NormalizeColumn0(in Matrix4x4f mat,  ref Vertex3f result)
        {
            result.x = mat[0, 0];
            result.y = mat[0, 1];
            result.z = mat[0, 2];
            Vertex3NoGC.Normalize(ref result);
        }

        /// <summary>
        /// 1번째 열을 정규화하여 result에 저장
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="result"></param>
        public static void NormalizeColumn1(in Matrix4x4f mat, ref Vertex3f result)
        {
            result.x = mat[1, 0];
            result.y = mat[1, 1];
            result.z = mat[1, 2];
            Vertex3NoGC.Normalize(ref result);
        }

        /// <summary>
        /// 2번째 열을 정규화하여 result에 저장
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="result"></param>
        public static void NormalizeColumn2(in Matrix4x4f mat, ref Vertex3f result)
        {
            result.x = mat[2, 0];
            result.y = mat[2, 1];
            result.z = mat[2, 2];
            Vertex3NoGC.Normalize(ref result);
        }

    }
}
