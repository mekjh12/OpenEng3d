using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 경첩 관절(Hinge Joint) 제한 클래스
    /// <br/>
    /// 팔꿈치, 무릎 등의 1DOF 관절에 사용하는 단일 축 회전 제한을 구현한다.
    /// <br/>
    /// 본이 하나의 축을 중심으로만 회전하도록 제한하며, 최소/최대 각도 범위를 설정할 수 있다.
    /// </summary>
    public class HingeConstraint : JointConstraint
    {
        private const float RAD_TO_DEG = 57.295779513f; // 180 / Math.PI
        private const float DEG_TO_RAD = 0.017453292519943295f; // Math.PI / 180
        private const float EPSILON_SMALL = 0.0001f;

        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Vertex3f _hingeAxis;        // 회전 축 (바인딩 포즈 기준, 정규화됨)
        private float _minAngle;            // 최소 각도 (도)
        private float _maxAngle;            // 최대 각도 (도)
        private LocalSpaceAxis _rotationAxis; // 회전 축 (X, Y, Z)

        // 재사용 가능한 임시 변수들
        private Vertex3f _tempAxis;
        private Quaternion _tempRotation;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public override string ConstraintType => "HingeConstraint";
        public float MinAngle { get => _minAngle; set => _minAngle = value.Clamp(-180, 180); }
        public float MaxAngle { get => _maxAngle; set => _maxAngle = value.Clamp(-180, 180); }
        public Vertex3f HingeAxis => _hingeAxis;
        public LocalSpaceAxis RotationAxis { get => _rotationAxis; set => _rotationAxis = value; }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public HingeConstraint(Bone bone, float minAngle = -90f, float maxAngle = 90f,
             LocalSpaceAxis rotationAxis = LocalSpaceAxis.X)
            : base(bone)
        {
            _rotationAxis = rotationAxis;

            // 제한 각도 설정
            MinAngle = minAngle;
            MaxAngle = maxAngle;

            // 바인딩 포즈에서 회전 축 추출
            Matrix4x4f localBindTransform = bone.BoneMatrixSet.LocalBindTransform;
            LocalSpaceAxisHelper.GetAxisVector(_rotationAxis, localBindTransform, ref _hingeAxis);
        }

        // -----------------------------------------------------------------------
        // 추상 메서드 구현
        // -----------------------------------------------------------------------

        /// <summary>
        /// 현재 변환에 힌지 제한을 적용한다
        /// </summary>
        /// <param name="currentTransform">현재 로컬 변환 행렬</param>
        /// <returns>제한이 적용된 변환 행렬</returns>
        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            // 각도 범위가 전체 범위를 커버하면 제한 없음
            if (_minAngle <= -180f && _maxAngle >= 180f)
                return currentTransform;

            // 위치 추출
            float posX = currentTransform[3, 0];
            float posY = currentTransform[3, 1];
            float posZ = currentTransform[3, 2];

            // 스케일 계산
            float c0x = currentTransform[0, 0], c0y = currentTransform[0, 1], c0z = currentTransform[0, 2];
            float c1x = currentTransform[1, 0], c1y = currentTransform[1, 1], c1z = currentTransform[1, 2];
            float c2x = currentTransform[2, 0], c2y = currentTransform[2, 1], c2z = currentTransform[2, 2];

            float scaleX = (float)Math.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            float scaleY = (float)Math.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            float scaleZ = (float)Math.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);

            // 쿼터니언 변환
            Quaternion currentRotation = currentTransform.ToQuaternion();
            Quaternion bindRotation = _bone.BoneMatrixSet.LocalBindTransform.ToQuaternion();

            // 바인드 포즈 대비 상대 회전
            Quaternion relativeRotation = bindRotation.Inversed() * currentRotation;

            // 힌지 축 기준으로 각도 추출
            float currentAngle = ExtractAngleAroundAxis(relativeRotation, ref _hingeAxis);

            // 각도 제한 적용
            float constrainedAngle = currentAngle.Clamp(_minAngle, _maxAngle);

            // 제한된 각도가 현재 각도와 다르면 새 회전 생성
            Quaternion constrainedRelative;
            if (Math.Abs(currentAngle - constrainedAngle) > 0.01f)
            {
                // 힌지 축 기준으로 제한된 각도의 회전 생성
                float halfAngle = constrainedAngle * 0.5f * DEG_TO_RAD;
                float sinHalf = (float)Math.Sin(halfAngle);
                float cosHalf = (float)Math.Cos(halfAngle);

                constrainedRelative = new Quaternion(
                    _hingeAxis.x * sinHalf,
                    _hingeAxis.y * sinHalf,
                    _hingeAxis.z * sinHalf,
                    cosHalf
                );
            }
            else
            {
                constrainedRelative = relativeRotation;
            }

            // 최종 회전 계산
            Quaternion constrainedRotation = bindRotation * constrainedRelative;

            // 행렬로 변환
            Matrix4x4f result = (Matrix4x4f)constrainedRotation;

            // 스케일 복원
            float r0x = result[0, 0], r0y = result[0, 1], r0z = result[0, 2];
            float r1x = result[1, 0], r1y = result[1, 1], r1z = result[1, 2];
            float r2x = result[2, 0], r2y = result[2, 1], r2z = result[2, 2];

            float len0 = (float)Math.Sqrt(r0x * r0x + r0y * r0y + r0z * r0z);
            float len1 = (float)Math.Sqrt(r1x * r1x + r1y * r1y + r1z * r1z);
            float len2 = (float)Math.Sqrt(r2x * r2x + r2y * r2y + r2z * r2z);

            float invLen0 = len0 > EPSILON_SMALL ? scaleX / len0 : scaleX;
            float invLen1 = len1 > EPSILON_SMALL ? scaleY / len1 : scaleY;
            float invLen2 = len2 > EPSILON_SMALL ? scaleZ / len2 : scaleZ;

            return new Matrix4x4f(
                r0x * invLen0, r0y * invLen0, r0z * invLen0, 0,
                r1x * invLen1, r1y * invLen1, r1z * invLen1, 0,
                r2x * invLen2, r2y * invLen2, r2z * invLen2, 0,
                posX, posY, posZ, 1
            );
        }

        /// <summary>
        /// 주어진 변환이 힌지 제한 범위 내에 있는지 확인한다
        /// </summary>
        /// <param name="transform">확인할 변환 행렬</param>
        /// <returns>제한 범위 내이면 true</returns>
        public override bool IsWithinLimits(Matrix4x4f transform)
        {
            if (!_enabled)
                return true;

            // 현재 회전을 쿼터니언으로 변환
            Quaternion currentRotation = transform.ToQuaternion();
            Quaternion bindRotation = _bone.BoneMatrixSet.LocalBindTransform.ToQuaternion();

            // 상대 회전 계산
            Quaternion relativeRotation = bindRotation.Inversed() * currentRotation;

            // 힌지 축 기준 각도 추출
            float currentAngle = ExtractAngleAroundAxis(relativeRotation, ref _hingeAxis);

            // 범위 내에 있는지 확인
            return currentAngle >= _minAngle && currentAngle <= _maxAngle;
        }

        /// <summary>
        /// 힌지 제한 파라미터를 설정한다
        /// </summary>
        /// <param name="limits">limits[0]: 최소 각도, limits[1]: 최대 각도</param>
        /// <exception cref="ArgumentException">파라미터 개수가 맞지 않는 경우</exception>
        public override void SetLimits(params float[] limits)
        {
            if (limits.Length != 2)
                throw new ArgumentException("HingeConstraint requires 2 parameters: [minAngle, maxAngle]");

            MinAngle = limits[0];
            MaxAngle = limits[1];

            // 최소값이 최대값보다 크면 교환
            if (_minAngle > _maxAngle)
            {
                float temp = _minAngle;
                _minAngle = _maxAngle;
                _maxAngle = temp;
            }
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 쿼터니언에서 특정 축 기준 회전 각도를 추출한다
        /// </summary>
        /// <param name="rotation">회전 쿼터니언</param>
        /// <param name="axis">회전 축 (정규화됨)</param>
        /// <returns>회전 각도 (도)</returns>
        private float ExtractAngleAroundAxis(Quaternion rotation, ref Vertex3f axis)
        {
            // 쿼터니언의 벡터 부분을 축에 투영
            float dot = rotation.X * axis.x + rotation.Y * axis.y + rotation.Z * axis.z;

            // 투영된 벡터 계산
            float projX = axis.x * dot;
            float projY = axis.y * dot;
            float projZ = axis.z * dot;

            // 투영된 쿼터니언 생성 (축 방향 회전만 포함)
            _tempRotation.X = projX;
            _tempRotation.Y = projY;
            _tempRotation.Z = projZ;
            _tempRotation.W = rotation.W;

            // 정규화
            float lengthSq = _tempRotation.X * _tempRotation.X +
                           _tempRotation.Y * _tempRotation.Y +
                           _tempRotation.Z * _tempRotation.Z +
                           _tempRotation.W * _tempRotation.W;

            if (lengthSq < EPSILON_SMALL * EPSILON_SMALL)
                return 0f;

            float invLength = 1f / (float)Math.Sqrt(lengthSq);
            _tempRotation.X *= invLength;
            _tempRotation.Y *= invLength;
            _tempRotation.Z *= invLength;
            _tempRotation.W *= invLength;

            // 각도 계산
            float angle = 2f * (float)Math.Acos(Math.Max(-1f, Math.Min(1f, _tempRotation.W))) * RAD_TO_DEG;

            // 각도 부호 결정
            if (dot < 0)
                angle = -angle;

            return angle;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public override string ToString()
        {
            return $"힌지제한 (Min: {_minAngle:F1}°, Max: {_maxAngle:F1}°, Axis: {_rotationAxis}, Enabled: {_enabled})";
        }
    }
}