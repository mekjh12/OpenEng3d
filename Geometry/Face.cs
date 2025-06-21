namespace Geometry
{
    public class Face
    {
        const int kCapacity = 2;
        const int kMaxPolyhedronFaceCount = 16 * kCapacity;
        const int kMaxPolyhedronFaceEdgeCount = kMaxPolyhedronFaceCount - 1;

        ushort _edgeCount;
        ushort[] _edgeIndices;

        public ushort EdgeCount
        {
            get => _edgeCount;
            set => _edgeCount = value;
        }

        public ushort GetEdge(int index) => _edgeIndices[index];

        public void SetEdge(int index, ushort edge) => _edgeIndices[index] = edge;

        public Face()
        {
            _edgeIndices = new ushort[kMaxPolyhedronFaceEdgeCount];
        }

        public Face(ushort[] edgeIndices)
        {
            _edgeIndices = edgeIndices;
            _edgeCount = (byte)edgeIndices.Length;
        }
    }

}
