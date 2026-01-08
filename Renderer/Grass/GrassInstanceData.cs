using OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer
{
    /// <summary>
    /// **GPU**로 전송할 풀 인스턴스 데이터
    /// 32바이트 정렬 필수!
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GrassInstanceData
    {
        public Vertex3f Position;  // 12 bytes
        public float Rotation;    // 4 bytes
        public float Scale;       // 4 bytes
        public float WindPhase;   // 4 bytes (바람 애니메이션용)
        public float _padding1;   // 4 bytes (32바이트 정렬)
        public float _padding2;   // 4 bytes

        // 총 32 bytes
    }
}
