using Geometry;
using Occlusion;
using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 지형 시스템의 가시성 컬링을 담당하는 인터페이스입니다.
    /// </summary>
    public interface ITerrainCulling
    {
        List<Chunk> FrustumedChunk { get; }
        List<AABB> FrustumedChunkAABB { get; }
        BVH BoundingVolumeHierarchy { get; }
    }
}