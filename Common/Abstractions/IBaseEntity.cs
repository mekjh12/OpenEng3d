using OpenGL;

namespace Common.Abstractions
{
    internal interface IBaseEntity
    {
        uint OBJECT_GUID { get; }

        string Name { get; set; }

        Vertex3f Color { get; set; }

        void Update(Camera camera);
    }
}
