using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    public class Animator
    {
        private const int MAX_BONES_COUNT = 128;

        // 멤버 변수
        Matrix4x4f[] _animatedTransforms = new Matrix4x4f[MAX_BONES_COUNT];  // 애니메이션된 행렬      
        float _motionTime = 0.0f;
        bool _isPlaying = true;

        Motion _currentMotion;
        Motion _blendMotion;
        Motion _nextMotion;

        Bone _rootBone;

        // 클래스내 처리 변수
        float _previousTime = 0.0f; // 이전프레임 시간을 기억하는 변수
        Action _actionOnceFinised = null; 
        Stack<(Bone, Matrix4x4f)> _boneStack = new Stack<(Bone bone, Matrix4x4f parentTransform)>(); // 뼈대트리탐색용 스택

        /// <summary>
        /// <code>
        /// 애니메이션이 적용된 최종 행렬들(뼈대의 개수만큼, 애니메이션이 적용된 최종 행렬들)
        /// Update함수를 통해서 행렬이 업데이트된다.
        /// </code>
        /// </summary>
        public Matrix4x4f[] AnimatedTransforms => _animatedTransforms;

        public Action OnceFinised
        {
            set
            {
                _actionOnceFinised = value;
            }
        }

        public Motion CurrentMotion => _currentMotion;

        public bool IsPlaying => _isPlaying;

        public float MotionTime
        {
            get => _motionTime;
            set => _motionTime = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="rootBone"></param>
        public Animator(Bone rootBone)
        {
            _rootBone = rootBone;
        }

        /// <summary>
        /// 모션을 지정한다.
        /// </summary>
        /// <param name="animation"></param>
        public void SetMotion(Motion motion, float blendingInterval = 0.2f)
        {
            // 진행하고 있는 모션이 잇는 경우에 블렌딩 인터벌동안 블렌딩 처리함.
            if (_currentMotion == null)
            {
                _currentMotion = motion;
            }
            else
            {
                _blendMotion = Motion.BlendMotion("switchMotion", _currentMotion, _motionTime, motion, 0.0f, blendingInterval);
                _currentMotion = _blendMotion;
                _nextMotion = motion;
            }

            _motionTime = 0;
        }

        public void Play()
        {
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        public void Toggle()
        {
            _isPlaying = !_isPlaying;
        }

        public void Update(float deltaTime)
        {
            // 현재 모션이 없으면 업데이트를 하지 않는다.
            if (_currentMotion == null) return;

            // 모션이 재생중이 아니면 시간을 업데이트하지 않는다.
            if (_isPlaying)
            {
                // 모션 시간을 업데이트한다.
                _motionTime += deltaTime;

                // 모션의 최대길이를 넘기면 
                if (_motionTime >= _currentMotion.Length)
                {
                    _motionTime = 0.0f;

                    // 중간 전환 모션이면 다음 모션으로 넘겨준다.
                    if (_currentMotion.Name == "switchMotion")
                    {
                        _currentMotion = _nextMotion;
                    }
                    else
                    {
                        // 만약 한번만 실행하는 모션인 경우에 한 번 실행 후 돌아가는 모션을 지정한다.
                        if (_actionOnceFinised != null) 
                            _actionOnceFinised();
                    }
                }

                // 모션의 재생이 역인 경우에 마이너스 시간을 조정한다.
                if (_motionTime < 0) _motionTime = _currentMotion.Length;
            }

            // 모션의 현재 시간에 맞는 애니메이션 최종 행렬을 루트본에 의하여 계층적으로 업데이트한다.
            UpdateAnimationTransforms(_motionTime, _rootBone);
        }


        private void UpdateAnimationTransforms(float motionTime, Bone rootBone)
        {
            // 키프레임으로부터 현재의 로컬포즈행렬을 가져온다.(bone name, mat4x4f)
            Dictionary<string, Matrix4x4f> currentPose = _currentMotion.InterpolatePoseAtTime(motionTime);

            // 로컬 포즈행렬로부터 캐릭터공간의 포즈행렬을 얻는다.
            _boneStack.Clear();
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
                foreach (Bone childbone in bone.Children) _boneStack.Push((childbone, animated));
            }
        }
    }
}