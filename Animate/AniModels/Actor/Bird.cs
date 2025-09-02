using AutoGenEnums;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public class Bird : AnimalActor<BIRD_ACTION>
    {
        private static readonly ActionHandler<BIRD_ACTION> BirdActionHandler =
            new ActionHandler<BIRD_ACTION>(
                BirdActions.ActionMap,
                () => (BIRD_ACTION)Rand.NextInt(0, (int)(BIRD_ACTION.RANDOM - 1)),
                BIRD_ACTION.RANDOM,
                new HashSet<string> { "RANDOM", "STOP", "NONE", "COUNT" }
            );

        public Bird(string name, AnimRig animRig) : base(name, animRig, BirdActionHandler, BIRD_ACTION.NONE)
        {

        }

    }
}
