using AutoGenEnums;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public class Donkey : AnimalActor<DONKEY_ACTION>
    {
        private static readonly ActionHandler<DONKEY_ACTION> DonkeyActionHandler =
            new ActionHandler<DONKEY_ACTION>(
                DonkeyActions.ActionMap,
                () => (DONKEY_ACTION)Rand.NextInt(0, (int)(DONKEY_ACTION.RANDOM - 1)),
                DONKEY_ACTION.RANDOM,
                new HashSet<string> { "RANDOM", "STOP", "NONE", "COUNT" }
            );

        public Donkey(string name, DonkeyRig donkeyRig) : base(name, donkeyRig, DonkeyActionHandler, DONKEY_ACTION.NONE)
        {


        }
    }
}
