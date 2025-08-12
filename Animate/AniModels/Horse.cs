using AutoGenEnums;
using ZetaExt;

namespace Animate
{
    public class Horse : AniActor<HORSE_ACTION>
    {
        public Horse(string name, AniRig aniRig) : base(name, aniRig, HORSE_ACTION.NONE)
        {


        }

        public override HORSE_ACTION RandomAction =>
            (HORSE_ACTION)Rand.NextInt(0, (int)(HORSE_ACTION.RANDOM - 1));

        public override void SetMotionImmediately(HORSE_ACTION action)
        {
            if (action == HORSE_ACTION.RANDOM) action = RandomAction;
            SetMotion(HorseActions.ActionMap[action], blendingInterval: 0.0f);
        }

        public override void SetMotion(HORSE_ACTION action)
        {
            if (action == HORSE_ACTION.RANDOM) action = RandomAction;
            SetMotion(HorseActions.ActionMap[action]);
        }

        public override void SetMotionOnce(HORSE_ACTION action)
        {
            if (action == HORSE_ACTION.RANDOM) action = RandomAction;
            SetMotionOnce(HorseActions.ActionMap[action]);
        }

        protected override string GetActionName(HORSE_ACTION action)
        {
            return action.IsCommonAction() ? action.GetName() : HorseActions.GetActionName(action);
        }
    }
}
