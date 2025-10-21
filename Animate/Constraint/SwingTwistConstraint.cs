using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 스윙-트위스트 분해 기반 관절 제한 클래스
    /// <br/>
    /// 회전을 스윙(본 방향 변화)과 트위스트(본 축 주변 회전)로 분해하여 각각 독립적으로 제한한다.
    /// <br/>
    /// 쿼터니언 대신 회전 행렬과 벡터 연산을 직접 사용하여 수학적 직관성을 높인다.
    /// </summary>
    public class SwingTwistConstraint : JointConstraint
    {
        private const float RAD_TO_DEG = 57.295779513f;
        private const float DEG_TO_RAD = 0.017453292f;
        private const float EPSILON = 0.001f;
        private const float EPSILON_SMALL = 0.0001f;

        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Vertex3f _xBind;  // 바인드 포즈 X축
        private Vertex3f _yBind;  // 바인드 포즈 Y축 (트위스트 축)
        private Vertex3f _zBind;  // 바인드 포즈 Z축

        private float _maxSwingAngle;   // 스윙 최대 각도 (도)
        private float _minTwistAngle;   // 트위스트 최소 각도 (도)
        private float _maxTwistAngle;   // 트위스트 최대 각도 (도)

        private LocalSpaceAxis _twistAxis; // 본의 길이 방향 축

        // 재사용 가능한 임시 변수들
        private Vertex3f _tempXTarget;
        private Vertex3f _tempYTarget;
        private Vertex3f _tempZTarget;
        private Vertex3f _tempSwingAxis;
        private Vertex3f _tempXSwing;
        private Vertex3f _tempZSwing;
        private Matrix4x4f _bindRotation;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public override string ConstraintType => "SwingTwistConstraint";
        public float MaxSwingAngle { get => _maxSwingAngle; set => _maxSwingAngle = value.Clamp(0, 180); }
        public float MinTwistAngle { get => _minTwistAngle; set => _minTwistAngle = value.Clamp(-180, 180); }
        public float MaxTwistAngle { get => _maxTwistAngle; set => _maxTwistAngle = value.Clamp(-180, 180); }
        public LocalSpaceAxis TwistAxis { get => _twistAxis; set => _twistAxis = value; }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public SwingTwistConstraint(Bone bone,
            float maxSwingAngle = 90f,
            float minTwistAngle = -90f,
            float maxTwistAngle = 90f,
            LocalSpaceAxis twistAxis = LocalSpaceAxis.Y)
            : base(bone)
        {
            _twistAxis = twistAxis;
            MaxSwingAngle = maxSwingAngle;
            MinTwistAngle = minTwistAngle;
            MaxTwistAngle = maxTwistAngle;

            // 바인드 포즈에서 정규직교 기저 추출
            _bindRotation = bone.BoneMatrixSet.LocalBindTransform;
            ExtractOrthonormalBasis(_bindRotation, ref _xBind, ref _yBind, ref _zBind);
        }

        // -----------------------------------------------------------------------
        // 제약 적용
        // -----------------------------------------------------------------------

        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            // 제한 없음 체크
            if (_maxSwingAngle >= 180f && _minTwistAngle <= -180f && _maxTwistAngle >= 180f)
                return currentTransform;

            // 1단계: 위치 및 스케일 추출
            float posX = currentTransform[3, 0];
            float posY = currentTransform[3, 1];
            float posZ = currentTransform[3, 2];

            float scaleX, scaleY, scaleZ;
            ExtractScale(currentTransform, out scaleX, out scaleY, out scaleZ);

            // 2단계: 타겟 프레임 추출
            ExtractOrthonormalBasis(currentTransform, ref _tempXTarget, ref _tempYTarget, ref _tempZTarget);

            // 3단계: 스윙 계산 및 제한
            float swingAngle;
            ComputeSwing(ref _yBind, ref _tempYTarget, ref _tempSwingAxis, out swingAngle);
            float swingAngleLimited = Math.Min(swingAngle, _maxSwingAngle);

            // 4단계: 중간 프레임 계산 (스윙만 적용)
            Matrix4x4f swingRotation = CreateRotationMatrix(ref _tempSwingAxis, swingAngleLimited * DEG_TO_RAD);
            ApplyRotationToVector(swingRotation, ref _xBind, ref _tempXSwing);
            ApplyRotationToVector(swingRotation, ref _zBind, ref _tempZSwing);

            // 5단계: 트위스트 계산 및 제한
            float twistAngle = ComputeTwist(ref _tempXSwing, ref _tempXTarget, ref _tempYTarget);
            float twistAngleLimited = twistAngle.Clamp(_minTwistAngle, _maxTwistAngle);

            // 디버그용 이름 표시
            _bone.TextNamePlate.CharacterName = $"Swing{swingAngleLimited:F0}도" + 
                $"Twist{twistAngleLimited:F0}도";

            // 6단계: 최종 회전 행렬 재구성
            Matrix4x4f swingLimited = CreateRotationMatrix(ref _tempSwingAxis, swingAngleLimited * DEG_TO_RAD);
            Matrix4x4f twistLimited = CreateRotationMatrix(ref _tempYTarget, twistAngleLimited * DEG_TO_RAD);
            Matrix4x4f finalRotation = MultiplyRotations(twistLimited, swingLimited) * _bindRotation;

            // 7단계: 스케일 및 위치 복원
            return ReconstructTransform(finalRotation, scaleX, scaleY, scaleZ, posX, posY, posZ);
        }

        // -----------------------------------------------------------------------
        // 핵심 수학 연산
        // -----------------------------------------------------------------------

        /// <summary>
        /// 행렬에서 정규직교 기저 추출
        /// </summary>
        private void ExtractOrthonormalBasis(Matrix4x4f matrix,
            ref Vertex3f xAxis, ref Vertex3f yAxis, ref Vertex3f zAxis)
        {
            xAxis.x = matrix[0, 0];
            xAxis.y = matrix[0, 1];
            xAxis.z = matrix[0, 2];
            NormalizeVector(ref xAxis);

            yAxis.x = matrix[1, 0];
            yAxis.y = matrix[1, 1];
            yAxis.z = matrix[1, 2];
            NormalizeVector(ref yAxis);

            zAxis.x = matrix[2, 0];
            zAxis.y = matrix[2, 1];
            zAxis.z = matrix[2, 2];
            NormalizeVector(ref zAxis);
        }

        /// <summary>
        /// 스윙 회전 계산: Y_bind를 Y_target으로 보내는 최단 회전
        /// </summary>
        private void ComputeSwing(ref Vertex3f yBind, ref Vertex3f yTarget,
            ref Vertex3f swingAxis, out float swingAngle)
        {
            float dotProduct = DotProduct(ref yBind, ref yTarget).Clamp(-1f, 1f);
            swingAngle = (float)Math.Acos(dotProduct) * RAD_TO_DEG;

            // 특수 케이스: 이미 정렬됨
            if (swingAngle < EPSILON)
            {
                swingAxis.x = 1f;
                swingAxis.y = 0f;
                swingAxis.z = 0f;
                swingAngle = 0f;
                return;
            }

            // 특수 케이스: 정반대 방향
            if (swingAngle > 180f - EPSILON)
            {
                swingAxis = _xBind;
                swingAngle = 180f;
                return;
            }

            // 일반 케이스: 외적으로 회전축 계산
            CrossProduct(ref yBind, ref yTarget, ref swingAxis);
            NormalizeVector(ref swingAxis);
        }

        /// <summary>
        /// 트위스트 각도 계산: X_swing을 X_target으로 보내는 Y_target 축 주변 회전
        /// </summary>
        private float ComputeTwist(ref Vertex3f xSwing, ref Vertex3f xTarget, ref Vertex3f yTarget)
        {
            Vertex3f cross;
            cross.x = xSwing.y * xTarget.z - xSwing.z * xTarget.y;
            cross.y = xSwing.z * xTarget.x - xSwing.x * xTarget.z;
            cross.z = xSwing.x * xTarget.y - xSwing.y * xTarget.x;

            float crossDotY = DotProduct(ref cross, ref yTarget);
            float xDot = DotProduct(ref xSwing, ref xTarget);

            return (float)Math.Atan2(crossDotY, xDot) * RAD_TO_DEG;
        }

        // -----------------------------------------------------------------------
        // 보조 수학 함수
        // -----------------------------------------------------------------------

        private void ExtractScale(Matrix4x4f matrix, out float scaleX, out float scaleY, out float scaleZ)
        {
            float c0x = matrix[0, 0], c0y = matrix[0, 1], c0z = matrix[0, 2];
            float c1x = matrix[1, 0], c1y = matrix[1, 1], c1z = matrix[1, 2];
            float c2x = matrix[2, 0], c2y = matrix[2, 1], c2z = matrix[2, 2];

            scaleX = (float)Math.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            scaleY = (float)Math.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            scaleZ = (float)Math.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);
        }

        private void NormalizeVector(ref Vertex3f v)
        {
            float lengthSq = v.x * v.x + v.y * v.y + v.z * v.z;
            if (lengthSq > EPSILON_SMALL * EPSILON_SMALL)
            {
                float invLength = 1f / (float)Math.Sqrt(lengthSq);
                v.x *= invLength;
                v.y *= invLength;
                v.z *= invLength;
            }
        }

        private float DotProduct(ref Vertex3f a, ref Vertex3f b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private void CrossProduct(ref Vertex3f a, ref Vertex3f b, ref Vertex3f result)
        {
            result.x = a.y * b.z - a.z * b.y;
            result.y = a.z * b.x - a.x * b.z;
            result.z = a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// 축-각도 표현으로부터 회전 행렬 생성 (쿼터니언 사용)
        /// </summary>
        private Matrix4x4f CreateRotationMatrix(ref Vertex3f axis, float angleRad)
        {
            if (Math.Abs(angleRad) < EPSILON_SMALL)
                return Matrix4x4f.Identity;

            float halfAngle = angleRad * 0.5f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            ZetaExt.Quaternion q = new ZetaExt.Quaternion(
                axis.x * sinHalf,
                axis.y * sinHalf,
                axis.z * sinHalf,
                cosHalf
            );

            return (Matrix4x4f)q;
        }

        private void ApplyRotationToVector(Matrix4x4f rotation, ref Vertex3f input, ref Vertex3f output)
        {
            output.x = rotation[0, 0] * input.x + rotation[1, 0] * input.y + rotation[2, 0] * input.z;
            output.y = rotation[0, 1] * input.x + rotation[1, 1] * input.y + rotation[2, 1] * input.z;
            output.z = rotation[0, 2] * input.x + rotation[1, 2] * input.y + rotation[2, 2] * input.z;
        }

        private Matrix4x4f MultiplyRotations(Matrix4x4f a, Matrix4x4f b)
        {
            Matrix4x4f result = new Matrix4x4f();

            for (uint row = 0; row < 3; row++)
            {
                for (uint col = 0; col < 3; col++)
                {
                    result[col, row] =
                        a[0, row] * b[col, 0] +
                        a[1, row] * b[col, 1] +
                        a[2, row] * b[col, 2];
                }
            }

            result[3, 3] = 1f;
            return result;
        }

        private Matrix4x4f ReconstructTransform(Matrix4x4f rotation,
            float scaleX, float scaleY, float scaleZ, float posX, float posY, float posZ)
        {
            return new Matrix4x4f(
                rotation[0, 0] * scaleX, rotation[0, 1] * scaleX, rotation[0, 2] * scaleX, 0,
                rotation[1, 0] * scaleY, rotation[1, 1] * scaleY, rotation[1, 2] * scaleY, 0,
                rotation[2, 0] * scaleZ, rotation[2, 1] * scaleZ, rotation[2, 2] * scaleZ, 0,
                posX, posY, posZ, 1
            );
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public override bool IsWithinLimits(Matrix4x4f transform)
        {
            if (!_enabled) return true;

            Vertex3f yTarget = new Vertex3f();
            ExtractOrthonormalBasis(transform, ref _tempXTarget, ref yTarget, ref _tempZTarget);

            float dotProduct = DotProduct(ref _yBind, ref yTarget).Clamp(-1f, 1f);
            float currentSwingAngle = (float)Math.Acos(dotProduct) * RAD_TO_DEG;

            return currentSwingAngle <= _maxSwingAngle;
        }

        public override void SetLimits(params float[] limits)
        {
            if (limits.Length < 1 || limits.Length > 3)
                throw new ArgumentException(
                    "SwingTwistConstraint requires 1-3 parameters: [maxSwing] or [maxSwing, minTwist, maxTwist]");

            MaxSwingAngle = limits[0];

            if (limits.Length >= 2)
            {
                MinTwistAngle = limits[1];
                MaxTwistAngle = limits[2];
            }
        }

        public override string ToString()
        {
            return $"스윙-트위스트 제한 (Swing: {_maxSwingAngle:F1}°, Twist: [{_minTwistAngle:F1}°, {_maxTwistAngle:F1}°], Enabled: {_enabled})";
        }
    }
}