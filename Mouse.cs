using OpenGL;

namespace OpenEng3d
{
    static class Mouse
    {
        public static Vertex2i PrevPosition = Vertex2i.Zero;
        
        public static Vertex2i CurrentPosition = Vertex2i.Zero;

        public static Vertex2i DeltaPosition => CurrentPosition - PrevPosition;
    }
}
