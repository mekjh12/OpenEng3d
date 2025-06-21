using OpenGL;
using System;
using System.Numerics;

namespace ZetaExt
{
    public static class Vertex4F
    {
        public static Vertex4f Yellow => new Vertex4f(1, 1, 0, 1);

        public static Vertex4f Red => new Vertex4f(1, 0, 0, 1);

        public static Vertex4f Green => new Vertex4f(0, 1, 0, 1);

        public static Vertex4f Blue => new Vertex4f(0, 0, 1, 1);

        public static Vertex4f White => new Vertex4f(1, 1, 1, 1);

        public static Vertex3f Vertex3f(this Vertex4f vec)
        {
            return new Vertex3f(vec.x, vec.y, vec.z);
        }

        public static Vertex3f Vertex3fDivideW(this Vertex4f vec)
        {
            return new Vertex3f(vec.x / vec.w, vec.y / vec.w, vec.z / vec.w);
        }
    }
}
