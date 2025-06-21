using Geometry;
using OpenGL;
using System.Collections.Generic;

namespace Occlusion
{
    public class Node
    {
        public static int GUID = -1;
        public static Stack<int> GuidStack = new Stack<int>(); // 제거한 노드의 guid를 수거하여 다음 삽입때 재사용한다.

        int _guid;
        Node _parent;
        Node _child1;
        Node _child2;
        bool _left = true;
        bool _right = true;
        AABB _aabb;
        AABB _enhanceBox;

        List<Node> _lowerNodeList;

        float _inheritedCost;
        string _txt;
        uint _depth;

        /// <summary>
        /// 원본을 링크를 손상하지 않기 위하여 순회 검색을 위한 왼쪽 자식노드의 연결 유무를 반환한다.
        /// </summary>
        public bool Left
        {
            get=> _left; 
            set=> _left = value;
        }

        /// <summary>
        /// 원본을 링크를 손상하지 않기 위하여 순회 검색을 위한 오른쪽 자식노드의 연결 유무를 반환한다.
        /// </summary>
        public bool Right
        {
            get => _right;
            set => _right = value;
        }

        public List<Node> LowerNodeList
        {
            get => _lowerNodeList;
            set => _lowerNodeList = value;
        }

        public bool HasLowerNodeList => _lowerNodeList != null;

        public string Txt
        {
            get => _txt; 
            set => _txt = value;
        }

        public uint Depth
        {
            get => _depth;
            set => _depth = value;
        }

        public bool ExistParent => _parent != null;

        public int Guid => _guid;

        public string Value => $"{_aabb.LowerBound.ToString()} {_aabb.UpperBound.ToString()}";

        public int Child1_GUID => (_child1 == null) ? -1 : _child1.Guid;

        public int Child2_GUID => (_child2 == null) ? -1 : _child2.Guid;

        /// <summary>
        /// 루트노드에서 현재 노드까지의 ∑{△SA(P_i)}이다.
        /// </summary>
        public float InheritedCost
        {
            get => _inheritedCost;
            set => _inheritedCost = value;
        }

        public AABB AABB => _aabb;

        public bool IsRoot => (_parent == null);

        public bool IsLeaf => (_child1 == null && _child2 == null);

        public bool HasChild => (_child1 != null || _child2 != null);

        public bool HasChild1 => (_child1 != null);

        public bool HasChild2 => (_child2 != null);

        public Node Child1
        {
            get => _child1;
            set => _child1 = value;
        }

        public Node Child2
        {
            get => _child2;
            set => _child2 = value;
        }

        public Node Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Node GrandParent
        {
            get
            {
                if (_parent == null) return null;
                if (_parent.Parent == null) return null;
                return _parent.Parent;
            }
        }

        public Node Brother
        {
            get
            {
                if (_parent == null) return null;
                return (_parent.Child1 == this) ? _parent.Child2 : _parent.Child1;
            }
        }

        public Node Uncle
        {
            get
            {
                if (_parent == null) return null;
                if (_parent.Parent == null) return null;
                return _parent.Brother;
            }
        }

        /// <summary>
        /// 내 부모에서 나의 노드의 Index
        /// </summary>
        public int ChildIndex
        {
            get
            {
                if (_parent != null)
                {
                    if (_parent.Child1 == this) return 0;
                    if (_parent.Child2 == this) return 1;
                    return -1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public Node()
        {
            // guid 생성
            _guid = (GuidStack.Count > 0) ? GuidStack.Pop() : Node.GUID++;
            _depth = 0;
        }

        public Node(AABB aabb)
        {
            // guid 생성
            _guid = (GuidStack.Count > 0) ? GuidStack.Pop(): Node.GUID++;
            _depth = 0;

            // 노드와 AABB를 연결
            _aabb = aabb;
            //aabb.Node = this;

            // Enhance AABB 생성
            if (aabb.UseEnhanceBox)
            {
                _enhanceBox = new AABB(useEnhanceBox: true);
                RefitEnhanceBox();
            }
        }

        /// <summary>
        /// 현재노드를 부모로부터 링크를 해제한다.
        /// </summary>
        public void UnLink()
        {
            if (ExistParent)
            {
                int childIndex = ChildIndex;
                if (childIndex == 0) Parent.Child1 = null;
                if (childIndex == 1) Parent.Child2 = null;
            }
        }

        /// <summary>
        /// 현재노드를 부모로부터 링크를 해제한다.
        /// </summary>
        public void UnLinkBackCopy()
        {
            if (ExistParent)
            {
                int childIndex = ChildIndex;
                if (childIndex == 0) Parent.Left = false;
                if (childIndex == 1) Parent.Right = false;
            }
            else
            {
                // 최상위가 보이지 않는 경우
                Left = false;
                Right = false;
            }
        }

        /// <summary>
        /// 노드의 새로 생성하여 AABB, Enhance AABB만 링크를 복사한다.
        /// </summary>
        /// <returns></returns>
        public Node Clone()
        {
            Node copy = new Node();
            AABB newAABB = (AABB)_aabb.Clone();
            copy._aabb = newAABB;
            //newAABB.Node = copy;
            copy._enhanceBox = _enhanceBox;
            return copy;
        }

        /// <summary>
        /// AABB Box를 감싸고 있는 EnhanceBox에 밖으로 이동하였는지 유무를 반환한다.
        /// </summary>
        /// <param name="enhanceBox"></param>
        /// <returns></returns>
        public bool IsOutBoundToEnhanceBox()
        {
            if (_aabb.LowerBound.x < _enhanceBox.LowerBound.x) return true;
            if (_aabb.UpperBound.x > _enhanceBox.UpperBound.x) return true;
            if (_aabb.LowerBound.y < _enhanceBox.LowerBound.y) return true;
            if (_aabb.UpperBound.y > _enhanceBox.UpperBound.y) return true;
            return false;
        }

        /// <summary>
        /// 같은 중심에서 AABB Box의 크기보다 margin만큼 더 큰 EnhanceBox를 조정해준다.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="margin"></param>
        public void RefitEnhanceBox(float margin = 5.0f)
        {
            Vertex3f marginVector = new Vertex3f(margin, margin, margin);
            _enhanceBox.LowerBound = _aabb.LowerBound - marginVector;
            _enhanceBox.UpperBound = _aabb.UpperBound + marginVector;
        }

        /// <summary>
        /// 노드의 box와 insertBox의 AABB박스의 LowerBound와 UpperBound를 조정한다. 
        /// </summary>
        /// <param name="insertBox"></param>
        public void Refit(AABB insertBox)
        {
            Vertex3f lower = Vertex3f.Min(new Vertex3f[] { _aabb.LowerBound, insertBox.LowerBound });
            Vertex3f upper = Vertex3f.Max(new Vertex3f[] { _aabb.UpperBound, insertBox.UpperBound });
            _aabb.LowerBound = lower;
            _aabb.UpperBound = upper;
        }

        /// <summary>
        /// 제거할 자식의 자리(왼쪽 또는 오른쪽)에 새롭게 붙일 자식을 그 자리에 붙인다.
        /// </summary>
        /// <param name="removeChild"></param>
        /// <param name="attachNode"></param>
        /// <returns></returns>
        public bool ReplaceChild(Node removeChild, Node attachNode)
        {
            if (_child1 == removeChild)
            {
                _child1 = attachNode;
                attachNode.Parent = this;
                return true;
            }

            if (_child2 == removeChild)
            {
                _child2 = attachNode;
                attachNode.Parent = this;
                return true;
            }
            return false;
        }

        public void Print(string txt = "")
        {
            if (IsLeaf)
            {
                System.Console.WriteLine($"{txt}[{Guid}]");
            }
            else
            {
                _child1?.Print($"{txt}({Guid})↗");
                _child2?.Print($"{txt}({Guid})↘");
            }
        }
    }
}
