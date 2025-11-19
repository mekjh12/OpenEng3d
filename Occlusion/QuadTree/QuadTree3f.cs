using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;

namespace Occlusion
{
    /*
        Z-up (OpenGL 전통, CAD: 3ds Max, Blender)
        ┌─────┬─────┐  ↑ Z (높이)
        │ NW  │ NE  │  
        ├─────┼─────┤  
        │ SW  │ SE  │  
        └─────┴─────┘→ X
        Y축으로 분할 (XY 평면)
    */
    public class QuadTree3f
    {
        // 트리 설정
        private QuadNode _root;
        private int _maxDepth;              // 최대 깊이
        private int _maxObjectsPerNode;     // 노드당 최대 객체 수

        // 객체 목록
        private List<TreeObject> _treeObjects;          // 전체 객체 목록
        private AABB3f[] _aabbList;                     // AABB 재사용 버퍼
        private HashSet<int> _cullingCache;             // 컬링 중복 체크용 캐시 
        private List<TreeObject> _visibleObjects;       // 컬링 결과 재사용 버퍼
        private List<TreeObject> _visibleObjsLod0;      // LOD0 전용 재사용 버퍼
        private List<TreeObject> _visibleObjsLod1;      // LOD1 전용 재사용 버퍼
        private List<TreeObject> _visibleObjsLod2;      // LOD2 전용 재사용 버퍼
        private int _visbleObjectIndex = 0;             // 재사용 인덱스
        private AABB3f[] _tempAABBs;                    // 임시 AABB 버퍼
        private int _tempAABBIndex = 0;

        // 통계
        private int _totalObjects = 0;
        private int _totalNodes = 0;
        private int _boundaryObjectCount = 0;

        // 속성
        public QuadNode Root => _root;
        public int TotalObjects => _totalObjects;
        public int TotalNodes => _totalNodes;
        public int BoundaryObjectCount => _boundaryObjectCount;
        public ref AABB3f[] AABBArray => ref _aabbList;
        public int VisibleObjectCount => _visibleObjects.Count;
        public ref AABB3f[] NodeAABBs => ref _tempAABBs;
        public int TempAABBCount => _tempAABBIndex;

        /// <summary>
        /// 생성자
        /// </summary>
        public QuadTree3f(AABB3f worldBounds, int maxDepth = 8, int maxObjectsPerNode = 16, int maxCapacity = 100000)
        {
            // 루트 노드 생성
            _root = new QuadNode(worldBounds, 0);
            _root.SetVisible(true);

            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            _totalNodes = 1;

            // 객체 목록 초기화
            _treeObjects = new List<TreeObject>();
            _aabbList = new AABB3f[maxCapacity];
            _cullingCache = new HashSet<int>();
            _visibleObjects = new List<TreeObject>(maxCapacity);
            _visibleObjsLod0 = new List<TreeObject>();
            _visibleObjsLod1 = new List<TreeObject>();
            _visibleObjsLod2 = new List<TreeObject>();

            _tempAABBs = new AABB3f[maxCapacity];
        }

        /// <summary>
        /// 객체 삽입
        /// </summary>
        public void Insert(AABB3f aabb, int objectID, Vertex3f position)
        {
            TreeObject obj = new TreeObject(aabb, objectID, position);
            InsertRecursive(_root, obj);
            _totalObjects++;
        }

        /// <summary>
        /// 재귀적 삽입
        /// </summary>
        private void InsertRecursive(QuadNode node, TreeObject obj)
        {
            // 리프 노드이고 최대 깊이가 아니면 분할 검토
            if (node.IsLeaf && node.Depth < _maxDepth)
            {
                // 객체 수가 임계값 초과 시 분할
                if (node.Objects.Count >= _maxObjectsPerNode)
                {
                    Subdivide(node);
                }
            }

            // 위 단계로 인하여 노드 객체수 임계값 도달하지 않음
            if (node.IsLeaf)
            {
                // 리프 노드
                node.Objects.Add(obj);
            }
            else
            {
                // 내부 노드
                
                // 면 교차하는 모든 자식에 삽입
                int intersectCount = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (Intersects(obj.AABB, node.Children[i].AABB))
                    {
                        InsertRecursive(node.Children[i], obj);
                        intersectCount++;
                    }
                }

                // 여러 자식과 교차하면 경계 객체로 분류
                if (intersectCount > 1)
                {
                    node.BoundaryObjects.Add(obj);
                    _boundaryObjectCount++;
                }
            }
        }

        /// <summary>
        /// 노드 4분할 (Z-up 좌표계: XY 평면 분할)
        /// </summary>
        private void Subdivide(QuadNode node)
        {
            Vertex3f min = node.AABB.Min;
            Vertex3f max = node.AABB.Max;
            Vertex3f center = node.AABB.Center;

            // 자식 노드 링크 플래그 설정
            node.Children = new QuadNode[4];
            node.SetVisible(true);

            // XY 평면으로 4분할 (Z는 그대로)

            // [0] 남서 (SW) - X-, Y-
            node.Children[0] = new QuadNode(
                new AABB3f(
                    new Vertex3f(min.x, min.y, min.z),
                    new Vertex3f(center.x, center.y, max.z)
                ),
                node.Depth + 1
            );

            // [1] 남동 (SE) - X+, Y-
            node.Children[1] = new QuadNode(
                new AABB3f(
                    new Vertex3f(center.x, min.y, min.z),
                    new Vertex3f(max.x, center.y, max.z)
                ),
                node.Depth + 1
            );

            // [2] 북서 (NW) - X-, Y+
            node.Children[2] = new QuadNode(
                new AABB3f(
                    new Vertex3f(min.x, center.y, min.z),
                    new Vertex3f(center.x, max.y, max.z)
                ),
                node.Depth + 1
            );

            // [3] 북동 (NE) - X+, Y+
            node.Children[3] = new QuadNode(
                new AABB3f(
                    new Vertex3f(center.x, center.y, min.z),
                    new Vertex3f(max.x, max.y, max.z)
                ),
                node.Depth + 1
            );

            _totalNodes += 4;

            // 기존 객체들을 자식으로 재분배
            List<TreeObject> objectsToRedistribute = new List<TreeObject>(node.Objects);
            node.Objects.Clear();

            foreach (var obj in objectsToRedistribute)
            {
                InsertRecursive(node, obj);
            }
        }

        /// <summary>
        /// Intersects 체크 (Z-up: XY만 체크)
        /// </summary>
        private bool Intersects(AABB3f a, AABB3f b)
        {
            return !(a.Max.x < b.Min.x || a.Min.x > b.Max.x ||
                    a.Max.y < b.Min.y || a.Min.y > b.Max.y);
            // Z축(높이)는 체크 안 함
        }

        /// <summary>
        /// 통계 출력
        /// </summary>
        public void PrintStatistics()
        {
            int leafCount = 0;
            int maxDepth = 0;
            int totalObjectsInNodes = 0;
            int totalBoundaryObjectsInNodes = 0;

            CountStatistics(_root, ref leafCount, ref maxDepth,
                          ref totalObjectsInNodes, ref totalBoundaryObjectsInNodes);

            Console.WriteLine("====== QuadTree 통계 ======");
            Console.WriteLine($"총 객체 수: {_totalObjects}");
            Console.WriteLine($"총 노드 수: {_totalNodes}");
            Console.WriteLine($"리프 노드 수: {leafCount}");
            Console.WriteLine($"최대 깊이: {maxDepth}");
            Console.WriteLine($"경계 객체 수: {_boundaryObjectCount} ({(float)_boundaryObjectCount / _totalObjects * 100:F1}%)");
            Console.WriteLine($"평균 객체/노드: {(float)totalObjectsInNodes / leafCount:F1}");
            Console.WriteLine($"평균 경계객체/노드: {(float)totalBoundaryObjectsInNodes / _totalNodes:F1}");
            Console.WriteLine("===========================");
        }

        /// <summary>
        /// Hi-Z Buffer Occlusion Culling
        /// </summary>
        public void CullingTestByHiZBuffer(Matrix4x4f vp, Matrix4x4f view,
                                          HierarchyZBuffer hiZBuffer,
                                          ref List<TreeObject> visibleObjects)
        {
            visibleObjects.Clear();
            HiZCullingRecursive(_root, vp, view, hiZBuffer, visibleObjects);
        }

        private void HiZCullingRecursive(QuadNode node, Matrix4x4f vp, Matrix4x4f view,
                                        HierarchyZBuffer hiZBuffer,
                                        List<TreeObject> visibleObjects)
        {
            // 노드 전체가 가려졌는지 체크
            if (!hiZBuffer.TestVisibility(vp, view, node.AABB))
                return;

            // 경계 객체들 체크
            foreach (var obj in node.BoundaryObjects)
            {
                if (hiZBuffer.TestVisibility(vp, view, obj.AABB))
                {
                    visibleObjects.Add(obj);
                }
            }

            // 일반 객체들 체크
            foreach (var obj in node.Objects)
            {
                if (hiZBuffer.TestVisibility(vp, view, obj.AABB))
                {
                    visibleObjects.Add(obj);
                }
            }

            // 자식 노드 재귀
            if (!node.IsLeaf)
            {
                for (int i = 0; i < 4; i++)
                {
                    HiZCullingRecursive(node.Children[i], vp, view, hiZBuffer, visibleObjects);
                }
            }
        }

        public void Clear()
        {
            _treeObjects.Clear();
        }

        // 멤버 변수에 큐 추가 (재사용)
        private Queue<QuadNode> _nodeQueue = new Queue<QuadNode>();

        /// <summary>
        /// View Frustum Culling (큐 기반)
        /// </summary>
        public void CullingTestByViewFrustum(Polyhedron viewFrustum)
        {
            // 재사용 버퍼 초기화
            _cullingCache.Clear();
            _visibleObjects.Clear();
            _nodeQueue.Clear();

            _visbleObjectIndex = 0;
            _tempAABBIndex = 0;

            // 루트 노드부터 시작
            _nodeQueue.Enqueue(_root);
            while (_nodeQueue.Count > 0)
            {
                QuadNode node = _nodeQueue.Dequeue();

                // 노드가 뷰 프러스텀과 교차하지 않으면 스킵
                if (!node.AABB.Visible(viewFrustum.Planes))
                    continue;

                if (node.IsLeaf)
                {
                    _tempAABBs[_tempAABBIndex] =(node.AABB);
                    _tempAABBIndex++;
                }

                // 경계 객체들 추가 (중복 체크)
                foreach (var obj in node.BoundaryObjects)
                {
                    if (obj.AABB.Visible(viewFrustum.Planes))
                    {
                        if (_cullingCache.Add(obj.ObjectID))
                        {
                            _visibleObjects.Add(obj);
                            _aabbList[_visbleObjectIndex] = obj.AABB;
                            _visbleObjectIndex++;
                        }
                    }
                }

                // 일반 객체들 추가
                for (int i = 0; i < node.Objects.Count; i++)
                {
                    TreeObject obj = node.Objects[i];
                    if (obj.AABB.Visible(viewFrustum.Planes))
                    {
                        if (_cullingCache.Add(obj.ObjectID))
                        {
                            _visibleObjects.Add(obj);
                            _aabbList[_visbleObjectIndex] = obj.AABB;
                            _visbleObjectIndex++;
                        }
                    }
                }

                // 자식 노드들을 큐에 추가
                if (!node.IsLeaf)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _nodeQueue.Enqueue(node.Children[i]);
                    }
                }
            }

            // AABB 리스트 업데이트
            for (int i = 0; i < _visibleObjects.Count; i++)
            {
                TreeObject obj = _visibleObjects[i];
                _aabbList[i] = obj.AABB;
            }
        }

        private void CountStatistics(QuadNode node, ref int leafCount, ref int maxDepth,
                                    ref int totalObjects, ref int totalBoundary)
        {
            if (node.IsLeaf)
            {
                leafCount++;
                totalObjects += node.Objects.Count;
            }

            totalBoundary += node.BoundaryObjects.Count;
            maxDepth = Math.Max(maxDepth, node.Depth);

            if (!node.IsLeaf)
            {
                for (int i = 0; i < 4; i++)
                {
                    CountStatistics(node.Children[i], ref leafCount, ref maxDepth,
                                  ref totalObjects, ref totalBoundary);
                }
            }
        }
    }
}