using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class Motion: Motionable
    {
        string _animationName; // 애니메이션 이름
        protected float _length; // 애니메이션 길이(초)
        Dictionary<float, KeyFrame> _keyframes; // 키프레임 딕셔너리 (시간 -> 키프레임)
        
        // 최적화 처리 변수
        TimeFinder _timeFinder;
        bool _isInitTimeFinder = false; // 시간 찾기 유틸리티 초기화 여부
        KeyFrame _previousFrame;
        KeyFrame _nextFrame;

        // ## 최적화를 위한 부분
        // 재사용이 가능하도록 사용시 사전을 비우도록 하고 초기 용량을 128로 설정
        readonly Dictionary<string, Matrix4x4f> _currentPose;

        // [추가] 시간순 정렬된 키프레임 캐시 - ElementAt() 제거용
        private KeyFrame[] _sortedKeyframes;
        private bool _cacheValid = false;

        public TimeFinder TimeFinder => _timeFinder;

        /// <summary>키프레임 딕셔너리를 반환합니다.</summary>
        public Dictionary<float, KeyFrame> Keyframes => _keyframes;

        /// <summary>애니메이션 길이를 반환합니다.</summary>
        public float Length => _length;

        /// <summary>애니메이션 이름을 반환합니다.</summary>
        public string Name => _animationName;

        /// <summary>키프레임 개수를 반환합니다.</summary>
        public int KeyFrameCount => _keyframes.Count;

        // [추가] 캐시 생성 및 유효성 확인
        private void EnsureCacheValid()
        {
            if (!_cacheValid || _sortedKeyframes?.Length != _keyframes.Count)
            {
                // 시간순으로 정렬하여 배열 생성
                _sortedKeyframes = _keyframes
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Value)
                    .ToArray();
                _cacheValid = true;
            }
        }

        // [추가] 캐시 무효화
        private void InvalidateCache()
        {
            _cacheValid = false;
        }

        /// <summary>첫 번째 키프레임을 반환합니다.</summary>
        public KeyFrame FirstKeyFrame
        {
            get
            {
                if (_keyframes.Count == 0) return null;
                EnsureCacheValid();
                return _sortedKeyframes[0];
            }
        }

        /// <summary>마지막 키프레임을 반환합니다.</summary>
        public KeyFrame LastKeyFrame
        {
            get
            {
                if (_keyframes.Count == 0) return null;
                EnsureCacheValid();
                return _sortedKeyframes[_keyframes.Count - 1];
            }
        }

        /// <summary>
        /// Motion 객체를 생성합니다.
        /// </summary>
        /// <param name="name">애니메이션 이름</param>
        /// <param name="lengthInSeconds">애니메이션 길이(초)</param>
        public Motion(string name, float lengthInSeconds)
        {
            _animationName = name;
            _length = lengthInSeconds;

            // 초기화
            _currentPose = new Dictionary<string, Matrix4x4f>(128);
            _keyframes = new Dictionary<float, KeyFrame>();

            // 시간 찾기 유틸리티
            _timeFinder = new TimeFinder(); 
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

        public KeyFrame GetFastKeyFrame(float time)
        {
            float currentKeyFrameTime = 0.0f;
            KeyFrame keyFrame = null;
            foreach (KeyValuePair<float, KeyFrame> item in _keyframes)
            {
                float keytime = item.Key;
                currentKeyFrameTime = keytime;
                if (time < currentKeyFrameTime)
                {
                    keyFrame = item.Value;
                    break;
                }
            }
            return keyFrame;
        }

        /// <summary>
        /// 인덱스로 키프레임을 가져옵니다.
        /// </summary>
        /// <param name="index">키프레임 인덱스</param>
        /// <returns>해당 인덱스의 키프레임</returns>
        public KeyFrame KeyFrame(int index)
        {
            if (index < 0 || index >= _keyframes.Count) return null;
            EnsureCacheValid();
            return _sortedKeyframes[index];
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
                InvalidateCache(); // [추가] 캐시 무효화
            }
        }

        /// <summary>
        /// 키프레임 객체를 추가합니다.
        /// </summary>
        /// <param name="keyFrame">추가할 키프레임</param>
        public void AddKeyFrame(KeyFrame keyFrame)
        {
            _keyframes[keyFrame.TimeStamp] = keyFrame;
            InvalidateCache(); // [추가] 캐시 무효화
        }

        /// <summary>
        /// 현재 포즈와 시각에 대한 뼈마다의 로컬포즈행렬(부모뼈공간)을 가져온다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="outPose">모션 시간</param>
        /// <returns>뼈 이름별 변환 행렬 딕셔너리</returns>
        public virtual bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            // 모션 시간 유효성 검사
            if (!_isInitTimeFinder)
            {
                _timeFinder.SetTimes(_keyframes.Keys.ToArray());
                _isInitTimeFinder = true;
            }

            // 모션이 비어 있거나 키프레임이 없는 경우
            if (FirstKeyFrame == null || KeyFrameCount == 0) return false;

            EnsureCacheValid();

            _timeFinder.FindInterpolationIndices(motionTime, out int lowerIndex, out int upperIndex, out float blenderFactor);
            _previousFrame = _sortedKeyframes[lowerIndex];
            _nextFrame = _sortedKeyframes[upperIndex];

            // 진행률 계산
            float totalTime = _nextFrame.TimeStamp - _previousFrame.TimeStamp;
            float currentTime = motionTime - _previousFrame.TimeStamp;
            float progression = (totalTime > 0) ? (currentTime / totalTime) : 0f;

            // ✅ _currentPose 대신 outPose 직접 사용
            outPose.Clear();

            // ✅ 배열 할당 없이 직접 컬렉션 사용
            string[] boneNames = _previousFrame.BoneNames; // 컬렉션 직접 사용

            if (boneNames == null) return false;

            for (int i = 0; i < boneNames.Length; i++)
            {
                string jointName = boneNames[i];
                BoneTransform previousTransform = _previousFrame[jointName];
                BoneTransform nextTransform = _nextFrame[jointName];

                BoneTransform currentTransform = BoneTransform.InterpolateSlerp(previousTransform, nextTransform, progression);

                outPose[jointName] = currentTransform.LocalTransform;

                // 유효성 검증
                if (!currentTransform.LocalTransform.IsValidMatrix())
                {
                    if (previousTransform.LocalTransform.IsValidMatrix())
                    {
                        outPose[jointName] = previousTransform.LocalTransform;
                    }
                    else if (nextTransform.LocalTransform.IsValidMatrix())
                    {
                        outPose[jointName] = nextTransform.LocalTransform;
                    }
                    else
                    {
                        outPose[jointName] = Matrix4x4f.Identity;
                    }
                }
            }

            return true;
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
            // [최적화] ToList().ToArray() 제거하고 캐시 사용
            EnsureCacheValid();

            // 첫번째, 마지막은 보장한다.
            KeyFrame previousKeyFrame = FirstKeyFrame;
            KeyFrame nextKeyFrame = LastKeyFrame;

            if (previousKeyFrame == null || nextKeyFrame == null)
            {
                throw new Exception("보간하기 전에 반드시 맨 처음과 맨 마지막 프레임이 채워진 후 실행해야 한다.");
            }

            // 길이가 1이상인 경우에만 실행한다.
            if (_sortedKeyframes.Length > 1)
            {
                // 매 타임을 순회하면서 빈 프레임을 찾아 보간한다.
                for (int i = 1; i < _sortedKeyframes.Length; i++)
                {
                    KeyFrame keyFrame = _sortedKeyframes[i];
                    float time = keyFrame.TimeStamp;

                    // 프레임이 비어 있다면
                    if (!keyFrame.ContainsKey(boneName))
                    {
                        // 비어 있지 않는 다음 프레임을 찾는다.
                        nextKeyFrame = keyFrame;
                        for (int j = i; j < _sortedKeyframes.Length; j++)
                        {
                            if (_sortedKeyframes[j].ContainsKey(boneName))
                            {
                                nextKeyFrame = _sortedKeyframes[j];
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

    }
}