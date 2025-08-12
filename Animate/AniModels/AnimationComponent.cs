using AutoGenEnums;

namespace Animate
{
    public class AnimationComponent : IComponent
    {
        private Animator _animator;
        private AniRig _aniRig;
        protected HUMAN_ACTION _prevMotion = HUMAN_ACTION.BREATHING_IDLE;
        protected HUMAN_ACTION _curMotion = HUMAN_ACTION.BREATHING_IDLE;

        public Animator Animator => _animator;
        public Motionable CurrentMotion => _animator.CurrentMotion;
        public float MotionTime => _animator.MotionTime;

        public AnimationComponent(AniRig aniRig, Bone rootBone)
        {
            _aniRig = aniRig;
            _animator = new Animator(rootBone); 
        }

        public void Initialize() { }

        public void Dispose() { }

        public void Update(float deltaTime)
        {
            _animator.Update(deltaTime);
        }

        public void SetMotion(string motionName, MotionCache motionCache, float blendingInterval = 0.2f)
        {
            _animator.OnceFinished = null;
            Motionable motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, motionCache, blendingInterval);
            }
        }


    }
}
