using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 3D 애니메이션 시스템의 뼈대(Bone) 클래스
    /// 캐릭터의 골격을 구성하는 기본 요소로, 계층 구조를 형성하여 애니메이션을 구현한다.
    /// </summary>
    public class Bone
    {
        private const float DEFAULT_BONE_LENGTH = 15.0f;            // 자식이 없는 뼈대의 기본 길이 (Y축 방향)
        private const string ARMATURE_HIPS_NAME = "mixamorig_Hips"; // Mixamo 리그에서 엉덩이(Hips) 뼈대의 이름

        // 기본 정보
        private int _index;
        private string _name;

        // 계층 구조
        private List<Bone> _children;
        private Bone _parent;

        private BoneTransforms _boneTransforms; // 뼈대의 변환 정보 (애니메이션 및 바인딩 포즈 변환 행렬들)
        private BoneKinematics _boneKinematics; // 뼈대의 운동학 정보 (추가 기능)

        /// <summary>
        /// 뼈대의 운동학 정보를 포함하는 객체
        /// </summary>
        public BoneKinematics BoneKinematics => _boneKinematics;

        /// <summary>
        /// 뼈대의 변환 정보를 포함하는 객체
        /// </summary>
        public BoneTransforms BoneTransforms => _boneTransforms;

        /// <summary>
        /// 뼈대의 고유 인덱스
        /// </summary>
        public int Index
        {
            get => _index;
            set => _index = value;
        }

        /// <summary>
        /// 뼈대의 이름
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// 부모 뼈대 (null이면 루트 뼈대)
        /// </summary>
        public Bone Parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>
        /// 자식 뼈대들의 리스트 (읽기 전용)
        /// </summary>
        public IReadOnlyList<Bone> Children => _children.AsReadOnly();

        /// <summary>
        /// 자식이 없는 말단 뼈대인지 여부
        /// </summary>
        public bool IsLeaf => _children.Count == 0;

        /// <summary>
        /// 루트 뼈대인지 여부 (부모가 없는 뼈대)
        /// </summary>
        public bool IsRoot => _parent == null;

        /// <summary>
        /// Mixamo 리그의 엉덩이(Hips) 뼈대인지 여부
        /// </summary>
        public bool IsHipBone => _name == ARMATURE_HIPS_NAME;

        /// <summary>
        /// 캐릭터 공간에서의 뼈대 시작점(피봇) 위치
        /// </summary>
        public Vertex3f PivotPosition
        {
            get => BoneTransforms.AnimatedTransform.Position;
            set
            {
                Matrix4x4f mat = BoneTransforms.AnimatedTransform;

                mat[3, 0] = value.x;
                mat[3, 1] = value.y;
                mat[3, 2] = value.z;

                BoneTransforms.AnimatedTransform = mat;
            }
        }

        /// <summary>
        /// 캐릭터 공간에서의 뼈대 끝점 위치
        /// 자식이 없으면 기본 길이만큼 Y축으로 연장된 위치를 반환
        /// 자식이 있으면 자식들의 평균 위치를 반환
        /// </summary>
        public Vertex3f TipPosition
        {
            get
            {
                if (IsLeaf)
                {
                    Matrix4x4f extendedTransform = _boneTransforms.AnimatedTransform * Matrix4x4f.Translated(0, DEFAULT_BONE_LENGTH, 0);
                    return extendedTransform.Position;
                }
                else
                {
                    Vertex3f averagePosition = Vertex3f.Zero;
                    foreach (Bone child in _children)
                    {
                        averagePosition += child.BoneTransforms.AnimatedTransform.Position;
                    }
                    return averagePosition * (1.0f / _children.Count);
                }
            }
        }

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
            _boneTransforms = new BoneTransforms();
            _boneKinematics = new BoneKinematics(); // 뼈대의 운동학 정보 초기화

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
        /// <param name="exceptBone">업데이트에서 제외할 뼈대 (null이면 모든 뼈대 업데이트)</param>
        public void UpdatePropagateTransform(bool isSelfIncluded = false, Bone exceptBone = null)
        {
            // 깊이 우선 탐색(DFS)을 위한 스택 생성
            Stack<Bone> stack = new Stack<Bone>();

            // 시작점 설정: 자신 포함 여부에 따라 초기 스택 구성
            if (isSelfIncluded)
            {
                stack.Push(this); // 현재 뼈대부터 시작
            }
            else
            {
                // 현재 뼈대는 제외하고 직계 자식들부터 시작
                foreach (Bone childBone in _children)
                    stack.Push(childBone);
            }

            // 스택 기반 반복으로 모든 하위 뼈대 순회
            while (stack.Count > 0)
            {
                Bone currentBone = stack.Pop();

                // 제외 대상 뼈대는 건너뛰기
                if (currentBone == exceptBone) continue;

                // 애니메이션 변환 행렬 계산: LocalTransform을 부모의 월드 변환과 결합
                // 공식: AnimatedTransform = Parent.AnimatedTransform * LocalTransform
                // 루트 뼈대의 경우 부모가 없으므로 LocalTransform을 그대로 사용
                currentBone.BoneTransforms.AnimatedTransform = currentBone.Parent == null
                    ? currentBone.BoneTransforms.LocalTransform
                    : currentBone.Parent.BoneTransforms.AnimatedTransform * currentBone.BoneTransforms.LocalTransform;

                // 현재 뼈대의 모든 자식들을 스택에 추가하여 계속 순회
                foreach (Bone childBone in currentBone.Children)
                    stack.Push(childBone);
            }
        }
    }
}