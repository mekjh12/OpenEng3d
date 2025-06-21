using OpenGL;

namespace ZetaExt
{
    public struct RenderPont
    {
        public Vertex3f Position;
        public Vertex4f Color;
        public float Thick;

        public RenderPont(Vertex3f position, Vertex4f color, float thick)
        {
            this.Position = position;
            this.Color = color;
            this.Thick = thick;
        }
    }
}
