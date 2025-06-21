using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui2d
{
    class RawModel2d
    {
        private uint _vao;
        private uint _vbo;
        private int _vertexCount;

        public uint VAO => _vao;

        public uint VBO => _vbo;

        public int VertexCount => _vertexCount;

        public RawModel2d(uint vao, uint vbo, int vertexCount)
        {
            _vao = vao;
            _vbo = vbo;
            _vertexCount = vertexCount;
        }
    }
}
