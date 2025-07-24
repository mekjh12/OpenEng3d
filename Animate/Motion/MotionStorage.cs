using AutoGenEnums;
using System.Collections.Generic;

namespace Animate
{
    public class MotionStorage
    {
        Dictionary<string, Motion> _motions = new Dictionary<string, Motion>();

        // [수정] DefaultMotion - List 생성 없이 첫 번째 요소 반환
        public Motion DefaultMotion
        {
            get
            {
                // 가장 빠른 방법: foreach로 첫 번째 요소만 가져오기
                foreach (Motion motion in _motions.Values)
                {
                    return motion; // 첫 번째 요소 즉시 반환
                }
                return null; // 모션이 없으면 null 반환
            }
        }

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

        public Motion GetMotion(HUMAN_ACTION motion)
        {
            return (_motions.ContainsKey(motion.ToString())) ? _motions[motion.ToString()] : null;
        }

        public Motion GetMotion(string motionName)
        {
            return (_motions.ContainsKey(motionName)) ? _motions[motionName] : null;
        }
    }
}