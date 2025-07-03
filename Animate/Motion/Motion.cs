using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class Motion
    {
        string _animationName; // 애니메이션 이름
        float _length; // 애니메이션 길이(초)
        Dictionary<float, KeyFrame> _keyframes; // 키프레임 딕셔너리 (시간 -> 키프레임)

        /// <summary>첫 번째 키프레임을 반환합니다.</summary>
        public KeyFrame FirstKeyFrame => (_keyframes.Values.Count > 0) ? _keyframes.Values.ElementAt(0) : null;

        /// <summary>마지막 키프레임을 반환합니다.</summary>
        public KeyFrame LastKeyFrame => (_keyframes.Values.Count > 0) ? _keyframes.Values.ElementAt(_keyframes.Count - 1) : null;

        /// <summary>키프레임 딕셔너리를 반환합니다.</summary>
        public Dictionary<float, KeyFrame> Keyframes => _keyframes;

        /// <summary>애니메이션 길이를 반환합니다.</summary>
        public float Length => _length;

        /// <summary>애니메이션 이름을 반환합니다.</summary>
        public string Name => _animationName;

        /// <summary>키프레임 개수를 반환합니다.</summary>
        public int KeyFrameCount => _keyframes.Count;

        /// <summary>
        /// Motion 객체를 생성합니다.
        /// </summary>
        /// <param name="name">애니메이션 이름</param>
        /// <param name="lengthInSeconds">애니메이션 길이(초)</param>
        public Motion(string name, float lengthInSeconds)
        {
            _animationName = name;
            _length = lengthInSeconds;
            _keyframes = new Dictionary<float, KeyFrame>();
        }

        /// <summary>
        /// 현재 Motion 객체의 복사본을 생성합니다.
        /// </summary>
        /// <returns>복사된 Motion 객체</returns>
        public Motion Clone()
        {
            Motion motion = new Motion(_animationName, _length);
            foreach (KeyValuePair<float, KeyFrame> srcKeyFrame in _keyframes)
            {
                KeyFrame dstKeyFrame = srcKeyFrame.Value.Clone();
                motion.AddKeyFrame(dstKeyFrame);
            }

            return motion;
        }

        /// <summary>
        /// 지정된 시간에 해당하는 키프레임을 복제합니다.
        /// </summary>
        /// <param name="time">대상 시간</param>
        /// <returns>복제된 키프레임</returns>
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

        /// <summary>
        /// 인덱스로 키프레임을 가져옵니다.
        /// </summary>
        /// <param name="index">키프레임 인덱스</param>
        /// <returns>해당 인덱스의 키프레임</returns>
        public KeyFrame KeyFrame(int index)
        {
            return _keyframes.Values.ElementAt(index);
        }

        /// <summary>
        /// 시간으로 키프레임을 가져옵니다.
        /// </summary>
        /// <param name="time">키프레임 시간</param>
        /// <returns>해당 시간의 키프레임 또는 첫 번째 키프레임</returns>
        public KeyFrame KeyFrame(float time)
        {
            return _keyframes.ContainsKey(time) ?
                ((_keyframes.Values.Count > 0) ? _keyframes[time] : null) : FirstKeyFrame;
        }

        /// <summary>
        /// 지정된 시간에 빈 키프레임을 추가합니다.
        /// </summary>
        /// <param name="time">키프레임 시간</param>
        public void AddKeyFrame(float time)
        {
            // 주어진 시간에 키프레임이 없으면 추가한다.
            if (!_keyframes.ContainsKey(time))
            {
                _keyframes[time] = new KeyFrame(time);
            }
        }

        /// <summary>
        /// 키프레임 객체를 추가합니다.
        /// </summary>
        /// <param name="keyFrame">추가할 키프레임</param>
        public void AddKeyFrame(KeyFrame keyFrame)
        {
            _keyframes[keyFrame.TimeStamp] = keyFrame;
        }

        /// <summary>
        /// 현재 포즈와 시각에 대한 뼈마다의 로컬포즈행렬(부모뼈공간)을 가져온다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <returns>뼈 이름별 변환 행렬 딕셔너리</returns>
        public Dictionary<string, Matrix4x4f> InterpolatePoseAtTime(float motionTime)
        {
            // 모션이 비어 있거나 키프레임이 없는 경우에는 null을 반환한다.
            if (FirstKeyFrame == null || KeyFrameCount == 0) return null;

            // 현재 시간(motionTime)에서 가장 근접한 사이의 두 개의 프레임을 가져온다.
            KeyFrame previousFrame = FirstKeyFrame;
            KeyFrame nextFrame = FirstKeyFrame;
            float firstTime = FirstKeyFrame.TimeStamp;
            for (int i = 1; i < KeyFrameCount; i++)
            {
                nextFrame = KeyFrame(i);
                if (nextFrame.TimeStamp >= motionTime - firstTime)
                {
                    break;
                }
                previousFrame = KeyFrame(i);
            }

            // 현재 진행률을 계산한다.
            //_previousTime = previousFrame.TimeStamp;
            float totalTime = nextFrame.TimeStamp - previousFrame.TimeStamp;
            float currentTime = motionTime - previousFrame.TimeStamp;
            float progression = currentTime / totalTime;

            // 두 키프레임 사이의 보간된 포즈를 딕셔러리로 가져온다.
            Dictionary<string, Matrix4x4f> currentPose = new Dictionary<string, Matrix4x4f>();
            foreach (string jointName in previousFrame.BoneNames)
            {
                BoneTransform previousTransform = previousFrame[jointName];
                BoneTransform nextTransform = nextFrame[jointName];
                BoneTransform currentTransform = BoneTransform.InterpolateSlerp(previousTransform, nextTransform, progression);
                currentPose[jointName] = currentTransform.LocalTransform;

                // 아래는 쿼터니온 에러로 인한 NaN인 경우에 대체 포즈로 강제 지정(좋은 코드는 아님)
                if (currentTransform.LocalTransform.Determinant.ToString() == "NaN")
                {
                    if (previousTransform.LocalTransform.Determinant.ToString() == "NaN")
                    {
                        currentTransform = BoneTransform.InterpolateSlerp(nextTransform, nextTransform, 0);
                        currentPose[jointName] = currentTransform.LocalTransform;
                    }
                    if (nextTransform.LocalTransform.Determinant.ToString() == "NaN")
                    {
                        currentTransform = BoneTransform.InterpolateSlerp(previousTransform, previousTransform, 0);
                        currentPose[jointName] = currentTransform.LocalTransform;
                    }
                }
            }

            return currentPose;
        }

        /// <summary>
        /// 지정된 시간에 특정 뼈의 변환 정보(위치, 회전)를 키프레임으로 추가합니다.
        /// 해당 시간에 키프레임이 없으면 가장 가까운 기존 키프레임을 사용합니다.
        /// </summary>
        /// <param name="time">키프레임 시간</param>
        /// <param name="boneName">대상 뼈 이름</param>
        /// <param name="pos">뼈의 위치</param>
        /// <param name="q">뼈의 회전 (쿼터니언)</param>
        public void AddBoneKeyFrame(float time, string boneName, Vertex3f pos, ZetaExt.Quaternion q)
        {
            // 삽입할 프레임을 찾는다.
            KeyFrame currentKeyFrame;

            if (_keyframes.ContainsKey(time))
            {
                // 현재 시간에 해당하는 키프레임이 있는지 확인한다.
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

            BoneTransform pose = new BoneTransform(pos, q);
            currentKeyFrame.AddBoneTransform(boneName, pose);
        }

        /// <summary>
        /// 지정한 본에서 빈 프레임을 찾아서 앞과 뒤 프레임을 이용하여 보간합니다.
        /// 보간하기 전에 반드시 맨 처음과 맨 마지막 프레임이 채워진 후 실행해야 합니다.
        /// </summary>
        /// <param name="boneName">대상 뼈 이름</param>
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

                        BoneTransform previousTransform = previousKeyFrame[boneName];
                        BoneTransform nextTransform = nextKeyFrame[boneName];
                        BoneTransform currentTransform = BoneTransform.InterpolateSlerp(previousTransform, nextTransform, progression);

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
        /// 두 모션을 블렌딩하여 새로운 모션을 생성합니다.
        /// </summary>
        /// <param name="name">블렌딩된 모션의 이름</param>
        /// <param name="prevMotion">이전 모션</param>
        /// <param name="prevTime">이전 모션의 시간</param>
        /// <param name="nextMotion">다음 모션</param>
        /// <param name="nextTime">다음 모션의 시간</param>
        /// <param name="blendingInterval">블렌딩 간격</param>
        /// <returns>블렌딩된 새로운 모션</returns>
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