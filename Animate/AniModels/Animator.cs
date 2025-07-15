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

        // 멤버 변수
        private Matrix4x4f[] _animatedTransforms = new Matrix4x4f[MAX_BONES_COUNT]; // 애니메이션된 행렬      
        private float _motionTime = 0.0f; // 현재 모션 시간
        private bool _isPlaying = true; // 재생 상태

        // 모션 관련 변수
        private Motion _currentMotion; // 현재 모션
        private Motion _blendMotion; // 블렌딩 모션
        private Motion _nextMotion; // 다음 모션
        private AnimationState _animationState = AnimationState.Normal; // 현재 애니메이션 상태


        private Bone _rootBone; // 루트 본

        // 클래스내 처리 변수
        private Action _actionOnceFinished = null; // 한번 실행 완료 콜백
        private Stack<(Bone, Matrix4x4f)> _boneStack = new Stack<(Bone bone, Matrix4x4f parentTransform)>(); // 뼈대트리탐색용 스택

        /// <summary>
        /// 애니메이션이 적용된 최종 행렬들(뼈대의 개수만큼, 애니메이션이 적용된 최종 행렬들)
        /// Update함수를 통해서 행렬이 업데이트된다.
        /// </summary>
        public Matrix4x4f[] AnimatedTransforms => _animatedTransforms;

        /// <summary>
        /// 한번 실행 완료 시 호출될 콜백 액션을 설정한다.
        /// </summary>
        public Action OnceFinished
        {
            set => _actionOnceFinished = value;
        }

        /// <summary>
        /// 현재 실행 중인 모션을 가져온다.
        /// </summary>
        public Motion CurrentMotion => _currentMotion;

        /// <summary>
        /// 애니메이션 재생 상태를 가져온다.
        /// </summary>
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
        }

        /// <summary>
        /// 모션을 지정한다.
        /// </summary>
        /// <param name="motion">설정할 모션</param>
        /// <param name="blendingInterval">블렌딩 시간</param>
        public void SetMotion(Motion motion, float blendingInterval = 0.2f)
        {
            // 모션이 null인 경우 예외를 발생시킨다.
            if (motion == null)
                throw new ArgumentNullException(nameof(motion));

            // 블렌딩 간격이 음수인 경우 0으로 설정
            if (blendingInterval < 0) blendingInterval = 0.0f; 
            
            // 현재 모션이 null이면 새로운 모션을 설정하고, 아니면 블렌딩 모션을 설정한다.
            if (_currentMotion == null)
            {
                _currentMotion = motion;
                _animationState = AnimationState.Normal;
            }
            else
            {
                _blendMotion = Motion.BlendMotion(SWITCH_MOTION_NAME, _currentMotion, _motionTime, motion, 0.0f, blendingInterval);
                _currentMotion = _blendMotion;
                _nextMotion = motion;
                _animationState = AnimationState.Blending;
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
                UpdateMotionTime(deltaTime);
                HandleMotionCompletion();
            }

            // 모션의 현재 시간에 맞는 애니메이션 최종 행렬을 루트본에 의하여 계층적으로 업데이트한다.
            UpdateAnimationTransforms(_motionTime, _rootBone);
        }

        /// <summary>
        /// 모션 시간을 업데이트한다.
        /// </summary>
        /// <param name="deltaTime">델타 타임</param>
        private void UpdateMotionTime(float deltaTime)
        {
            _motionTime += deltaTime;

            // 모션의 재생이 역인 경우에 마이너스 시간을 조정한다.
            if (_motionTime < 0) _motionTime = _currentMotion.Length;
        }

        /// <summary>
        /// 모션 완료 시 처리를 담당한다.
        /// </summary>
        private void HandleMotionCompletion()
        {
            if (_motionTime >= _currentMotion.Length)
            {
                _motionTime = 0.0f;

                if (_animationState == AnimationState.Blending)
                {
                    _currentMotion = _nextMotion;
                    _animationState = AnimationState.Normal;
                    _nextMotion = null;
                }
                else
                {
                    _actionOnceFinished?.Invoke();
                }
            }
        }

        /// <summary>
        /// 애니메이션 변환 행렬을 업데이트한다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="rootBone">루트 본</param>
        private void UpdateAnimationTransforms(float motionTime, Bone rootBone)
        {
            // 키프레임으로부터 현재의 로컬포즈행렬을 가져온다.(bone name, mat4x4f)
            Dictionary<string, Matrix4x4f> currentPose = _currentMotion.InterpolatePoseAtTime(motionTime);

            // 로컬 포즈행렬로부터 캐릭터공간의 포즈행렬을 얻는다.
            if (_boneStack.Count > 0) _boneStack.Clear();
            _boneStack.Push((rootBone, Matrix4x4f.Identity));

            while (_boneStack.Count > 0)
            {
                // 스택에서 뼈대를 꺼내고, 부모 행렬을 꺼낸다.
                var (bone, parentTransform) = _boneStack.Pop();

                // 현재 포즈 딕셔너리에 뼈대의 이름이 있으면 그 행렬을 가져오고, 없으면 기본 로컬바인딩행렬을 사용한다.
                bone.BoneTransforms.LocalTransform =
                    (currentPose != null && currentPose.TryGetValue(bone.Name, out Matrix4x4f poseTransform)) ?
                    poseTransform : bone.BoneTransforms.LocalBindTransform;

                // 뼈대의 인덱스가 유효한지 확인한다.
                int boneIndex = bone.Index;
                if (boneIndex < 0) continue;

                // 부모 행렬과 로컬 변환 행렬을 곱하여 애니메이션된 행렬을 계산한다.
                Matrix4x4f animated = parentTransform * bone.BoneTransforms.LocalTransform;

                // 애니메이션된 행렬에 역바인드포즈를 적용한다.
                _animatedTransforms[boneIndex] = animated * bone.BoneTransforms.InverseBindPoseTransform;

                // 자식 뼈대가 있다면 스택에 추가한다.
                foreach (Bone childbone in bone.Children)
                    _boneStack.Push((childbone, animated));
            }
        }
    }
}