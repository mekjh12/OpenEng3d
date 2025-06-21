using OpenGL;

namespace Common.Abstractions
{
    internal interface IBoundVolumable
    {
        Vertex3f Center { get; set; }

        Matrix4x4f ModelMatrix { get; }

        Vertex3f Size { get; set; }

        Vertex3f[] Vertices { get; }

        float CalculateScreenSpaceArea(Matrix4x4f vpMatrix);

        BoundVolume Clone();

    }
}
