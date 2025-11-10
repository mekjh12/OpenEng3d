using FastMath;
using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 경첩 관절(Hinge Joint) 제한 클래스
    /// <br/>
    /// 팔꿈치, 무릎, 손가락 등의 2DOF 관절에 사용하는 제한을 구현한다.
    /// <br/>
    /// 굽힘(Bending)과 비틀기(Twisting)를 독립적으로 제한한다.
    /// <br/>
    /// GC 압박을 최소화하기 위해 모든 임시 변수를 멤버로 재사용한다.
    /// </summary>
    public class HingeConstraint : JointConstraint
    {
        private const float RAD_TO_DEG = 57.295779513f;
        private const float DEG_TO_RAD = 0.017453292f;
        private const float EPSILON = 0.001f;
        private const float EPSILON_SMALL = 0.0001f;

        // -----------------------------------------------------------------------
        // 바인드 포즈 정보
        // -----------------------------------------------------------------------

        private Vertex3f _xBind;  // 바인드 포즈 X축 (굽힘 참조 벡터)
        private Vertex3f _yBind;  // 바인드 포즈 Y축 (힌지 축)
        private Vertex3f _zBind;  // 바인드 포즈 Z축 (비틀기 참조 벡터)
        private Matrix4x4f _bindRotation;  // 바인드 포즈 회전 행렬

        // -----------------------------------------------------------------------
        // 제한 파라미터
        // -----------------------------------------------------------------------

        private float _minBendAngle;   // 굽힘 최소 각도 (도)
        private float _maxBendAngle;   // 굽힘 최대 각도 (도)
        private float _minTwistAngle;  // 비틀기 최소 각도 (도)
        private float _maxTwistAngle;  // 비틀기 최대 각도 (도)
        private LocalSpaceAxis _hingeAxis; // 힌지 축 (본의 길이 방향)

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 타겟 프레임 추출용
        // -----------------------------------------------------------------------

        private Vertex3f _tempXTarget;  // 현재 변환의 X축
        private Vertex3f _tempYTarget;  // 현재 변환의 Y축
        private Vertex3f _tempZTarget;  // 현재 변환의 Z축

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 굽힘 계산용
        // -----------------------------------------------------------------------

        private Vertex3f _tempBendRefProjected;     // 굽힘 참조 벡터 투영 (바인드)
        private Vertex3f _tempBendCurrentProjected; // 굽힘 참조 벡터 투영 (현재)
        private Vertex3f _tempBendAxis;             // 굽힘 회전축

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 비틀기 계산용
        // -----------------------------------------------------------------------

        private Vertex3f _tempTwistMid;   // 중간 프레임의 비틀기 참조 벡터
        private Vertex3f _tempCross;      // 외적 결과 저장용

        // -----------------------------------------------------------------------
        // 재사용 임시 변수 - 행렬 연산용
        // -----------------------------------------------------------------------

        private Matrix4x4f _tempBendRotation;     // 굽힘 회전 행렬
        private Matrix4x4f _tempTwistRotation;    // 비틀기 회전 행렬
        private Matrix4x4f _tempCombinedRotation; // 결합 회전 행렬
        private Matrix4x4f _tempFinalRotation;    // 최종 회전 행렬

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public override string ConstraintType => "HingeConstraint";
        public float MinBendAngle { get => _minBendAngle; set => _minBendAngle = value.Clamp(-180, 180); }
        public float MaxBendAngle { get => _maxBendAngle; set => _maxBendAngle = value.Clamp(-180, 180); }
        public float MinTwistAngle { get => _minTwistAngle; set => _minTwistAngle = value.Clamp(-180, 180); }
        public float MaxTwistAngle { get => _maxTwistAngle; set => _maxTwistAngle = value.Clamp(-180, 180); }
        public LocalSpaceAxis HingeAxis { get => _hingeAxis; set => _hingeAxis = value; }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// HingeConstraint를 생성한다.
        /// </summary>
        /// <param name="bone">제약을 적용할 본</param>
        /// <param name="minBendAngle">굽힘 최소 각도 (도)</param>
        /// <param name="maxBendAngle">굽힘 최대 각도 (도)</param>
        /// <param name="minTwistAngle">비틀기 최소 각도 (도)</param>
        /// <param name="maxTwistAngle">비틀기 최대 각도 (도)</param>
        /// <param name="hingeAxis">힌지 축 (본의 길이 방향)</param>
        public HingeConstraint(Bone bone,
            float minBendAngle = 0f,
            float maxBendAngle = 145f,
            float minTwistAngle = -90f,
            float maxTwistAngle = 90f,
            LocalSpaceAxis hingeAxis = LocalSpaceAxis.Y)
            : base(bone)
        {
            _hingeAxis = hingeAxis;
            MinBendAngle = minBendAngle;
            MaxBendAngle = maxBendAngle;
            MinTwistAngle = minTwistAngle;
            MaxTwistAngle = maxTwistAngle;

            // 바인드 포즈에서 정규직교 기저 추출
            _bindRotation = bone.BoneMatrixSet.LocalBindTransform;
            ExtractOrthonormalBasis(_bindRotation, ref _xBind, ref _yBind, ref _zBind);

            // 재사용 변수 초기화 (GC 방지)
            _tempXTarget = new Vertex3f();
            _tempYTarget = new Vertex3f();
            _tempZTarget = new Vertex3f();
            _tempBendRefProjected = new Vertex3f();
            _tempBendCurrentProjected = new Vertex3f();
            _tempBendAxis = new Vertex3f();
            _tempTwistMid = new Vertex3f();
            _tempCross = new Vertex3f();
            _tempBendRotation = new Matrix4x4f();
            _tempTwistRotation = new Matrix4x4f();
            _tempCombinedRotation = new Matrix4x4f();
            _tempFinalRotation = new Matrix4x4f();
        }

        // -----------------------------------------------------------------------
        // 제약 적용
        // -----------------------------------------------------------------------

        /// <summary>
        /// 현재 변환에 힌지 제약을 적용한다.
        /// </summary>
        /// <param name="currentTransform">현재 로컬 변환 행렬</param>
        /// <returns>제약이 적용된 변환 행렬</returns>
        public override Matrix4x4f ApplyConstraint(Matrix4x4f currentTransform)
        {
            if (!_enabled) return currentTransform;

            // 제한 없음 체크
            if (_minBendAngle <= -180f && _maxBendAngle >= 180f &&
                _minTwistAngle <= -180f && _maxTwistAngle >= 180f)
                return currentTransform;

            // 위치 및 스케일 추출
            float posX = currentTransform[3, 0];
            float posY = currentTransform[3, 1];
            float posZ = currentTransform[3, 2];

            float scaleX, scaleY, scaleZ;
            ExtractScale(currentTransform, out scaleX, out scaleY, out scaleZ);

            // 타겟 프레임 추출 (재사용 변수 사용)
            ExtractOrthonormalBasis(currentTransform, ref _tempXTarget, ref _tempYTarget, ref _tempZTarget);

            // 힌지 축에 따라 bind/target 벡터 선택
            Vertex3f hingeAxis, bendRefBind, bendRefCurrent, twistRefBind, twistRefCurrent;

            switch (_hingeAxis)
            {
                case LocalSpaceAxis.X:
                    hingeAxis = _xBind;
                    bendRefBind = _yBind;
                    bendRefCurrent = _tempYTarget;
                    twistRefBind = _zBind;
                    twistRefCurrent = _tempZTarget;
                    break;

                case LocalSpaceAxis.Z:
                    hingeAxis = _zBind;
                    bendRefBind = _xBind;
                    bendRefCurrent = _tempXTarget;
                    twistRefBind = _yBind;
                    twistRefCurrent = _tempYTarget;
                    break;

                default: // LocalSpaceAxis.Y
                    hingeAxis = _yBind;
                    bendRefBind = _xBind;
                    bendRefCurrent = _tempXTarget;
                    twistRefBind = _zBind;
                    twistRefCurrent = _tempZTarget;
                    break;
            }

            // 굽힘 계산 및 제한
            float bendAngle;
            ComputeBend(ref hingeAxis, ref bendRefBind, ref bendRefCurrent,
                ref _tempBendAxis, out bendAngle);
            float bendAngleLimited = bendAngle.Clamp(_minBendAngle, _maxBendAngle);

            // 중간 프레임 계산 (굽힘만 적용)
            CreateRotationMatrixInPlace(ref _tempBendAxis, bendAngleLimited * DEG_TO_RAD, ref _tempBendRotation);
            ApplyRotationToVector(_tempBendRotation, ref twistRefBind, ref _tempTwistMid);

            // 비틀기 계산 및 제한
            float twistAngle = ComputeTwist(ref _tempTwistMid, ref twistRefCurrent, ref hingeAxis);
            float twistAngleLimited = twistAngle.Clamp(_minTwistAngle, _maxTwistAngle);

            // 디버그
            _bone.TextNamePlate.Text = $"Bend{bendAngleLimited:F0}도Twist{twistAngleLimited:F0}도";

            // 최종 회전 행렬 재구성 - 재사용 행렬 사용
            CreateRotationMatrixInPlace(ref _tempBendAxis, bendAngleLimited * DEG_TO_RAD, ref _tempBendRotation);
            CreateRotationMatrixInPlace(ref hingeAxis, twistAngleLimited * DEG_TO_RAD, ref _tempTwistRotation);
            MultiplyRotationsInPlace(_tempTwistRotation, _tempBendRotation, ref _tempCombinedRotation);
            MultiplyRotationsInPlace(_bindRotation, _tempCombinedRotation, ref _tempFinalRotation);

            // 스케일 및 위치 복원
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
        /// 굽힘 회전을 계산한다. 힌지 축에 수직인 평면에서의 회전이다.
        /// </summary>
        /// <param name="hingeAxis">힌지 축</param>
        /// <param name="bendRefBind">바인드 포즈 굽힘 참조 벡터</param>
        /// <param name="bendRefCurrent">현재 굽힘 참조 벡터</param>
        /// <param name="bendAxis">출력: 굽힘 회전축 (ref)</param>
        /// <param name="bendAngle">출력: 굽힘 각도 (도)</param>
        private void ComputeBend(ref Vertex3f hingeAxis,
            ref Vertex3f bendRefBind, ref Vertex3f bendRefCurrent,
            ref Vertex3f bendAxis, out float bendAngle)
        {
            // 힌지 축에 수직인 평면에 투영
            ProjectToPlane(ref bendRefBind, ref hingeAxis, ref _tempBendRefProjected);
            ProjectToPlane(ref bendRefCurrent, ref hingeAxis, ref _tempBendCurrentProjected);

            // 정규화
            NormalizeVector(ref _tempBendRefProjected);
            NormalizeVector(ref _tempBendCurrentProjected);

            // 각도 계산
            float cosAngle = DotProduct(ref _tempBendRefProjected, ref _tempBendCurrentProjected);
            cosAngle = cosAngle.Clamp(-1f, 1f);

            // 외적으로 회전축 및 부호 계산
            CrossProduct(ref _tempBendRefProjected, ref _tempBendCurrentProjected, ref bendAxis);
            float sinAngle = DotProduct(ref bendAxis, ref hingeAxis);

            bendAngle = (float)MathFast.Atan2(sinAngle, cosAngle) * RAD_TO_DEG;

            // 회전축 정규화
            float axisLength = (float)MathFast.Sqrt(
                bendAxis.x * bendAxis.x +
                bendAxis.y * bendAxis.y +
                bendAxis.z * bendAxis.z);

            // 특수 케이스: 정렬 또는 정반대
            if (axisLength < EPSILON_SMALL)
            {
                // 기본 회전축 사용 (힌지 축과 수직)
                CrossProduct(ref hingeAxis, ref bendRefBind, ref bendAxis);
                NormalizeVector(ref bendAxis);

                if (cosAngle > 0)
                    bendAngle = 0f;
                else
                    bendAngle = 180f;
            }
            else
            {
                float invLength = 1f / axisLength;
                bendAxis.x *= invLength;
                bendAxis.y *= invLength;
                bendAxis.z *= invLength;
            }
        }

        /// <summary>
        /// 비틀기 각도를 계산한다. 힌지 축 주변의 회전이다.
        /// </summary>
        /// <param name="twistRefMid">중간 프레임의 비틀기 참조 벡터</param>
        /// <param name="twistRefCurrent">현재 비틀기 참조 벡터</param>
        /// <param name="hingeAxis">힌지 축</param>
        /// <returns>비틀기 각도 (도)</returns>
        private float ComputeTwist(ref Vertex3f twistRefMid, ref Vertex3f twistRefCurrent, ref Vertex3f hingeAxis)
        {
            // 정규직교 기저의 성질: 이미 힌지 축에 수직이므로 투영 불필요
            float cosAngle = DotProduct(ref twistRefMid, ref twistRefCurrent);

            // 외적으로 부호 결정
            CrossProduct(ref twistRefMid, ref twistRefCurrent, ref _tempCross);
            float sinAngle = DotProduct(ref _tempCross, ref hingeAxis);

            return (float)MathFast.Atan2(sinAngle, cosAngle) * RAD_TO_DEG;
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

            scaleX = (float)MathFast.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            scaleY = (float)MathFast.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            scaleZ = (float)MathFast.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);
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
                float invLength = 1f / (float)MathFast.Sqrt(lengthSq);
                v.x *= invLength;
                v.y *= invLength;
                v.z *= invLength;
            }
        }

        /// <summary>
        /// 벡터를 평면에 투영한다 (평면 법선에 수직인 성분만 남김).
        /// </summary>
        /// <param name="v">투영할 벡터</param>
        /// <param name="normal">평면 법선 (정규화됨)</param>
        /// <param name="result">출력: 투영된 벡터 (ref)</param>
        private void ProjectToPlane(ref Vertex3f v, ref Vertex3f normal, ref Vertex3f result)
        {
            float dotProduct = v.x * normal.x + v.y * normal.y + v.z * normal.z;
            result.x = v.x - dotProduct * normal.x;
            result.y = v.y - dotProduct * normal.y;
            result.z = v.z - dotProduct * normal.z;
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
            if (MathFast.Abs(angleRad) < EPSILON_SMALL)
            {
                result = Matrix4x4f.Identity;
                return;
            }

            float halfAngle = angleRad * 0.5f;
            float sinHalf = (float)MathFast.Sin(halfAngle);
            float cosHalf = (float)MathFast.Cos(halfAngle);

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

            // 간단히 굽힘 각도만 체크
            ExtractOrthonormalBasis(transform, ref _tempXTarget, ref _tempYTarget, ref _tempZTarget);

            Vertex3f hingeAxis, bendRefCurrent;
            switch (_hingeAxis)
            {
                case LocalSpaceAxis.X:
                    hingeAxis = _xBind;
                    bendRefCurrent = _tempYTarget;
                    break;
                case LocalSpaceAxis.Z:
                    hingeAxis = _zBind;
                    bendRefCurrent = _tempXTarget;
                    break;
                default:
                    hingeAxis = _yBind;
                    bendRefCurrent = _tempXTarget;
                    break;
            }

            Vertex3f bendRefBind = (_hingeAxis == LocalSpaceAxis.Y) ? _xBind :
                                   (_hingeAxis == LocalSpaceAxis.X) ? _yBind : _xBind;

            float bendAngle;
            ComputeBend(ref hingeAxis, ref bendRefBind, ref bendRefCurrent, ref _tempBendAxis, out bendAngle);

            return bendAngle >= _minBendAngle && bendAngle <= _maxBendAngle;
        }

        /// <summary>
        /// 제한 값들을 설정한다.
        /// </summary>
        /// <param name="limits">제한 파라미터 배열 [minBend, maxBend, minTwist, maxTwist]</param>
        public override void SetLimits(params float[] limits)
        {
            if (limits.Length < 2 || limits.Length > 4)
                throw new ArgumentException(
                    "HingeConstraint requires 2-4 parameters: [minBend, maxBend] or [minBend, maxBend, minTwist, maxTwist]");

            MinBendAngle = limits[0];
            MaxBendAngle = limits[1];

            if (limits.Length >= 4)
            {
                MinTwistAngle = limits[2];
                MaxTwistAngle = limits[3];
            }
        }

        /// <summary>
        /// 제약 정보를 문자열로 반환한다.
        /// </summary>
        /// <returns>제약 정보 문자열</returns>
        public override string ToString()
        {
            return $"힌지 제한 (Bend: [{_minBendAngle:F1}°, {_maxBendAngle:F1}°], Twist: [{_minTwistAngle:F1}°, {_maxTwistAngle:F1}°], Axis: {_hingeAxis}, Enabled: {_enabled})";
        }
    }
}