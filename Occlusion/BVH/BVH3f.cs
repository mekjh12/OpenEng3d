using Geometry;
using Model3d;
using OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Occlusion
{
    /// <summary>
    /// Bounding Volume Hierarchy 트리 구조 (AABB3f 구조체 사용)
    /// Node3f를 이용하여 메모리 효율과 성능 최적화
    /// </summary>
    public partial class BVH3f
    {
        AABB3f[] _visibleAABBs;             // 뷰프러스텀 통과 AABB 배열

        // 성능 향상용 
        Node3f _cnode = null;               // 순회를 위한 현재 노드
        Queue<Node3f> _queue;               // 순회를 위한 노드 큐

        // -----------------------------------------------------------
        // 속성
        // -----------------------------------------------------------

        public ref AABB3f[] VisibleAABBs { get => ref _visibleAABBs; }

        /// <summary>
        /// 생성자
        /// </summary>
        public BVH3f(int capacity = 1000)
        {
            _visibleAABBs = new AABB3f[capacity];

            // 성능 향상용 초기화
            _queue = new Queue<Node3f>();
        }

        // -----------------------------------------------------------
        // 공개 메소드
        // -----------------------------------------------------------

        /// <summary>
        /// [순회비용 발생] 노드의 전체 개수를 반환한다.
        /// <code>
        /// 전체 트리를 순회하여 노드의 개수를 센다.
        /// N이 leaf의 개수이면, 노드의 전체 수는 2N-1이다.
        /// </code>
        /// </summary>
        public uint TotalNodeCount
        {
            get
            {
                if (IsEmpty) return 0;
                uint num = 0;
                _queue = new Queue<Node3f>();
                _queue.Enqueue(_root);
                while (_queue.Count > 0)
                {
                    Node3f tourNode = _queue.Dequeue();
                    num++;
                    if (tourNode.Child1 != null) _queue.Enqueue(tourNode.Child1);
                    if (tourNode.Child2 != null) _queue.Enqueue(tourNode.Child2);
                }
                return num;
            }
        }

        /// <summary>
        /// [순회비용 발생] 트리의 최대 깊이를 반환한다.
        /// </summary>
        public int MaxDepth
        {
            get
            {
                if (IsEmpty) return 0;
                _queue = new Queue<Node3f>();
                _queue.Enqueue(_root);
                int depth = 0;
                while (_queue.Count > 0)
                {
                    Node3f tourNode = _queue.Dequeue();
                    tourNode.Depth = (tourNode.IsRoot) ? 0 : tourNode.Parent.Depth + 1;
                    depth = (int)Math.Max(depth, tourNode.Depth);
                    if (tourNode.Child1 != null) _queue.Enqueue(tourNode.Child1);
                    if (tourNode.Child2 != null) _queue.Enqueue(tourNode.Child2);
                }
                return depth;
            }
        }

        public void CullingTestByHiZBuffer(Matrix4x4f vp, Matrix4x4f view, HierarchicalZBuffer hiZbuffer, bool canMineVisibleAABB = false)
        {
            _recentTravNodeCount = 0;
            _linkLeafCount = 0;

            _queue.Clear();
            _queue.Enqueue(_root);

            while (_queue.Count > 0)
            {
                _cnode = _queue.Dequeue();
                if (_cnode == null) continue;

                if (hiZbuffer.TestVisibility(vp, view, _cnode.AABB))
                {
                    _recentTravNodeCount++;
                    if (_cnode.IsLeaf)
                    {
                        if (canMineVisibleAABB && _linkLeafCount < _visibleAABBs.Length)
                        {
                            _visibleAABBs[_linkLeafCount] = _cnode.AABB;
                        }

                        _linkLeafCount++;
                    }
                    if (_cnode.Left) _queue.Enqueue(_cnode.Child1);
                    if (_cnode.Right) _queue.Enqueue(_cnode.Child2);
                }
                else
                {
                    _cnode.UnLinkBackCopy();
                }
            }
        }

        /// <summary>
        /// 시야절두체(View Frustum)를 사용하여 객체를 컬링합니다.
        /// </summary>
        public void CullingTestByViewFrustum(Polyhedron viewPolyhedron, bool canMineVisibleAABB = false)
        {
            _recentTravNodeCount = 0;
            _linkLeafCount = 0;

            _queue.Clear();
            _queue.Enqueue(_root);

            while (_queue.Count > 0)
            {
                _cnode = _queue.Dequeue();
                if (_cnode == null) continue;

                if (_cnode.AABB.Visible(viewPolyhedron.Planes))
                {
                    _recentTravNodeCount++;
                    if (_cnode.IsLeaf)
                    {
                        if (canMineVisibleAABB && _linkLeafCount < _visibleAABBs.Length)
                        {
                            _visibleAABBs[_linkLeafCount] = _cnode.AABB;
                        }

                        _linkLeafCount++;
                    }
                    if (_cnode.Left) _queue.Enqueue(_cnode.Child1);
                    if (_cnode.Right) _queue.Enqueue(_cnode.Child2);
                }
                else
                {
                    _cnode.UnLinkBackCopy();
                }
            }
        }

        /// <summary>
        /// 모든 AABB3f를 추출하여 리스트로 반환
        /// </summary>
        public int ExtractAABB(ref AABB3f[] aabbs)
        {
            if (_root == null) return 0;

            _linkLeafCount = 0;
            _recentTravNodeCount = 0;

            _queue.Clear();

            _queue.Enqueue(_root);

            while (_queue.Count > 0)
            {
                _cnode = _queue.Dequeue();
                if (_cnode == null) continue;
                _recentTravNodeCount++;

                if (_cnode.IsLeaf)
                {
                    aabbs[_linkLeafCount] = _cnode.AABB;
                    _linkLeafCount++;
                }
                else
                {
                    if (_cnode.Left) _queue.Enqueue(_cnode.Child1);
                    if (_cnode.Right) _queue.Enqueue(_cnode.Child2);
                }
            }

            return _linkLeafCount;
        }

        /// <summary>
        /// 트리에서 연결된 노드만 추출하여 리스트로 가져온다.
        /// </summary>
        [Obsolete("Node3f에 BaseEntity가 없어 사용 불가")]
        public List<Entity> ExtractEntity()
        {
            int travNodeCount = 0;
            _linkLeafCount = 0;

            List<Entity> list = new List<Entity>();
            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f currentNode = queue.Dequeue();
                if (currentNode == null) continue;
                travNodeCount++;

                if (currentNode.IsLeaf)
                {
                    _linkLeafCount++;
                    // BaseEntity 연결 필요
                    // if (currentNode.BaseEntity != null)
                    // {
                    //     list.Add(currentNode.BaseEntity as Entity);
                    // }
                }
                else
                {
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
            }

            return list;
        }

        /// <summary>
        /// 원본을 손상하지 않기 위해서 복사본의 모든 노드를 true로 연결한다.
        /// <code>
        /// 원본 트리에 각 노드는 Left, Right 플래그를 가지고 있다.
        /// 이 플래그는 복사본 트리에서 해당 노드가 활성화(연결) 되었는지를 나타낸다.
        /// 빠른 컬링 테스트를 위해 사용된다.
        /// 컬링 테스트 후에 이 **플래그를 초기화**해야 한다.
        /// </code>
        /// </summary>
        public void ClearBackTreeNodeLink(bool visible = true)
        {
            if (_root == null) return;

            _queue.Clear();
            _queue.Enqueue(_root);
            _recentTravNodeCount = 0;

            while (_queue.Count > 0)
            {
                _cnode = _queue.Dequeue();
                if (_cnode == null) break;

                _cnode.Left = visible;
                _cnode.Right = visible;
                _recentTravNodeCount++;

                if (_cnode.Child1 != null) _queue.Enqueue(_cnode.Child1);
                if (_cnode.Child2 != null) _queue.Enqueue(_cnode.Child2);
            }
        }

        /// <summary>
        /// 오클루더를 추출하여 리스트로 가져온다.
        /// </summary>
        /// <param name="cameraPos">카메라의 위치</param>
        /// <param name="OCCLUDER_MINIMAL_AREA">거리에 따라 오클루더의 넓이를 고려하여 임계치를 설정</param>
        /// <returns></returns>
        [Obsolete("Node3f에 BaseEntity가 없어 사용 불가")]
        public List<OccluderEntity> ExtractOccluder(Vertex3f cameraPos, float OCCLUDER_MINIMAL_AREA = 0.1f)
        {
            List<OccluderEntity> occluders = new List<OccluderEntity>();
            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f currentNode = queue.Dequeue();
                if (currentNode == null) break;

                if (currentNode.IsLeaf)
                {
                    // BaseEntity 연결 필요 (Node3f에 추가해야 함)
                    // OccluderEntity occlusionEntity = currentNode.BaseEntity as OccluderEntity;
                    // if (occlusionEntity != null && occlusionEntity.IsOccluder)
                    // {
                    //     float distance = (cameraPos - occlusionEntity.Position).Length();
                    //     float perspectiveArea = occlusionEntity.OBB.Area / distance;
                    //     if (perspectiveArea > OCCLUDER_MINIMAL_AREA)
                    //     {
                    //         occluders.Add(occlusionEntity);
                    //     }
                    // }
                }
                else
                {
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
            }

            return occluders;
        }

    }
}