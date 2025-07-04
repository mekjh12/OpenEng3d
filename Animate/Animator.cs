using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    public class Animator
    {
        AniActor _aniActor;

        float _motionTime = 0.0f;
        bool _isPlaying = true;

        Motion _currentMotion;
        Motion _blendMotion;
        Motion _nextMotion;

        float _previousTime = 0.0f; // 이전프레임 시간을 기억하는 변수

        Action _actionOnceFinised = null;

        Matrix4x4f[] _animatedTransforms;

        public Matrix4x4f[] AnimatedTransforms
        {
            get => _animatedTransforms;
        }

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
        /// <param name="aniActor"></param>
        public Animator(AniActor aniActor)
        {
            _aniActor = aniActor;

            _animatedTransforms = new Matrix4x4f[_aniActor.AniRig.BoneCount];
        }

        /// <summary>
        /// 모션을 지정한다.
        /// </summary>
        /// <param name="animation"></param>
        public void SetMotion(Motion motion, float blendingInterval = 0.2f)
        {
            //Console.WriteLine("현재 지정하는 모션: " + motion?.Name);

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

            // 키프레임으로부터 현재의 로컬포즈행렬을 가져온다.(bone name, mat4x4f)
            Dictionary<string, Matrix4x4f> currentPose = _currentMotion.InterpolatePoseAtTime(_motionTime);

            // 로컬 포즈행렬로부터 캐릭터공간의 포즈행렬을 얻는다.
            Stack<Bone> stack = new Stack<Bone>();
            Stack<Matrix4x4f> mStack = new Stack<Matrix4x4f>();
            stack.Push(_aniActor.RootBone); // 뼈대스택
            mStack.Push(Matrix4x4f.Identity);    // 행렬스택
            while (stack.Count > 0)
            {
                Bone bone = stack.Pop();
                Matrix4x4f parentTransform = mStack.Pop();

                // 현재 포즈 딕셔너리에 뼈대의 이름이 있으면 그 행렬을 가져오고, 없으면 기본 로컬바인딩행렬을 사용한다.
                if (currentPose != null)
                {
                    bone.LocalTransform = (currentPose.ContainsKey(bone.Name)) ?
                        currentPose[bone.Name] : bone.LocalBindTransform;
                }

                // 행렬곱을 누적하기 위하여, 순서는 자식부터  v' = ... P2 P1 L v
                int boneIndex = bone.Index;

                Matrix4x4f animated = Matrix4x4f.Identity;
                if (boneIndex >= 0)
                {
                    animated = parentTransform * bone.LocalTransform;
                    _animatedTransforms[boneIndex] = animated;

                    bone.AnimatedTransform = parentTransform * bone.LocalTransform;
                }

                foreach (Bone childbone in bone.Childrens) // 트리탐색을 위한 자식 스택 입력
                {
                    stack.Push(childbone);
                    mStack.Push(bone.AnimatedTransform);
                }
            }
        }

    }
}