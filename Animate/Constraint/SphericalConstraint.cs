using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 구관절(Ball-and-Socket Joint) 제한 클래스
    /// <br/>
    /// 어깨, 고관절 등의 3DOF 관절에 사용하는 원뿔 형태 제한을 구현한다.
    /// <br/>
    /// 본의 방향벡터가 기준 방향을 중심으로 한 원뿔 범위 내에서만 움직이도록 제한한다.
    /// </summary>
    public class SphericalConstraint : JointConstraint
    {
        private const float RAD_TO_DEG = 57.295779513f; // 180 / Math.PI
        private const float EPSILON = 0.001f;
        private const float EPSILON_SMALL = 0.0001f;

        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Vertex3f _referenceFowardDirection; // 기준 전방 방향 (바인딩 포즈)
        private Vertex3f _referenceUpDirection;     // 기준 상방 방향 (바인딩 포즈)
        private float _maxConeAngle;                // 원뿔 최대 각도 (도)
        private float _maxTwistAngle;               // 트위스트 최대 각도 (도)
        private LocalSpaceAxis _forwardAxis;        // 본의 전방 축 (X, Y, Z)
        private LocalSpaceAxis _upAxis;             // 본의 상방 축

        // 재사용 가능한 임시 변수들 (클래스 레벨)
        private Vertex3f _tempRotationAxis;
        private Quaternion _tempSwing;
        private Quaternion _tempTwist;
        private Vertex3f _tempForward;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public override string ConstraintType => "SphericalConstraint";
        public float MaxConeAngle { get => _maxConeAngle; set => _maxConeAngle = value.Clamp(0, 180); }
        public float MaxTwistAngle { get => _maxTwistAngle; set => _maxTwistAngle = value.Clamp(0, 180); }
        public Vertex3f ReferenceUpDirection => _referenceUpDirection;
        public Vertex3f ReferenceFowardDirection => _referenceFowardDirection;
        public LocalSpaceAxis ForwardAxis { get => _forwardAxis; set => _forwardAxis = value; }
        public LocalSpaceAxis UpAxis { get => _upAxis; set => _upAxis = value; }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public SphericalConstraint(Bone bone, float maxConeAngle = 40f, float maxTwistAngle = 90f,
             LocalSpaceAxis forwardAxis = LocalSpaceAxis.Y,
             LocalSpaceAxis upAxis = LocalSpaceAxis.Z)
            : base(bone)
        {
            // 축 설정
            _forwardAxis = forwardAxis;
            _upAxis = upAxis;

            // 제한 각도 설정
            MaxConeAngle = maxConeAngle;
            MaxTwistAngle = maxTwistAngle;

            // 바인딩 포즈에서 기준 방향 추출
            Matrix4x4f localBindTransform = bone.BoneMatrixSet.LocalBindTransform;
            LocalSpaceAxisHelper.GetAxisVector(_forwardAxis, localBindTransform, ref _referenceFowardDirection);
            LocalSpaceAxisHelper.GetAxisVector(_upAxis, localBindTransform, ref _referenceUpDirection);
        }

        // -----------------------------------------------------------------------
        // 추상 메서드 구현
        // -----------------------------------------------------------------------
        /// <summary>
        /// 현재 변환에 구면 제한을 적용한다
        /// </summary>
        /// <param name="currentTransform">현재 로컬 변환 행렬</param>
        /// <returns>제한이 적용된 변환 행렬</returns>
        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            if (_maxConeAngle >= 180f && _maxTwistAngle >= 180f)
                return currentTransform;

            // 위치 추출 (Position 프로퍼티는 새 구조체 생성)
            float posX = currentTransform[3, 0];
            float posY = currentTransform[3, 1];
            float posZ = currentTransform[3, 2];

            // 스케일 계산 (Length() 대신 직접 계산)
            float c0x = currentTransform[0, 0], c0y = currentTransform[0, 1], c0z = currentTransform[0, 2];
            float c1x = currentTransform[1, 0], c1y = currentTransform[1, 1], c1z = currentTransform[1, 2];
            float c2x = currentTransform[2, 0], c2y = currentTransform[2, 1], c2z = currentTransform[2, 2];

            float scaleX = (float)Math.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            float scaleY = (float)Math.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            float scaleZ = (float)Math.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);

            // 쿼터니언 변환 (여기는 어쩔 수 없음)
            Quaternion currentRotation = currentTransform.ToQuaternion();
            Quaternion bindRotation = _bone.BoneMatrixSet.LocalBindTransform.ToQuaternion();

            Quaternion relativeRotation = bindRotation.Inversed() * currentRotation;

            // 재사용 변수 사용
            DecomposeSwingTwist(relativeRotation, ref _referenceFowardDirection,
                               ref _tempSwing, ref _tempTwist);

            _tempSwing = ConstrainSwing(_tempSwing, _maxConeAngle);
            _tempTwist = ConstrainTwist(_tempTwist, _maxTwistAngle);

            Quaternion constrainedRelative = _tempSwing * _tempTwist;
            Quaternion constrainedRotation = bindRotation * constrainedRelative;

            Matrix4x4f result = (Matrix4x4f)constrainedRotation;

            // 스케일 복원 (Normalized 대신 직접 계산)
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
        /// 쿼터니언을 Swing과 Twist로 분해한다
        /// </summary>
        private void DecomposeSwingTwist(Quaternion rotation, ref Vertex3f direction,
            ref Quaternion swing, ref Quaternion twist)
        {
            _tempRotationAxis.x = rotation.X;
            _tempRotationAxis.y = rotation.Y;
            _tempRotationAxis.z = rotation.Z;

            float dot = _tempRotationAxis.x * direction.x +
                        _tempRotationAxis.y * direction.y +
                        _tempRotationAxis.z * direction.z;

            // projection 계산 (새 구조체 생성 없이)
            float projX = direction.x * dot;
            float projY = direction.y * dot;
            float projZ = direction.z * dot;

            // Twist 쿼터니언 생성
            twist.X = projX;
            twist.Y = projY;
            twist.Z = projZ;
            twist.W = rotation.W;

            // Twist가 거의 0이면 항등 쿼터니언
            float twistLength = (float)Math.Sqrt(
                twist.X * twist.X +
                twist.Y * twist.Y +
                twist.Z * twist.Z +
                twist.W * twist.W);

            if (twistLength < EPSILON_SMALL)
            {
                twist.X = 0f;
                twist.Y = 0f;
                twist.Z = 0f;
                twist.W = 1f;
            }
            else
            {
                // 정규화
                float invLength = 1f / twistLength;
                twist.X *= invLength;
                twist.Y *= invLength;
                twist.Z *= invLength;
                twist.W *= invLength;
            }

            // Swing = Rotation * Twist^(-1)
            swing = rotation * twist.Inversed();
        }

        /// <summary>
        /// Swing 회전을 제약한다 (Cone 각도)
        /// </summary>
        private Quaternion ConstrainSwing(Quaternion swing, float maxAngle)
        {
            float angle = 2f * (float)Math.Acos(Math.Max(-1f, Math.Min(1f, swing.W))) * RAD_TO_DEG;

            if (angle <= maxAngle)
                return swing;

            // 축 길이 계산 (Vertex3f 생성 없이)
            float axisLengthSq = swing.X * swing.X + swing.Y * swing.Y + swing.Z * swing.Z;

            if (axisLengthSq < EPSILON_SMALL * EPSILON_SMALL)
                return Quaternion.Identity;

            // 정규화 (직접 계산)
            float invLength = 1f / (float)Math.Sqrt(axisLengthSq);
            float axisX = swing.X * invLength;
            float axisY = swing.Y * invLength;
            float axisZ = swing.Z * invLength;

            float halfAngle = maxAngle * 0.5f * (float)Math.PI / 180f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            return new Quaternion(
                axisX * sinHalf,
                axisY * sinHalf,
                axisZ * sinHalf,
                cosHalf
            );
        }

        /// <summary>
        /// Twist 회전을 제약한다 (비틀림 각도)
        /// </summary>
        private Quaternion ConstrainTwist(Quaternion twist, float maxAngle)
        {
            float angle = 2f * (float)Math.Acos(Math.Max(-1f, Math.Min(1f, twist.W))) * RAD_TO_DEG;

            // 각도 부호 결정 (Vertex3f 생성 없이)
            float dot = twist.X * _referenceFowardDirection.x +
                        twist.Y * _referenceFowardDirection.y +
                        twist.Z * _referenceFowardDirection.z;
            if (dot < 0)
                angle = -angle;

            float constrainedAngle = angle.Clamp(-maxAngle, maxAngle);

            if (Math.Abs(angle - constrainedAngle) < 0.1f)
                return twist;

            // 정규화된 축 직접 사용
            float axisX = _referenceFowardDirection.x;
            float axisY = _referenceFowardDirection.y;
            float axisZ = _referenceFowardDirection.z;

            float halfAngle = constrainedAngle * 0.5f * (float)Math.PI / 180f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            return new Quaternion(
                axisX * sinHalf,
                axisY * sinHalf,
                axisZ * sinHalf,
                cosHalf
            );
        }

        /// <summary>
        /// 각도 계산 최적화 버전 (ref 사용)
        /// </summary>
        private float GetConeAngleFast(ref Vertex3f v1, ref Vertex3f v2)
        {
            float dotProduct = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
            dotProduct = Math.Max(-1f, Math.Min(1f, dotProduct));
            return (float)(Math.Acos(dotProduct) * RAD_TO_DEG);
        }

        /// <summary>
        /// 주어진 변환이 구면 제한 범위 내에 있는지 확인한다
        /// </summary>
        /// <param name="transform">확인할 변환 행렬</param>
        /// <returns>제한 범위 내이면 true</returns>
        public override bool IsWithinLimits(Matrix4x4f transform)
        {
            if (!_enabled)
                return true;

            LocalSpaceAxisHelper.GetAxisVector(_forwardAxis, transform, ref _tempForward);
            float currentConeAngle = GetConeAngleFast(ref _referenceFowardDirection, ref _tempForward);
            return currentConeAngle <= _maxConeAngle;
        }

        /// <summary>
        /// 구면 제한 파라미터를 설정한다
        /// </summary>
        /// <param name="limits">limits[0]: 원뿔 각도, limits[1]: 트위스트 각도 (선택)</param>
        /// <exception cref="ArgumentException">파라미터 개수가 맞지 않는 경우</exception>
        public override void SetLimits(params float[] limits)
        {
            if (limits.Length < 1 || limits.Length > 2)
                throw new ArgumentException("SphericalConstraint requires 1-2 parameters: [coneAngle] or [coneAngle, twistAngle]");

            MaxConeAngle = limits[0];
            if (limits.Length == 2)
                MaxTwistAngle = limits[1];
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public override string ToString()
        {
            return $"원뿔제한 (Cone: {_maxConeAngle:F1}°, Twist: {_maxTwistAngle:F1}°, Enabled: {_enabled})";
        }
    }

}