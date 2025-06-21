using OpenGL;

namespace Geometry
{
    /// <summary>
    /// 물체에 바운딩 박스를 붙인다.
    /// </summary>
    public interface IBoundBoxable
    {
        AABB AABB { get; }

        OBB OBB { get; }

        AABB ModelAABB { get; }

        OBB ModelOBB { get; }

        bool IsVisibleOBB { get; set; }

        bool IsVisibleAABB { get; set; }

        void UpdateBoundingBox();
    }
}


//BoundVolume LocalOBB { get; }
