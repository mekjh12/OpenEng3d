using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    public class Animator
    {
        AniModel _animatedModel;

        float _motionTime = 0.0f;
        bool _isPlaying = true;

        Motion _currentMotion;
        Motion _blendMotion;
        Motion _nextMotion;

        float _previousTime = 0.0f; // 이전프레임 시간을 기억하는 변수

        Action _actionOnceFinised = null;

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
        /// <param name="entity"></param>
        public Animator(AniModel animatedModel)
        {
            _animatedModel = animatedModel;
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
            if (_currentMotion == null) return;

            if (_isPlaying) // 재생시에만 
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
            Dictionary<string, Matrix4x4f> currentPose = CalculateCurrentAnimationLocalPose();

            // [트리구조 탐색] 로컬 포즈행렬로부터 캐릭터공간의 포즈행렬을 얻는다.
            Stack<Bone> stack = new Stack<Bone>();
            Stack<Matrix4x4f> mStack = new Stack<Matrix4x4f>();
            stack.Push(_animatedModel.RootBone); // 뼈대스택
            mStack.Push(Matrix4x4f.Identity);    // 행렬스택
            while (stack.Count > 0)
            {
                Bone bone = stack.Pop();
                Matrix4x4f parentTransform = mStack.Pop();

                // 로컬포즈행렬이 없으면 기본바인딩행렬로 가져온다.
                Matrix4x4f boneLocalTransform = (currentPose.ContainsKey(bone.Name)) ?
                    currentPose[bone.Name] : bone.LocalBindTransform;

                bone.LocalTransform = boneLocalTransform;
                bone.AnimatedTransform = parentTransform * boneLocalTransform; // 행렬곱을 누적하기 위하여, 순서는 자식부터  v' = ... P2 P1 L v

                foreach (Bone childJoint in bone.Childrens) // 트리탐색을 위한 자식 스택 입력
                {
                    stack.Push(childJoint);
                    mStack.Push(bone.AnimatedTransform);
                }
            }
        }

        /// <summary>
        /// * 현재 시간의 현재 모션 애니메이션 로컬 포즈를 가져온다. <br/>
        /// * 반환값의 딕셔너리는 jointName, Matrix4x4f이다.<br/>
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Matrix4x4f> CalculateCurrentAnimationLocalPose()
        {
            // 현재 시간에서 가장 근접한 사이의 두 개의 프레임을 가져온다.
            KeyFrame previousFrame = _currentMotion.FirstKeyFrame;
            KeyFrame nextFrame = _currentMotion.FirstKeyFrame;
            float firstTime = _currentMotion.FirstKeyFrame.TimeStamp;
            for (int i = 1; i < _currentMotion.KeyFrameCount; i++)
            {
                nextFrame = _currentMotion.Frame(i);
                if (nextFrame.TimeStamp >= _motionTime - firstTime)
                {
                    break;
                }
                previousFrame = _currentMotion.Frame(i);
            }

            // 현재 진행률을 계산한다.
            _previousTime = previousFrame.TimeStamp;
            float totalTime = nextFrame.TimeStamp - previousFrame.TimeStamp;
            float currentTime = _motionTime - previousFrame.TimeStamp;
            float progression = currentTime / totalTime;

            // 두 키프레임 사이의 보간된 포즈를 딕셔러리로 가져온다.
            Dictionary<string, Matrix4x4f> currentPose = new Dictionary<string, Matrix4x4f>();
            foreach (string jointName in previousFrame.Pose.JointNames)
            {
                BonePose previousTransform = previousFrame[jointName];
                BonePose nextTransform = nextFrame[jointName];
                BonePose currentTransform = BonePose.InterpolateSlerp(previousTransform, nextTransform, progression);
                currentPose[jointName] = currentTransform.LocalTransform;

                // 아래는 쿼터니온 에러로 인한 NaN인 경우에 대체 포즈로 강제 지정(좋은 코드는 아님)
                if (currentTransform.LocalTransform.Determinant.ToString() == "NaN")
                {
                    if (previousTransform.LocalTransform.Determinant.ToString() == "NaN")
                    {
                        currentTransform = BonePose.InterpolateSlerp(nextTransform, nextTransform, 0);
                        currentPose[jointName] = currentTransform.LocalTransform;
                    }
                    if (nextTransform.LocalTransform.Determinant.ToString() == "NaN")
                    {
                        currentTransform = BonePose.InterpolateSlerp(previousTransform, previousTransform, 0);
                        currentPose[jointName] = currentTransform.LocalTransform;
                    }
                }
            }

            return currentPose;
        }

    }
}