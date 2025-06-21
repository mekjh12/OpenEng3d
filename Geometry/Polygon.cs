namespace Geometry
{
    /// <summary>
    /// 버텍스로 닫힌 유향 폐곡면을 만든다. 버텍스들이 한 평면 위가 아닐 수 있다.
    /// </summary>
    public class Polygon
    {
        uint[] _vertexIndex;

        public uint Count => (uint)_vertexIndex.Length;

        public uint Index(uint index) => _vertexIndex[index];

        /// <summary>
        /// 버텍스의 인덱스로 폴리곤을 생성한다. <br/>
        /// 버텍스리스트와 함께 사용해야 한다.<br/>
        /// </summary>
        /// <param name="verticesIndex"></param>
        public Polygon(params uint[] verticesIndex)
        {
            _vertexIndex = new uint[verticesIndex.Length];
            for (int i = 0; i < verticesIndex.Length; i++)
            {
                _vertexIndex[i] = verticesIndex[i];
            }
        }

        public new string ToString()
        {
            string txt = "";
            for (int i = 0; i < _vertexIndex.Length; i++)
                txt += " " + _vertexIndex[i];
            return txt;
        }
    }
}
