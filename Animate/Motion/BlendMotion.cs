using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Animate
{
    /// <summary>
    /// (W,T1, t1, s1)
    /// (R,T2, t2, s2)
    /// </summary>
    public class BlendMotion : Motionable
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        string _name;

        Motion _motion1;
        Motion _motion2;

        float _factor1;
        float _factor2;

        float _blendFactor;
        float _periodTime;

        Dictionary<string, Matrix4x4f> _outPose1;
        Dictionary<string, Matrix4x4f> _outPose2;

        // 최적화
        string[] _boneNamesCache;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public string Name => "blendmotionTest";
        public float Length => _periodTime;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public BlendMotion(string name, Motion motion1, Motion motion2, float factor1, float factor2, float blendFactor)
        {
            _name = name;

            _motion1 = motion1;
            _motion2 = motion2;

            _blendFactor = blendFactor;
            _factor1 = factor1;
            _factor2 = factor2;

            SetBlendFactor(blendFactor);
        }

        public void SetBlendFactor(float blendFactor)
        {
            _blendFactor = blendFactor;
            float alpha = (_blendFactor - _factor1) / (_factor2 - _factor1);
            _periodTime = _motion1.Length * (1.0f - alpha) + _motion2.Length * alpha;
        }

        public bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            float n = motionTime / _periodTime;
            float t1 = _motion1.Length * n;
            float t2 = _motion2.Length * n;

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

        /// <summary>
        /// 회전 변화가 크면 스무딩 적용
        /// </summary>
        private Matrix4x4f SmoothRotationIfNeeded(Matrix4x4f prevMatrix, Matrix4x4f currentMatrix)
        {
            // 회전 부분만 추출 (3x3 상단 왼쪽)
            ZetaExt.Quaternion prevRot = ExtractRotation(prevMatrix);
            ZetaExt.Quaternion currRot = ExtractRotation(currentMatrix);

            // 회전 차이 계산 (도 단위)
            float rotDiff = GetRotationDifference(prevRot, currRot);

            // 15도 이상 차이나면 스무딩 적용
            if (rotDiff > 25.0f)
            {
                return prevMatrix;
            }

            return currentMatrix;
        }

        /// <summary>
        /// 행렬에서 회전(쿼터니온) 추출
        /// </summary>
        private ZetaExt.Quaternion ExtractRotation(Matrix4x4f matrix)
        {
            // 스케일 제거를 위해 각 축 벡터 정규화
            float sx = new Vertex3f(matrix[0, 0], matrix[1, 0], matrix[2, 0]).Module();
            float sy = new Vertex3f(matrix[0, 1], matrix[1, 1], matrix[2, 1]).Module();
            float sz = new Vertex3f(matrix[0, 2], matrix[1, 2], matrix[2, 2]).Module();

            // 정규화된 회전 행렬
            float m00 = matrix[0, 0] / sx, m01 = matrix[0, 1] / sy, m02 = matrix[0, 2] / sz;
            float m10 = matrix[1, 0] / sx, m11 = matrix[1, 1] / sy, m12 = matrix[1, 2] / sz;
            float m20 = matrix[2, 0] / sx, m21 = matrix[2, 1] / sy, m22 = matrix[2, 2] / sz;

            // 쿼터니온 변환 (Shepperd's method)
            float trace = m00 + m11 + m22;
            if (trace > 0)
            {
                float s = (float)Math.Sqrt(trace + 1.0) * 2f;
                return new ZetaExt.Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s);
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1.0 + m00 - m11 - m22) * 2f;
                return new ZetaExt.Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1.0 + m11 - m00 - m22) * 2f;
                return new ZetaExt.Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            else
            {
                float s = (float)Math.Sqrt(1.0 + m22 - m00 - m11) * 2f;
                return new ZetaExt.Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s);
            }
        }

        /// <summary>
        /// 두 쿼터니온 간 각도 차이 (도 단위)
        /// </summary>
        private float GetRotationDifference(ZetaExt.Quaternion q1, ZetaExt.Quaternion q2)
        {
            float dot = Math.Abs(ZetaExt.Quaternion.Dot(q1, q2));
            dot = Math.Min(1.0f, dot);
            return 2.0f * (float)Math.Acos(dot) * 180.0f / (float)Math.PI;
        }

        /// <summary>
        /// 행렬의 회전 부분만 교체
        /// </summary>
        private Matrix4x4f ReplaceRotation(Matrix4x4f original, ZetaExt.Quaternion newRotation)
        {
            // 원본에서 스케일과 위치 보존
            float sx = new Vertex3f(original[0, 0], original[1, 0], original[2, 0]).Module();
            float sy = new Vertex3f(original[0, 1], original[1, 1], original[2, 1]).Module();
            float sz = new Vertex3f(original[0, 2], original[1, 2], original[2, 2]).Module();

            // 새 회전 행렬 생성
            Matrix4x4f rotMatrix = (Matrix4x4f)newRotation;

            // 스케일 적용
            rotMatrix[0, 0] *= sx; rotMatrix[0, 1] *= sy; rotMatrix[0, 2] *= sz;
            rotMatrix[1, 0] *= sx; rotMatrix[1, 1] *= sy; rotMatrix[1, 2] *= sz;
            rotMatrix[2, 0] *= sx; rotMatrix[2, 1] *= sy; rotMatrix[2, 2] *= sz;

            // 위치 복사
            rotMatrix[3, 0] = original[3, 0];
            rotMatrix[3, 1] = original[3, 1];
            rotMatrix[3, 2] = original[3, 2];

            return rotMatrix;
        }
    }
}
