using FastMath;
using OpenGL;
using System.Runtime.InteropServices;

namespace Geometry
{
    /// <summary>
    /// 3차원 축 정렬 바운딩 박스 (float 정밀도)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB3f
    {
        public Vertex3f Min;  // Lower -> Min (더 수학적)
        public Vertex3f Max;  // Upper -> Max

        public Vertex3f Center => (Min + Max) * 0.5f;
        public Vertex3f Size => Max - Min;
        public float Radius => MathFast.Sqrt(Size.x * Size.x + Size.y * Size.y + Size.z * Size.z) * 0.5f;

        public AABB3f(Vertex3f min, Vertex3f max)
        {
            Min = min;
            Max = max;
        }
    }
}
