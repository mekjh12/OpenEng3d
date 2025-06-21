namespace Geometry
{
    public class Edge
    {
        byte[] _vertexIndex;
        byte[] _faceIndex;

        public ushort Vertex0 => _vertexIndex[0];

        public ushort Vertex1 => _vertexIndex[1];

        public ushort Face0 => _faceIndex[0];

        public ushort Face1 => _faceIndex[1];

        public ushort GetVertexIndex(int index) => _vertexIndex[index];

        public void SetVertexIndex(int index, byte value) => _vertexIndex[index] = value;

        public ushort GetFaceIndex(int index) => _faceIndex[index];

        public void SetFaceIndex(int index, byte value) => _faceIndex[index] = value;

        public Edge()
        {
            _vertexIndex = new byte[2];
            _faceIndex = new byte[2];
        }

        public Edge(byte vertexIndex0, byte vertexIndex1, byte faceIndex0, byte faceIndex1)
        {
            _vertexIndex = new byte[] { vertexIndex0, vertexIndex1 };
            _faceIndex = new byte[] { faceIndex0, faceIndex1 };
        }

        public new string ToString()
            => $"v=({_vertexIndex[0]},{_vertexIndex[1]}), f=({_faceIndex[0]},{_faceIndex[1]})";
    }

}
