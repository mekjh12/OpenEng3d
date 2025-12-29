using System.Numerics;
using System.Runtime.InteropServices;

namespace Light
{
    // ✅ UBO용 구조체 (std140 정렬 규칙 준수!)
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct LightingUBO
    {
        // Ambient Light (16 bytes)
        public Vector3 ambientColor;      // 12 bytes
        private float _padding1;          // 4 bytes (정렬)

        // Directional Light (32 bytes)
        public Vector3 lightDirection;    // 12 bytes
        private float _padding2;          // 4 bytes (정렬)

        public Vector3 lightColor;        // 12 bytes
        private float _padding3;          // 4 bytes (정렬)

        // 총 48 bytes (std140 규칙 만족)
    }
}
