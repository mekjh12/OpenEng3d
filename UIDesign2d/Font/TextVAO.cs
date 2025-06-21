using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZetaEngine
{
    public class TextVAO
    {
        int verticeCount = 0;
        int vao;
        int vbo;

        public int VerticesCount
        {
            get
            {
                return this.verticeCount;
            }
        }

        public uint VAO
        {
            get
            {
                return (uint)this.vao;
            }

            set
            {
                this.vao = (int)value;
            }
        }

        public uint VBO
        {
            get
            {
                return (uint)this.vbo;
            }

            set
            {
                this.vbo = (int)value;
            }
        }

        public TextVAO(uint vao, uint vbo, int verticesCount)
        {
            this.vao = (int)vao;
            this.vbo = (int)vbo;
            this.verticeCount = verticesCount;
        }
    }
}
