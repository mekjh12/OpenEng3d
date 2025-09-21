using OpenGL;
using ZetaExt;
using System.Collections.Generic;
using System;

namespace Occlusion
{
    public class BSH<T>
    {
        Sphere<T> _root;

        /// <summary>
        /// BVHs구현을 위한 트리를 반환한다.
        /// </summary>
        public Sphere<T> Root
        {
            get => _root;
            set => _root = value;
        }

        /// <summary>
        /// 트리에 적어도 하나의 촤상위 노드 유무를 반환한다.
        /// </summary>
        public bool ExistRoot => (_root != null);

        /// <summary>
        /// 
        /// </summary>
        public bool IsEmpty => (_root == null);

        /// <summary>
        /// Bounding Volume Heirerchical을 만든다.
        /// </summary>
        public BSH()
        {
            //_rawSphere = Loader3d.LoadSphere(1, 3);
        }


        /// <summary>
        /// 트리의 최대 깊이를 반환한다.
        /// </summary>
        public int MaxDepth
        {
            get
            {
                if (IsEmpty) return 0;
                Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
                queue.Enqueue(_root);
                int depth = 0;
                while (queue.Count > 0)
                {
                    Sphere<T> tourNode = queue.Dequeue();
                    if (tourNode == null) continue;

                    tourNode.Depth = (tourNode.IsRoot) ? 0 : tourNode.Parent.Depth + 1;
                    depth = (int)Math.Max(depth, tourNode.Depth);

                    for (int i = 0; i < 2; i++)
                    {
                        queue.Enqueue(tourNode.Childs[i]);
                    }
                }
                return depth;
            }
        }

        /// <summary>
        /// 트리의 내용을 깊게 복사한다.
        /// </summary>
        /// <returns></returns>
        public BSH<T> CopyTo()
        {
            BSH<T> dst = new BSH<T>();
            dst.Root = new Sphere<T>(_root.Center, _root.Radius) { Object = _root.Object };

            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            Queue<Sphere<T>> dstQueue = new Queue<Sphere<T>>();
            queue.Enqueue(_root);
            dstQueue.Enqueue(dst.Root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();
                Sphere<T> dstCurrentNode = dstQueue.Dequeue();

                if (currentNode == null) continue;
                //dstCurrentNode = currentNode.Clone();
                if (currentNode.Parent == null)
                {
                    dst.Root = dstCurrentNode;
                }

                if (!currentNode.IsLeaf)
                {
                    if (currentNode.Childs[0] != null)
                    {
                        dstCurrentNode.Childs[0] = currentNode.Childs[0].Clone();
                        dstCurrentNode.Childs[0].Parent = dstCurrentNode;
                    }

                    if (currentNode.Childs[1] != null)
                    {
                        dstCurrentNode.Childs[1] = currentNode.Childs[1].Clone();
                        dstCurrentNode.Childs[1].Parent = dstCurrentNode;
                    }
                }

                // traversal child tree
                if (currentNode.Childs[0] != null)
                {
                    queue.Enqueue(currentNode.Childs[0]);
                    dstQueue.Enqueue(dstCurrentNode.Childs[0]);
                }
                if (currentNode.Childs[1] != null)
                {
                    queue.Enqueue(currentNode.Childs[1]);
                    dstQueue.Enqueue(dstCurrentNode.Childs[1]);
                }
            }

            return dst;
        }

        /// <summary>
        /// 트리의 잎에 있는 오브젝트를 리스트로 모두 모은다.
        /// </summary>
        /// <returns></returns>
        public List<T> CollectLeaf()
        {
            List<T> list = new List<T>();
            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (!currentNode.IsLeaf)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        queue.Enqueue(currentNode.Childs[i]);
                    }
                }
                else
                {
                    if (currentNode != null && currentNode.Object != null)
                    {
                        list.Add(currentNode.Object);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 트리의 구조를 콘솔에 출력한다.
        /// </summary>
        public void Print()
        {
            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (currentNode.ExistParent)
                {
                    currentNode.Depth = currentNode.Parent.Depth + 1;
                    currentNode.Txt = currentNode.Parent.Txt + $"->[{currentNode.Depth}]" + currentNode.GUID + "";
                }
                else
                {
                    currentNode.Depth = 0;
                    currentNode.Txt = $"[{currentNode.Depth}]" + currentNode.GUID + "";
                }

                if (currentNode.IsLeaf)
                {
                    System.Console.WriteLine(currentNode.Txt);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                        queue.Enqueue(currentNode.Childs[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public List<T> IntersectRadius(Vertex3f position, float radius)
        {
            List<T> intersectList = new List<T>();

            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                // 두 점 사이의 거리
                float distance = MathF.Abs((currentNode.Center - position).Length());
                float R = currentNode.Radius;

                if (radius + R > distance)
                {
                    if (currentNode.IsLeaf) intersectList.Add(currentNode.Object);
                    if (currentNode.Childs[0] != null) queue.Enqueue(currentNode.Childs[0]);
                    if (currentNode.Childs[1] != null) queue.Enqueue(currentNode.Childs[1]);
                }

            }

            return intersectList;
        }

        /// <summary>
        /// * 최대조명거리를 기반으로 뷰프러스텀 안의 광원(점, 스팟)만 가져온다.<br/>
        /// * 접촉 여부의 판정은 점광원, 스팟광원 모두 구의 경계로 판단하다.<br/>
        /// * 이진트리 BSH를 이용한다.<br/>
        /// </summary>
        /// <param name="planes">뷰프러스럼의 6개의 안쪽을 향하는 평면</param>
        /// <param name="intersectList">출력 되는 구</param>
        public void IntersectViewFrustum(Vertex4f[] planes, out List<T> intersectList)
        {
            intersectList = new List<T>();

            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();
                if (currentNode == null) continue;
                if (SphereVisible(planes, currentNode.Center, currentNode.Radius))
                {
                    if (currentNode.IsLeaf) intersectList.Add(currentNode.Object);
                    if (currentNode.Childs[0] != null) queue.Enqueue(currentNode.Childs[0]);
                    if (currentNode.Childs[1] != null) queue.Enqueue(currentNode.Childs[1]);
                }
            }            
        }

        /// <summary>
        /// 구가 평면의 앞쪽에 있는지 여부를 검사한다.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool SphereVisible(Vertex4f[] planes, Vertex3f center, float radius)
        {
            if (planes == null) return true;

            float negativeRadius = -radius;

            for (int i = 0; i < planes.Length; i++)
            {
                float distance = planes[i].x * center.x + planes[i].y * center.y + planes[i].z * center.z + planes[i].w;
                if (distance < negativeRadius) return false; // 구가 완전히 평면 뒤쪽에 있어야 하므로 조건식이 d<-r이다.
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="startSphere"></param>
        /// <param name="lightRenderEntities"></param>
        /// <returns></returns>
        public static BSH<T> IntersectViewFrustum2(Vertex4f[] planes, Sphere<T> startSphere, ref List<T> lightRenderEntities)
        {
            BSH<T> bsh = new BSH<T>();
            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();
            queue.Enqueue(startSphere);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();

                if (SphereVisible(planes, currentNode.Center, currentNode.Radius))
                {
                    if (currentNode.IsLeaf)
                    {
                        Sphere<T> sphere = currentNode.Clone();
                        bsh.InsertLeaf(sphere);
                        lightRenderEntities.Add(currentNode.Object);
                    }

                    if (currentNode.Childs[0] != null) queue.Enqueue(currentNode.Childs[0]);
                    if (currentNode.Childs[1] != null) queue.Enqueue(currentNode.Childs[1]);
                }
            }
            return bsh;
        }

        /// <summary>
        /// Sphere를 트리에서 삭제한다.
        /// </summary>
        /// <returns></returns>
        public bool RemoveLeaf(Sphere<T> sphere)
        {
            // Use a queue Q to explore best candidates first
            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();

            // initialize
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();

                if (currentNode.IsLeaf)
                {
                    if (currentNode == sphere)
                    {
                        if (currentNode.Parent == null)
                        {
                            // 하나의 노드만 있는 경우
                            _root = null;
                            return true;
                        }
                        else if (currentNode.Parent.Parent == null)
                        {
                            // 조부모 노드가 없는 경우
                            _root = currentNode.Brother;
                            return true;
                        }
                        else
                        {
                            // 조부모가 있는 경우
                            Sphere<T> gParent = currentNode.GrandParent;
                            Sphere<T> brother = currentNode.Brother;
                            int childIndex = currentNode.Parent.ChildIndex;
                            gParent.Childs[childIndex] = brother;
                            brother.Parent = gParent;
                            return true;
                        }
                    }
                }

                if (currentNode.Childs[0] != null) queue.Enqueue(currentNode.Childs[0]);
                if (currentNode.Childs[1] != null) queue.Enqueue(currentNode.Childs[1]);
            }

            return false;
        }


        /// <summary>
        /// T를 포함하는 구를 트리의 적정위치에 삽입한다.
        /// Sorted Insert Problem and RotateNode
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        public bool InsertLeaf(Sphere<T> sphere)
        {
            Sphere<T> insertNode = sphere;

            // stage 0: if tree is null, then inserted node is root.
            if (_root == null)
            {
                insertNode = sphere;
                _root = insertNode;
                return true;
            }

            // Use a queue Q to explore best candidates first
            Queue<Sphere<T>> queue = new Queue<Sphere<T>>();

            // initialize
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                Sphere<T> currentNode = queue.Dequeue();

                // 현재 노드에 속하지 않으면
                if (!currentNode.IsInclude(insertNode))
                {
                    Sphere<T> c1 = currentNode;
                    Sphere<T> I = insertNode;

                    if (currentNode.Parent == null)
                    {
                        Sphere<T> N = Sphere<T>.Union(c1, I);
                        _root = N;
                    }
                    else
                    {
                        Sphere<T> Parent = c1.Parent;
                        int childIndex = c1.ChildIndex;
                        Sphere<T> N = Sphere<T>.Union(c1, I);
                        N.Parent = Parent;
                        Parent.Childs[childIndex] = N;
                    }
                }
                // 현재 노드에 속하면
                else
                {
                    // 잎 노드인 경우에
                    if (currentNode.IsLeaf)
                    {
                        Sphere<T> c1 = currentNode;
                        Sphere<T> I = insertNode;
                        Sphere<T> Parent = c1.Parent;
                        int childIndex = c1.ChildIndex;
                        Sphere<T> N = Sphere<T>.Union(c1, I);

                        if (Parent != null)
                        {
                            N.Parent = Parent;
                            Parent.Childs[childIndex] = N;
                        }
                        else
                        {
                            _root = N;
                        }
                    }
                    else
                    {
                        float d0 = (insertNode.Center - currentNode.Childs[0].Center).Length();
                        float d1 = (insertNode.Center - currentNode.Childs[1].Center).Length();
                        int bestIndex = (d0 < d1) ? 0 : 1;

                        bool i0 = currentNode.Childs[0].IsInclude(insertNode);
                        bool i1 = currentNode.Childs[1].IsInclude(insertNode);

                        if (i0 == true && i1 == true)
                        {
                            queue.Enqueue(currentNode.Childs[bestIndex]);
                        }
                        else if (i0 == false && i1 == false)
                        {
                            queue.Enqueue(currentNode.Childs[bestIndex]);
                        }
                        else if (i0 == true && i1 == false)
                        {
                            queue.Enqueue(currentNode.Childs[0]);
                        }
                        else if (i0 == false && i1 == true)
                        {
                            queue.Enqueue(currentNode.Childs[1]);
                        }
                    }
                }

            }

            return true;
        }

        /// <summary>
        /// 두 구를 포함하는 구의 겉넓이 비용을 반환한다.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static float UnionCost(Sphere<T> A, Sphere<T> B)
        {
            Sphere<T> sphere = Sphere<T>.Union(A, B);
            return sphere.Area;
        }

    }
}
