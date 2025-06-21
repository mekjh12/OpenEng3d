namespace Geometry
{
    public class Triangle : Polygon
    {
        public Triangle(uint a, uint b, uint c) : base(a, b, c)
        {
            // Polygon의 특수한 경우로 구현이 된 것임.
        }

        public Triangle(int a, int b, int c) : base((ushort)a, (ushort)b, (ushort)c)
        {
            // Polygon의 특수한 경우로 구현이 된 것임.
        }
    }
}
