using Common.Abstractions;
using Model3d;
using OpenGL;
using System.Collections.Generic;

namespace Terrain
{
    public abstract class WorldSectorManager
    {
        // 필드
        protected TerrainSector[,] _sectors;
        protected List<TerrainSector> _activeSectors;
        protected float _sectorSize;
        protected Vertex2f _worldSize;
        protected Vertex2i _sectorCount;
        protected bool[] _visibleSectorMask;
        protected Camera _camera;

        // 속성
        public float SectorSize => _sectorSize;
        public Vertex2f WorldSize => _worldSize;
        public int ActiveSectorCount => _activeSectors.Count;
        public int VisibleSectorCount { get; protected set; }
        public TerrainSector CurrentSector { get; protected set; }

        // 초기화/설정 관련 추상 메서드
        public abstract void Initialize(Vertex2f worldSize, float sectorSize);
        protected abstract void SetupSectors();

        // 업데이트 관련 추상 메서드
        public abstract void Update(Camera camera);
        protected abstract void UpdateVisibleSectors(Camera camera);
        protected abstract void UpdateActiveSectors();

        // 섹터 관리 관련 추상 메서드
        public abstract TerrainSector GetSectorAt(Vertex3f position);
        protected abstract void ActivateSector(int x, int y);
        protected abstract void DeactivateSector(int x, int y);

        // 컬링/마스킹 관련 추상 메서드
        protected abstract void CullSectors(Camera camera);
        protected abstract void UpdateSectorMask(Camera camera);

        // 엔티티 관리 관련 추상 메서드
        public abstract void RegisterEntity(Entity entity);
        public abstract void UnregisterEntity(Entity entity);
        public abstract void UpdateEntityPosition(Entity entity);
    }
}
