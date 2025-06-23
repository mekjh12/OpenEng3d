using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class Motion
    {
        string _animationName;
        float _length;
        Dictionary<float, KeyFrame> _keyframes;

        public KeyFrame FirstKeyFrame => (_keyframes.Values.Count > 0) ? _keyframes.Values.ElementAt(0) : null;

        public KeyFrame LastKeyFrame => (_keyframes.Values.Count > 0) ? _keyframes.Values.ElementAt(_keyframes.Count - 1) : null;

        public Dictionary<float, KeyFrame> Keyframes => _keyframes;

        public float Length => _length;

        public string Name => _animationName;

        public int KeyFrameCount => _keyframes.Count;

        public KeyFrame MiddleKeyFrame
        {
            get
            {
                int numKeyFrame = _keyframes.Count;
                int idx = (int)(numKeyFrame / 2.0f);
                return _keyframes.Values.ElementAt(idx);
            }
        }

        public KeyFrame OneQuaterKeyFrame
        {
            get
            {
                int numKeyFrame = _keyframes.Count;
                int idx = (int)(numKeyFrame / 4.0f);
                return _keyframes.Values.ElementAt(idx);
            }
        }

        public KeyFrame ThreeQuaterKeyFrame
        {
            get
            {
                int numKeyFrame = _keyframes.Count;
                int idx = (int)(3.0f * numKeyFrame / 4.0f);
                return _keyframes.Values.ElementAt(idx);
            }
        }

        public Motion(string name, float lengthInSeconds)
        {
            _animationName = name;
            _length = lengthInSeconds;
            _keyframes = new Dictionary<float, KeyFrame>();
        }

        public KeyFrame CloneKeyFrame(float time)
        {
            float currentKeyFrameTime = 0.0f;
            foreach (KeyValuePair<float, KeyFrame> item in _keyframes)
            {
                float keytime = item.Key;
                currentKeyFrameTime = keytime;
                if (time < currentKeyFrameTime) break;
            }

            KeyFrame keyFrame = new KeyFrame(currentKeyFrameTime);
            keyFrame = _keyframes[currentKeyFrameTime].Clone();
            return keyFrame;
        }

        public KeyFrame this[float time]
        {
            get
            {
                if (_keyframes.ContainsKey(time))
                {
                    return (_keyframes.Values.Count > 0) ? _keyframes[time] : null;
                }
                else
                {
                    return FirstKeyFrame;
                }
            }

            set
            {
                if (_keyframes.ContainsKey(time))
                {
                    _keyframes[time] = value;
                }
                else
                {
                    _keyframes.Add(time, value);
                }
            }
        }

        public KeyFrame Frame(int index)
        {
            return _keyframes.Values.ElementAt(index);
        }

        public void AddKeyFrame(float time)
        {
            if (!_keyframes.ContainsKey(time))
            {
                _keyframes[time] = new KeyFrame(time);
            }
        }

        public void AddKeyFrame(KeyFrame keyFrame)
        {
            _keyframes[keyFrame.TimeStamp] = keyFrame;
        }

        public void AddKeyActionByBone(string boneName, float time, Vertex3f pos, ZetaExt.Quaternion q)
        {
            // 삽입할 프레임을 찾는다.
            KeyFrame currentKeyFrame;
            if (_keyframes.ContainsKey(time))
            {
                currentKeyFrame = _keyframes[time];
            }
            else
            {
                // 가장 가까운 프레임을 찾는다.
                currentKeyFrame = FirstKeyFrame;
                foreach (KeyValuePair<float, KeyFrame> item in _keyframes)
                {
                    if (time > item.Key) break;
                    currentKeyFrame = item.Value;
                }
            }

            BonePose pose = new BonePose(pos, q);
            currentKeyFrame.AddBoneTransform(boneName, pose);
        }

        /// <summary>
        /// * 지정한 본에서 빈 프레임을 찾아서 앞과 뒤 프레임을 이용하여 보간한다. <br/>
        /// * 보간하기 전에 반드시 맨 처음과 맨 마지막 프레임이 채워진 후 실행해야 한다.<br/>
        /// </summary>
        /// <param name="boneName"></param>
        public void InterpolateEmptyFrame(string boneName)
        {
            float[] times = _keyframes.Keys.ToList().ToArray();

            // 첫번째, 마지막은 보장한다.
            KeyFrame previousKeyFrame = FirstKeyFrame;
            KeyFrame nextKeyFrame = LastKeyFrame;

            if (previousKeyFrame == null || nextKeyFrame == null)
            {
                throw new Exception("보간하기 전에 반드시 맨 처음과 맨 마지막 프레임이 채워진 후 실행해야 한다.");
            }

            // 길이가 1이상인 경우에만 실행한다.
            if (times.Length > 1)
            {
                // 매 타임을 순회하면서 빈 프레임을 찾아 보간한다.
                for (int i = 1; i < times.Length; i++)
                {
                    float time = times[i];
                    KeyFrame keyFrame = _keyframes[time];

                    // 프레임이 비어 있다면
                    if (!keyFrame.ContainsKey(boneName))
                    {
                        // 비어 있지 않는 다음 프레임을 찾는다.
                        nextKeyFrame = keyFrame;
                        for (int j = i; j < times.Length; j++)
                        {
                            if (_keyframes[times[j]].ContainsKey(boneName))
                            {
                                nextKeyFrame = _keyframes[times[j]];
                                break;
                            }
                        }

                        // 현재 진행률을 계산한다.
                        float previousTime = previousKeyFrame.TimeStamp;
                        float totalTime = nextKeyFrame.TimeStamp - previousTime;
                        float currentTime = time - previousTime;
                        float progression = currentTime / totalTime;

                        BonePose previousTransform = previousKeyFrame[boneName];
                        BonePose nextTransform = nextKeyFrame[boneName];
                        BonePose currentTransform = BonePose.InterpolateSlerp(previousTransform, nextTransform, progression);

                        keyFrame.AddBoneTransform(boneName, currentTransform);
                    }
                    // 비어 있지 않으면 이전 프레임을 업데이트한다.
                    else
                    {
                        previousKeyFrame = keyFrame;
                    }
                }
            }

        }

        /// <summary>
        /// 두 모션을 블렌딩한 모션을 반환한다.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prevMotion"></param>
        /// <param name="prevTime"></param>
        /// <param name="nextMotion"></param>
        /// <param name="nextTime"></param>
        /// <param name="blendingInterval"></param>
        /// <returns></returns>
        public static Motion BlendMotion(string name, Motion prevMotion, float prevTime, Motion nextMotion, float nextTime, float blendingInterval)
        {
            KeyFrame k0 = prevMotion.CloneKeyFrame(prevTime);
            k0.TimeStamp = 0;
            KeyFrame k1 = nextMotion.CloneKeyFrame(nextTime);
            k1.TimeStamp = blendingInterval;
            Motion blendMotion = new Motion(name, blendingInterval);
            if (k0 != null) blendMotion.AddKeyFrame(k0);
            if (k1 != null) blendMotion.AddKeyFrame(k1);
            return blendMotion;
        }

    }
}
