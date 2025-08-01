using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// 애니메이션을 관리하고 업데이트하는 클래스
    /// </summary>
    public class Animator
    {
        /// <summary>
        /// 애니메이션 상태를 나타내는 열거형
        /// </summary>
        private enum AnimationState
        {
            Normal,     // 일반 모션 재생
            Blending    // 모션 블렌딩 중
        }

        private const int MAX_BONES_COUNT = 128;
        private const string SWITCH_MOTION_NAME = "switchMotion";
        private const float MIN_MOTION_TIME = 0.0f;

        // 멤버 변수 (Update함수를 통해서 행렬이 업데이트된다.)
        private Matrix4x4f[] _animatedTransforms; // 애니메이션된 행렬
        private Matrix4x4f[] _rootTransforms ; // 뼈대의 캐릭터공간 변환 행렬들

        private float _motionTime = 0.0f; // 현재 모션 시간
        private bool _isPlaying = true; // 재생 상태

        // 모션 관련 변수
        private AnimationState _animationState = AnimationState.Normal; // 현재 애니메이션 상태
        private Motion _currentMotion; // 현재 모션
        private Motion _blendMotion; // 블렌딩 모션
        private Motion _nextMotion; // 다음 모션

        private Bone _rootBone; // 루트 본

        // 클래스내 처리 변수
        private Action _actionOnceFinished = null; // 한번 실행 완료 콜백

        // 최적화용 변수
        Dictionary<string, Matrix4x4f> _currentPose;

        // 초기화 시 한 번만 생성되는 순회 순서 배열
        private Bone[] _boneTraversalOrder;
        private Matrix4x4f[] _parentTransforms; // 각 본의 부모 변환을 미리 계산해둘 배열
        private int[] _parentIndices; // 각 본의 부모 인덱스

        // Identity 행렬 재사용
        private readonly Matrix4x4f _identityMatrix = Matrix4x4f.Identity;

        /// <summary>
        /// 애니메이션이 적용된 최종 행렬로서 스키닝행렬이다. 
        /// <code>
        /// - 이는 각 뼈대의 애니메이션 변환 행렬에 역바인드포즈를 적용한 것이다.
        /// </code>
        /// </summary>
        public Matrix4x4f[] AnimatedTransforms => _animatedTransforms;

        /// <summary>
        /// 캐릭터 공간에서 변환되는 뼈대의 애니메이션 변환 행렬들.
        /// </summary>
        public Matrix4x4f[] RootTransforms => _rootTransforms;

        /// <summary>
        /// 한번 실행 완료 시 호출될 콜백 액션을 설정한다.
        /// </summary>
        public Action OnceFinished
        {
            set => _actionOnceFinished = value;
        }

        public Motion CurrentMotion => _currentMotion;
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 현재 모션의 재생 시간을 가져오거나 설정한다.
        /// </summary>
        public float MotionTime
        {
            get => _motionTime;
            set => _motionTime = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="rootBone">루트 본</param>
        public Animator(Bone rootBone)
        {
            _rootBone = rootBone ?? throw new ArgumentNullException(nameof(rootBone));

            _currentPose = new Dictionary<string, Matrix4x4f>();

            _animatedTransforms = new Matrix4x4f[MAX_BONES_COUNT];
            _rootTransforms = new Matrix4x4f[MAX_BONES_COUNT];

            // ✅ 초기화 시 순회 순서를 미리 계산
            BuildBoneTraversalOrder(rootBone);

        }

        /// <summary>
        /// 초기화 시 한 번만 실행 - 순회 순서를 미리 계산
        /// </summary>
        private void BuildBoneTraversalOrder(Bone rootBone)
        {
            var boneList = new List<Bone>();
            var parentIndexList = new List<int>();

            // 큐를 사용하여 순회 순서 결정 (초기화 시에만)
            var queue = new Queue<(Bone bone, int parentIndex)>();
            queue.Enqueue((rootBone, -1)); // 루트는 부모가 없음

            while (queue.Count > 0)
            {
                var (bone, parentIndex) = queue.Dequeue();

                boneList.Add(bone);
                parentIndexList.Add(parentIndex);

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

        public Matrix4x4f GetRootTransform(Bone bone)
        {
            return _rootTransforms[bone.Index];
        }

        public void SetRootTransform(int index, Matrix4x4f transform)
        {
            _rootTransforms[index] = transform;
        }

        public Matrix4x4f GetAnimatedTransform(Bone bone)
        {
            return _animatedTransforms[bone.Index];
        }

        public void SetAnimatedTransform(int index, Matrix4x4f transform)
        {
            _animatedTransforms[index] = transform;
        }

        /// <summary>
        /// 모션을 지정한다.
        /// </summary>
        /// <param name="motion">설정할 모션</param>
        /// <param name="blendingInterval">블렌딩 시간(초)</param>
        public void SetMotion(Motion motion, MotionCache motionCache, float blendingInterval = 0.2f)
        {
            // 모션이 null인 경우 예외를 발생시킨다.
            if (motion == null)
                throw new ArgumentNullException(nameof(motion));

            // 블렌딩 간격이 음수인 경우 0으로 설정
            if (blendingInterval < 0) blendingInterval = 0.0f; 
            
            // 현재 모션이 null이면 새로운 모션을 설정하고,
            if (_currentMotion == null)
            {
                _currentMotion = motion;
                _animationState = AnimationState.Normal;
            }
            // 아니면 블렌딩 모션을 설정한다.
            else
            {
                // 블렌딩 시간이 작으면 바로 다음 모션으로
                if (blendingInterval < 0.005f)
                {
                    _currentMotion = motion;
                    _animationState = AnimationState.Normal;
                }
                // 블렌딩 시간이 있으면
                else
                {
                    string blendMotionName = $"{_currentMotion.Name}\t{motion.Name}";

                    _blendMotion = motionCache.GetMotionFromCache(blendMotionName);
                    if (_blendMotion == null)
                    {
                        _blendMotion = MotionBlend.BlendMotion(blendMotionName, _currentMotion, _motionTime, motion, 0.0f, blendingInterval);
                        motionCache.AddMotionToCache(_blendMotion);
                        Console.WriteLine("cache 추가");
                    }
                    _currentMotion = _blendMotion;
                    _nextMotion = motion;
                    _animationState = AnimationState.Blending;
                }
            }

            _motionTime = 0;
        }

        /// <summary>
        /// 애니메이션을 재생한다.
        /// </summary>
        public void Play()
        {
            _isPlaying = true;
        }

        /// <summary>
        /// 애니메이션을 정지한다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 애니메이션 재생/정지를 토글한다.
        /// </summary>
        public void Toggle()
        {
            _isPlaying = !_isPlaying;
        }

        /// <summary>
        /// 애니메이션을 업데이트한다.
        /// </summary>
        /// <param name="deltaTime">델타 타임</param>
        public void Update(float deltaTime)
        {
            // 현재 모션이 없으면 업데이트를 하지 않는다.
            if (_currentMotion == null) return;

            // 델타 타임이 음수인 경우 0으로 설정한다.
            if (deltaTime < 0) deltaTime = MIN_MOTION_TIME;

            // 현재 모션이 null인 경우 업데이트를 하지 않는다.
            if (_currentMotion == null) return;

            // 모션이 재생중이 아니면 시간을 업데이트하지 않는다.
            if (_isPlaying)
            {
                // 모션 시간을 업데이트한다.
                _motionTime += deltaTime;

                // 모션의 재생이 역인 경우에 마이너스 시간을 조정한다.
                if (_motionTime < 0) _motionTime = _currentMotion.Length;

                // 모션 완료 시
                if (_motionTime >= _currentMotion.Length)
                {
                    _motionTime = 0.0f;

                    // 모션을 한 번만 실행되는 경우 콜백을 호출한다.
                    if (_actionOnceFinished != null)
                    {
                        _actionOnceFinished();
                    }
                    else
                    {
                        // 블렌딩 모션이 완료되면
                        if (_animationState == AnimationState.Blending)
                        {
                            _animationState = AnimationState.Normal;
                            _currentMotion = _nextMotion;
                            _nextMotion = null;
                        }
                    }
                }
            }

            // 모션의 현재 시간에 맞는 애니메이션 최종 행렬을 루트본에 의하여 계층적으로 업데이트한다.
            UpdateAnimationTransforms(_motionTime, _rootBone);
        }

        /// <summary>
        /// 애니메이션 변환 행렬을 업데이트한다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="rootBone">루트 본</param>
        /// <summary>
        /// 매 프레임마다 실행 - 순회 순서에 따라 빠르게 처리
        /// </summary>
        private void UpdateAnimationTransforms(float motionTime, Bone rootBone)
        {
            // 키프레임으로부터 현재의 로컬포즈행렬을 가져온다.
            if (!_currentMotion.InterpolatePoseAtTime(motionTime, _currentPose))
            {
                return; // 실패시 처리
            }

            // ✅ 미리 계산된 순회 순서로 처리 - GC 없음, 큐 없음
            for (int i = 0; i < _boneTraversalOrder.Length; i++)
            {
                Bone bone = _boneTraversalOrder[i];
                int boneIndex = bone.Index;
                if (boneIndex < 0) continue;

                // 부모 변환 가져오기
                Matrix4x4f parentTransform = _parentIndices[i] == -1 ?
                    _identityMatrix : // 루트 본
                    _rootTransforms[_boneTraversalOrder[_parentIndices[i]].Index]; // 부모 본의 변환

                // 현재 포즈 처리
                bone.BoneTransforms.LocalTransform =
                    (_currentPose != null && _currentPose.TryGetValue(bone.Name, out Matrix4x4f poseTransform)) ?
                    poseTransform : bone.BoneTransforms.LocalBindTransform;

                // 행렬 계산
                _rootTransforms[boneIndex] = parentTransform * bone.BoneTransforms.LocalTransform;
                _animatedTransforms[boneIndex] = _rootTransforms[boneIndex] * bone.BoneTransforms.InverseBindPoseTransform;
            }
        }
    }
}