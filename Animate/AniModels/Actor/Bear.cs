using AutoGenEnums;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public class Bear : AnimalActor<BEAR_ACTION>
    {
        private static readonly ActionHandler<BEAR_ACTION> BearActionHandler =
            new ActionHandler<BEAR_ACTION>(
                BearActions.ActionMap,
                () => (BEAR_ACTION)Rand.NextInt(0, (int)(BEAR_ACTION.RANDOM - 1)),
                BEAR_ACTION.RANDOM,
                new HashSet<string> { "RANDOM", "STOP", "NONE", "COUNT" }
            );

        public Bear(string name, AnimRig animRig)
            : base(name, animRig, BearActionHandler, BEAR_ACTION.IDLE) { }
    }
}
