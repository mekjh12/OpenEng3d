using OpenGL;

namespace Common.Abstractions
{
    internal interface IBoundVolumeCostable
    {
        float Area { get; }

        BoundVolume Union(BoundVolume boundVoulume);

        BoundVolume Intersect(BoundVolume boundVoulume);
    }
}
