using OpenGL;
using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    /// <summary>
    /// 두 행동으로 블렌딩된 모션을 나타냅니다.
    /// <code>
    /// (M1, T1, t1, s1), (M2, T2, t2, s2)를 이용하여 blendFactor를 적용한 모션을 생성합니다.
    /// </code>
    /// </summary>
    public class BlendMotion : Motionable
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        string _name;                    // 블렌드 모션 이름

        Motionable _motion1;             // 첫 번째 모션
        Motionable _motion2;             // 두 번째 모션

        float _factor1;                  // 첫 번째 모션의 가중치
        float _factor2;                  // 두 번째 모션의 가중치

        float _blendFactor;              // 현재 블렌딩 비율 (0~1)
        float _periodTime;               // 블렌딩된 모션의 주기 시간

        Dictionary<string, Matrix4x4f> _outPose1;   // 첫 번째 모션의 포즈 결과
        Dictionary<string, Matrix4x4f> _outPose2;   // 두 번째 모션의 포즈 결과

        Dictionary<string, Matrix4x4f> _outPose;    // 최종 블렌딩된 포즈

        // 성능 최적화용 캐시
        string[] _boneNamesCache;        // 본 이름 배열 캐시
        KeyFrame _cloneKeyFrame;         // 재사용 가능한 키프레임

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public string Name => _name;
        public float PeriodTime => _periodTime;

        /// <summary>두 모션의 속도를 블렌딩 비율에 따라 보간</summary>
        public float Speed => _motion1.Speed * (1 - _blendFactor) + _motion2.Speed * _blendFactor;

        /// <summary>두 모션의 이동 타입이 같으면 그대로, 다르면 정지 상태로 설정</summary>
        public FootStepAnalyzer.MovementType MovementType
        {
            get
            {
                if (_motion1.MovementType == _motion2.MovementType)
                {
                    return _motion1.MovementType;
                }
                else
                {
                    return FootStepAnalyzer.MovementType.Stationary;
                }
            }
        }

        public Bone RootBone => _motion1.RootBone;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 두 모션을 블렌딩하는 새로운 모션을 생성합니다.
        /// </summary>
        /// <param name="newName">새 블렌드 모션의 이름</param>
        /// <param name="motion1">첫 번째 모션</param>
        /// <param name="motion2">두 번째 모션</param>
        /// <param name="factor1">첫 번째 모션의 가중치</param>
        /// <param name="factor2">두 번째 모션의 가중치</param>
        /// <param name="blendFactor">초기 블렌딩 비율</param>
        public BlendMotion(string newName, Motionable motion1, Motionable motion2, float factor1, float factor2, float blendFactor)
        {
            _name = newName;

            _motion1 = motion1;
            _motion2 = motion2;

            _blendFactor = blendFactor;
            _factor1 = factor1;
            _factor2 = factor2;

            SetBlendFactor(blendFactor);
        }

        /// <summary>
        /// 블렌딩된 모션의 주기 시간을 설정합니다.
        /// </summary>
        public void SetPeriodTime(float periodTime)
        {
            _periodTime = periodTime;
        }

        /// <summary>
        /// 지정된 시간에서의 키프레임을 복제하여 반환합니다.
        /// </summary>
        /// <param name="time">키프레임을 생성할 시간</param>
        /// <returns>블렌딩된 키프레임</returns>
        public KeyFrame CloneKeyFrame(float time)
        {
            // 키프레임 재사용을 위한 캐시 처리
            if (_cloneKeyFrame == null)
            {
                _cloneKeyFrame = new KeyFrame(time);
            }
            else
            {
                _cloneKeyFrame.TimeStamp = time;
            }

            // 각 모션의 시간 계산
            float n = time / _periodTime;
            float t1 = _motion1.PeriodTime * n;
            float t2 = _motion2.PeriodTime * n;

            // 포즈 딕셔너리 초기화
            if (_outPose1 == null) _outPose1 = new Dictionary<string, Matrix4x4f>();
            if (_outPose2 == null) _outPose2 = new Dictionary<string, Matrix4x4f>();

            // 각 모션에서 포즈 계산
            _motion1.InterpolatePoseAtTime(t1, ref _outPose1);
            _motion2.InterpolatePoseAtTime(t2, ref _outPose2);

            // 블렌딩 알파값 계산
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);

            // 본 이름 캐시 생성 (최초 1회만)
            if (_boneNamesCache == null)
            {
                if (_outPose1.Count > 0)
                {
                    _boneNamesCache = _outPose1.Keys.ToArray();
                }
                else if (_outPose2.Count > 0)
                {
                    _boneNamesCache = _outPose2.Keys.ToArray();
                }
                else
                {
                    return null;
                }
            }

            // 각 본에 대해 블렌딩 수행
            for (int i = 0; i < _boneNamesCache.Length; i++)
            {
                string jointName = _boneNamesCache[i];
                Matrix4x4f currentTransform = Matrix4x4f.Identity;

                // 두 모션 모두에 해당 본이 있는 경우 - 구면 선형 보간
                if (_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    currentTransform = BoneTransform.InterpolateSlerp(_outPose1[jointName], _outPose2[jointName], alpha);
                }
                // 첫 번째 모션에만 있는 경우
                else if (_outPose1.ContainsKey(jointName))
                {
                    currentTransform = _outPose1[jointName];
                }
                // 두 번째 모션에만 있는 경우
                else if (_outPose2.ContainsKey(jointName))
                {
                    currentTransform = _outPose2[jointName];
                }
                else
                {
                    continue; // 해당 본이 없으면 건너뛰기
                }

                _cloneKeyFrame[jointName] = BoneTransform.FromMatrix(currentTransform);
            }

            return _cloneKeyFrame;
        }

        /// <summary>
        /// 블렌딩 비율을 설정하고 주기 시간을 다시 계산합니다.
        /// </summary>
        /// <param name="blendFactor">새로운 블렌딩 비율 (0~1)</param>
        public void SetBlendFactor(float blendFactor)
        {
            _blendFactor = blendFactor;
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);

            // 두 모션의 주기 시간을 보간하여 새로운 주기 시간 계산
            _periodTime = _motion1.PeriodTime * (1.0f - alpha) + _motion2.PeriodTime * alpha;
        }

        /// <summary>
        /// 지정된 시간에서 블렌딩된 포즈를 계산합니다.
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="outPose">결과 포즈를 저장할 딕셔너리</param>
        /// <param name="searchStartBone">검색 시작 본 (기본값: 루트 본)</param>
        /// <returns>성공 여부</returns>
        public bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose, Bone searchStartBone = null)
        {
            // 뼈가 지정되지 않은 경우 루트 본을 사용
            if (searchStartBone == null) searchStartBone = _motion1.RootBone;

            // 각 모션의 시간 계산
            float n = motionTime / _periodTime;
            float t1 = _motion1.PeriodTime * n;
            float t2 = _motion2.PeriodTime * n;

            // 포즈 딕셔너리 초기화
            if (_outPose1 == null) _outPose1 = new Dictionary<string, Matrix4x4f>();
            if (_outPose2 == null) _outPose2 = new Dictionary<string, Matrix4x4f>();

            // 각 모션에서 포즈 계산
            _motion1.InterpolatePoseAtTime(t1, ref _outPose1);
            _motion2.InterpolatePoseAtTime(t2, ref _outPose2);

            // 블렌딩 알파값 계산
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);

            // 본 이름 캐시 생성 (최초 1회만)
            if (_boneNamesCache == null)
            {
                if (_outPose1.Count > 0)
                {
                    _boneNamesCache = _outPose1.Keys.ToArray();
                }
                else if (_outPose2.Count > 0)
                {
                    _boneNamesCache = _outPose2.Keys.ToArray();
                }
                else
                {
                    return false; // 블렌딩할 포즈가 없음
                }
            }

            // 결과 포즈 초기화
            outPose.Clear();

            // 각 본에 대해 블렌딩 수행
            for (int i = 0; i < _boneNamesCache.Length; i++)
            {
                string jointName = _boneNamesCache[i];
                Matrix4x4f currentTransform = Matrix4x4f.Identity;

                // 두 모션 모두에 해당 본이 있는 경우 - 구면 선형 보간
                if (_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    currentTransform = BoneTransform.InterpolateSlerp(_outPose1[jointName], _outPose2[jointName], alpha);
                }
                // 첫 번째 모션에만 있는 경우
                else if (_outPose1.ContainsKey(jointName))
                {
                    currentTransform = _outPose1[jointName];
                }
                // 두 번째 모션에만 있는 경우
                else if (_outPose2.ContainsKey(jointName))
                {
                    currentTransform = _outPose2[jointName];
                }
                else
                {
                    continue; // 해당 본이 없으면 건너뛰기
                }

                outPose[jointName] = currentTransform;
            }

            return true;
        }
    }
}