using AutoGenEnums;

namespace Animate
{
    public class AnimationComponent : IComponent
    {
        private Animator _animator;
        private AniRig _aniRig;
        private ACTION _prevMotion = ACTION.BREATHING_IDLE;
        private ACTION _curMotion = ACTION.BREATHING_IDLE;

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

        public void SetMotion(string motionName, float blendingInterval = 0.2f)
        {
            _animator.OnceFinished = null;
            Motion motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, blendingInterval);
            }
        }

        public void SetMotionOnce(string motionName, ACTION returnMotion)
        {
            Motion curMotion = _aniRig.Motions.GetMotion(Actions.ActionMap[_curMotion]);
            Motion nextMotion = _aniRig.Motions.GetMotion(motionName);
            if (nextMotion == null) nextMotion = _aniRig.Motions.DefaultMotion;

            _animator.OnceFinished = () =>
            {
                _animator.SetMotion(curMotion);
                _animator.OnceFinished = null;
            };

            _animator.SetMotion(nextMotion);
        }

    }
}
