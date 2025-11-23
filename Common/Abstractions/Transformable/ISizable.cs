using OpenGL;

namespace Common.Abstractions
{
    /// <summary>
    /// 크기 조정 기능을 제공한다.
    /// </summary>
    public interface ISizable
    {
        Vertex3f Size { get; set; } // 크기

        /// <summary>
        /// 크기를 조정한다.
        /// </summary>
        /// <param name="scalex">x축 스케일</param>
        /// <param name="scaleY">Y축 스케일</param>
        /// <param name="scaleZ">Z축 스케일</param>
        void Scale(float scalex, float scaleY, float scaleZ);
    }
}
