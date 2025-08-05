using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    public class BlendMotion : Motion
    {
        Motion _motion1;
        Motion _motion2;
        float _s1;
        float _s2;

        float _blendFactor;

        TimeFinder _timeFinder;
        bool _isInitTimeFinder = false; // 시간 찾기 유틸리티 초기화 여부

        Dictionary<string, Matrix4x4f> _outPose1;
        Dictionary<string, Matrix4x4f> _outPose2;

        Bone[] _bones;

        public BlendMotion(Bone[] bones, Motion motion1, Motion motion2, float s1, float s2, float blendFactor): base("", s1)
        {
            _bones = bones;
            // (W,T1, t1, s1)
            // (R,T2, t2, s2)
            _motion1 = motion1;
            _motion2 = motion2;

            _blendFactor = blendFactor;
            _s1 = s1;
            _s2 = s2;

            SetBlendFactor(blendFactor);
        }

        public void SetBlendFactor(float blendFactor)
        {
            _blendFactor = blendFactor;
            float alpha = (_blendFactor - _s1) / (_s2 - _s1);
            _length = _motion1.Length * (1.0f - alpha) + _motion2.Length * alpha;
        }

        public override bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            float n = motionTime / _length;
            float t1 = _motion1.Length * n;
            float t2 = _motion2.Length * n;

            if (_outPose1 == null) _outPose1 = new Dictionary<string, Matrix4x4f>();
            if (_outPose2 == null) _outPose2 = new Dictionary<string, Matrix4x4f>();

            _motion1.InterpolatePoseAtTime(t1, ref _outPose1);
            _motion2.InterpolatePoseAtTime(t2, ref _outPose2);

            float alpha = (_blendFactor - _s1) / (_s2 - _s1);

            for (int i = 0; i < _bones.Length; i++)
            {
                Bone bone =_bones[i];
                string jointName = bone.Name;
                if (_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    outPose[jointName] = BoneTransform.InterpolateSlerp(_outPose1[jointName], _outPose2[jointName], alpha);
                }
                else if (_outPose1.ContainsKey(jointName) && !_outPose2.ContainsKey(jointName))
                {
                    outPose[jointName] = _outPose1[jointName];
                }
                else if (!_outPose1.ContainsKey(jointName) && _outPose2.ContainsKey(jointName))
                {
                    outPose[jointName] = _outPose2[jointName];
                }
            }

            return true;
        }
    }
}
