using OpenGL;

namespace ZetaExt
{
    public struct RenderLine
    {
        public Vertex3f Start;
        public Vertex3f End;
        public Vertex4f Color;
        public float Thick;

        public RenderLine(Vertex3f start, Vertex3f end, Vertex4f color, float thick)
        {
            this.Start = start;
            this.End = end;
            this.Color = color;
            this.Thick = 10.0f * thick;
        }
    }
}
