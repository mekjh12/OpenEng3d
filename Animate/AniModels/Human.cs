using AutoGenEnums;
using Geometry;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class Human : Primate
    {
        
        public Human(string name, AniRig aniRig) : base(name, aniRig)
        {
           
        }

        public HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public void SetMotionImmediately(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(Actions.ActionMap[action], blendingInterval: 0.0f);
        }

        public void SetMotion(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(Actions.ActionMap[action]);
        }

        /// <summary>
        /// 다음 모션을 한번만 하고 이후에는 이전 모션으로 돌아간다.
        /// </summary>
        /// <param name="action"></param>
        public void SetMotionOnce(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotionOnce(Actions.ActionMap[action]);
        }
    }
}