using AutoGenEnums;

namespace Animate
{
    public class Horse : AniActor
    {
        public Horse(string name, AniRig aniRig) : base(name, aniRig)
        {


        }

        public void SetMotion(HORSE_ACTION action)
        {
            SetMotion("Stand");
        }
    }
}
