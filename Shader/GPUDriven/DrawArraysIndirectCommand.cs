using System.Runtime.InteropServices;

namespace Shader.GPUDriven
{
    // DrawArraysIndirectCommand 구조체도 필요하면 추가
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DrawArraysIndirectCommand
    {
        public uint VertexCount;
        public uint InstanceCount;
        public uint First;
        public uint BaseInstance;
    }
}
