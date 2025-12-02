using Common;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Occlusion
{
    /// <summary>
    /// 확장 AABB 방식의 쿼드트리 (경계 객체 없음, GPU 인스턴싱 최적화)
    /// </summary>
    public class QuadTreeEx
    {
        // ----------------------------------------------------------------------
        // 멤버 변수
        // ----------------------------------------------------------------------

        // 트리 설정
        private QuadNodeEx _root;
        private int _maxDepth;
        private int _maxObjectsPerNode;
        private float _maxObjectSize;  // 객체 최대 크기 (확장 여유)

        // GPU 전송용 데이터
        private List<QuadNodeEx> _leafNodes;      // 리프 노드 리스트

        // Frustum Culling 결과 재사용 (GC-Free)
        private uint[] _visibleLeafIDsArray;      // 미리 할당된 배열
        private int _visibleLeafCount = 0;        // 실제 사용 개수
        private Queue<QuadNodeEx> _nodeQueue;     // 재사용 큐
        private QuadNodeEx _cnode;                // 재사용 순회 노드

        // 통계
        private int _testedNodeCount = 0;
        private int _rejectedNodeCount = 0;
        private int _totalObjects = 0;
        private int _totalNodes = 0;

        // ----------------------------------------------------------------------
        // 속성
        // ----------------------------------------------------------------------

        public QuadNodeEx Root => _root;
        public int TotalObjects => _totalObjects;
        public int TotalNodes => _totalNodes;
        public IReadOnlyList<QuadNodeEx> LeafNodes => _leafNodes;       // 리프 노드 리스트 (읽기 전용)
        public int TestedNodeCount => _testedNodeCount;
        public int RejectedNodeCount => _rejectedNodeCount;
        public int VisibleLeafCount => _visibleLeafCount;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="worldBounds">월드 전체 영역</param>
        /// <param name="maxObjectSize">객체 최대 반지름 (확장 여유)</param>
        /// <param name="maxDepth">최대 깊이</param>
        /// <param name="maxObjectsPerNode">노드당 최대 객체 수</param>
        public QuadTreeEx(AABB3f worldBounds, float maxObjectSize,
                                int maxDepth = 8, int maxObjectsPerNode = 64)
        {
            _maxObjectSize = maxObjectSize;
            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;

            // 루트 노드 생성
            AABB3f expandedBounds = ExpandAABB(worldBounds, maxObjectSize);
            _root = new QuadNodeEx(worldBounds, expandedBounds, 0);
            _totalNodes = 1;

            // GC-Free를 위한 배열 미리 할당
            _visibleLeafIDsArray = new uint[10000];  // 최대 용량
            _nodeQueue = new Queue<QuadNodeEx>(1000);
        }

        // ----------------------------------------------------------------------
        // 쿼드트리 구축
        // ----------------------------------------------------------------------

        /// <summary>
        /// 주어진 노드의 확장 AABB를 생성한다. (XY 평면만)
        /// </summary>
        private AABB3f ExpandAABB(AABB3f aabb, float margin)
        {
            return new AABB3f(
                new Vertex3f(
                    aabb.Min.x - margin,
                    aabb.Min.y - margin,
                    aabb.Min.z  // Z는 그대로
                ),
                new Vertex3f(
                    aabb.Max.x + margin,
                    aabb.Max.y + margin,
                    aabb.Max.z // Z는 그대로
                )
            );
        }

        /// <summary>
        /// 객체 삽입
        /// </summary>
        public void Insert(AABB3f aabb, int objectID)
        {
            WorldObject obj = new WorldObject(aabb, objectID);
            QuadNodeEx currentNode = _root;

            while (true)
            {
                // 분할 검토
                if (currentNode.IsLeaf && currentNode.Depth < _maxDepth)
                {
                    if (currentNode.Objects.Count >= _maxObjectsPerNode)
                    {
                        Subdivide(currentNode);
                    }
                }

                if (currentNode.IsLeaf)
                {
                    // 리프 노드에 추가
                    currentNode.Objects.Add(obj);

                    // Z 범위 업데이트
                    currentNode.AABB.Max.z = Math.Max(currentNode.AABB.Max.z, obj.AABB.Max.z);
                    currentNode.AABB.Min.z = Math.Min(currentNode.AABB.Min.z, obj.AABB.Min.z);
                    currentNode.ExpandedAABB.Max.z = currentNode.AABB.Max.z;
                    currentNode.ExpandedAABB.Min.z = currentNode.AABB.Min.z;

                    _totalObjects++;

                    return;  // 삽입 완료
                }
                else
                {
                    // 내부 노드: 확장 AABB와 교차하는 첫 번째 자식 찾기
                    bool found = false;
                    for (int i = 0; i < 4; i++)
                    {
                        if (IntersectsXY(obj.AABB, currentNode.Children[i].ExpandedAABB))
                        {
                            currentNode = currentNode.Children[i];  // 자식으로 이동
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Fallback (이론상 발생 안 함)
                        Console.WriteLine($"[Warning] Object {objectID} doesn't fit in any child at depth {currentNode.Depth}");
                        currentNode.Objects.Add(obj);
                        _totalObjects++;
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// XY 평면 교차 테스트
        /// </summary>
        private bool IntersectsXY(AABB3f a, AABB3f b)
        {
            return !(a.Max.x < b.Min.x || a.Min.x > b.Max.x ||
                     a.Max.y < b.Min.y || a.Min.y > b.Max.y);
        }

        /// <summary>
        /// 현재 노드를 4분할한다.
        /// </summary>
        private void Subdivide(QuadNodeEx node)
        {
            Vertex3f min = node.AABB.Min;
            Vertex3f max = node.AABB.Max;
            Vertex3f center = node.AABB.Center;

            node.Children = new QuadNodeEx[4];

            // 4개 자식 생성
            for (int i = 0; i < 4; i++)
            {
                AABB3f childAABB = CalculateChildAABB(i, min, max, center);
                AABB3f expandedAABB = ExpandAABB(childAABB, _maxObjectSize);
                node.Children[i] = new QuadNodeEx(childAABB, expandedAABB, node.Depth + 1);
            }

            _totalNodes += 4;

            // 기존 객체 재분배 (반복문 방식)
            List<WorldObject> objectsToRedistribute = new List<WorldObject>(node.Objects);
            node.Objects.Clear();

            foreach (var obj in objectsToRedistribute)
            {
                // 확장 AABB와 교차하는 첫 번째 자식 찾기
                for (int i = 0; i < 4; i++)
                {
                    if (IntersectsXY(obj.AABB, node.Children[i].ExpandedAABB))
                    {
                        node.Children[i].Objects.Add(obj);

                        // Z 범위 업데이트
                        node.Children[i].AABB.Max.z = Math.Max(node.Children[i].AABB.Max.z, obj.AABB.Max.z);
                        node.Children[i].AABB.Min.z = Math.Min(node.Children[i].AABB.Min.z, obj.AABB.Min.z);
                        node.Children[i].ExpandedAABB.Max.z = node.Children[i].AABB.Max.z;
                        node.Children[i].ExpandedAABB.Min.z = node.Children[i].AABB.Min.z;

                        break;  // 첫 번째 자식에만 저장
                    }
                }
            }
        }

        /// <summary>
        /// 자식 AABB 계산
        /// </summary>
        private AABB3f CalculateChildAABB(int quadrant, Vertex3f min, Vertex3f max, Vertex3f center)
        {
            switch (quadrant)
            {
                case 0: // SW (남서)
                    return new AABB3f(
                        new Vertex3f(min.x, min.y, min.z),
                        new Vertex3f(center.x, center.y, max.z)
                    );

                case 1: // SE (남동)
                    return new AABB3f(
                        new Vertex3f(center.x, min.y, min.z),
                        new Vertex3f(max.x, center.y, max.z)
                    );

                case 2: // NW (북서)
                    return new AABB3f(
                        new Vertex3f(min.x, center.y, min.z),
                        new Vertex3f(center.x, max.y, max.z)
                    );

                case 3: // NE (북동)
                    return new AABB3f(
                        new Vertex3f(center.x, center.y, min.z),
                        new Vertex3f(max.x, max.y, max.z)
                    );

                default:
                    throw new ArgumentException($"Invalid quadrant: {quadrant}");
            }
        }


        // ----------------------------------------------------------------------
        // GPU 전송
        // ----------------------------------------------------------------------

        /// <summary>
        /// 리프 노드 수집 (재귀)
        /// </summary>
        private void CollectLeafNodes(QuadNodeEx node, List<QuadNodeEx> leafs)
        {
            if (node.IsLeaf)
            {
                leafs.Add(node);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    CollectLeafNodes(node.Children[i], leafs);
                }
            }
        }

        /// <summary>
        /// 쿼드트리 구축 완료 후 GPU 전송 준비
        /// </summary>
        public void FinalizeForGPU()
        {
            // 1. 모든 리프 노드 수집
            _leafNodes = new List<QuadNodeEx>();
            CollectLeafNodes(_root, _leafNodes);

            Console.WriteLine($"리프 노드 수집 완료: {_leafNodes.Count}개");

            // 2. 각 리프 노드에 고유 ID 할당
            for (int i = 0; i < _leafNodes.Count; i++)
            {
                _leafNodes[i].LeafID = i;
            }

            // 3. 배열 크기 조정 (정확한 크기로)
            if (_visibleLeafIDsArray.Length < _leafNodes.Count)
            {
                _visibleLeafIDsArray = new uint[_leafNodes.Count];  // 처음만 재할당한다.
                Console.WriteLine($"가시 리프 배열 재할당: {_leafNodes.Count}개");
            }
        }

        /// <summary>
        /// GPU 업로드용 인스턴스 배열 생성 (리프 순서로 정렬)
        /// Transform은 외부에서 제공받음!
        /// </summary>
        public int[] CreateInstanceIDArray()
        {
            if (_leafNodes == null)
            {
                throw new InvalidOperationException("FinalizeForGPU()를 먼저 호출하세요!");
            }

            int[] instanceIDs = new int[_totalObjects];
            int index = 0;

            // 리프 노드 순서대로 인스턴스 ID 추가
            for (int i = 0; i < _leafNodes.Count; i++)
            {
                QuadNodeEx leaf = _leafNodes[i];

                foreach (var obj in leaf.Objects)
                {
                    instanceIDs[index] = obj.ObjectID;  // ID만 저장!
                    index++;
                }
            }

            Console.WriteLine($"GPU 인스턴스 ID 배열 생성 완료: {instanceIDs.Length}개");
            return instanceIDs;
        }

        /// <summary>
        /// GPU 업로드용 리프 노드 배열 생성
        /// ObjectID를 그대로 사용 (정렬하지 않음!)
        /// </summary>
        public GPULeafNode[] CreateGPULeafNodeArray()
        {
            if (_leafNodes == null)
            {
                throw new InvalidOperationException("FinalizeForGPU()를 먼저 호출하세요!");
            }

            GPULeafNode[] gpuNodes = new GPULeafNode[_leafNodes.Count];

            for (int i = 0; i < _leafNodes.Count; i++)
            {
                QuadNodeEx leaf = _leafNodes[i];

                // ⚠️ 중요: StartIndex는 이 리프의 첫 번째 ObjectID가 아니라
                //           GPU SSBO에서의 시작 위치!
                //           하지만 우리는 ObjectID를 그대로 사용할 것이므로
                //           Objects 리스트의 ObjectID들을 별도로 전달해야 함!

                // 일단은 개수만 저장 (StartIndex는 나중에 수정)
                gpuNodes[i] = new GPULeafNode(
                    0,  // 임시 (나중에 수정)
                    (uint)leaf.Objects.Count
                );
            }

            Console.WriteLine($"GPU 리프 노드 배열 생성 완료: {gpuNodes.Length}개");
            return gpuNodes;
        }

        /// <summary>
        /// 리프 노드별 ObjectID 배열 생성 (새로 추가!)
        /// </summary>
        public uint[][] CreateLeafObjectIDArrays()
        {
            if (_leafNodes == null)
            {
                throw new InvalidOperationException("FinalizeForGPU()를 먼저 호출하세요!");
            }

            uint[][] leafObjectIDs = new uint[_leafNodes.Count][];

            for (int i = 0; i < _leafNodes.Count; i++)
            {
                QuadNodeEx leaf = _leafNodes[i];
                leafObjectIDs[i] = new uint[leaf.Objects.Count];

                for (int j = 0; j < leaf.Objects.Count; j++)
                {
                    leafObjectIDs[i][j] = (uint)leaf.Objects[j].ObjectID;
                }
            }

            Console.WriteLine($"리프 ObjectID 배열 생성 완료: {leafObjectIDs.Length}개 리프");
            return leafObjectIDs;
        }


        // ----------------------------------------------------------------------
        // 가시성 테스트
        // ----------------------------------------------------------------------

        /// <summary>
        /// Frustum Culling 수행 (GC-Free)
        /// </summary>
        /// <param name="frustumPlanes">6개의 Frustum 평면</param>
        /// <param name="outVisibleCount">가시 리프 개수 (출력)</param>
        /// <returns>가시 리프 노드 ID 배열 (재사용됨!)</returns>
        public uint[] CullByFrustum(Plane[] frustumPlanes, ref int outVisibleCount)
        {
            // 통계 초기화
            _testedNodeCount = 0;
            _rejectedNodeCount = 0;
            _visibleLeafCount = 0;

            // 큐 초기화 (Clear는 내부 배열 재사용)
            _nodeQueue.Clear();

            // 루트부터 시작
            _nodeQueue.Enqueue(_root);

            while (_nodeQueue.Count > 0)
            {
                _cnode = _nodeQueue.Dequeue();
                _testedNodeCount++;

                // Frustum 테스트 (ExpandedAABB 사용!)
                if (!IsVisible(_cnode.ExpandedAABB, frustumPlanes))
                {
                    _rejectedNodeCount++;
                    _cnode.IsVisible = false;
                    continue;  // 서브트리 전체 제거!
                }

                // 노드가 보이면
                if (_cnode.IsLeaf)
                {
                    // 가시 리프 노드 추가 (배열에 직접 쓰기)
                    _cnode.IsVisible = true;
                    _visibleLeafIDsArray[_visibleLeafCount] = (uint)_cnode.LeafID;
                    _visibleLeafCount++;
                }
                else
                {
                    // 자식 노드 탐색
                    for (int i = 0; i < 4; i++)
                    {
                        _nodeQueue.Enqueue(_cnode.Children[i]);
                    }
                }
            }

            outVisibleCount = _visibleLeafCount;
            return _visibleLeafIDsArray;  // 같은 배열 재사용!
        }


        /// <summary>
        /// AABB가 Frustum과 교차하는지 테스트 (Zero-Allocation 최적화)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVisible(AABB3f aabb, Plane[] planes)
        {
            // AABB의 중심과 반경 계산 (스택 변수만 사용)
            float centerX = (aabb.Min.x + aabb.Max.x) * 0.5f;
            float centerY = (aabb.Min.y + aabb.Max.y) * 0.5f;
            float centerZ = (aabb.Min.z + aabb.Max.z) * 0.5f;

            float extentX = (aabb.Max.x - aabb.Min.x) * 0.5f;
            float extentY = (aabb.Max.y - aabb.Min.y) * 0.5f;
            float extentZ = (aabb.Max.z - aabb.Min.z) * 0.5f;

            // 평면 개수 (배열 길이 캐싱)
            int planeCount = planes.Length;

            for (int i = 0; i < planeCount; i++)
            {
                // 평면 참조 (구조체 복사 최소화)
                ref Plane plane = ref planes[i];

                // 법선 캐싱
                float nx = plane.Normal.x;
                float ny = plane.Normal.y;
                float nz = plane.Normal.z;

                // 중심점에서 평면까지의 거리
                float dist = nx * centerX + ny * centerY + nz * centerZ + plane.W;

                // AABB 반경 (절댓값 연산 인라인)
                float absNx = nx < 0 ? -nx : nx;
                float absNy = ny < 0 ? -ny : ny;
                float absNz = nz < 0 ? -nz : nz;

                float radius = extentX * absNx + extentY * absNy + extentZ * absNz;

                // Early exit (가장 빈번한 경우 최적화)
                if (dist + radius < 0f)
                {
                    return false;
                }
            }

            return true;
        }

        // ----------------------------------------------------------------------
        // 유틸리티
        // ----------------------------------------------------------------------

        /// <summary>
        /// Frustum Culling 통계 출력
        /// </summary>
        public void PrintCullingStatistics()
        {
            Console.WriteLine("====== Frustum Culling 통계 ======");
            Console.WriteLine($"테스트 노드 수:    {_testedNodeCount:N0}");
            Console.WriteLine($"제거 노드 수:      {_rejectedNodeCount:N0}");
            Console.WriteLine($"가시 리프 수:      {_visibleLeafCount:N0}");
            Console.WriteLine($"제거 비율:         {(float)_rejectedNodeCount / _testedNodeCount * 100:F1}%");
            Console.WriteLine($"전송 데이터:       {_visibleLeafCount * 4} bytes");
            Console.WriteLine("===================================");
        }

        /// <summary>
        /// 통계 출력
        /// </summary>
        public void PrintStatistics()
        {
            int leafCount = 0;
            int maxDepth = 0;
            int totalObjectsInLeaves = 0;
            int minObjectsPerLeaf = int.MaxValue;
            int maxObjectsPerLeaf = 0;

            CountStatistics(_root, ref leafCount, ref maxDepth,
                          ref totalObjectsInLeaves, ref minObjectsPerLeaf, ref maxObjectsPerLeaf);

            float avgObjectsPerLeaf = leafCount > 0 ? (float)totalObjectsInLeaves / leafCount : 0;

            Console.WriteLine("====== InstanceQuadTree 통계 ======");
            Console.WriteLine($"총 객체 수:        {_totalObjects:N0}");
            Console.WriteLine($"총 노드 수:        {_totalNodes:N0}");
            Console.WriteLine($"리프 노드 수:      {leafCount:N0}");
            Console.WriteLine($"최대 깊이:         {maxDepth}");
            Console.WriteLine($"객체 최대 크기:    {_maxObjectSize:F1}m");
            Console.WriteLine($"평균 객체/리프:    {avgObjectsPerLeaf:F1}");
            Console.WriteLine($"최소 객체/리프:    {minObjectsPerLeaf}");
            Console.WriteLine($"최대 객체/리프:    {maxObjectsPerLeaf}");
            Console.WriteLine($"메모리 절약:       경계 객체 없음 (100% 효율)");
            Console.WriteLine("=====================================");
        }

        /// <summary>
        /// 통계 수집
        /// </summary>
        private void CountStatistics(QuadNodeEx node, ref int leafCount, ref int maxDepth,
                                     ref int totalObjects, ref int minObjects, ref int maxObjects)
        {
            if (node.IsLeaf)
            {
                leafCount++;
                totalObjects += node.Objects.Count;

                if (node.Objects.Count > 0)
                {
                    minObjects = Math.Min(minObjects, node.Objects.Count);
                    maxObjects = Math.Max(maxObjects, node.Objects.Count);
                }
            }

            maxDepth = Math.Max(maxDepth, node.Depth);

            if (!node.IsLeaf)
            {
                for (int i = 0; i < 4; i++)
                {
                    CountStatistics(node.Children[i], ref leafCount, ref maxDepth,
                                  ref totalObjects, ref minObjects, ref maxObjects);
                }
            }
        }

    }

    /// <summary>
    /// GPU로 전송할 리프 노드 정보
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GPULeafNode
    {
        public uint StartIndex;      // 인스턴스 배열에서의 시작 위치
        public uint InstanceCount;   // 이 노드의 인스턴스 개수

        public GPULeafNode(uint startIndex, uint instanceCount)
        {
            StartIndex = startIndex;
            InstanceCount = instanceCount;
        }
    }

    /// <summary>
    /// GPU로 전송할 인스턴스 데이터
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GPUInstanceData
    {
        public Matrix4x4f Transform;    // 64 bytes
        public Vertex4f AABBMin;        // 16 bytes
        public Vertex4f AABBMax;        // 16 bytes
        // 총 96 bytes

        public GPUInstanceData(Matrix4x4f transform, AABB3f aabb)
        {
            Transform = transform;
            AABBMin = new Vertex4f(aabb.Min.x, aabb.Min.y, aabb.Min.z, 0);
            AABBMax = new Vertex4f(aabb.Max.x, aabb.Max.y, aabb.Max.z, 0);
        }
    }

    /// <summary>
    /// 확장 방식 쿼드트리 노드
    /// </summary>
    public class QuadNodeEx
    {
        public AABB3f AABB;                 // 원래 경계 (분할용)
        public AABB3f ExpandedAABB;         // 확장 경계 (컬링용)
        public int Depth;                   // 노드 깊이
        public List<WorldObject> Objects;   // 
        public QuadNodeEx[] Children;       // null이면 리프

        // GPU 전송용 추가 필드
        public int LeafID;              // 리프 노드 고유 ID (-1이면 리프 아님)
        public bool IsVisible;          // Frustum Culling 결과

        public bool IsLeaf => Children == null;

        public QuadNodeEx(AABB3f aabb, AABB3f expandedAABB, int depth)
        {
            AABB = aabb;
            ExpandedAABB = expandedAABB;
            Depth = depth;
            Objects = new List<WorldObject>();
            Children = null;
            LeafID = -1;
            IsVisible = false;
        }
    }
}