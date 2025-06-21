using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace glEng.BSP
{
    public class Wall
    {
        int state;
        int attr;
        int texture;
        Vertex3f normal;
        Vertex3f[] vlist = new Vertex3f[4];
        Vertex3f[] tvlist = new Vertex3f[4];
        Vertex2f texCoord;
        float tu;
        float tv;
        float height;

        public float Tu
        {
            get
            {
                return this.tu;
            }
            set
            {
                this.tu = value;
            }
        }

        public float Tv
        {
            get
            {
                return this.tv;
            }
            set
            {
                this.tv = value;
            }
        }

        public float Height
        {
            get
            {
                return this.height;
            }
            set
            {
                this.height = value;
            }
        }


        public Vertex3f Normal
        {
            get
            {
                return normal;
            }
        }

        public Vertex2f TexCoord
        {
            get
            {
                return texCoord;
            }
        }

        public int Texture
        {
            get
            {
                return texture;
            }

            set
            {
                texture = value;
            }
        }

        public Wall(float x1, float z1, float x2, float z2, float height, float tu = 1, float tv = 1)
        {
            Random random = new Random();
            GenVertexList(x1, z1, x2, z2, height);
            texCoord.x = tu;
            texCoord.y = tv;
            this.tu = tu;
            this.tv = tv;
            this.height = height;
            this.texture = random.Next(0, 2);
        }

        private void GenVertexList(float x1, float z1, float x2, float z2, float height)
        {            
            vlist[0] = new Vertex3f(x1, 0, z1);
            vlist[1] = new Vertex3f(x2, 0, z2);
            vlist[2] = vlist[1] + new Vertex3f(0, height, 0);
            vlist[3] = vlist[0] + new Vertex3f(0, height, 0);
            normal = new Vertex3f(-(z2 - z1), 0, (x2 - x1));
        }

        public Vertex3f[] Vlist
        {
            get
            {
                return vlist;
            }
        }
    }
}
