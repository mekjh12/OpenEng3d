using Common;
using Common.Abstractions;
using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ZetaExt;

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
        // LOD 거리 임계값
        const int Lod0Distance = 300;
        const int Lod1Distance = 600;
        const int DistanceSquaredThreshold0 = Lod0Distance * Lod0Distance;
        const int DistanceSquaredThreshold1 = Lod1Distance * Lod1Distance;

        // 트리 설정
        private QuadNode _root;
        private int _maxDepth;              // 최대 깊이
        private int _maxObjectsPerNode;     // 노드당 최대 객체 수

        // 객체 목록
        private List<WorldObject> _treeObjects;          // 전체 객체 목록
        private HashSet<int> _cullingCache;             // 컬링 중복 체크용 캐시

        // 컬링 결과 재사용 버퍼
        private AABB3f[] _visibleObjects;               // AABB 재사용 버퍼
        private int _visibleObjectIndex = 0;            // 재사용 인덱스

        // LOD 재사용 버퍼
        private int _indexLod0 = 0;                     // LOD0 전용 인덱스
        private int _indexLod1 = 0;                     // LOD1 전용 인덱스
        private int _indexLod2 = 0;                     // LOD2 전용 인덱스
        private AABB3f[] _lod0;                         // LOD0 전용 재사용 버퍼
        private AABB3f[] _lod1;                         // LOD1 전용 재사용 버퍼
        private AABB3f[] _lod2;                         // LOD0 전용 재사용 버퍼

        // 통계
        private int _totalObjects = 0;
        private int _totalNodes = 0;
        private int _boundaryObjectCount = 0;

        // 멤버 변수에 큐 추가 (재사용)
        private Queue<QuadNode> _nodeQueue = new Queue<QuadNode>();

        // 속성
        public QuadNode Root => _root;
        public int TotalCountObjects => _totalObjects;
        public int TotalNodes => _totalNodes;
        public int BoundaryObjectCount => _boundaryObjectCount;
        public ref AABB3f[] VisibleObjects => ref _visibleObjects;
        public int VisibleObjectCount => _visibleObjectIndex;
        public ref AABB3f[] LOD0  => ref _lod0;
        public int CountLod0 { get => _indexLod0; }

        /// <summary>
        /// 생성자
        /// </summary>
        public QuadTree3f(AABB3f worldBounds, int maxDepth = 8, int maxObjectsPerNode = 16, int maxCapacity = 100000)
        {
            // 루트 노드 생성
            _root = new QuadNode(worldBounds, 0);

            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            _totalNodes = 1;

            // 객체 목록 초기화
            _treeObjects = new List<WorldObject>();
            _visibleObjects = new AABB3f[maxCapacity];
            _cullingCache = new HashSet<int>();

            _lod0 = new AABB3f[maxCapacity];
            _lod1 = new AABB3f[maxCapacity];
            _lod2 = new AABB3f[maxCapacity];
        }

        /// <summary>
        /// 객체 삽입
        /// </summary>
        public void Insert(AABB3f aabb, int objectID)
        {
            WorldObject obj = new WorldObject(aabb, objectID);
            InsertRecursive(_root, obj);
            _totalObjects++;
        }

        /// <summary>
        /// 재귀적 삽입
        /// </summary>
        private void InsertRecursive(QuadNode node, WorldObject obj)
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
                node.AABB.Max.z = Math.Max(node.AABB.Max.z, obj.AABB.Max.z);
                node.AABB.Min.z = Math.Min(node.AABB.Min.z, obj.AABB.Min.z);
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
                    node.AABB.Max.z = Math.Max(node.AABB.Max.z, obj.AABB.Max.z);
                    node.AABB.Min.z = Math.Min(node.AABB.Min.z, obj.AABB.Min.z);
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

            for (int i = 0; i < 4; i++)
            {
                node.Children[i].IsLinked = true;
            }
            _totalNodes += 4;

            // 기존 객체들을 자식으로 재분배
            List<WorldObject> objectsToRedistribute = new List<WorldObject>(node.Objects);
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
        public void CullingTestByHiZBuffer(Camera camera, Matrix4x4f vp, Matrix4x4f view,
                                          HierarchyZBuffer hiZBuffer, bool enablePickLeaf = false)
        {            
            if (enablePickLeaf)
            {
                // 재사용 버퍼 초기화
                _visibleObjectIndex = 0;

                // 재사용 버퍼 초기화
                _cullingCache.Clear();
            }

            // 루트 노드부터 시작
            _nodeQueue.Clear();
            _nodeQueue.Enqueue(_root);
            while (_nodeQueue.Count > 0)
            {
                QuadNode node = _nodeQueue.Dequeue();

                // 깊이가 낮은 노드는 화면 공간의 넓이가 크고 갯수가 적으므로 무조건 통과시킨다.
                if (node.Depth >= 3)
                {
                    // 노드가 뷰 프러스텀과 교차하지 않으면 스킵
                    float distSquared = (camera.Position - node.AABB.Center).LengthSquared();
                    
                    if (!hiZBuffer.TestVisibility(vp, view, node.AABB))
                    {
                        node.IsLinked = false;
                        continue;
                    }
                    else
                    {
                        node.IsLinked = true;
                    }

                    if (enablePickLeaf && node.IsLeaf)
                    {
                        // 경계 객체들 추가 (중복 체크)
                        foreach (var obj in node.BoundaryObjects)
                        {
                            if (hiZBuffer.TestVisibility(vp, view, obj.AABB))
                            {
                                if (_cullingCache.Add(obj.ObjectID))
                                {
                                    _visibleObjects[_visibleObjectIndex] = obj.AABB;
                                    _visibleObjectIndex++;
                                }
                            }
                        }

                        // 일반 객체들 추가
                        for (int i = 0; i < node.Objects.Count; i++)
                        {
                            WorldObject obj = node.Objects[i];
                            if (hiZBuffer.TestVisibility(vp, view, obj.AABB))
                            {
                                if (_cullingCache.Add(obj.ObjectID))
                                {
                                    _visibleObjects[_visibleObjectIndex] = obj.AABB;
                                    _visibleObjectIndex++;
                                }
                            }
                        }
                    }
                }                

                // 자식 노드들을 큐에 추가
                if (!node.IsLeaf)
                {
                    if (node.Children[0].IsLinked) _nodeQueue.Enqueue(node.Children[0]);
                    if (node.Children[1].IsLinked) _nodeQueue.Enqueue(node.Children[1]);
                    if (node.Children[2].IsLinked) _nodeQueue.Enqueue(node.Children[2]);
                    if (node.Children[3].IsLinked) _nodeQueue.Enqueue(node.Children[3]);
                }
            }
        }

        private void HiZCullingRecursive(QuadNode node, Matrix4x4f vp, Matrix4x4f view,
                                        HierarchyZBuffer hiZBuffer,
                                        List<WorldObject> visibleObjects)
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
            ClearLinked();
        }

        [Obsolete("사용하지 마세요.")]
        public void CullTestFinish(Vertex3f cameraPosition)
        {
            _indexLod0 = 0;
            _cullingCache.Clear();

            // 루트 노드부터 시작
            _nodeQueue.Clear();
            _nodeQueue.Enqueue(_root);
            while (_nodeQueue.Count > 0)
            {
                QuadNode node = _nodeQueue.Dequeue();

                if (node.IsLeaf)
                {
                    float distSquared = (cameraPosition - node.AABB.Center).LengthSquared();
                    if (distSquared < DistanceSquaredThreshold0)
                    {

                        // 경계 객체들 추가 (중복 체크)
                        foreach (var obj in node.BoundaryObjects)
                        {
                            if (_cullingCache.Add(obj.ObjectID))
                            {
                                _lod0[_indexLod0] = obj.AABB;
                                _indexLod0++;
                            }
                        }

                        // 일반 객체들 추가
                        foreach (var obj in node.Objects)
                        {
                            if (_cullingCache.Add(obj.ObjectID))
                            {
                                _lod0[_indexLod0] = obj.AABB;
                                _indexLod0++;
                            }
                        }
                    }

                }

                // 자식 노드들을 큐에 추가
                if (!node.IsLeaf)
                {
                    if (node.Children[0].IsLinked) _nodeQueue.Enqueue(node.Children[0]);
                    if (node.Children[1].IsLinked) _nodeQueue.Enqueue(node.Children[1]);
                    if (node.Children[2].IsLinked) _nodeQueue.Enqueue(node.Children[2]);
                    if (node.Children[3].IsLinked) _nodeQueue.Enqueue(node.Children[3]);
                }
            }
        }

        public void ClearLinked()
        {
            // 루트 노드부터 시작
            _nodeQueue.Clear();
            _nodeQueue.Enqueue(_root);
            while (_nodeQueue.Count > 0)
            {
                QuadNode node = _nodeQueue.Dequeue();
                node.IsLinked = true;

                // 자식 노드들을 큐에 추가
                if (!node.IsLeaf)
                {
                    if (node.Children[0].IsLinked) _nodeQueue.Enqueue(node.Children[0]);
                    if (node.Children[1].IsLinked) _nodeQueue.Enqueue(node.Children[1]);
                    if (node.Children[2].IsLinked) _nodeQueue.Enqueue(node.Children[2]);
                    if (node.Children[3].IsLinked) _nodeQueue.Enqueue(node.Children[3]);
                }
            }
        }

        private void PickUpObject(QuadNode node, Polyhedron viewFrustum, ref AABB3f[] aabbList, ref int index)
        {
            // 경계 객체들 추가 (중복 체크)
            foreach (var obj in node.BoundaryObjects)
            {
                if (obj.AABB.Visible(viewFrustum.Planes))
                {
                    if (_cullingCache.Add(obj.ObjectID))
                    {
                        aabbList[index] = obj.AABB;
                        index++;
                    }
                }
            }

            // 일반 객체들 추가
            for (int i = 0; i < node.Objects.Count; i++)
            {
                WorldObject obj = node.Objects[i];
                if (obj.AABB.Visible(viewFrustum.Planes))
                {
                    if (_cullingCache.Add(obj.ObjectID))
                    {
                        aabbList[index] = obj.AABB;
                        index++;
                    }
                }
            }
        }
        /// <summary>
        /// View Frustum Culling (큐 기반), LOD0은 무조건 픽업한다.
        /// </summary>
        public void CullingTestByViewFrustum(Polyhedron viewFrustum, Camera camera, bool enablePickLeaf = false)
        {
            Vertex3f cameraPosition = camera.Position;

            _indexLod0 = 0;

            if (enablePickLeaf)
            {
                // 재사용 버퍼 초기화
                _visibleObjectIndex = 0;

                // 재사용 버퍼 초기화
                _cullingCache.Clear();
            }

            // 루트 노드부터 시작
            _nodeQueue.Clear();
            _nodeQueue.Enqueue(_root);
            while (_nodeQueue.Count > 0)
            {
                QuadNode node = _nodeQueue.Dequeue();

                // 리프 노드 처리
                if (node.IsLeaf)
                {
                    float distSquared = (cameraPosition - node.AABB.Center).LengthSquared();
                    if (distSquared < DistanceSquaredThreshold0)
                    {
                        PickUpObject(node, viewFrustum, ref _lod0, ref _indexLod0);
                    }

                    if (enablePickLeaf)
                    {
                        PickUpObject(node, viewFrustum, ref _visibleObjects, ref _visibleObjectIndex);
                    }
                }

                // 노드가 뷰 프러스텀과 교차 테스트
                if (!node.AABB.Visible(viewFrustum.Planes))
                {
                    node.IsLinked = false;
                    continue;
                }
                else
                {
                    node.IsLinked = true;
                }

                // 자식 노드들을 큐에 추가
                if (!node.IsLeaf)
                {
                    if (node.Children[0].IsLinked) _nodeQueue.Enqueue(node.Children[0]);
                    if (node.Children[1].IsLinked) _nodeQueue.Enqueue(node.Children[1]);
                    if (node.Children[2].IsLinked) _nodeQueue.Enqueue(node.Children[2]);
                    if (node.Children[3].IsLinked) _nodeQueue.Enqueue(node.Children[3]);
                }
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