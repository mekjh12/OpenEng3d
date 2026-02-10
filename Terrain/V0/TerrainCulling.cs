using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 지형 컬링 시스템의 기본 구현체입니다.
    /// </summary>
    public class TerrainCulling : ITerrainCulling
    {
        private BVH _bvh;                                   // 청크의 이진계층트리구조
        private List<Entity> _frustumedEntities;            // 가시성 테스트를 통과한 물체 리스트
        private List<AABB> _frustumedChunkAABB;             // 가시성 테스트를 통과한 청크 AABB 리스트
        private Dictionary<string, Chunk> _dicChunkList;    // 청크의 캐싱데이터

        // 성능테스트용
        private int _travNodeCountViewfrustum = 0;          // 뷰프러스텀 탐색노드 갯수
        private int _travNodeCountHzm = 0;                  // 계층적깊이맵 탐색노드 갯수

        /// <summary> 가시성 테스트를 통과한 엔티티 목록 반환 </summary>
        public List<Entity> FrustumedEntities => _frustumedEntities;
        /// <summary> 바운딩 볼륨 계층 구조 반환 </summary>
        public BVH BoundingVolumeHierarchy => _bvh;
        public Dictionary<string, Chunk> DicChunkList => _dicChunkList;
        public List<AABB> FrustumedChunkAABB => _frustumedChunkAABB;
        public int TravNodeCountViewfrustum => _travNodeCountViewfrustum;
        public int TravNodeCountHzm => _travNodeCountHzm;
        List<Chunk> ITerrainCulling.FrustumedChunk => throw new System.NotImplementedException();

        /// <summary>
        /// TerrainCulling 클래스의 생성자입니다.
        /// 컬링에 필요한 데이터 구조를 초기화합니다.
        /// </summary>
        public TerrainCulling()
        {
            _bvh = new BVH();
            _frustumedEntities = new List<Entity>();
            _frustumedChunkAABB = new List<AABB>();
            _dicChunkList = new Dictionary<string, Chunk>();
        }

        public void Clear()
        {
            _bvh.Clear();
            _frustumedEntities?.Clear();
            _frustumedChunkAABB?.Clear();
            _dicChunkList?.Clear();
        }

        public void AddAABB(AABB aabb)
        {
            _bvh.InsertLeaf(aabb);
        }

        public int CullingFastLargeAABBTestByHierarchicalZBuffer(HierarchicalZBuffer zbuffer, Matrix4x4f vp, Matrix4x4f view)
        {
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            int travCount = 0;

            while (queue.Count > 0)
            {
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                //if (zbuffer.TestVisibility(vp, view, cnode.AABB))
                {
                    travCount++;
                    if (cnode.AABB.SphereRadius > 500.0f)
                    {
                        if (cnode.Left) queue.Enqueue(cnode.Child1);
                        if (cnode.Right) queue.Enqueue(cnode.Child2);
                    }
                }
                //else
                {
                    cnode.UnLinkBackCopy();
                }
            }

            return travCount;
        }

        /// <summary>
        /// 시야절두체(View Frustum)를 사용하여 객체를 컬링합니다.
        /// </summary>
        public void CullingTestByViewFrustum(Polyhedron viewPolyhedron)
        {
            _travNodeCountViewfrustum = 0;

            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (currentNode.AABB.Visible(viewPolyhedron.Planes))
                {
                    _travNodeCountViewfrustum++;
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
                else
                {
                    currentNode.UnLinkBackCopy();
                }
            }
        }

        /// <summary>
        /// 계층적 Z-버퍼를 사용하여 객체를 컬링합니다.
        /// </summary>
        public void CullingTestByHierarchicalZBuffer(HierarchicalZBuffer zbuffer, Matrix4x4f vp, Matrix4x4f view)
        {
            _travNodeCountHzm = 0;

            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            while (queue.Count > 0)
            {
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                //if (zbuffer.TestVisibility(vp, view, cnode.AABB))
                {
                    _travNodeCountHzm++;

                    if (cnode.Left) queue.Enqueue(cnode.Child1);
                    if (cnode.Right) queue.Enqueue(cnode.Child2);
                }
                //else
                {
                    cnode.UnLinkBackCopy();
                }
            }
        }

        /// <summary>
        /// 컬링 시스템의 백 링크를 초기화합니다.
        /// </summary>
        /// <remarks>속도는 매우 빠름(0.000000초)</remarks>
        public void ClearBackLink(bool visible = true)
        {
            _bvh.ClearBackCopy(visible);
        }

        /// <summary>
        /// 컬링된 엔티티 목록을 갱신합니다.
        /// </summary>
        public void PrepareEntities()
        {
            _frustumedEntities?.Clear();
            _frustumedChunkAABB?.Clear();
            _frustumedChunkAABB = _bvh.ExtractAABB();

            _frustumedEntities = _bvh.ExtractEntity();
        }

        /// <summary>
        /// 지정된 이름을 가진 패치 엔티티를 반환합니다.
        /// </summary>
        /// <param name="name">검색할 패치의 이름</param>
        /// <returns>찾은 패치 엔티티</returns>
        public Chunk GetChunk(string name)
        {
            if (_dicChunkList.ContainsKey(name))
            {
                return _dicChunkList[name];
            }
            else
            {
                return null;
            }
        }
    }
}
