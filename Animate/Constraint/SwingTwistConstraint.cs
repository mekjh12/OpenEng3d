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
    /// GC 압박을 최소화하기 위해 모든 임시 변수를 멤버로 재사용한다.
    /// </summary>
    public class SwingTwistConstraint : JointConstraint
    {
        private const float RAD_TO_DEG = 57.295779513f;
        private const float DEG_TO_RAD = 0.017453292f;
        private const float EPSILON = 0.001f;
        private const float EPSILON_SMALL = 0.0001f;

        // -----------------------------------------------------------------------
        // 바인드 포즈 정보
        // -----------------------------------------------------------------------

        private Vertex3f _xBind;  // 바인드 포즈 X축
        private Vertex3f _yBind;  // 바인드 포즈 Y축 (트위스트 축)
        private Vertex3f _zBind;  // 바인드 포즈 Z축
        private Matrix4x4f _bindRotation;  // 바인드 포즈 회전 행렬

        // -----------------------------------------------------------------------
        // 제한 파라미터
        // -----------------------------------------------------------------------

        private float _maxSwingAngle;   // 스윙 최대 각도 (도)
        private float _minTwistAngle;   // 트위스트 최소 각도 (도)
        private float _maxTwistAngle;   // 트위스트 최대 각도 (도)
        private LocalSpaceAxis _twistAxis; // 본의 길이 방향 축

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 타겟 프레임 추출용
        // -----------------------------------------------------------------------

        private Vertex3f _tempXTarget;  // 현재 변환의 X축
        private Vertex3f _tempYTarget;  // 현재 변환의 Y축
        private Vertex3f _tempZTarget;  // 현재 변환의 Z축

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 스윙 계산용
        // -----------------------------------------------------------------------

        private Vertex3f _tempSwingAxis;  // 스윙 회전축
        private Vertex3f _tempXSwing;     // 스윙 적용 후 X축
        private Vertex3f _tempYSwing;     // 스윙 적용 후 Y축
        private Vertex3f _tempZSwing;     // 스윙 적용 후 Z축

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 트위스트 계산용
        // -----------------------------------------------------------------------

        private Vertex3f _tempCross;           // 외적 결과 저장용
        private Vertex3f _tempReferenceSwing;  // 스윙 적용 후 참조 벡터

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 행렬 연산용
        // -----------------------------------------------------------------------

        private Matrix4x4f _tempSwingRotation;   // 스윙 회전 행렬
        private Matrix4x4f _tempTwistRotation;   // 트위스트 회전 행렬
        private Matrix4x4f _tempCombinedRotation; // 결합 회전 행렬
        private Matrix4x4f _tempFinalRotation;    // 최종 회전 행렬

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

        /// <summary>
        /// SwingTwistConstraint를 생성한다.
        /// </summary>
        /// <param name="bone">제약을 적용할 본</param>
        /// <param name="maxSwingAngle">스윙 최대 각도 (도)</param>
        /// <param name="minTwistAngle">트위스트 최소 각도 (도)</param>
        /// <param name="maxTwistAngle">트위스트 최대 각도 (도)</param>
        /// <param name="twistAxis">트위스트 축 (본의 길이 방향)</param>
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

            // 재사용 변수 초기화 (GC 방지)
            _tempXTarget = new Vertex3f();
            _tempYTarget = new Vertex3f();
            _tempZTarget = new Vertex3f();
            _tempSwingAxis = new Vertex3f();
            _tempXSwing = new Vertex3f();
            _tempYSwing = new Vertex3f();
            _tempZSwing = new Vertex3f();
            _tempCross = new Vertex3f();
            _tempReferenceSwing = new Vertex3f();
            _tempSwingRotation = new Matrix4x4f();
            _tempTwistRotation = new Matrix4x4f();
            _tempCombinedRotation = new Matrix4x4f();
            _tempFinalRotation = new Matrix4x4f();
        }

        // -----------------------------------------------------------------------
        // 제약 적용
        // -----------------------------------------------------------------------

        /// <summary>
        /// 현재 변환에 스윙-트위스트 제약을 적용한다.
        /// </summary>
        /// <param name="currentTransform">현재 로컬 변환 행렬</param>
        /// <returns>제약이 적용된 변환 행렬</returns>
        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            // 제한 없음 체크
            if (_maxSwingAngle >= 180f && _minTwistAngle <= -180f && _maxTwistAngle >= 180f)
                return currentTransform;

            // 위치 및 스케일 추출
            float posX = currentTransform[3, 0];
            float posY = currentTransform[3, 1];
            float posZ = currentTransform[3, 2];

            float scaleX, scaleY, scaleZ;
            ExtractScale(currentTransform, out scaleX, out scaleY, out scaleZ);

            // 타겟 프레임 추출 (재사용 변수 사용)
            ExtractOrthonormalBasis(currentTransform, ref _tempXTarget, ref _tempYTarget, ref _tempZTarget);

            // 트위스트 축에 따라 bind/target 벡터 선택
            Vertex3f twistBind, twistTarget, referenceBind, referenceTarget;

            switch (_twistAxis)
            {
                case LocalSpaceAxis.X:
                    twistBind = _xBind;
                    twistTarget = _tempXTarget;
                    referenceBind = _yBind;
                    referenceTarget = _tempYTarget;
                    break;

                case LocalSpaceAxis.Z:
                    twistBind = _zBind;
                    twistTarget = _tempZTarget;
                    referenceBind = _xBind;
                    referenceTarget = _tempXTarget;
                    break;

                default: // LocalSpaceAxis.Y
                    twistBind = _yBind;
                    twistTarget = _tempYTarget;
                    referenceBind = _xBind;
                    referenceTarget = _tempXTarget;
                    break;
            }

            // 스윙 계산 및 제한
            float swingAngle;
            ComputeSwing(ref twistBind, ref twistTarget, ref _tempSwingAxis, out swingAngle);
            float swingAngleLimited = Math.Min(swingAngle, _maxSwingAngle);

            // 중간 프레임 계산 (스윙만 적용) - 재사용 행렬 사용
            CreateRotationMatrixInPlace(ref _tempSwingAxis, swingAngleLimited * DEG_TO_RAD, ref _tempSwingRotation);
            ApplyRotationToVector(_tempSwingRotation, ref referenceBind, ref _tempReferenceSwing);

            // 트위스트 계산 및 제한
            float twistAngle = ComputeTwist(ref _tempReferenceSwing, ref referenceTarget, ref twistTarget);
            float twistAngleLimited = twistAngle.Clamp(_minTwistAngle, _maxTwistAngle);

            // 디버그
            _bone.TextNamePlate.Text = $"Swing{swingAngleLimited:F0}도Twist{twistAngleLimited:F0}도";

            // 최종 회전 행렬 재구성 - 재사용 행렬 사용
            CreateRotationMatrixInPlace(ref _tempSwingAxis, swingAngleLimited * DEG_TO_RAD, ref _tempSwingRotation);
            CreateRotationMatrixInPlace(ref twistTarget, twistAngleLimited * DEG_TO_RAD, ref _tempTwistRotation);
            MultiplyRotationsInPlace(_tempTwistRotation, _tempSwingRotation, ref _tempCombinedRotation);
            MultiplyRotationsInPlace(_tempCombinedRotation, _bindRotation, ref _tempFinalRotation);

            // 스케일 및 위치 복원 - 재사용 행렬 사용
            return ReconstructTransform(_tempFinalRotation, scaleX, scaleY, scaleZ, posX, posY, posZ);
        }

        // -----------------------------------------------------------------------
        // 핵심 수학 연산
        // -----------------------------------------------------------------------

        /// <summary>
        /// 행렬에서 정규직교 기저를 추출한다.
        /// </summary>
        /// <param name="matrix">입력 변환 행렬</param>
        /// <param name="xAxis">출력 X축 (ref)</param>
        /// <param name="yAxis">출력 Y축 (ref)</param>
        /// <param name="zAxis">출력 Z축 (ref)</param>
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
        /// 스윙 회전을 계산한다. yBind를 yTarget으로 보내는 최단 회전이다.
        /// </summary>
        /// <param name="yBind">바인드 포즈 트위스트 축</param>
        /// <param name="yTarget">타겟 트위스트 축</param>
        /// <param name="swingAxis">출력: 스윙 회전축 (ref)</param>
        /// <param name="swingAngle">출력: 스윙 각도 (도)</param>
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
        /// 트위스트 각도를 계산한다. xSwing을 xTarget으로 보내는 yTarget 축 주변 회전이다.
        /// </summary>
        /// <param name="xSwing">스윙 적용 후 X축</param>
        /// <param name="xTarget">타겟 X축</param>
        /// <param name="yTarget">타겟 Y축 (트위스트 축)</param>
        /// <returns>트위스트 각도 (도)</returns>
        private float ComputeTwist(ref Vertex3f xSwing, ref Vertex3f xTarget, ref Vertex3f yTarget)
        {
            // 재사용 변수에 외적 저장
            _tempCross.x = xSwing.y * xTarget.z - xSwing.z * xTarget.y;
            _tempCross.y = xSwing.z * xTarget.x - xSwing.x * xTarget.z;
            _tempCross.z = xSwing.x * xTarget.y - xSwing.y * xTarget.x;

            float crossDotY = DotProduct(ref _tempCross, ref yTarget);
            float xDot = DotProduct(ref xSwing, ref xTarget);

            return (float)Math.Atan2(crossDotY, xDot) * RAD_TO_DEG;
        }

        // -----------------------------------------------------------------------
        // 보조 수학 함수
        // -----------------------------------------------------------------------

        /// <summary>
        /// 행렬에서 스케일 성분을 추출한다.
        /// </summary>
        /// <param name="matrix">입력 변환 행렬</param>
        /// <param name="scaleX">출력 X 스케일</param>
        /// <param name="scaleY">출력 Y 스케일</param>
        /// <param name="scaleZ">출력 Z 스케일</param>
        private void ExtractScale(Matrix4x4f matrix, out float scaleX, out float scaleY, out float scaleZ)
        {
            float c0x = matrix[0, 0], c0y = matrix[0, 1], c0z = matrix[0, 2];
            float c1x = matrix[1, 0], c1y = matrix[1, 1], c1z = matrix[1, 2];
            float c2x = matrix[2, 0], c2y = matrix[2, 1], c2z = matrix[2, 2];

            scaleX = (float)Math.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            scaleY = (float)Math.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            scaleZ = (float)Math.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);
        }

        /// <summary>
        /// 벡터를 정규화한다 (단위 벡터로 만듦).
        /// </summary>
        /// <param name="v">정규화할 벡터 (ref)</param>
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

        /// <summary>
        /// 두 벡터의 내적을 계산한다.
        /// </summary>
        /// <param name="a">벡터 A</param>
        /// <param name="b">벡터 B</param>
        /// <returns>내적 값</returns>
        private float DotProduct(ref Vertex3f a, ref Vertex3f b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        /// <summary>
        /// 두 벡터의 외적을 계산한다.
        /// </summary>
        /// <param name="a">벡터 A</param>
        /// <param name="b">벡터 B</param>
        /// <param name="result">출력: 외적 결과 (ref)</param>
        private void CrossProduct(ref Vertex3f a, ref Vertex3f b, ref Vertex3f result)
        {
            result.x = a.y * b.z - a.z * b.y;
            result.y = a.z * b.x - a.x * b.z;
            result.z = a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// 축-각도 표현으로부터 회전 행렬을 생성한다 (쿼터니언 사용).
        /// 기존 행렬을 재사용하여 GC를 방지한다.
        /// </summary>
        /// <param name="axis">회전축</param>
        /// <param name="angleRad">회전 각도 (라디안)</param>
        /// <param name="result">출력: 회전 행렬 (ref)</param>
        private void CreateRotationMatrixInPlace(ref Vertex3f axis, float angleRad, ref Matrix4x4f result)
        {
            if (Math.Abs(angleRad) < EPSILON_SMALL)
            {
                result = Matrix4x4f.Identity;
                return;
            }

            float halfAngle = angleRad * 0.5f;
            float sinHalf = (float)Math.Sin(halfAngle);
            float cosHalf = (float)Math.Cos(halfAngle);

            // 쿼터니언 생성 (임시 할당 불가피)
            ZetaExt.Quaternion q = new ZetaExt.Quaternion(
                axis.x * sinHalf,
                axis.y * sinHalf,
                axis.z * sinHalf,
                cosHalf
            );

            result = (Matrix4x4f)q;
        }

        /// <summary>
        /// 회전 행렬을 벡터에 적용한다.
        /// </summary>
        /// <param name="rotation">회전 행렬</param>
        /// <param name="input">입력 벡터</param>
        /// <param name="output">출력 벡터 (ref)</param>
        private void ApplyRotationToVector(Matrix4x4f rotation, ref Vertex3f input, ref Vertex3f output)
        {
            output.x = rotation[0, 0] * input.x + rotation[1, 0] * input.y + rotation[2, 0] * input.z;
            output.y = rotation[0, 1] * input.x + rotation[1, 1] * input.y + rotation[2, 1] * input.z;
            output.z = rotation[0, 2] * input.x + rotation[1, 2] * input.y + rotation[2, 2] * input.z;
        }

        /// <summary>
        /// 두 회전 행렬을 곱한다. 기존 행렬을 재사용하여 GC를 방지한다.
        /// </summary>
        /// <param name="a">회전 행렬 A</param>
        /// <param name="b">회전 행렬 B</param>
        /// <param name="result">출력: A * B (ref)</param>
        private void MultiplyRotationsInPlace(Matrix4x4f a, Matrix4x4f b, ref Matrix4x4f result)
        {
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

            // 4x4 항등 행렬 완성
            result[0, 3] = 0f;
            result[1, 3] = 0f;
            result[2, 3] = 0f;
            result[3, 0] = 0f;
            result[3, 1] = 0f;
            result[3, 2] = 0f;
            result[3, 3] = 1f;
        }

        /// <summary>
        /// 회전, 스케일, 위치로부터 변환 행렬을 재구성한다.
        /// </summary>
        /// <param name="rotation">회전 행렬</param>
        /// <param name="scaleX">X 스케일</param>
        /// <param name="scaleY">Y 스케일</param>
        /// <param name="scaleZ">Z 스케일</param>
        /// <param name="posX">X 위치</param>
        /// <param name="posY">Y 위치</param>
        /// <param name="posZ">Z 위치</param>
        /// <returns>재구성된 변환 행렬</returns>
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

        /// <summary>
        /// 주어진 변환이 제한 범위 내에 있는지 확인한다.
        /// </summary>
        /// <param name="transform">확인할 변환 행렬</param>
        /// <returns>제한 범위 내이면 true</returns>
        public override bool IsWithinLimits(Matrix4x4f transform)
        {
            if (!_enabled) return true;

            // 임시 변수 재사용
            ExtractOrthonormalBasis(transform, ref _tempXTarget, ref _tempYTarget, ref _tempZTarget);

            float dotProduct = DotProduct(ref _yBind, ref _tempYTarget).Clamp(-1f, 1f);
            float currentSwingAngle = (float)Math.Acos(dotProduct) * RAD_TO_DEG;

            return currentSwingAngle <= _maxSwingAngle;
        }

        /// <summary>
        /// 제한 값들을 설정한다.
        /// </summary>
        /// <param name="limits">제한 파라미터 배열 [maxSwing] 또는 [maxSwing, minTwist, maxTwist]</param>
        public override void SetLimits(params float[] limits)
        {
            if (limits.Length < 1 || limits.Length > 3)
                throw new ArgumentException(
                    "SwingTwistConstraint requires 1-3 parameters: [maxSwing] or [maxSwing, minTwist, maxTwist]");

            MaxSwingAngle = limits[0];

            if (limits.Length >= 3)
            {
                MinTwistAngle = limits[1];
                MaxTwistAngle = limits[2];
            }
        }

        /// <summary>
        /// 제약 정보를 문자열로 반환한다.
        /// </summary>
        /// <returns>제약 정보 문자열</returns>
        public override string ToString()
        {
            return $"스윙-트위스트 제한 (Swing: {_maxSwingAngle:F1}°, Twist: [{_minTwistAngle:F1}°, {_maxTwistAngle:F1}°], Enabled: {_enabled})";
        }
    }
}