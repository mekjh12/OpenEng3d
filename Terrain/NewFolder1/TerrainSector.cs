using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 지형을 섹터 단위로 관리하기 위한 추상 기본 클래스입니다.
    /// 각 섹터는 지형의 일정 영역을 담당하며 해당 영역의 엔티티들을 관리합니다.
    /// </summary>
    public abstract class TerrainSector
    {
        protected Vertex2i _gridPosition;      // 섹터의 그리드 좌표 (x,y 인덱스)
        protected AABB _bounds;                // 섹터가 차지하는 3D 공간의 경계 상자
        protected BVH _localBVH;               // 섹터 내 객체들의 공간 분할 트리
        protected bool _isActive;              // 섹터의 현재 활성화 상태
        protected bool _isVisible;             // 섹터의 현재 가시성 상태

        protected List<Entity> _entities;      // 섹터에 완전히 포함된 엔티티들의 목록
        protected HashSet<Entity> _borderEntities;  // 섹터 경계에 걸쳐있는 엔티티들의 집합

        // 속성
        public Vertex2i GridPosition => _gridPosition;
        public AABB Bounds => _bounds;
        public bool IsActive => _isActive;
        public bool IsVisible => _isVisible;
        public BVH LocalBVH => _localBVH;

        /// <summary>
        /// TerrainSector의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="gridPosition">섹터의 그리드 좌표</param>
        /// <param name="bounds">섹터의 경계 영역</param>
        public TerrainSector(Vertex2i gridPosition, AABB bounds)
        {
            _gridPosition = gridPosition;
            _bounds = bounds;
            _localBVH = new BVH();
            _entities = new List<Entity>();
            _borderEntities = new HashSet<Entity>();
        }


        /// <summary>
        /// 섹터를 활성화 상태로 전환합니다.
        /// 리소스 로딩 및 초기화 작업을 수행합니다.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// 섹터를 비활성화 상태로 전환합니다.
        /// 리소스 해제 및 정리 작업을 수행합니다.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// 섹터의 상태를 업데이트합니다.
        /// 엔티티 업데이트 및 카메라 기반 가시성 검사를 수행합니다.
        /// </summary>
        /// <param name="camera">현재 활성화된 카메라</param>
        public abstract void Update(Camera camera);

        /// <summary>
        /// 섹터에 새로운 엔티티를 추가합니다.
        /// 엔티티는 섹터의 BVH에 등록되며 업데이트 대상이 됩니다.
        /// </summary>
        /// <param name="entity">추가할 엔티티</param>
        public abstract void AddEntity(Entity entity);

        /// <summary>
        /// 섹터에서 엔티티를 제거합니다.
        /// 엔티티는 섹터의 BVH에서 제거되며 더 이상 업데이트되지 않습니다.
        /// </summary>
        /// <param name="entity">제거할 엔티티</param>
        public abstract void RemoveEntity(Entity entity);

        /// <summary>
        /// 섹터 경계에 걸친 엔티티를 추가합니다.
        /// 이 엔티티들은 인접 섹터와의 동기화가 필요할 수 있습니다.
        /// </summary>
        /// <param name="entity">추가할 경계 엔티티</param>
        public abstract void AddBorderEntity(Entity entity);

        /// <summary>
        /// 섹터 경계에서 엔티티를 제거합니다.
        /// 인접 섹터와의 동기화 작업도 함께 수행됩니다.
        /// </summary>
        /// <param name="entity">제거할 경계 엔티티</param>
        public abstract void RemoveBorderEntity(Entity entity);

        /// <summary>
        /// 카메라 기준으로 섹터의 가시성을 업데이트합니다.
        /// 가시성 상태에 따라 렌더링 최적화가 수행됩니다.
        /// </summary>
        /// <param name="camera">현재 활성화된 카메라</param>
        public abstract void UpdateVisibility(Camera camera);

        /// <summary>
        /// 섹터가 카메라의 뷰 프러스텀 내에 있는지 확인합니다.
        /// AABB와 뷰 프러스텀 간의 교차 테스트를 수행합니다.
        /// </summary>
        /// <param name="camera">현재 활성화된 카메라</param>
        /// <returns>프러스텀 내에 있으면 true, 그렇지 않으면 false</returns>
        public abstract bool IsInFrustum(Camera camera);
    }
}
