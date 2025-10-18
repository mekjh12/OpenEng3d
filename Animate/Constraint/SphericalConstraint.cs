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
            _referenceFowardDirection = GetAxisVector(_forwardAxis, ref localBindTransform);
            _referenceUpDirection = GetAxisVector(_upAxis, ref localBindTransform);
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

            // 원본 위치와 스케일은 미리 추출 (재사용)
            Vertex3f originalPosition = currentTransform.Position;
            float scaleX = currentTransform.Column0.xyz().Length();
            float scaleY = currentTransform.Column1.xyz().Length();
            float scaleZ = currentTransform.Column2.xyz().Length();

            // 속성을 지역 변수로 받아서 ref 전달
            Matrix4x4f boneLocalTransform = _bone.BoneMatrixSet.LocalTransform;

            // 방향 벡터 추출 (한 번만)
            Vertex3f targetForwardDir = GetAxisVector(_forwardAxis, ref currentTransform);
            Vertex3f poseForwardAxis = GetAxisVector(_forwardAxis, ref boneLocalTransform);
            Vertex3f poseUpAxis = GetAxisVector(_upAxis, ref boneLocalTransform);

            // 각도 계산
            float coneAngle = GetConeAngleFast(ref _referenceFowardDirection, ref targetForwardDir);

            // 콘 각도 체크 - 제약이 필요 없으면 조기 반환
            bool needsConeConstraint = coneAngle > _maxConeAngle;

            // 콘 제약 적용
            Vertex3f constrainedForwardDir;
            if (needsConeConstraint)
            {
                constrainedForwardDir = ApplyConeConstraint(
                    ref _referenceFowardDirection,
                    ref targetForwardDir,
                    _maxConeAngle);
            }
            else
            {
                constrainedForwardDir = targetForwardDir;
            }

            // 트위스트 각도 계산 및 제약
            Quaternion poseToTarget = QuaternionExtensions.FromToRotation(poseForwardAxis, targetForwardDir);
            Quaternion bindToTarget = QuaternionExtensions.FromToRotation(_referenceFowardDirection, targetForwardDir);

            Vertex3f targetUpFromPose = (poseToTarget * poseUpAxis);
            Vertex3f targetUpFromBind = (bindToTarget * _referenceUpDirection);

            float twistAngle = GetConeAngleFast(ref targetUpFromBind, ref targetUpFromPose);
            float constrainedTwistAngle = twistAngle.Clamp(-_maxTwistAngle, _maxTwistAngle);

            // 제약된 변환 재구성 (in-place 계산으로 할당 최소화)
            Quaternion constrainedBindToTarget = QuaternionExtensions.FromToRotation(
                _referenceFowardDirection,
                constrainedForwardDir);

            Vertex3f constrainedUpBase = (constrainedBindToTarget * _referenceUpDirection).Normalized;
            Quaternion twistCorrection = new Quaternion(constrainedForwardDir, constrainedTwistAngle);
            Vertex3f constrainedUp = (twistCorrection * constrainedUpBase).Normalized;

            // Look Rotation 행렬 생성 (직접 계산으로 메서드 호출 오버헤드 제거)
            return CreateLookRotationOptimized(
                ref constrainedForwardDir,
                ref constrainedUp,
                ref originalPosition,
                scaleX, scaleY, scaleZ);
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

            Vertex3f currentFowardDirection = GetAxisVector(_forwardAxis, ref transform);
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

    /// <summary>
    /// 본의 로컬 축을 나타내는 열거형
    /// </summary>
    public enum LocalSpaceAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }
}