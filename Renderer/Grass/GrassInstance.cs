using OpenGL;

namespace Renderer
{
    /// <summary>
    /// **CPU용** 풀 인스턴스 데이터 구조체
    /// </summary>
    public struct GrassInstance
    {
        /// <summary>
        /// xyz (Z가 높이! - Z-up 좌표계)
        /// </summary>
        public Vertex3f Position;

        /// <summary>
        /// XY 평면에서의 회전 (라디안)
        /// </summary>
        public float Rotation;

        /// <summary>
        /// 크기 배율
        /// </summary>
        public float Scale;
    }
}
