//------------------------------------------------------------------------------
//
// 설명:
//    Bounding Volume Hierarchy (BVH) 동적 트리 구조를 구현합니다.
//    AABB3f 구조체 사용으로 메모리 효율 및 성능 최적화
//    이 구현의 주요 아이디어(Branch and Bound 기반 삽입/최적화)는
//    Box2D 물리 엔진의 개발자인 에린 카토(Erin Catto)의 연구를 기반으로 합니다.
//    주로 오클루전 컬링 및 공간 질의(Spatial Query)에 사용됩니다.
//
//------------------------------------------------------------------------------
//
// == 주요 참고 자료 (Primary Reference) ==
//
//    저자:    Erin Catto (에린 카토)
//    제목:    "Dynamic Bounding Volume Hierarchies" (동적 경계 볼륨 계층 구조)
//    발표:    Game Developers Conference (GDC) 2019
//    영상/자료: https://www.gdcvault.com/play/1026481/Dynamic-Bounding-Volume-Hierarchies
//
// == 코드 구현 참고 (C++ 원본) ==
//
//    프로젝트: Box2D (b2_dynamic_tree.cpp 파일 참고)
//    URL:     https://github.com/erincatto/box2d
//
//------------------------------------------------------------------------------

using Geometry;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;

namespace Occlusion
{
    /// <summary>
    /// Bounding Volume Hierarchy 트리 구조 (AABB3f 구조체 사용)
    /// Node3f를 이용하여 메모리 효율과 성능 최적화
    /// </summary>
    public partial class BVH3f
    {
        Node3f _root;                       // 루트 노드
        Node3f _recentNode;                 // 최근에 삽입된 노드
        uint _countLeafByBB;                // Branch and Bound 기반 삽입된 잎노드의 개수(검사용으로만 사용)

        int _linkLeafCount;                 // 링크된 잎 노드 카운터
        int _finalLeafCount;                // 최종 잎 노드 카운터  
        int _recentTravNodeCount;           // 최근 트래버스된 노드 카운터

        public enum INSERT_ALGORITHM_METHOD
        {
            GLOBAL_SEARCH,
            BRANCH_AND_BOUND,
            BRANCH_AND_BOUND_BALANCED
        }

        private const string STRING_NODE_ISNOT_LEAF = "현재 선택한 노드 ({0})는 leaf가 아닙니다.";

        // -----------------------------------------------------------
        // 속성
        // -----------------------------------------------------------

        public int LinkLeafCount => _linkLeafCount;
        public int FinalLeafCount => _finalLeafCount;
        public Node3f RecentNode => _recentNode;
        public Node3f Root { get => _root; set => _root = value; }
        public bool IsEmpty => (_root == null);

        #region 비용 계산

        /// <summary>
        /// 삽입할 자리에 드는 전체 비용을 반환한다.
        /// <code>
        /// 비용 = sum{부모의 변화량} + cost(T ∪ L)
        /// L = 삽입하는 노드
        /// T = 삽입할 자리의 노드
        /// </code>
        /// </summary>
        private float Cost(Node3f targetNode, in AABB3f insertBox)
        {
            // targetNode의 AABB와 insertBox의 합집합 면적
            AABB3f unionBox = AABB3f.Union(in targetNode.AABB, in insertBox);
            float cost = unionBox.Area;

            if (targetNode.Parent != null)
            {
                Node3f tourNode = targetNode.Parent;
                while (tourNode != null)
                {
                    AABB3f parentUnion = AABB3f.Union(in tourNode.AABB, in insertBox);
                    float deltaSA = parentUnion.Area - tourNode.AABB.Area;
                    cost += deltaSA;
                    tourNode = tourNode.Parent;
                }
            }

            return cost;
        }

        /// <summary>
        /// 두 노드의 AABB 합집합의 표면적을 반환한다.
        /// </summary>
        private static float UnionArea(Node3f A, Node3f B)
        {
            AABB3f unionBox = AABB3f.Union(in A.AABB, in B.AABB);
            return unionBox.Area;
        }

        #endregion

        #region 최적 삽입 위치 찾기

        /// <summary>
        /// Best Global Search 알고리즘을 사용하여 최적의 삽입 위치 찾기
        /// Branch and Bound보다 느리지만 전체 탐색
        /// </summary>
        private Node3f PickBestGlobalSearch(in AABB3f insertBox)
        {
            Node3f bestSibling = _root;
            float cost = float.MaxValue;

            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f tourNode = queue.Dequeue();

                float costNode = Cost(tourNode, in insertBox);
                if (costNode < cost)
                {
                    cost = costNode;
                    bestSibling = tourNode;
                }

                if (tourNode.Child1 != null) queue.Enqueue(tourNode.Child1);
                if (tourNode.Child2 != null) queue.Enqueue(tourNode.Child2);
            }

            return bestSibling;
        }

        /// <summary>
        /// Branch And Bound 알고리즘을 사용하여 최적의 삽입 위치 찾기
        /// </summary>
        private Node3f PickBestBranchAndBound(in AABB3f insertBox)
        {
            Queue<Node3f> queue = new Queue<Node3f>();

            // Branch And Bound를 위한 초기화
            Node3f bestSibling = _root;
            AABB3f rootUnion = AABB3f.Union(in bestSibling.AABB, in insertBox);
            float bestCost = rootUnion.Area + 1.0f;
            queue.Enqueue(_root);

            // C_low < C_best 인 경우에 하위 트리를 탐색
            while (queue.Count > 0)
            {
                Node3f currentNode = queue.Dequeue();

                AABB3f currentUnion = AABB3f.Union(in currentNode.AABB, in insertBox);
                float directedCost = currentUnion.Area;
                float prevInheritedCost = (currentNode.Parent == null) ? 0.0f : currentNode.Parent.InheritedCost;
                float currentDeltaSA = directedCost - currentNode.AABB.Area;
                currentNode.InheritedCost = prevInheritedCost + currentDeltaSA;
                float costLow = directedCost + prevInheritedCost;

                if (costLow < bestCost)
                {
                    bestSibling = currentNode;
                    bestCost = costLow;

                    if (currentNode.Child1 != null) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Child2 != null) queue.Enqueue(currentNode.Child2);
                }
            }

            return bestSibling;
        }

        #endregion

        #region 노드 합병

        /// <summary>
        /// 두 노드의 합집합 노드를 생성하여 반환
        /// </summary>
        private static Node3f Union(Node3f A, Node3f B)
        {
            AABB3f unionBox = AABB3f.Union(in A.AABB, in B.AABB);
            Node3f C = new Node3f(in unionBox);
            C.Child1 = A;
            C.Child2 = B;
            A.Parent = C;
            B.Parent = C;
            return C;
        }

        #endregion

        #region 회전 최적화

        /// <summary>
        /// SAH cost를 줄이기 위한 트리 재배치
        /// 노드를 기준으로 자식과 형제를 교환 가능한지 판단
        /// </summary>
        private void RotateNode(Node3f node)
        {
            // 부모 노드가 없으면 회전 불가
            if (node.Parent == null) return;

            // 자식 노드가 없으면 회전 불가
            if (node.IsLeaf) return;

            // 부모가 있으면 brother가 반드시 존재
            Node3f brother = node.Brother;

            // 검색할 4개의 노드를 선정
            List<Node3f> nodes = new List<Node3f>();
            nodes.Add(node.Child1);
            nodes.Add(node.Child2);
            if (brother.HasChild1) nodes.Add(brother.Child1);
            if (brother.HasChild2) nodes.Add(brother.Child2);

            // 4개의 노드를 순회하면서 회전이 필요하면 교체
            foreach (Node3f me in nodes)
            {
                Node3f parent = me.Parent;
                Node3f grandParent = me.GrandParent;
                Node3f anotherChild = me.Brother;
                Node3f uncle = parent.Brother;

                float candidateCost = BVH3f.UnionArea(anotherChild, uncle);
                float nodeArea = node.AABB.Area;

                if (candidateCost < nodeArea) // 회전이 필요
                {
                    // 회전을 위한 링크 수정
                    grandParent.ReplaceChild(uncle, me);
                    uncle.Parent = parent;

                    if (parent.Child1 == anotherChild)
                    {
                        parent.Child2 = uncle;
                    }
                    else
                    {
                        parent.Child1 = uncle;
                    }
                }
            }
        }

        #endregion

        #region 삽입 및 제거

        /// <summary>
        /// 노드를 재삽입 (최적화)
        /// </summary>
        public Node3f ReInsert(Node3f node, INSERT_ALGORITHM_METHOD mode = INSERT_ALGORITHM_METHOD.BRANCH_AND_BOUND)
        {
            AABB3f box = node.AABB;
            RemoveLeaf(node);
            return InsertLeaf(in box, mode);
        }

        /// <summary>
        /// 삽입 알고리즘에 따라 AABB를 트리에 삽입
        /// 기본 알고리즘은 Branch And Bound
        /// </summary>
        public Node3f InsertLeaf(in AABB3f box, INSERT_ALGORITHM_METHOD mode = INSERT_ALGORITHM_METHOD.BRANCH_AND_BOUND)
        {
            Node3f insertNode = new Node3f(in box, useEnhanceBox: true);

            // Stage 0: 트리가 비어있으면 루트로 설정
            if (IsEmpty)
            {
                _root = insertNode;
                _countLeafByBB++;
                _recentNode = insertNode;
                return insertNode;
            }

            // Stage 1: 최적의 형제 노드 찾기
            Node3f bestSibling;
            if (mode == INSERT_ALGORITHM_METHOD.GLOBAL_SEARCH)
            {
                bestSibling = this.PickBestGlobalSearch(in box);
            }
            else if (mode == INSERT_ALGORITHM_METHOD.BRANCH_AND_BOUND)
            {
                bestSibling = this.PickBestBranchAndBound(in box);
            }
            else
            {
                bestSibling = this.PickBestBranchAndBoundBalanced(in box);
            }

            // Stage 2: 새로운 부모 생성
            Node3f newParent;
            if (bestSibling.IsRoot)
            {
                newParent = BVH3f.Union(bestSibling, insertNode);
                _root = newParent;
            }
            else
            {
                Node3f oldParent = bestSibling.Parent;
                newParent = BVH3f.Union(bestSibling, insertNode);
                oldParent.ReplaceChild(bestSibling, newParent);
            }

            // Stage 3: 트리를 거슬러 올라가며 AABB 재계산 및 회전 적용
            Node3f tourNode = newParent;
            while (tourNode != null)
            {
                tourNode.Refit(in box);
                //RotateNode(tourNode); // 필요시 활성화
                tourNode = tourNode.Parent;
            }

            // ✅ 추가: 형제 노드도 회전 시도
            if (newParent.Parent != null)
            {
                //RotateNode(newParent.Brother);
            }

            // 정상적으로 삽입하면 leaf의 개수를 증가
            _countLeafByBB++;
            _recentNode = insertNode;

            return insertNode;
        }

        /// <summary>
        /// 불균형 지수 계산
        /// </summary>
        public float GetImbalanceFactor()
        {
            if (IsEmpty) return 0.0f;

            int maxDepth = MaxDepth;
            int leafCount = (int)_countLeafByBB;
            float idealDepth = (float)Math.Log(leafCount, 2);

            return (maxDepth - idealDepth) / Math.Max(idealDepth, 1.0f);
        }

        /// <summary>
        /// 반복적으로 회전 최적화 (수렴할 때까지)
        /// </summary>
        public void OptimizeTreeIterative(int maxIterations = 10)
        {
            if (IsEmpty) return;

            float prevImbalance = GetImbalanceFactor();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                OptimizeTree();

                float currentImbalance = GetImbalanceFactor();
                float improvement = prevImbalance - currentImbalance;

                Console.WriteLine($"[BVH] 반복 {iter + 1}: Imbalance={currentImbalance:F3}, " +
                                 $"Improvement={improvement:F3}, MaxDepth={MaxDepth}");

                // 개선이 미미하면 중단
                if (improvement < 0.01f)
                {
                    Console.WriteLine($"[BVH] 수렴 완료 (반복 {iter + 1}회)");
                    break;
                }

                prevImbalance = currentImbalance;
            }
        }

        /// <summary>
        /// 전체 트리를 순회하며 회전 최적화
        /// </summary>
        public void OptimizeTree()
        {
            if (IsEmpty) return;

            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            int rotationCount = 0;

            while (queue.Count > 0)
            {
                Node3f node = queue.Dequeue();

                if (!node.IsLeaf)
                {
                    // 회전 전 비용
                    float beforeCost = node.AABB.Area;

                    RotateNode(node);

                    // 회전 후 비용
                    float afterCost = node.AABB.Area;

                    if (afterCost < beforeCost)
                    {
                        rotationCount++;
                    }

                    if (node.Child1 != null) queue.Enqueue(node.Child1);
                    if (node.Child2 != null) queue.Enqueue(node.Child2);
                }
            }
        }


        /// <summary>
        /// SAH + 깊이 페널티를 고려한 비용 함수
        /// </summary>
        private float CostWithBalance(Node3f targetNode, in AABB3f insertBox)
        {
            float sahCost = Cost(targetNode, in insertBox);

            // 깊이가 깊을수록 페널티 증가
            float depthPenalty = targetNode.Depth * 0.1f; // 튜닝 가능한 가중치

            return sahCost * (1.0f + depthPenalty);
        }

        private Node3f PickBestBranchAndBoundBalanced(in AABB3f insertBox)
        {
            Queue<Node3f> queue = new Queue<Node3f>();

            Node3f bestSibling = _root;
            float bestCost = float.MaxValue;
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f currentNode = queue.Dequeue();

                float cost = CostWithBalance(currentNode, in insertBox); // ✅ 변경

                if (cost < bestCost)
                {
                    bestSibling = currentNode;
                    bestCost = cost;

                    if (currentNode.Child1 != null) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Child2 != null) queue.Enqueue(currentNode.Child2);
                }
            }

            return bestSibling;
        }

        /// <summary>
        /// 잎 노드를 제거한다.
        /// 내부 노드는 제거할 수 없다.
        /// </summary>
        public bool RemoveLeaf(Node3f node)
        {
            if (node == null) return false;

            if (!node.IsLeaf)
            {
                System.Console.WriteLine(string.Format(STRING_NODE_ISNOT_LEAF, node.Guid));
                return false;
            }

            if (node.IsRoot)
            {
                if (node.HasChild)
                {
                    System.Console.WriteLine(string.Format(STRING_NODE_ISNOT_LEAF, node.Guid));
                    return false;
                }
                else
                {
                    NodeGuid.ReleaseGuid(_root.Guid);
                    _root = null;
                }
            }
            else
            {
                Node3f parent = node.Parent;
                Node3f restNode = (parent.Child1 == node) ? parent.Child2 : parent.Child1;

                NodeGuid.ReleaseGuid(parent.Guid);
                NodeGuid.ReleaseGuid(node.Guid);

                if (parent.IsRoot)
                {
                    _root = restNode;
                    restNode.Parent = null;
                }
                else
                {
                    Node3f grandParent = parent.Parent;
                    if (grandParent.Child1 == parent)
                    {
                        grandParent.Child1 = restNode;
                    }

                    if (grandParent.Child2 == parent)
                    {
                        grandParent.Child2 = restNode;
                    }
                    restNode.Parent = grandParent;
                }
            }

            _countLeafByBB = Math.Max(_countLeafByBB - 1, 0);
            return true;
        }

        #endregion

        #region 검색 및 최적화

        /// <summary>
        /// 노드의 guid를 이용하여 노드를 찾는다.
        /// </summary>
        public Node3f FindNode(uint guid)
        {
            if (IsEmpty) return null;

            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f tourNode = queue.Dequeue();

                if (tourNode.Guid == guid) return tourNode;

                if (tourNode.Child1 != null) queue.Enqueue(tourNode.Child1);
                if (tourNode.Child2 != null) queue.Enqueue(tourNode.Child2);
            }

            return null;
        }

        /// <summary>
        /// 트리 최적화 (회전 적용)
        /// </summary>
        public void Optimize(Node3f node)
        {
            if (node == null) return;

            System.Console.WriteLine("* Optimize start!");
            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(node);

            while (queue.Count > 0)
            {
                Node3f tourNode = queue.Dequeue();

                RotateNode(tourNode);
                if (tourNode.Child1 != null) queue.Enqueue(tourNode.Child1);
                if (tourNode.Child2 != null) queue.Enqueue(tourNode.Child2);
            }

            System.Console.WriteLine("* Optimize finished!");
        }

        #endregion

        #region 트리 관리

        /// <summary>
        /// 트리의 전체 노드를 삭제한다.
        /// 링크를 해제한 것이므로 GC 실행 시 메모리를 정리한다.
        /// </summary>
        public void Clear()
        {
            if (IsEmpty) return;

            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f tourNode = queue.Dequeue();
                if (tourNode.Child1 != null) queue.Enqueue(tourNode.Child1);
                if (tourNode.Child2 != null) queue.Enqueue(tourNode.Child2);
                // 노드는 GC가 자동 수거
            }

            _root = null;
            _countLeafByBB = 0;
            NodeGuid.Reset();
        }

        /// <summary>
        /// 트리의 구조를 콘솔에 출력한다.
        /// </summary>
        public void Print()
        {
            if (IsEmpty)
            {
                System.Console.WriteLine("BVH3f is empty.");
                return;
            }

            Queue<Node3f> queue = new Queue<Node3f>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Node3f currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (currentNode.ExistParent)
                {
                    currentNode.Depth = currentNode.Parent.Depth + 1;
                    currentNode.Txt = currentNode.Parent.Txt + $"->[{currentNode.Depth}]" + currentNode.Guid;
                }
                else
                {
                    currentNode.Depth = 0;
                    currentNode.Txt = $"[{currentNode.Depth}]" + currentNode.Guid;
                }

                if (currentNode.IsLeaf)
                {
                    System.Console.WriteLine(currentNode.Txt);
                }
                else
                {
                    if (currentNode.Child1 != null) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Child2 != null) queue.Enqueue(currentNode.Child2);
                }
            }
        }

        #endregion
    }
}