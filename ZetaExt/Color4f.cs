using OpenGL;
using System;
using System.Numerics;

namespace ZetaExt
{
    public static class Color4f
    {
        public static Vertex4f Yellow => new Vertex4f(1, 1, 0, 1);

        public static Vertex4f Red => new Vertex4f(1, 0, 0, 1);

        public static Vertex4f Green => new Vertex4f(0, 1, 0, 1);

        public static Vertex4f Blue => new Vertex4f(0, 0, 1, 1);

        public static Vertex4f White => new Vertex4f(1, 1, 1, 1);

        public static Vertex4f RGB(float r, float g, float b) => new Vertex4f(r, g, b, 1);
    }
}
