using OpenGL;
using System.Collections;
using System.Collections.Generic;
using ZetaExt;

namespace BSP
{
    public class BspTree
    {
        BspNode _root;

        public BspNode Root
        {
            get => _root;
            set => _root = value;
        }

        BspNode _recentBspNode = null;

        public BspNode RecentBspNode =>_recentBspNode;


        public BspTree()
        {

        }

        public void Clear()
        {
            _root = null;
        }

        public void Insert(Segment3 segment)
        {
            if (_root == null)
            {
                _root = new BspNode(segment);
                return;
            }

            Queue<BspNode> queue = new Queue<BspNode>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                BspNode node = queue.Dequeue();
                Segment3 line = node.Segment;

                if (line.IsSplitNeed(segment))
                {
                    Vertex3f[] c = line.CrossPoint(segment);
                    if (c != null)
                    {
                        Segment3 s1 = new Segment3(segment.Start, c[1]);
                        Segment3 s2 = new Segment3(c[0], segment.End);

                        Insert(s1);
                        Insert(s2);
                    }
                }
                else
                {
                    if (line.IsFront(segment))
                    {
                        if (node.Front != null)
                        {
                            queue.Enqueue(node.Front);
                        }
                        else
                        {
                            BspNode front = new BspNode(segment);
                            front.Parent = node;
                            node.Front = front;
                        }
                    }
                    else
                    {
                        if (node.Back != null)
                        {
                            queue.Enqueue(node.Back);
                        }
                        else
                        {
                            BspNode back = new BspNode(segment);
                            back.Parent = node;
                            node.Back = back;
                        }
                    }
                }
            }
        }

        public List<BspNode> GetConvexNodes(Vertex2f cameraPosition)
        {
            List<BspNode> bspNodes = new List<BspNode>();
            Stack<BspNode> stack = new Stack<BspNode>();
            stack.Push(_root);

            int index = 0;
            while (stack.Count > 0)
            {
                BspNode node = stack.Pop();
                node.Text = node.Parent == null ? "0" : $"{index}";

                bspNodes.Add(node);
                index++;
                float dist = node.Segment.LineEquation.Dot(cameraPosition);

                if (dist > 0)
                {
                    if (node.Front != null) stack.Push(node.Front);
                }
                else
                {
                    if (node.Back != null) stack.Push(node.Back);
                }
            }

            return bspNodes;
        }


        public List<BspNode> GetNodes(Vertex2f cameraPosition)
        {
            List<BspNode> bspNodes = new List<BspNode>();

            RenderBSP(_root, cameraPosition);

            void RenderBSP(BspNode node, Vertex2f p)
            {
                if (node.Segment.LineEquation.Dot(p) < 0)
                {
                    if (node.Back != null) RenderBSP(node.Back, p);
                    bspNodes.Add(node);
                    if (node.Front != null) RenderBSP(node.Front, p);
                }
                else
                {
                    if (node.Front != null) RenderBSP(node.Front, p);
                    bspNodes.Add(node);
                    if (node.Back != null) RenderBSP(node.Back, p);
                }
            }

            return bspNodes;
        }


        public void Print()
        {
            Queue<BspNode> queue = new Queue<BspNode>();
            queue.Enqueue(_root);

            while (queue.Count > 0)
            {
                BspNode node = queue.Dequeue();

                node.Text = node.Parent==null ? $"{node.ID}" : $"{node.Parent.Text}->{node.ID}";

                if (node.Front == null && node.Back == null)
                {
                    System.Console.WriteLine(node.Text);
                }

                if (node.Front != null) queue.Enqueue(node.Front);
                if (node.Back != null) queue.Enqueue(node.Back);
            }
        }

    }
}
