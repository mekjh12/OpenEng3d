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
        /// <summary>
        /// 현재 변환에 구면 제한을 적용한다 (개선된 알고리즘)
        /// </summary>
        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            // 조기 반환 최적화
            if (_maxConeAngle >= 180f && _maxTwistAngle >= 180f)
                return currentTransform;

            // 원본 데이터 추출
            Vertex3f originalPosition = currentTransform.Position;
            float scaleX = currentTransform.Column0.xyz().Length();
            float scaleY = currentTransform.Column1.xyz().Length();
            float scaleZ = currentTransform.Column2.xyz().Length();

            // 현재 회전을 쿼터니언으로 변환
            Quaternion currentRotation = currentTransform.ToQuaternion();
            Quaternion bindRotation = _bone.BoneMatrixSet.LocalBindTransform.ToQuaternion();

            // 바인드 포즈 대비 상대 회전
            Quaternion relativeRotation = bindRotation.Inversed() * currentRotation;

            // Swing-Twist 분해
            Quaternion swing, twist;
            DecomposeSwingTwist(relativeRotation, _referenceFowardDirection, out swing, out twist);

            // Swing 제약 (Cone 각도)
            Quaternion constrainedSwing = ConstrainSwing(swing, _maxConeAngle);

            // Twist 제약 (비틀림 각도)
            Quaternion constrainedTwist = ConstrainTwist(twist, _maxTwistAngle);

            // 제약된 회전 재구성
            Quaternion constrainedRelative = constrainedSwing * constrainedTwist;
            Quaternion constrainedRotation = bindRotation * constrainedRelative;

            // 행렬로 변환
            Matrix4x4f result = (Matrix4x4f)constrainedRotation;

            // 스케일 복원
            Vertex3f col0 = result.Column0.xyz().Normalized * scaleX;
            Vertex3f col1 = result.Column1.xyz().Normalized * scaleY;
            Vertex3f col2 = result.Column2.xyz().Normalized * scaleZ;

            return new Matrix4x4f(
                col0.x, col0.y, col0.z, 0,
                col1.x, col1.y, col1.z, 0,
                col2.x, col2.y, col2.z, 0,
                originalPosition.x, originalPosition.y, originalPosition.z, 1
            );
        }

        /// <summary>
        /// 쿼터니언을 Swing과 Twist로 분해한다
        /// </summary>
        private void DecomposeSwingTwist(Quaternion rotation, Vertex3f direction,
            out Quaternion swing, out Quaternion twist)
        {
            // Twist 축을 정규화
            Vertex3f twistAxis = direction.Normalized;

            // 쿼터니언의 벡터 부분을 Twist 축에 투영
            Vertex3f rotationAxis = new Vertex3f(rotation.X, rotation.Y, rotation.Z);
            float dot = rotationAxis.Dot(twistAxis);
            Vertex3f projection = twistAxis * dot;

            // Twist 쿼터니언 생성
            twist = new Quaternion(projection.x, projection.y, projection.z, rotation.W);

            // Twist가 거의 0이면 항등 쿼터니언
            float twistLength = (float)Math.Sqrt(
                twist.X * twist.X +
                twist.Y * twist.Y +
                twist.Z * twist.Z +
                twist.W * twist.W);

            if (twistLength < EPSILON_SMALL)
            {
                twist = Quaternion.Identity;
            }
            else
            {
                // 정규화
                float invLength = 1f / twistLength;
                twist = new Quaternion(
                    twist.X * invLength,
                    twist.Y * invLength,
                    twist.Z * invLength,
                    twist.W * invLength);
            }

            // Swing = Rotation * Twist^(-1)
            swing = rotation * twist.Inversed();
        }

        /// <summary>
        /// Swing 회전을 제약한다 (Cone 각도)
        /// </summary>
        private Quaternion ConstrainSwing(Quaternion swing, float maxAngle)
        {
            // Swing 각도 계산
            float angle = 2f * (float)Math.Acos(Math.Max(-1f, Math.Min(1f, swing.W))) * RAD_TO_DEG;

            // 제약 필요 없으면 그대로 반환
            if (angle <= maxAngle)
                return swing;

            // 회전축 추출
            Vertex3f axis = new Vertex3f(swing.X, swing.Y, swing.Z);
            float axisLength = axis.Length();

            if (axisLength < EPSILON_SMALL)
                return Quaternion.Identity;

            // 정규화
            axis = axis * (1f / axisLength);

            // 제한된 각도로 새 쿼터니언 생성
            float halfAngle = maxAngle * 0.5f * (float)Math.PI / 180f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            return new Quaternion(
                axis.x * sinHalf,
                axis.y * sinHalf,
                axis.z * sinHalf,
                cosHalf
            );
        }

        /// <summary>
        /// Twist 회전을 제약한다 (비틀림 각도)
        /// </summary>
        private Quaternion ConstrainTwist(Quaternion twist, float maxAngle)
        {
            // Twist 각도 계산
            float angle = 2f * (float)Math.Acos(Math.Max(-1f, Math.Min(1f, twist.W))) * RAD_TO_DEG;

            // 각도 부호 결정
            Vertex3f twistAxis = new Vertex3f(twist.X, twist.Y, twist.Z);
            if (twistAxis.Dot(_referenceFowardDirection) < 0)
                angle = -angle;

            // 제약 적용
            float constrainedAngle = angle.Clamp(-maxAngle, maxAngle);

            // 제약이 필요 없으면 그대로 반환
            if (Math.Abs(angle - constrainedAngle) < 0.1f)
                return twist;

            // 제한된 각도로 새 쿼터니언 생성
            float halfAngle = constrainedAngle * 0.5f * (float)Math.PI / 180f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            Vertex3f normalizedAxis = _referenceFowardDirection.Normalized;

            return new Quaternion(
                normalizedAxis.x * sinHalf,
                normalizedAxis.y * sinHalf,
                normalizedAxis.z * sinHalf,
                cosHalf
            );
        }

        /// <summary>
        /// 두 쿼터니언이 같은 방향으로 회전하도록 보정한다
        /// </summary>
        private Quaternion EnsureShortestPath(Quaternion from, Quaternion to)
        {
            // 내적이 음수면 반대 방향으로 회전하는 것
            float dot = from.X * to.X + from.Y * to.Y + from.Z * to.Z + from.W * to.W;

            if (dot < 0f)
            {
                // 반대 부호로 변경 (같은 회전을 나타내지만 최단 경로)
                return new Quaternion(-to.X, -to.Y, -to.Z, -to.W);
            }

            return to;
        }

        /// <summary>
        /// 콘 제약을 적용한 방향 벡터 계산 (GC 최소화)
        /// </summary>
        private Vertex3f ApplyConeConstraint(ref Vertex3f bindForward, ref Vertex3f targetForward, float maxAngle)
        {
            Vertex3f rotationAxis = bindForward.Cross(targetForward);
            float axisLength = rotationAxis.Length();

            if (axisLength > 0.001f)
            {
                rotationAxis = rotationAxis * (1f / axisLength); // Normalized 대신 직접 계산
                Quaternion coneLimit = new Quaternion(rotationAxis, maxAngle);
                return (coneLimit * bindForward).Normalized;
            }

            return bindForward;
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
        /// Look Rotation 생성 최적화 버전
        /// </summary>
        private Matrix4x4f CreateLookRotationOptimized(
            ref Vertex3f forward,
            ref Vertex3f up,
            ref Vertex3f position,
            float scaleX, float scaleY, float scaleZ)
        {
            // Right 벡터 계산 (Cross 직접 계산)
            float rx = up.y * forward.z - up.z * forward.y;
            float ry = up.z * forward.x - up.x * forward.z;
            float rz = up.x * forward.y - up.y * forward.x;
            float rLen = (float)Math.Sqrt(rx * rx + ry * ry + rz * rz);

            if (rLen > EPSILON_SMALL)
            {
                float invRLen = 1f / rLen;
                rx *= invRLen;
                ry *= invRLen;
                rz *= invRLen;
            }

            // Up 재계산 (직교성 보장)
            float ux = forward.y * rz - forward.z * ry;
            float uy = forward.z * rx - forward.x * rz;
            float uz = forward.x * ry - forward.y * rx;
            float uLen = (float)Math.Sqrt(ux * ux + uy * uy + uz * uz);

            if (uLen > EPSILON_SMALL)
            {
                float invULen = 1f / uLen;
                ux *= invULen;
                uy *= invULen;
                uz *= invULen;
            }

            // Forward 축에 따라 행렬 구성 (스케일 직접 적용)
            switch (_forwardAxis)
            {
                case LocalSpaceAxis.X:
                    return new Matrix4x4f(
                        forward.x * scaleX, forward.y * scaleX, forward.z * scaleX, 0,
                        ux * scaleY, uy * scaleY, uz * scaleY, 0,
                        rx * scaleZ, ry * scaleZ, rz * scaleZ, 0,
                        position.x, position.y, position.z, 1
                    );
                case LocalSpaceAxis.Y:
                    return new Matrix4x4f(
                        rx * scaleX, ry * scaleX, rz * scaleX, 0,
                        forward.x * scaleY, forward.y * scaleY, forward.z * scaleY, 0,
                        ux * scaleZ, uy * scaleZ, uz * scaleZ, 0,
                        position.x, position.y, position.z, 1
                    );
                case LocalSpaceAxis.Z:
                    return new Matrix4x4f(
                        rx * scaleX, ry * scaleX, rz * scaleX, 0,
                        ux * scaleY, uy * scaleY, uz * scaleY, 0,
                        forward.x * scaleZ, forward.y * scaleZ, forward.z * scaleZ, 0,
                        position.x, position.y, position.z, 1
                    );
                default:
                    return Matrix4x4f.Identity;
            }
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

            Vertex3f currentFowardDirection = Vertex3f.Zero;
            LocalSpaceAxisHelper.GetAxisVector(_forwardAxis,  transform, ref currentFowardDirection);
            float currentConeAngle = GetConeAngleFast(ref _referenceFowardDirection, ref currentFowardDirection);
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
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 축 벡터 추출 (ref로 전달하여 복사 최소화)
        /// </summary>
        private Vertex3f GetAxisVector(LocalSpaceAxis axis, ref Matrix4x4f transform)
        {
            Vertex3f vec;
            switch (axis)
            {
                case LocalSpaceAxis.X:
                    vec = transform.Column0.xyz();
                    break;
                case LocalSpaceAxis.Y:
                    vec = transform.Column1.xyz();
                    break;
                case LocalSpaceAxis.Z:
                    vec = transform.Column2.xyz();
                    break;
                default:
                    return Vertex3f.UnitY;
            }

            // Normalized 호출 최소화
            float length = vec.Length();
            return length > 0.0001f ? vec * (1f / length) : vec;
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