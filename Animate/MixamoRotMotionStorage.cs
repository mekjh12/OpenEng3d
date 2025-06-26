using System.Collections.Generic;

namespace Animate
{
    public class MixamoRotMotionStorage
    {
        Dictionary<string, Motion> _motions = new Dictionary<string, Motion>();

        public void AddMotion(Motion motion)
        {
            string motionName = motion.Name;
            if (_motions.ContainsKey(motionName))
            {
                _motions[motionName] = motion;
            }
            else
            {
                _motions.Add(motionName, motion);
            }
        }

        public void RetargetMotionsTransfer(AniDae destAniDae)
        {
            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motion> item in _motions)
            {
                // 모션 이름과 모션 객체를 가져온다.
                string motionName = item.Key;
                Motion srcMotion = item.Value;

                // 리타켓팅 알고리즘 구현하기
                // TODO 목이 긴 abe캐릭터에서 모션을 읽어와 전이하여 아직 구현이 안된 목이 긴 캐릭터





                // 지정된 애니메이션 모델에 모션을 추가한다.
                destAniDae.AddMotion(srcMotion);
            }

            return;
        }
    }
}
