using OpenGL;
using System.Runtime.InteropServices;

namespace GPUDriven
{
    // AABB 구조체 (GPU와 공유)
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB
    {
        public Vertex3f Min;
        private float _padding1;
        public Vertex3f Max;
        private float _padding2;

        public AABB(Vertex3f min, Vertex3f max)
        {
            Min = min;
            Max = max;
            _padding1 = 0;
            _padding2 = 0;
        }

        // 크기: 32 bytes (GPU 정렬 맞춤)
    }

    // Indirect Draw 파라미터
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawArraysIndirectCommand
    {
        public uint VertexCount;
        public uint InstanceCount;  // GPU가 쓸 값
        public uint First;
        public uint BaseInstance;
    }
}
