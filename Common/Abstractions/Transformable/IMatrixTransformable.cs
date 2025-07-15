using OpenGL;

namespace Common.Abstractions
{
    /// <summary>
    /// 고급 변환 행렬 기능을 제공한다.
    /// </summary>
    public interface IMatrixTransformable
    {
        Matrix4x4f LocalBindMatrix { get; } // 로컬 바인드 행렬
        Matrix4x4f ModelMatrix { get; } // 모델 행렬

        /// <summary>
        /// 로컬 바인드 변환을 적용한다.
        /// </summary>
        /// <param name="sx">X축 스케일</param>
        /// <param name="sy">Y축 스케일</param>
        /// <param name="sz">Z축 스케일</param>
        /// <param name="rotx">X축 회전</param>
        /// <param name="roty">Y축 회전</param>
        /// <param name="rotz">Z축 회전</param>
        /// <param name="x">X축 위치</param>
        /// <param name="y">Y축 위치</param>
        /// <param name="z">Z축 위치</param>
        void LocalBindTransform(float sx = 1.0f, float sy = 1.0f, float sz = 1.0f,
            float rotx = 0, float roty = 0, float rotz = 0,
            float x = 0, float y = 0, float z = 0);
    }
}
