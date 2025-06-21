using Geometry;
using Model3d;
using System.Collections.Generic;

namespace Terrain
{
    // 지형 패치 생성과 관리를 위한 인터페이스
    public interface ITerrainPatchFactory                         
    {        
        List<AABB> CreateChunks(RegionCoord coord, int n, int unitSize, TerrainData terrainData);  // 전체 지형 패치들을 생성 (n: 분할 수, unitSize: 단위 크기)
        Entity CreateUnifiedTerrainEntity(int n, int unitSize, Texture heightMapTexture, int mx, int my);  // 단일 지형 엔티티를 생성 (n: 분할 수, unitSize: 단위 크기)
    }
}