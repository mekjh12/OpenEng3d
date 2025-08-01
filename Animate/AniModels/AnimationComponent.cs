using AutoGenEnums;

namespace Animate
{
    public class AnimationComponent : IComponent
    {
        private Animator _animator;
        private AniRig _aniRig;
        private HUMAN_ACTION _prevMotion = HUMAN_ACTION.BREATHING_IDLE;
        private HUMAN_ACTION _curMotion = HUMAN_ACTION.BREATHING_IDLE;

        public Animator Animator => _animator;
        public Motion CurrentMotion => _animator.CurrentMotion;
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
            Motion motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, motionCache, blendingInterval);
            }
        }

        public void SetMotionOnce(string motionName, HUMAN_ACTION returnMotion)
        {
            Motion curMotion = _aniRig.Motions.GetMotion(Actions.ActionMap[_curMotion]);
            Motion nextMotion = _aniRig.Motions.GetMotion(motionName);
            if (nextMotion == null) nextMotion = _aniRig.Motions.DefaultMotion;

            _animator.OnceFinished = () =>
            {
                _animator.SetMotion(curMotion, _aniRig.MotionCache);
                _animator.OnceFinished = null;
            };

            _animator.SetMotion(nextMotion, _aniRig.MotionCache);
        }

    }
}
