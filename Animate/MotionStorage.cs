using System.Collections.Generic;

namespace Animate
{
    public class MotionStorage
    {
        Dictionary<string, Motion> _motions = new Dictionary<string, Motion>();

        public Motion DefaultMotion
        {
            get
            {
                List<Motion> list = new List<Motion>(_motions.Values);
                return _motions.Count > 0 ? list[0] : null;
            }
        }

        public void AddMotion(string motionName, Motion motion)
        {
            if (_motions.ContainsKey(motionName))
            {
                _motions[motionName] = motion;
            }
            else
            {
                _motions.Add(motionName, motion);
            }
        }

        public Motion GetMotion(string motionName)
        {
            return (_motions.ContainsKey(motionName)) ? _motions[motionName] : null;
        }
    }
}
