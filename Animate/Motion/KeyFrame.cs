using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    /// <summary>
    /// 키프레임으로 애니메이션의 특정 시점에 뼈대의 포즈를 저장합니다.
    /// </summary>
    public class KeyFrame
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        private float _timeStamp;
        private Dictionary<string, BoneTransform> _pose; // 뼈대의 이름과 뼈대 포즈를 저장하는 딕셔너리
        private string[] _cacheBoneNames;   

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public bool ContainsKey(string boneName) => _pose.ContainsKey(boneName);
        public BoneTransform[] BoneTransforms => _pose.Values.ToArray();

        public string[] BoneNames
        {
            get
            {
                if (_cacheBoneNames == null)
                {
                    _cacheBoneNames = _pose.Keys.ToArray();
                }

                return _cacheBoneNames;
            }
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

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public KeyFrame(float timeStamp)
        {
            _timeStamp = timeStamp;
            _pose = new Dictionary<string, BoneTransform>();
            _cacheBoneNames = null;
        }

        public void AddBoneTransform(string boneName, BoneTransform boneTransform)
        {
            // 뼈대가 있으면 기존 뼈대에 덮어쓰고 없으면 새로운 뼈대를 추가한다.
            _pose[boneName] = boneTransform;
        }

        public KeyFrame Clone()
        {
            // 캐시된 뼈대 이름이 null인 경우, 현재 키프레임의 뼈대 이름을 캐시한다.
            if (_cacheBoneNames == null)
            {
                _cacheBoneNames = _pose.Keys.ToArray();
            }

            // 새로운 키프레임을 생성하고 현재 키프레임의 정보를 복사한다.
            KeyFrame dest = new KeyFrame(_timeStamp);
            foreach (var kvp in _pose)
            {
                dest._pose[kvp.Key] = kvp.Value;
            }

            // 캐시된 뼈대 이름을 복사한다.
            dest._cacheBoneNames = _cacheBoneNames?.ToArray();
            
            return dest;
        }
    }
}
