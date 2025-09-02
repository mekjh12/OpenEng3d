using AutoGenEnums;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public class Horse : AnimalActor<HORSE_ACTION>
    {
        private static readonly ActionHandler<HORSE_ACTION> HorseActionHandler =
            new ActionHandler<HORSE_ACTION>(
                HorseActions.ActionMap,
                () => (HORSE_ACTION)Rand.NextInt(0, (int)(HORSE_ACTION.RANDOM - 1)),
                HORSE_ACTION.RANDOM,
                new HashSet<string> { "RANDOM", "STOP", "NONE", "COUNT" }
            );

        public Horse(string name, AnimRig aniRig) : base(name, aniRig, HorseActionHandler, HORSE_ACTION.NONE)
        {

        }
    }
}
