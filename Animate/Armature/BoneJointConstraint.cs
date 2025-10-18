using OpenGL;
using System;

namespace Animate
{
    public partial class Bone
    {
        // -----------------------------------------------------------------------
        // JointConstraint 관련 멤버
        // -----------------------------------------------------------------------

        private JointConstraint _jointConstraint;

        /// <summary>
        /// 이 본에 적용된 관절 제약
        /// </summary>
        public JointConstraint JointConstraint
        {
            get => _jointConstraint;
            set => _jointConstraint = value;
        }

        /// <summary>
        /// 관절 제약이 설정되어 있는지 확인한다
        /// </summary>
        public bool HasConstraint => _jointConstraint != null;

        // -----------------------------------------------------------------------
        // LocalTransform 설정 시 제약 적용
        // -----------------------------------------------------------------------

        /// <summary>
        /// 로컬 변환을 설정한다 (제약 적용됨)
        /// </summary>
        /// <param name="transform">설정할 변환 행렬</param>
        public void SetLocalTransform(Matrix4x4f transform)
        {
            Matrix4x4f finalTransform = transform;

            // 제약이 있고 활성화되어 있으면 적용
            if (_jointConstraint != null && _jointConstraint.Enabled)
            {
                finalTransform = _jointConstraint.ApplyConstraint(transform);
            }

            // 실제 LocalTransform에 적용
            BoneMatrixSet.LocalTransform = finalTransform;

            // 필요시 업데이트 플래그 설정 또는 즉시 업데이트
            // UpdateWorldTransform();
        }

        /// <summary>
        /// 관절 제약을 설정한다
        /// </summary>
        /// <param name="constraint">적용할 관절 제약</param>
        public void SetJointConstraint(JointConstraint constraint)
        {
            if (constraint != null && constraint.Bone != this)
            {
                throw new ArgumentException("Constraint must be created for this bone");
            }

            _jointConstraint = constraint;
        }

        /// <summary>
        /// 관절 제약을 제거한다
        /// </summary>
        public void RemoveJointConstraint()
        {
            _jointConstraint = null;
        }

        /// <summary>
        /// 현재 변환이 제약 범위 내에 있는지 확인한다
        /// </summary>
        /// <returns>제약 범위 내이면 true, 제약이 없으면 true</returns>
        public bool IsWithinConstraintLimits()
        {
            if (_jointConstraint == null || !_jointConstraint.Enabled)
                return true;

            return _jointConstraint.IsWithinLimits(BoneMatrixSet.LocalTransform);
        }

        /// <summary>
        /// 현재 변환에 제약을 강제 적용한다
        /// </summary>
        public void ApplyConstraint()
        {
            if (_jointConstraint != null && _jointConstraint.Enabled)
            {
                Matrix4x4f constrained = _jointConstraint.ApplyConstraint(BoneMatrixSet.LocalTransform);
                BoneMatrixSet.LocalTransform = constrained;
            }
        }
    }
}
