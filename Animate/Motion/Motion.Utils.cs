using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public partial class Motion
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        float _footStepDistance = 0.0f;                 // 두 발자국 사이의 거리
        float _speed = 0.0f;                            // 모션의 속도 (초당 이동 거리)
        bool _isApplySpeed = true;                      // 모션의 속도를 적용할지 여부
        FootStepAnalyzer.MovementType _movementType;    // 모션의 이동 타입 (걷기, 달리기 등)

        // 초기화 시 한 번만 생성되는 순회 순서 배열
        private Bone[] _boneTraversalOrder;
        private Matrix4x4f[] _parentTransforms; // 각 본의 부모 변환을 미리 계산해둘 배열
        private int[] _parentIndices; // 각 본의 부모 인덱스
        // 멤버 변수 (Update함수를 통해서 행렬이 업데이트된다.)
        private Matrix4x4f[] _animatedTransforms; // 애니메이션된 행렬
        private Matrix4x4f[] _rootTransforms; // 뼈대의 캐릭터공간 변환 행렬들
        private Dictionary<string, int> _boneIndices;
        // Identity 행렬 재사용
        private readonly Matrix4x4f _identityMatrix = Matrix4x4f.Identity;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------
            
        public bool IsApplySpeed
        {
            get => _isApplySpeed; 
            set => _isApplySpeed = value;
        }

        /// <summary>
        /// 모션의 속도 (초당 이동 거리)
        /// </summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        /// <summary>
        /// 발자국 사이의 거리 (두 발자국 사이의 거리)
        /// </summary>
        public float FootStepDistance
        {
            get => _footStepDistance;
            set => _footStepDistance = value;
        }

        public FootStepAnalyzer.MovementType MovementType
        {
            get => _movementType; 
            set => _movementType = value;
        }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        private void UpdateAnimationTransforms(float motionTime, Bone rootBone)
        {
            if (_animatedTransforms == null) _animatedTransforms = new Matrix4x4f[_boneTraversalOrder.Length];
            if (_rootTransforms == null) _rootTransforms = new Matrix4x4f[_boneTraversalOrder.Length];
            if (_currentPose == null) _currentPose = new Dictionary<string, Matrix4x4f>();

            if (!InterpolatePoseAtTime(motionTime, ref _currentPose))
            {
                return; // 실패시 처리
            }

            // _currentPose로부터 현재 포즈를 가져온다.
            for (int i = 0; i < _boneTraversalOrder.Length; i++)
            {
                Bone bone = _boneTraversalOrder[i];
                int boneIndex = bone.Index;
                if (boneIndex < 0) continue;

                // 부모 변환 가져오기
                Matrix4x4f parentTransform = _parentIndices[i] == -1 ?
                    _identityMatrix : // 루트 본
                    _rootTransforms[_boneTraversalOrder[_parentIndices[i]].Index]; // 부모 본의 변환

                // 현재 포즈로부터 본의 로컬 변환을 가져온다.
                bone.BoneTransforms.LocalTransform =
                    (_currentPose != null && _currentPose.TryGetValue(bone.Name, out Matrix4x4f poseTransform)) ?
                    poseTransform : bone.BoneTransforms.LocalBindTransform;

                // 행렬 계산
                _rootTransforms[boneIndex] = parentTransform * bone.BoneTransforms.LocalTransform;
                _animatedTransforms[boneIndex] = _rootTransforms[boneIndex] * bone.BoneTransforms.InverseBindPoseTransform;
            }
        }

        /// <summary>
        /// 초기화 시 한 번만 실행 - 순회 순서를 미리 계산
        /// </summary>
        private void BuildBoneTraversalOrder(Bone rootBone)
        {
            var boneList = new List<Bone>();
            var parentIndexList = new List<int>();

            if (_boneIndices == null)
            {
                _boneIndices = new Dictionary<string, int>(MAX_BONES_COUNT);
            }
            else
            {
                _boneIndices.Clear(); // 기존 인덱스 초기화
            }

            // 큐를 사용하여 순회 순서 결정 (초기화 시에만)
            var queue = new Queue<(Bone bone, int parentIndex)>();
            queue.Enqueue((rootBone, -1)); // 루트는 부모가 없음

            while (queue.Count > 0)
            {
                var (bone, parentIndex) = queue.Dequeue();

                boneList.Add(bone);
                parentIndexList.Add(parentIndex);
                _boneIndices[bone.Name] = bone.Index;

                // 자식들을 큐에 추가
                foreach (var child in bone.Children)
                {
                    queue.Enqueue((child, boneList.Count - 1)); // 현재 본이 자식의 부모
                }
            }

            // 배열로 변환 (한 번만)
            _boneTraversalOrder = boneList.ToArray();
            _parentIndices = parentIndexList.ToArray();
            _parentTransforms = new Matrix4x4f[_boneTraversalOrder.Length];
        }
    }
}
