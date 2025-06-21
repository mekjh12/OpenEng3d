using OpenGL;

namespace Geometry
{
    public interface IBoundingVolume
    {
        /// <summary>
        /// 객체를 깊은 복사한다.
        /// </summary>
        /// <returns></returns>
        BoundVolume Clone();

        /// <summary>
        /// 볼륨의 겉넓이
        /// </summary>
        float Area { get; }

        Matrix4x4f ModelMatrix { get; }

        Vertex3f[] Vertices { get; }

    }
}
