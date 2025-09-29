using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 단일 본 Look At IK 시스템
    /// <br/>
    /// 하나의 본이 월드 공간의 특정 위치를 바라보도록 회전시키는 IK 구현이다.
    /// 머리, 눈, 척추 등 단일 관절 회전에 적합하다.
    /// 
    /// <code>
    /// 사용 예시:
    /// var headLookAt = new SingleBoneLookAt(headBone, Vertex3f.UnitY, Vertex3f.UnitZ);
    /// headLookAt.LookAt(targetPosition, modelMatrix, animator);
    /// </code>
    /// </summary>
    public class SingleBoneLookAt
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _bone;               // 제어할 본
        private readonly Vertex3f _localForward;   // 본의 로컬 전방 벡터 (바라보는 방향)
        private readonly Vertex3f _localUp;        // 본의 로컬 상향 벡터 (업 방향)

        // 각도 제한 설정
        private bool _useAngleLimits;               // 각도 제한 사용 여부
        private float _maxYawAngle;                 // 최대 좌우 회전 각도 (도)
        private float _maxPitchAngle;               // 최대 상하 회전 각도 (도)

        // 멤버 변수 추가
        private float _smoothSpeed = 5.0f;           // 전환 속도 (값이 클수록 빠름)
        private Quaternion _currentRotation;         // 현재 회전 (내부 상태)
        private bool _isInitialized = false;         // 초기화 여부

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>본의 로컬 전방 벡터</summary>
        public Vertex3f LocalForward => _localForward;

        /// <summary>각도 제한 사용 여부</summary>
        public bool UseAngleLimits => _useAngleLimits;

        /// <summary>최대 좌우 회전 각도 (도)</summary>
        public float MaxYawAngle => _maxYawAngle;

        /// <summary>최대 상하 회전 각도 (도)</summary>
        public float MaxPitchAngle => _maxPitchAngle;

        /// <summary>부드러운 전환 속도 설정 (기본: 5.0)</summary>
        public float SmoothSpeed
        {
            get => _smoothSpeed;
            set => _smoothSpeed = Math.Max(0.1f, value);
        }
        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// SingleBoneLookAt 생성자
        /// </summary>
        /// <param name="bone">제어할 본</param>
        /// <param name="localForward">본의 로컬 전방 벡터 (기본: Y축)</param>
        /// <param name="localUp">본의 로컬 상향 벡터 (기본: Z축)</param>
        /// <exception cref="ArgumentException">Forward와 Up 벡터가 평행한 경우</exception>
        public SingleBoneLookAt(Bone bone, Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _localForward = (localForward == default ? Vertex3f.UnitY : localForward).Normalized;
            _localUp = (localUp == default ? Vertex3f.UnitZ : localUp).Normalized;

            // Forward와 Up 벡터가 평행하면 안 됨
            if (Math.Abs(_localForward.Dot(_localUp)) > 0.99f)
                throw new ArgumentException("Local Forward와 Up 벡터는 평행하지 않아야 한다.");

            // 각도 제한 기본값 (제한 없음)
            _useAngleLimits = false;
            _maxYawAngle = 180f;
            _maxPitchAngle = 180f;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 각도 제한을 설정한다
        /// </summary>
        /// <param name="maxYawAngle">최대 좌우 회전 각도 (도, 0~180)</param>
        /// <param name="maxPitchAngle">최대 상하 회전 각도 (도, 0~180)</param>
        public void SetAngleLimits(float maxYawAngle, float maxPitchAngle)
        {
            _useAngleLimits = true;
            _maxYawAngle = Math.Max(0f, Math.Min(180f, maxYawAngle));
            _maxPitchAngle = Math.Max(0f, Math.Min(180f, maxPitchAngle));
        }

        /// <summary>
        /// 각도 제한을 해제한다
        /// </summary>
        public void DisableAngleLimits()
        {
            _useAngleLimits = false;
        }

        /// <summary>
        /// 본에 지정된 회전을 적용한다
        /// </summary>
        /// <param name="animator">애니메이터</param>
        /// <param name="rotation">적용할 회전 (쿼터니언)</param>
        public void Rotate(Animator animator, Quaternion rotation)
        {
            // 현재 로컬 변환에 회전 적용
            _bone.BoneMatrixSet.LocalTransform *= (Matrix4x4f)rotation;

            // 애니메이터 변환 업데이트
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// 본이 월드 공간의 특정 위치를 바라보도록 회전시킨다
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트 (기본: Z축)</param>
        public void LookAt(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator,
                          Vertex3f worldUpHint = default)
        {
            var rotationInfo = CalculateRotation(worldTargetPosition, modelMatrix, animator, worldUpHint);

            // 계산된 변환 행렬을 본에 적용
            _bone.BoneMatrixSet.LocalTransform = rotationInfo.Matrix;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// 부드러운 전환을 적용하여 LookAt 수행
        /// </summary>
        public void SmoothLookAt(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix,
                                 Animator animator, float deltaTime,
                                 Vertex3f worldUpHint = default)
        {
            // 목표 회전 계산 (각도 제한 포함)
            var rotationInfo = CalculateRotation(worldTargetPosition, modelMatrix, animator, worldUpHint);

            // 초기화
            if (!_isInitialized)
            {
                _currentRotation = rotationInfo.Quaternion;
                _isInitialized = true;
            }

            // 부드럽게 보간 (Slerp)
            float t = Math.Min(1.0f, _smoothSpeed * deltaTime);
            _currentRotation = _currentRotation.Interpolate(rotationInfo.Quaternion, t);

            // 보간된 회전을 행렬로 변환
            var smoothMatrix = (Matrix4x4f)_currentRotation;

            // 원래 위치 복원
            var originalPosition = _bone.BoneMatrixSet.LocalTransform.Position;
            smoothMatrix[3, 0] = originalPosition.x;
            smoothMatrix[3, 1] = originalPosition.y;
            smoothMatrix[3, 2] = originalPosition.z;

            // 최종 적용
            _bone.BoneMatrixSet.LocalTransform = smoothMatrix;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// Look At에 필요한 회전 정보를 계산한다 (실제 적용하지 않음)
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트</param>
        /// <returns>회전 정보 (쿼터니언과 행렬)</returns>
        public RotationInfo CalculateRotation(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix,
                                            Animator animator, Vertex3f worldUpHint = default)
        {
            // 현재 본의 월드 변환 계산
            var currentWorldTransform = animator.GetRootTransform(_bone);
            var finalWorldTransform = modelMatrix * currentWorldTransform;
            var currentWorldPosition = finalWorldTransform.Position;

            // 기본 월드 업 벡터 설정
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 타겟 방향 벡터 계산
            var targetDirection = (worldTargetPosition - currentWorldPosition).Normalized;

            // Look At 변환 행렬 생성
            var lookAtMatrix = CreateLocalSpaceTransform(targetDirection, worldUpHint, finalWorldTransform);

            // 각도 제한 적용
            if (_useAngleLimits)
            {
                lookAtMatrix = ApplyAngleLimits(lookAtMatrix, targetDirection);
            }

            return new RotationInfo(lookAtMatrix.ToQuaternion(), lookAtMatrix);
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 좌표계 변환을 통해 Look At 로컬 변환 행렬을 생성한다
        /// <br/>
        /// From 좌표계(원래 본의 로컬 축) → To 좌표계(목표 방향 기준)로 변환하는 행렬을 계산한다.
        /// </summary>
        /// <param name="worldTargetDirection">월드 공간에서의 목표 방향</param>
        /// <param name="worldUpHint">월드 공간에서의 업 벡터 힌트</param>
        /// <param name="finalWorldTransform">본의 최종 월드 변환</param>
        /// <returns>새로운 로컬 변환 행렬</returns>
        private Matrix4x4f CreateLocalSpaceTransform(Vertex3f worldTargetDirection, Vertex3f worldUpHint,
                                                   Matrix4x4f finalWorldTransform)
        {
            // 1단계: 월드 방향을 부모 로컬 공간으로 변환
            Matrix4x4f parentWorldTransform = _bone.Parent == null ?
                Matrix4x4f.Identity : finalWorldTransform * _bone.BoneMatrixSet.LocalTransform.Inversed();

            Matrix4x4f worldToParentLocal = parentWorldTransform.Inversed();
            Vertex4f localDir = worldToParentLocal * new Vertex4f(worldTargetDirection.x, worldTargetDirection.y, worldTargetDirection.z, 0);
            Vertex4f localUp = worldToParentLocal * new Vertex4f(worldUpHint.x, worldUpHint.y, worldUpHint.z, 0);

            Vertex3f targetDir = new Vertex3f(localDir.x, localDir.y, localDir.z).Normalized;
            Vertex3f upHint = new Vertex3f(localUp.x, localUp.y, localUp.z).Normalized;

            // 2단계: From 좌표계 구성 (원래 본의 로컬 축들)
            Vertex3f fromForward = _localForward;
            Vertex3f fromRight = fromForward.Cross(_localUp).Normalized;
            Vertex3f fromUp = fromRight.Cross(fromForward).Normalized;

            // 3단계: To 좌표계 구성 (목표 방향 기준)
            Vertex3f toForward = targetDir;
            Vertex3f toRight = toForward.Cross(upHint).Normalized;

            // Right 벡터 검증 및 보정
            if (toRight.Length() < 0.001f)
            {
                Vertex3f altUp = Math.Abs(toForward.Dot(Vertex3f.UnitX)) < 0.9f ?
                    Vertex3f.UnitX : Vertex3f.UnitY;
                toRight = toForward.Cross(altUp).Normalized;
            }
            Vertex3f toUp = toRight.Cross(toForward).Normalized;

            // 4단계: 좌표계 변환 행렬 생성
            Matrix4x4f fromBasis = new Matrix4x4f(
                fromRight.x, fromRight.y, fromRight.z, 0,
                fromForward.x, fromForward.y, fromForward.z, 0,
                fromUp.x, fromUp.y, fromUp.z, 0,
                0, 0, 0, 1);

            Matrix4x4f toBasis = new Matrix4x4f(
                toRight.x, toRight.y, toRight.z, 0,
                toForward.x, toForward.y, toForward.z, 0,
                toUp.x, toUp.y, toUp.z, 0,
                0, 0, 0, 1);

            // 5단계: From → To 변환 = toBasis * fromBasis^(-1)
            Matrix4x4f basisTransform = toBasis * fromBasis.Inversed();

            // 6단계: 원래 위치 보존
            Vertex3f originalPosition = _bone.BoneMatrixSet.LocalTransform.Position;
            basisTransform[3, 0] = originalPosition.x;
            basisTransform[3, 1] = originalPosition.y;
            basisTransform[3, 2] = originalPosition.z;

            return basisTransform;
        }

        /// <summary>
        /// 각도 제한을 적용하여 변환 행렬을 조정한다
        /// </summary>
        /// <param name="targetTransform">목표 변환 행렬</param>
        /// <returns>제한된 변환 행렬</returns>
        private Matrix4x4f ApplyAngleLimits(Matrix4x4f targetTransform, Vertex3f targetDirection)
        {
            // 원래 바인드 포즈에서의 Forward 방향
            var originalForward = _localForward;

            // 목표 변환에서의 Forward 방향
            Vertex3f targetForward = Vertex3f.UnitX;
            if (_localForward == Vertex3f.UnitX) targetForward = targetTransform.Column0.xyz().Normalized;
            if (_localForward == Vertex3f.UnitY) targetForward = targetTransform.Column1.xyz().Normalized;
            if (_localForward == Vertex3f.UnitZ) targetForward = targetTransform.Column2.xyz().Normalized;

            // 원래 방향과 목표 방향 사이의 각도 계산
            var angleBetween = Math.Acos(Math.Max(-1f, Math.Min(1f, originalForward.Dot(targetForward)))) * 180f / Math.PI;

            // 각도가 제한을 초과하지 않으면 그대로 반환
            if (angleBetween <= _maxYawAngle && angleBetween <= _maxPitchAngle)
            {
                return targetTransform;
            }

            // 제한된 각도로 조정
            var limitedAngle = Math.Min(_maxYawAngle, _maxPitchAngle);// * Math.PI / 180f;

            // 회전축 계산 (원래 방향과 목표 방향의 외적)
            var rotationAxis = originalForward.Cross(targetForward).Normalized;

            // 축이 0벡터에 가까우면 회전이 필요 없음
            if (rotationAxis.Length() < 0.001f)
            {
                return _bone.BoneMatrixSet.LocalBindTransform;
            }

            // 제한된 각도로 회전 쿼터니언 생성
            var limitedRotation = new Quaternion(rotationAxis, (float)limitedAngle);
            var limitedMatrix = (Matrix4x4f)limitedRotation;

            // 로컬바인딩행렬과의 곱으로 목표 변환 행렬의 위치는 모든 성분이 0이 되어야 함
            limitedMatrix[3, 0] = 0;
            limitedMatrix[3, 1] = 0;
            limitedMatrix[3, 2] = 0;

            return _bone.BoneMatrixSet.LocalBindTransform * limitedMatrix; // 순서 중요
        }

        // -----------------------------------------------------------------------
        // 내부 구조체
        // -----------------------------------------------------------------------

        /// <summary>
        /// Look At 회전 정보를 담는 구조체
        /// </summary>
        public readonly struct RotationInfo
        {
            /// <summary>회전을 나타내는 쿼터니언</summary>
            public readonly Quaternion Quaternion;

            /// <summary>회전을 나타내는 4x4 행렬</summary>
            public readonly Matrix4x4f Matrix;

            public RotationInfo(Quaternion quaternion, Matrix4x4f matrix)
            {
                Quaternion = quaternion;
                Matrix = matrix;
            }
        }
    }
}