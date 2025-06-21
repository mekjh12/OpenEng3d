using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Shader;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// Facade 패턴을 적용한 지형 시스템 인터페이스입니다.
    /// </summary>
    public interface ITerrainRegion
    {
        List<Chunk> FrustumedChunk { get; }
        List<AABB> FrustumedChunkAABB { get; }
        BVH BoundingVolumeHierarchy { get; }
    }
}