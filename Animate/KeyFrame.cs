using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    public class KeyFrame
    {
        private float _timeStamp;
        private Dictionary<string, BoneTransform> _pose; // 뼈대의 이름과 뼈대 포즈를 저장하는 딕셔너리

        public bool ContainsKey(string boneName) => _pose.ContainsKey(boneName);

        public KeyFrame(float timeStamp)
        {
            _timeStamp = timeStamp;
            _pose = new Dictionary<string, BoneTransform>();
        }

        public float TimeStamp
        {
            get => _timeStamp;
            set => _timeStamp = value;
        }

        public BoneTransform this[string boneName]
        {
            get => _pose.ContainsKey(boneName) ? _pose[boneName] : BoneTransform.Identity;
            set => _pose[boneName] = value;
        }

        public void AddBoneTransform(string boneName, BoneTransform boneTransform)
        {
            // 뼈대가 있으면 기존 뼈대에 덮어쓰고 없으면 새로운 뼈대를 추가한다.
            _pose[boneName] = boneTransform;
        }

        public string[] BoneNames => _pose.Keys.ToArray();

        public BoneTransform[] BoneTransforms => _pose.Values.ToArray();

        public KeyFrame Clone()
        {
            KeyFrame dest = new KeyFrame(_timeStamp);
            foreach (var kvp in _pose)
            {
                dest._pose[kvp.Key] = kvp.Value;
            }
            return dest;
        }
    }
}
