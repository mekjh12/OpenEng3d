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

        string _name;

        Motionable _motion1;
        Motionable _motion2;

        float _factor1;
        float _factor2;

        float _blendFactor;
        float _periodTime;

        Dictionary<string, Matrix4x4f> _outPose1;
        Dictionary<string, Matrix4x4f> _outPose2;


        Dictionary<string, Matrix4x4f> _outPose;

        // 최적화
        string[] _boneNamesCache;
        KeyFrame _cloneKeyFrame;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public string Name => _name;
        public float PeriodTime => _periodTime;
        public float Speed => _motion1.Speed * (1 - _blendFactor) + _motion2.Speed * _blendFactor;
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

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

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

        public KeyFrame CloneKeyFrame(float time)
        {
            if (_cloneKeyFrame == null)
            {
                _cloneKeyFrame = new KeyFrame(time);
            }
            else
            {
                _cloneKeyFrame.TimeStamp = time;
            }

            float n = time / _periodTime;
            float t1 = _motion1.PeriodTime * n;
            float t2 = _motion2.PeriodTime * n;

            if (_outPose1 == null) _outPose1 = new Dictionary<string, Matrix4x4f>();
            if (_outPose2 == null) _outPose2 = new Dictionary<string, Matrix4x4f>();

            _motion1.InterpolatePoseAtTime(t1, ref _outPose1);
            _motion2.InterpolatePoseAtTime(t2, ref _outPose2);
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);

            // 뼈 이름 캐시 수정
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

            for (int i = 0; i < _boneNamesCache.Length; i++)
            {
                string jointName = _boneNamesCache[i];
                Matrix4x4f currentTransform = Matrix4x4f.Identity;

                // 두 개가 있는 경우
                if (_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    currentTransform = BoneTransform.InterpolateSlerp(_outPose1[jointName], _outPose2[jointName], alpha);
                }
                else if (_outPose1.ContainsKey(jointName))
                {
                    currentTransform = _outPose1[jointName];
                }
                else if (_outPose2.ContainsKey(jointName))
                {
                    currentTransform = _outPose2[jointName];
                }
                else
                {
                    continue;
                }

                _cloneKeyFrame[jointName] = BoneTransform.FromMatrix(currentTransform);
            }

            return _cloneKeyFrame;
        }

        public void SetBlendFactor(float blendFactor)
        {
            _blendFactor = blendFactor;
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);
            _periodTime = _motion1.PeriodTime * (1.0f - alpha) + _motion2.PeriodTime * alpha;
        }

        public bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            float n = motionTime / _periodTime;
            float t1 = _motion1.PeriodTime * n;
            float t2 = _motion2.PeriodTime * n;
                
            if (_outPose1 == null) _outPose1 = new Dictionary<string, Matrix4x4f>();
            if (_outPose2 == null) _outPose2 = new Dictionary<string, Matrix4x4f>();

            _motion1.InterpolatePoseAtTime(t1, ref _outPose1);
            _motion2.InterpolatePoseAtTime(t2, ref _outPose2);
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);

            // 뼈 이름 캐시 수정
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
                    return false;
                }
            }

            outPose.Clear();
            for (int i = 0; i < _boneNamesCache.Length; i++)
            {
                string jointName = _boneNamesCache[i];
                Matrix4x4f currentTransform = Matrix4x4f.Identity;

                // 두 개가 있는 경우
                if (_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    currentTransform = BoneTransform.InterpolateSlerp(_outPose1[jointName], _outPose2[jointName], alpha);
                }
                else if (_outPose1.ContainsKey(jointName))
                {
                    currentTransform = _outPose1[jointName];
                }
                else if (_outPose2.ContainsKey(jointName))
                {
                    currentTransform = _outPose2[jointName];
                }
                else
                {
                    continue;
                }

                outPose[jointName] = currentTransform;
            }

            return true;
        }
    }
}
