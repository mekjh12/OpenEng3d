using System;

namespace Animate
{
    /// <summary>
    /// 액션 핸들러를 사용하는 단순화된 동물 액터
    /// </summary>
    public abstract class AnimalActor<TAction> : AnimActor<TAction>
        where TAction : struct, Enum
    {
        protected readonly ActionHandler<TAction> ActionHandler;

        protected AnimalActor(string name, AnimRig aniRig,
            ActionHandler<TAction> actionHandler, TAction defaultAction)
            : base(name, aniRig, defaultAction)
        {
            ActionHandler = actionHandler;
        }

        public override TAction RandomAction => ActionHandler.GetRandomAction();

        public override void SetMotionImmediately(TAction action)
        {
            var motionName = ActionHandler.GetMotionName(action);
            if (motionName != null)
                SetMotion(motionName, transitionDuration: 0.0f);
        }

        public override void SetMotion(TAction action)
        {
            var motionName = ActionHandler.GetMotionName(action);
            if (motionName != null)
                SetMotion(motionName);
        }

        public void SetRandomMotion()
        {
            var action = ActionHandler.GetRandomAction();
            SetMotion(action);
        }

        public override void SetMotionOnce(TAction action)
        {
            var motionName = ActionHandler.GetMotionName(action);
            if (motionName != null)
                SetMotionOnce(motionName);
        }

        protected override string GetActionName(TAction action) =>
            ActionHandler.GetActionName(action);
    }
}
