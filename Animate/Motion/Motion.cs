using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class Motion: Motionable
    {
        // ------------------------------------------------------------------------------
        // 멤버 변수
        // ------------------------------------------------------------------------------
        const int MAX_BONES_COUNT = 128;    // 최대 뼈대 개수
        string _animationName;                      // 애니메이션 이름
        float _length;                              // 애니메이션 길이(초)
        Dictionary<float, KeyFrame> _keyframes;     // 키프레임 딕셔너리 (시간 -> 키프레임)

        // ---------------  ---------------------------------------------------------------
        // 최적화 처리 변수
        // ------------------------------------------------------------------------------
        TimeFinder _timeFinder;
        bool _isInitTimeFinder = false;         // 시간 찾기 유틸리티 초기화 여부
        KeyFrame _previousFrame;                // 시간 찾기 후에 사용할 이전 키프레임
        KeyFrame _nextFrame;                    // 시간 찾기 후에 사용할 다음 키프레임

        // 시간 순 정렬된 키프레임 캐시 - ElementAt() 제거용
        private KeyFrame[] _sortedKeyframes;    // 키프레임 사전으로부터 시간순으로 정렬된 키프레임 배열
        private bool _cacheValid = false;       // 캐시 유효성 플래그

        readonly Dictionary<string, Matrix4x4f> _currentPose;   // 재사용시 초기 용량을 설정
                                                                // ✅ 배열 할당 없이 직접 컬렉션 사용
        string[] _boneNames;

        // ------------------------------------------------------------------------------
        // 속성
        // ------------------------------------------------------------------------------
        public Dictionary<float, KeyFrame> Keyframes => _keyframes;
        public float Length => _length;
        public string Name => _animationName;
        public int KeyFrameCount => _keyframes.Count;
        public KeyFrame FirstKeyFrame
        {
            get
            {
                if (_keyframes.Count == 0) return null;
                EnsureCacheValid();
                return _sortedKeyframes[0];
            }
        }
        public KeyFrame LastKeyFrame
        {
            get
            {
                if (_keyframes.Count == 0) return null;
                EnsureCacheValid();
                return _sortedKeyframes[_keyframes.Count - 1];
            }
        }

        // ------------------------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------------------------

        /// <summary
        /// Motion 객체를 생성합니다.
        /// </summary>
        /// <param name="name">애니메이션 이름</param>
        /// <param name="lengthInSeconds">애니메이션 길이(초)</param>
        public Motion(string name, float lengthInSeconds)
        {
            _animationName = name;
            _length = lengthInSeconds;

            // 초기화
            _currentPose = new Dictionary<string, Matrix4x4f>(MAX_BONES_COUNT);
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
        /// 시간순 정렬된 키프레임 캐시에 대해서만 시간을 빠르게 찾을 수 있도록 합니다.
        /// </summary>
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

        /// <summary>
        /// 시간순 정렬된 키프레임 캐시를 무효화합니다.
        /// </summary>
        private void InvalidateCache()
        {
            _cacheValid = false;
        }

        /// <summary>
        /// 현재 포즈와 시각에 대한 뼈마다의 로컬포즈행렬(부모뼈공간)을 가져온다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="outPose">모션 시간</param>
        /// <returns>뼈 이름별 변환 행렬 딕셔너리</returns>
        public virtual bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            // 모션 시간 유효성 검사 및 초기화
            if (!_isInitTimeFinder)
            {
                _timeFinder.SetTimes(_keyframes.Keys.ToArray());
                _isInitTimeFinder = true;
            }

            // 모션이 비어 있거나 키프레임이 없는 경우
            if (FirstKeyFrame == null || KeyFrameCount == 0)
                return false;

            // 캐시 유효성 확인
            EnsureCacheValid();

            // 보간을 위한 인덱스 찾기
            _timeFinder.FindInterpolationIndices(motionTime, out int lowerIndex, out int upperIndex, out float blendFactor);

            // === 디버깅 로그 추가 ===
            float lowerTime = _sortedKeyframes[lowerIndex].TimeStamp;
            float upperTime = _sortedKeyframes[upperIndex].TimeStamp;

            // 진행률 계산
            float totalTime = upperTime - lowerTime;
            float currentTime = motionTime - lowerTime;
            float progression = (totalTime > 0) ? (currentTime / totalTime) : 0f;

            // 출력 딕셔너리 초기화
            outPose.Clear();

            // 뼈 이름이 없으면 이전 프레임의 뼈 이름을 사용
            if (_boneNames == null)
                _boneNames = _sortedKeyframes[lowerIndex].BoneNames;

            // 각 뼈에 대해 보간 수행
            for (int i = 0; i < _boneNames.Length; i++)
            {
                string jointName = _boneNames[i];

                // 직접 배열에서 뼈 변환 정보 가져오기
                BoneTransform previousTransform = _sortedKeyframes[lowerIndex][jointName];
                BoneTransform nextTransform = _sortedKeyframes[upperIndex][jointName];

                // Slerp를 사용한 보간
                BoneTransform currentTransform = BoneTransform.InterpolateSlerp(
                    previousTransform, nextTransform, progression);

                // 현재 뼈의 로컬 변환 행렬을 추출
                Matrix4x4f localTransform = currentTransform.LocalTransform;

                // 유효성 검증 및 폴백 처리
                if (!localTransform.IsValidMatrix())
                {
                    if (previousTransform.LocalTransform.IsValidMatrix())   
                    {
                        localTransform = previousTransform.LocalTransform;
                    }
                    else if (nextTransform.LocalTransform.IsValidMatrix())
                    {
                        localTransform = nextTransform.LocalTransform;
                    }
                    else
                    {
                        localTransform = Matrix4x4f.Identity;
                    }
                }

                // 결과 딕셔너리에 추가
                outPose[jointName] = localTransform;
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