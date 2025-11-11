using Geometry;
using OpenGL;
using System.Collections.Generic;

namespace Occlusion
{
    /// <summary>
    /// BVH (Bounding Volume Hierarchy) 트리의 노드
    /// AABB3f 구조체 사용으로 메모리 효율 및 성능 최적화
    /// </summary>
    public class Node3f
    {
        public static int GUID = -1;
        public static Stack<int> _guidStack = new Stack<int>(); // 제거한 노드의 guid를 재사용

        int _guid;
        Node3f _parent;
        Node3f _child1;
        Node3f _child2;
        bool _left = true;
        bool _right = true;

        // ✅ 클래스 → 구조체로 변경
        AABB3f _aabb;
        AABB3f _enhanceBox;
        bool _useEnhanceBox; // EnhanceBox 사용 여부 플래그

        List<Node3f> _lowerNodeList;

        float _inheritedCost;
        string _txt;
        uint _depth;

        #region 속성

        /// <summary>
        /// 원본 링크를 손상하지 않기 위한 왼쪽 자식노드 연결 유무
        /// </summary>
        public bool Left
        {
            get => _left;
            set => _left = value;
        }

        /// <summary>
        /// 원본 링크를 손상하지 않기 위한 오른쪽 자식노드 연결 유무
        /// </summary>
        public bool Right
        {
            get => _right;
            set => _right = value;
        }

        public List<Node3f> LowerNodeList
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

        public string Value => $"{_aabb.Min} {_aabb.Max}";

        public int Child1_GUID => (_child1 == null) ? -1 : _child1.Guid;

        public int Child2_GUID => (_child2 == null) ? -1 : _child2.Guid;

        /// <summary>
        /// 루트노드에서 현재 노드까지의 ∑{△SA(P_i)}
        /// </summary>
        public float InheritedCost
        {
            get => _inheritedCost;
            set => _inheritedCost = value;
        }

        /// <summary>
        /// AABB 구조체 (읽기 전용 참조 반환)
        /// </summary>
        public ref readonly AABB3f AABB => ref _aabb;

        /// <summary>
        /// EnhanceBox 구조체 (읽기 전용 참조 반환)
        /// </summary>
        public ref readonly AABB3f EnhanceBox => ref _enhanceBox;

        public bool UseEnhanceBox => _useEnhanceBox;

        public bool IsRoot => (_parent == null);

        public bool IsLeaf => (_child1 == null && _child2 == null);

        public bool HasChild => (_child1 != null || _child2 != null);

        public bool HasChild1 => (_child1 != null);

        public bool HasChild2 => (_child2 != null);

        public Node3f Child1
        {
            get => _child1;
            set => _child1 = value;
        }

        public Node3f Child2
        {
            get => _child2;
            set => _child2 = value;
        }

        public Node3f Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Node3f GrandParent
        {
            get
            {
                if (_parent == null) return null;
                if (_parent.Parent == null) return null;
                return _parent.Parent;
            }
        }

        public Node3f Brother
        {
            get
            {
                if (_parent == null) return null;
                return (_parent.Child1 == this) ? _parent.Child2 : _parent.Child1;
            }
        }

        public Node3f Uncle
        {
            get
            {
                if (_parent == null) return null;
                if (_parent.Parent == null) return null;
                return _parent.Brother;
            }
        }

        /// <summary>
        /// 부모에서 나의 노드 인덱스
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

        #endregion

        #region 생성자

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Node3f()
        {
            _guid = (_guidStack.Count > 0) ? _guidStack.Pop() : Node3f.GUID++;
            _depth = 0;
            _useEnhanceBox = false;
        }

        /// <summary>
        /// AABB3f로 노드 생성
        /// </summary>
        public Node3f(in AABB3f aabb, bool useEnhanceBox = false)
        {
            _guid = (_guidStack.Count > 0) ? _guidStack.Pop() : Node3f.GUID++;
            _depth = 0;
            _aabb = aabb;
            _useEnhanceBox = useEnhanceBox;

            // Enhance AABB 생성
            if (_useEnhanceBox)
            {
                RefitEnhanceBox();
            }
        }

        /// <summary>
        /// Min/Max 좌표로 노드 생성
        /// </summary>
        public Node3f(Vertex3f min, Vertex3f max, bool useEnhanceBox = false)
            : this(new AABB3f(min, max), useEnhanceBox)
        {
        }

        #endregion

        #region 링크 관리

        /// <summary>
        /// 현재 노드를 부모로부터 링크 해제
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
        /// 백업 복사용 링크 해제
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
        /// 제거할 자식의 자리에 새 자식을 붙임
        /// </summary>
        public bool ReplaceChild(Node3f removeChild, Node3f attachNode)
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

        #endregion

        #region AABB 관리

        /// <summary>
        /// AABB 설정 (구조체이므로 값 복사)
        /// </summary>
        public void SetAABB(in AABB3f aabb)
        {
            _aabb = aabb;

            if (_useEnhanceBox)
            {
                RefitEnhanceBox();
            }
        }

        /// <summary>
        /// AABB 수정 (ref로 직접 접근)
        /// </summary>
        public ref AABB3f GetAABBRef()
        {
            return ref _aabb;
        }

        /// <summary>
        /// 노드의 AABB와 삽입 AABB를 포함하도록 확장
        /// </summary>
        public void Refit(in AABB3f insertBox)
        {
            _aabb = AABB3fHelper.Union(in _aabb, in insertBox);
        }

        /// <summary>
        /// 자식들의 AABB를 기반으로 현재 노드 AABB 재계산
        /// </summary>
        public void RefitFromChildren()
        {
            if (!HasChild) return;

            if (HasChild1 && HasChild2)
            {
                _aabb = AABB3fHelper.Union(in _child1._aabb, in _child2._aabb);
            }
            else if (HasChild1)
            {
                _aabb = _child1._aabb;
            }
            else if (HasChild2)
            {
                _aabb = _child2._aabb;
            }
        }

        #endregion

        #region EnhanceBox 관리

        /// <summary>
        /// AABB보다 margin만큼 큰 EnhanceBox 조정
        /// </summary>
        public void RefitEnhanceBox(float margin = 5.0f)
        {
            if (!_useEnhanceBox) return;

            _enhanceBox = AABB3fHelper.Expand(in _aabb, margin);
        }

        /// <summary>
        /// AABB가 EnhanceBox 밖으로 나갔는지 확인
        /// </summary>
        public bool IsOutBoundToEnhanceBox()
        {
            if (!_useEnhanceBox) return false;

            // AABB가 EnhanceBox를 벗어났는지 검사
            return _aabb.Min.x < _enhanceBox.Min.x ||
                   _aabb.Max.x > _enhanceBox.Max.x ||
                   _aabb.Min.y < _enhanceBox.Min.y ||
                   _aabb.Max.y > _enhanceBox.Max.y ||
                   _aabb.Min.z < _enhanceBox.Min.z ||
                   _aabb.Max.z > _enhanceBox.Max.z;
        }

        #endregion

        #region 복사 및 유틸리티

        /// <summary>
        /// 노드 복사 (AABB와 EnhanceBox만 복사)
        /// </summary>
        public Node3f Clone()
        {
            Node3f copy = new Node3f();
            copy._aabb = _aabb;  // 구조체이므로 값 복사
            copy._enhanceBox = _enhanceBox;
            copy._useEnhanceBox = _useEnhanceBox;
            return copy;
        }

        /// <summary>
        /// 트리 구조 출력 (디버깅용)
        /// </summary>
        public void Print(string txt = "")
        {
            if (IsLeaf)
            {
                System.Console.WriteLine($"{txt}[{Guid}] Center: {_aabb.Center}");
            }
            else
            {
                _child1?.Print($"{txt}({Guid})↗");
                _child2?.Print($"{txt}({Guid})↘");
            }
        }

        #endregion

        #region 충돌 및 쿼리 헬퍼

        /// <summary>
        /// 점이 노드의 AABB 내부에 있는지 검사
        /// </summary>
        public bool Contains(Vertex3f point)
        {
            return AABB3fHelper.Contains(in _aabb, point);
        }

        /// <summary>
        /// 다른 AABB와 교차하는지 검사
        /// </summary>
        public bool Intersects(in AABB3f other)
        {
            return AABB3fHelper.Intersects(in _aabb, in other);
        }

        /// <summary>
        /// 광선과 교차하는지 검사
        /// </summary>
        public bool IntersectsRay(Vertex3f rayOrigin, Vertex3f rayDirection, out float tMin, out float tMax)
        {
            return AABB3fHelper.IntersectsRay(in _aabb, rayOrigin, rayDirection, out tMin, out tMax);
        }

        /// <summary>
        /// 표면적 계산 (SAH - Surface Area Heuristic)
        /// </summary>
        public float GetSurfaceArea()
        {
            return AABB3fHelper.GetSurfaceArea(in _aabb);
        }

        /// <summary>
        /// 부피 계산
        /// </summary>
        public float GetVolume()
        {
            return AABB3fHelper.GetVolume(in _aabb);
        }

        #endregion
    }
}