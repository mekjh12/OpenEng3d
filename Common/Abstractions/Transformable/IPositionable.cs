using OpenGL;

namespace Common.Abstractions
{
    /// <summary>
    /// 기본 위치 정보를 제공한다.
    /// </summary>
    public interface IPositionable
    {
        Vertex3f Position { get; set; } // 위치
        bool IsMoved { get; set; } // 이동 여부
    }
}
