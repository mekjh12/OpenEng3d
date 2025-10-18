using OpenGL;
using System;
using System.Collections.Generic;
using Ui3d;

namespace Animate
{
    /// <summary>
    /// 3D 애니메이션 시스템의 뼈대(Bone) 클래스
    /// 캐릭터의 골격을 구성하는 기본 요소로, 계층 구조를 형성하여 애니메이션을 구현한다.
    /// </summary>
    public partial class Bone
    {
        // 기본 정보
        private int _index;                     // 뼈대의 고유 인덱스
        private string _name;                   // 뼈대의 이름
        private string _id;                     // 뼈대의 고유 ID (외부 시스템과의 연동용)
        private float _length = 1.0f;     // 뼈대의 길이        

        // 계층 구조
        private List<Bone> _children;           // 자식 뼈대들
        private Bone _parent;                   // 부모 뼈대 (null이면 루트 뼈대)
        private bool _isHipBone = false;        // 힙본 여부 (캐릭터의 중심 뼈대인지 여부)
        private BoneMatrixSet _boneMatrixSet;   // 뼈대의 변환 정보 (애니메이션 및 바인딩 포즈 변환 행렬들)
        private JointAngle _jointAngle;         // 관절 각도 제한 (옵션)

        // 캐릭터 공간 변환 행렬(성능 최적화용)
        Matrix4x4f _rootTransform;              // 캐릭터 공간의 본 변환 행렬
        Matrix4x4f _parentRootTransform;        // 부모 뼈대의 캐릭터 공간 본 변환 행렬(**자신의 본 변환 행렬 계산에 사용**)

        // 속성
        public BoneMatrixSet BoneMatrixSet => _boneMatrixSet;
        public JointAngle JointAngle { get => _jointAngle; set => _jointAngle = value; }
        public bool IsHipBone { get => _isHipBone; set => _isHipBone = value; }
        public string ID { get => _id; set => _id = value; }
        public int Index { get => _index; set => _index = value; }
        public string Name { get => _name; set => _name = value; }
        public Bone Parent { get => _parent; set => _parent = value; }
        public IReadOnlyList<Bone> Children => _children.AsReadOnly();
        public bool IsLeaf => _children.Count == 0;
        public bool IsRoot => _parent == null;
        public float Length { get => _length; set => _length = value; }

        public TextNamePlate TextNamePlate { get; set; } = null;

        /// <summary>
        /// 새로운 뼈대를 생성한다
        /// </summary>
        /// <param name="name">뼈대 이름</param>
        /// <param name="index">뼈대 인덱스</param>
        /// <exception cref="ArgumentException">이름이 null 또는 빈 문자열인 경우</exception>
        /// <exception cref="ArgumentOutOfRangeException">인덱스가 음수인 경우</exception>
        public Bone(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("뼈대 이름은 null이거나 빈 문자열일 수 없습니다.", nameof(name));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "뼈대 인덱스는 0 이상이어야 합니다.");

            _children = new List<Bone>();
            _name = name;
            _index = index;
            _boneMatrixSet = new BoneMatrixSet();
            _jointAngle = null;
        }

        public void AttachJointAngle(JointAngle jointAngle)
        {
            _jointAngle = jointAngle;
        }

        /// <summary>
        /// 자식 뼈대를 추가한다
        /// 부모-자식 관계를 양방향으로 설정한다
        /// </summary>
        /// <param name="child">추가할 자식 뼈대</param>
        /// <exception cref="ArgumentNullException">자식 뼈대가 null인 경우</exception>
        /// <exception cref="InvalidOperationException">자식 뼈대가 이미 다른 부모를 가진 경우</exception>
        public void AddChild(Bone child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != null && child.Parent != this)
                throw new InvalidOperationException("자식 뼈대가 이미 다른 부모를 가지고 있습니다.");

            if (!_children.Contains(child))
            {
                _children.Add(child);
                child.Parent = this;
            }
        }

        /// <summary>
        /// 자식 뼈대를 제거한다
        /// </summary>
        /// <param name="child">제거할 자식 뼈대</param>
        /// <returns>성공적으로 제거되었으면 true, 그렇지 않으면 false</returns>
        public bool RemoveChild(Bone child)
        {
            if (child == null) return false;

            if (_children.Remove(child))
            {
                child.Parent = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 로컬 변환 행렬로부터 캐릭터 공간의 애니메이션 변환 행렬을 계산하고 자식 뼈대들에게 전파한다.
        /// </summary>
        /// <param name="isSelfIncluded">현재 뼈대부터 업데이트할지 여부 (true: 자신 포함, false: 자식들만)</param>
        public void UpdateAnimatorTransforms(Animator animator, bool isSelfIncluded = false)
        {
            foreach (Bone bone in this.ToBFSList(isSelfIncluded ? null: this))
            {
                int index = bone.Index;

                // 부모와 자신의 본 변환 행렬을 가져온다
                _parentRootTransform = animator.GetRootTransform(bone.Parent);
                _rootTransform = animator.GetRootTransform(bone);

                // 자신의 본 변환 행렬을 계산한다
                if (bone.Parent == null)
                {
                    animator.SetRootTransform(index, Matrix4x4f.Identity);
                }
                else
                {
                    animator.SetRootTransform(index, _parentRootTransform * bone.BoneMatrixSet.LocalTransform);
                }

                // 자신의 애니메이션 변환 행렬을 계산한다
                _rootTransform = animator.GetRootTransform(bone);
                animator.SetAnimatedTransform(index, _rootTransform * bone.BoneMatrixSet.InverseBindPoseTransform);
            }
        }


    }
}