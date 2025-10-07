using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    public class SingleBoneLookAt
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _bone;               // 제어할 본
        private readonly Vertex3f _localForward;   // 본의 로컬 전방 벡터 (바라보는 방향)

        // 각도 제한 설정
        private bool _useAngleLimits;               // 각도 제한 사용 여부
        private float _maxYawAngle;                 // 최대 좌우 회전 각도 (도)
        private float _maxPitchAngle;               // 최대 상하 회전 각도 (도)
        private float _maxRollAngle;                // 최대 롤 각도 (도)


        // 멤버 변수 추가
        private float _smoothSpeed = 5.0f;           // 전환 속도 (값이 클수록 빠름)
        private Quaternion _currentRotation;         // 현재 회전 (내부 상태)
        private bool _isInitialized = false;         // 초기화 여부

        // 계산용 임시 변수
        Matrix4x4f _parentWorldTransform;
        Matrix4x4f _worldToParentLocal;
        Vertex3f _targetPositionLocal;
        Vertex3f _targetLocal;
        Vertex3f _rotationAxis;
        Quaternion _rotation;
        Vertex4f _localTarget;
        Matrix4x4f _rotationMatrix;
        Vertex3f _originalPositionLocal;
        Matrix4x4f _finalLocalTransform;
        float _tempLength;

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

        public SingleBoneLookAt(Bone bone, Vertex3f localForward = default)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _localForward = (localForward == default ? Vertex3f.UnitY : localForward).Normalized;

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
        /// <param name="maxRollAngle">최대 롤 회전 각도 (도, 0~90)</param>
        public void SetAngleLimits(float maxYawAngle, float maxPitchAngle, float maxRollAngle)
        {
            _useAngleLimits = true;
            _maxYawAngle = Math.Max(0f, Math.Min(180f, maxYawAngle));
            _maxPitchAngle = Math.Max(0f, Math.Min(180f, maxPitchAngle));
            _maxRollAngle = Math.Max(0f, Math.Min(90f, maxRollAngle));
        }

        /// <summary>
        /// 각도 제한을 해제한다
        /// </summary>
        public void DisableAngleLimits()
        {
            _useAngleLimits = false;
        }

        public void Solve(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator)
        {
            // 1. 부모 본의 월드 변환 계산
            _parentWorldTransform = _bone.Parent == null ?
                modelMatrix : modelMatrix * animator.GetRootTransform(_bone.Parent);

            // 2. 부모 본에 대한 로컬 공간 변환 행렬
            _worldToParentLocal = _parentWorldTransform.Inversed();

            // 3. 월드 타겟 위치를 부모 본의 로컬 공간으로 변환 (먼저 계산!)
            _localTarget = _worldToParentLocal * new Vertex4f(worldTargetPosition.x, worldTargetPosition.y, worldTargetPosition.z, 1);
            _targetPositionLocal.x = _localTarget.x;
            _targetPositionLocal.y = _localTarget.y;
            _targetPositionLocal.z = _localTarget.z;

            // 4. 본의 현재 위치 가져오기
            _originalPositionLocal = _bone.BoneMatrixSet.LocalTransform.Position;

            // 5. 본에서 타겟으로의 방향 벡터 계산
            _targetLocal.x = _targetPositionLocal.x - _originalPositionLocal.x;
            _targetLocal.y = _targetPositionLocal.y - _originalPositionLocal.y;
            _targetLocal.z = _targetPositionLocal.z - _originalPositionLocal.z;

            // 6. 타겟 방향 정규화 (길이 체크)
            float targetLength = MathF.Sqrt(_targetLocal.x * _targetLocal.x +
                                             _targetLocal.y * _targetLocal.y +
                                             _targetLocal.z * _targetLocal.z);

            if (targetLength < 0.001f) return; // 타겟이 너무 가까움

            // 정규화
            float invTargetLength = 1f / targetLength;
            _targetLocal.x *= invTargetLength;
            _targetLocal.y *= invTargetLength;
            _targetLocal.z *= invTargetLength;

            // 7. 본의 로컬 전방 벡터 (이미 정규화된 상태)
            Vertex3f forward = _localForward;
            if (_localForward == Vertex3f.UnitX) forward = _bone.BoneMatrixSet.LocalTransform.Column0.xyz().Normalized;
            if (_localForward == Vertex3f.UnitY) forward = _bone.BoneMatrixSet.LocalTransform.Column1.xyz().Normalized;
            if (_localForward == Vertex3f.UnitZ) forward = _bone.BoneMatrixSet.LocalTransform.Column2.xyz().Normalized;

            // 8. 회전축 계산 (외적)
            _rotationAxis.x = forward.y * _targetLocal.z - forward.z * _targetLocal.y;
            _rotationAxis.y = forward.z * _targetLocal.x - forward.x * _targetLocal.z;
            _rotationAxis.z = forward.x * _targetLocal.y - forward.y * _targetLocal.x;

            // 9. 회전축 정규화
            float axisLength = MathF.Sqrt(_rotationAxis.x * _rotationAxis.x +
                                           _rotationAxis.y * _rotationAxis.y +
                                           _rotationAxis.z * _rotationAxis.z);

            if (axisLength < 0.001f) return; // 이미 타겟을 향하고 있음

            float invAxisLength = 1f / axisLength;
            _rotationAxis.x *= invAxisLength;
            _rotationAxis.y *= invAxisLength;
            _rotationAxis.z *= invAxisLength;

            // 10. 회전 각도 계산 (내적)
            float dot = forward.x * _targetLocal.x +
                        forward.y * _targetLocal.y +
                        forward.z * _targetLocal.z;
            dot = Math.Max(-1f, Math.Min(1f, dot)); // 클램핑
            float angleRad = (float)Math.Acos(dot);
            float angleDeg = angleRad * 180f / MathF.Pi;

            // 11. 쿼터니언 생성 및 회전 행렬 변환
            _rotation = new Quaternion(_rotationAxis, angleDeg);
            _rotationMatrix = (Matrix4x4f)_rotation;

            // 12. 최종 로컬 변환 = 회전 * 바인드 포즈 (LocalTransform이 아님!)
            Matrix4x4f finalLocalTransform = _rotationMatrix * _bone.BoneMatrixSet.LocalTransform;

            // 13. 위치 복원 (회전만 적용, 위치는 유지)
            finalLocalTransform[3, 0] = _originalPositionLocal.x;
            finalLocalTransform[3, 1] = _originalPositionLocal.y;
            finalLocalTransform[3, 2] = _originalPositionLocal.z;

            // 14. 본에 적용
            _bone.BoneMatrixSet.LocalTransform = finalLocalTransform;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }


        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 각도 제한을 적용하여 변환 행렬을 조정한다
        /// </summary>
        /// <param name="targetTransform">목표 변환 행렬</param>
        /// <returns>제한된 변환 행렬</returns>
        private Matrix4x4f ApplyAngleLimits(Matrix4x4f targetTransform, Vertex3f targetDirection)
        {
            // 목표 변환에서의 Forward 방향
            Vertex3f targetForward = Vertex3f.UnitX;
            if (_localForward == Vertex3f.UnitX) targetForward = targetTransform.Column0.xyz().Normalized;
            if (_localForward == Vertex3f.UnitY) targetForward = targetTransform.Column1.xyz().Normalized;
            if (_localForward == Vertex3f.UnitZ) targetForward = targetTransform.Column2.xyz().Normalized;

            // 원래 방향과 목표 방향 사이의 각도 계산
            var angleBetween = Math.Acos(Math.Max(-1f, Math.Min(1f, _localForward.Dot(targetForward)))) * 180f / Math.PI;

            // 각도가 제한을 초과하지 않으면 그대로 반환
            if (angleBetween <= _maxYawAngle && angleBetween <= _maxPitchAngle)
            {
                return targetTransform;
            }

            // 제한된 각도로 조정
            var limitedAngle = Math.Min(_maxYawAngle, _maxPitchAngle);// * Math.PI / 180f;

            // 회전축 계산 (원래 방향과 목표 방향의 외적)
            var rotationAxis = _localForward.Cross(targetForward).Normalized;

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
        
    }
}