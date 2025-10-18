using OpenGL;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 본의 로컬 축을 나타내는 열거형
    /// </summary>
    public enum LocalSpaceAxis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    public static class LocalSpaceAxisHelper
    {
        /// <summary>
        /// 축 벡터 추출 (ref로 전달하여 복사 최소화)
        /// </summary>
        public static void GetAxisVector(LocalSpaceAxis axis, Matrix4x4f transform, ref Vertex3f axisVertex3f)
        {
            // 현재 포즈에서 본의 로컬 전방 벡터 계산
            switch (axis)
            {
                case LocalSpaceAxis.X:
                    Matrix4x4NoGC.NormalizeColumn0(transform, ref axisVertex3f);
                    break;
                case LocalSpaceAxis.Y:
                    Matrix4x4NoGC.NormalizeColumn1(transform, ref axisVertex3f);
                    break;
                case LocalSpaceAxis.Z:
                    Matrix4x4NoGC.NormalizeColumn2(transform, ref axisVertex3f);
                    break;
            }

            // Normalized 호출 최소화
            float length = axisVertex3f.Length();
            axisVertex3f = length > 0.0001f ? axisVertex3f * (1f / length) : axisVertex3f;
        }

    }

}
