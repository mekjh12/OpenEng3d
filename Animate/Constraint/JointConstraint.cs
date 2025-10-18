using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 관절 제한을 위한 추상 기본 클래스
    /// <br/>
    /// 다양한 관절 타입(구관절, 경첩관절, 복합관절)에 대한 공통 인터페이스를 제공한다.
    /// 각 관절의 생체역학적 특성에 맞는 움직임 제한을 구현할 수 있다.
    /// </summary>
    public abstract class JointConstraint
    {
        // -----------------------------------------------------------------------
        // 보호된 멤버 변수
        // -----------------------------------------------------------------------

        protected readonly Bone _bone; // 제어할 본
        protected bool _enabled; // 제한 활성화 여부
        protected readonly Matrix4x4f _bindPoseTransform; // 기준이 되는 바인딩 포즈 변환

        // -----------------------------------------------------------------------
        // 공개 속성
        // -----------------------------------------------------------------------

        public Bone Bone => _bone;
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public abstract string ConstraintType { get; }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// JointConstraint 기본 생성자
        /// </summary>
        /// <param name="bone">제어할 본</param>
        /// <exception cref="ArgumentNullException">bone이 null인 경우</exception>
        protected JointConstraint(Bone bone)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _enabled = true;

            // 바인딩 포즈를 기준 변환으로 저장
            _bindPoseTransform = bone.BoneMatrixSet.LocalBindTransform;
        }

        // -----------------------------------------------------------------------
        // 추상 메서드 (하위 클래스에서 구현 필수)
        // -----------------------------------------------------------------------

        /// <summary>
        /// 현재 변환에 관절 제한을 적용한다
        /// </summary>
        /// <param name="currentTransform">현재 로컬 변환 행렬</param>
        /// <returns>제한이 적용된 변환 행렬</returns>
        public abstract Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform);

        /// <summary>
        /// 주어진 변환이 관절 제한 범위 내에 있는지 확인한다
        /// </summary>
        /// <param name="transform">확인할 변환 행렬</param>
        /// <returns>제한 범위 내이면 true, 그렇지 않으면 false</returns>
        public abstract bool IsWithinLimits(Matrix4x4f transform);

        /// <summary>
        /// 관절별 제한 파라미터를 설정한다
        /// </summary>
        /// <param name="limits">제한 파라미터 배열 (관절 타입별로 다름)</param>
        /// <exception cref="ArgumentException">잘못된 파라미터 개수나 값</exception>
        public abstract void SetLimits(params float[] limits);

        // -----------------------------------------------------------------------
        // 가상 메서드 (하위 클래스에서 선택적 오버라이드)
        // -----------------------------------------------------------------------

        /// <summary>
        /// 바인딩 포즈 기준 상대 변환을 계산한다
        /// </summary>
        /// <param name="currentTransform">현재 변환</param>
        /// <returns>바인딩 포즈 기준 상대 변환</returns>
        protected virtual Matrix4x4f GetRelativeTransform(Matrix4x4f currentTransform)
        {
            return currentTransform * _bindPoseTransform.Inversed();
        }

        /// <summary>
        /// 상대 변환을 바인딩 포즈와 결합하여 최종 변환을 생성한다
        /// </summary>
        /// <param name="relativeTransform">상대 변환</param>
        /// <param name="originalPosition">원래 위치 (보존용)</param>
        /// <returns>최종 변환 행렬</returns>
        protected virtual Matrix4x4f CombineWithBindPose(Matrix4x4f relativeTransform, Vertex3f originalPosition)
        {
            Matrix4x4f result = relativeTransform * _bindPoseTransform;

            // 위치는 원래 값 유지 (회전만 제한)
            result[3, 0] = originalPosition.x;
            result[3, 1] = originalPosition.y;
            result[3, 2] = originalPosition.z;

            return result;
        }

        /// <summary>
        /// 제한 강도에 따라 원래 변환과 제한된 변환을 보간한다
        /// </summary>
        /// <param name="originalTransform">원래 변환</param>
        /// <param name="constrainedTransform">제한된 변환</param>
        /// <returns>보간된 최종 변환</returns>
        protected virtual Matrix4x4f InterpolateTransforms(Matrix4x4f originalTransform, Matrix4x4f constrainedTransform)
        {
            // 위치는 선형 보간
            Vertex3f originalPos = originalTransform.Position;
            Vertex3f constrainedPos = constrainedTransform.Position;
            Vertex3f finalPos = originalPos + (constrainedPos - originalPos);

            // 회전은 쿼터니언 구면 선형 보간
            ZetaExt.Quaternion originalRot = originalTransform.ToQuaternion();
            ZetaExt.Quaternion constrainedRot = constrainedTransform.ToQuaternion();
            ZetaExt.Quaternion finalRot = constrainedRot;

            // 스케일 선형 보간
            float scaleX = originalTransform.Column0.xyz().Length();
            float scaleY = originalTransform.Column1.xyz().Length();
            float scaleZ = originalTransform.Column2.xyz().Length();

            float constrainedScaleX = constrainedTransform.Column0.xyz().Length();
            float constrainedScaleY = constrainedTransform.Column1.xyz().Length();
            float constrainedScaleZ = constrainedTransform.Column2.xyz().Length();

            float finalScaleX = scaleX + (constrainedScaleX - scaleX);
            float finalScaleY = scaleY + (constrainedScaleY - scaleY);
            float finalScaleZ = scaleZ + (constrainedScaleZ - scaleZ);

            // 최종 변환 행렬 구성
            Matrix4x4f result = (Matrix4x4f)finalRot;

            // 스케일 적용
            Vertex3f col0 = result.Column0.xyz().Normalized * finalScaleX;
            Vertex3f col1 = result.Column1.xyz().Normalized * finalScaleY;
            Vertex3f col2 = result.Column2.xyz().Normalized * finalScaleZ;

            return new Matrix4x4f(
                col0.x, col0.y, col0.z, 0,
                col1.x, col1.y, col1.z, 0,
                col2.x, col2.y, col2.z, 0,
                finalPos.x, finalPos.y, finalPos.z, 1
            );
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 제한을 비활성화한다
        /// </summary>
        public virtual void Disable()
        {
            _enabled = false;
        }

        /// <summary>
        /// 제한을 활성화한다
        /// </summary>
        public virtual void Enable()
        {
            _enabled = true;
        }

        /// <summary>
        /// 관절 제한 정보를 문자열로 반환한다
        /// </summary>
        /// <returns>제한 정보 문자열</returns>
        public override string ToString()
        {
            return $"타입:{ConstraintType}, 활성화:{_enabled})";
        }
    }
}