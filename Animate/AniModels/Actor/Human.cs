using AutoGenEnums;
using ZetaExt;

namespace Animate
{
    public class Human : Primate<HUMAN_ACTION>
    {

        public Human(string name, AnimRig aniRig) : base(name, aniRig, HUMAN_ACTION.A_T_POSE)
        {
           
        }

        public override HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public override void SetMotionImmediately(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(HumanActions.ActionMap[action], transitionDuration: 0.0f);
        }

        public override void SetMotion(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(HumanActions.ActionMap[action]);
        }

        public override void SetMotionOnce(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotionOnce(HumanActions.ActionMap[action]);
        }

        protected override string GetActionName(HUMAN_ACTION action)
        {
            return action.IsCommonAction() ? action.GetName() : HumanActions.GetActionName(action);
        }
    }
}