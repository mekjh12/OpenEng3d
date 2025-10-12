using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    public class SingleBoneLookAt
    {
        public enum ForwardAxis { X, Y, Z }         // 본의 로컬 전방 벡터 옵션

        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _bone;                // 제어할 본
        private readonly ForwardAxis _localForward; // 본의 로컬 전방 벡터 (바라보는 방향)

        // 각도 제한 설정
        private bool _useAngleLimits;               // 각도 제한 사용 여부
        private float _maxYawAngle;                 // 최대 좌우 회전 각도 (도)
        private float _maxPitchAngle;               // 최대 상하 회전 각도 (도)
        private float _maxRollAngle;                // 최대 롤 각도 (도)
        private EulerOrder _eulerOrder
            = EulerOrder.ZXY;                       // 오일러 각 순서 (ZXY가 일반적)

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
        Vertex3f _localTarget;
        Matrix4x4f _rotationMatrix;
        Vertex3f _originalPositionLocal;
        Matrix4x4f _finalLocalTransform;
        Vertex3f _forwardLocal = Vertex3f.UnitY;
        float _tempLength;

        // 계산용 임시 변수 추가
        private EulerAngle _currentAngles;
        private EulerAngle _clampedAngles;
        private Matrix4x4f _constrainedRotation;
        private float _scaleX, _scaleY, _scaleZ;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public ForwardAxis LocalForward => _localForward;
        public bool UseAngleLimits => _useAngleLimits;
        public float MaxYawAngle => _maxYawAngle;
        public float MaxPitchAngle => _maxPitchAngle;
        public float SmoothSpeed { get => _smoothSpeed; set => _smoothSpeed = Math.Max(0.1f, value); }
    
        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public SingleBoneLookAt(Bone bone, ForwardAxis localForward = ForwardAxis.Y)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));

            // 로컬 전방 벡터 설정
            _localForward = localForward;

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
        /// <param name="maxPitchAngle">최대 상하 회전 각도 (도, 0~180)</param>
        /// <param name="maxYawAngle">최대 좌우 회전 각도 (도, 0~180)</param>
        /// <param name="maxRollAngle">최대 롤 회전 각도 (도, 0~90)</param>
        public void SetAngleLimits(float maxPitchAngle, float maxYawAngle, float maxRollAngle, EulerOrder eulerOrder = EulerOrder.ZXY)
        {
            _useAngleLimits = true;
            _eulerOrder = eulerOrder;
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
            // 부모 본에 대한 로컬 공간 변환 행렬
            _bone.WorldToParentLocal(modelMatrix, animator, ref _worldToParentLocal);

            // 월드 타겟 위치를 부모 본의 로컬 공간으로 변환 (먼저 계산!)
            Vertex3NoGC.Transform(_worldToParentLocal, worldTargetPosition, ref _targetPositionLocal);

            // 현재 포즈에서 본의 조인트 위치 가져오기
            _originalPositionLocal = _bone.BoneMatrixSet.LocalTransform.Position;

            // 본에서 타겟으로의 방향 벡터 계산
            Vertex3NoGC.Subtract(_targetPositionLocal, _originalPositionLocal, ref _targetLocal);

            // 타겟 방향 정규화
            Vertex3NoGC.Normalize(ref _targetLocal);

            // 현재 포즈에서 본의 로컬 전방 벡터 계산
            if (_localForward == ForwardAxis.X) 
                Matrix4x4NoGC.NormalizeColumn0(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);
            if (_localForward == ForwardAxis.Y) 
                Matrix4x4NoGC.NormalizeColumn1(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);
            if (_localForward == ForwardAxis.Z) 
                Matrix4x4NoGC.NormalizeColumn2(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);

            // 회전축 계산 (외적)
            Vertex3NoGC.Cross(_forwardLocal, _targetLocal, ref _rotationAxis);

            // 회전축 정규화
            Vertex3NoGC.Normalize(ref _rotationAxis);

            // 회전 각도 계산
            float angleDeg = Vertex3NoGC.AngleBetween(_forwardLocal, _targetLocal);

            // 쿼터니언 생성 및 회전 행렬 변환
            _rotation = new Quaternion(_rotationAxis, angleDeg);
            _rotationMatrix = (Matrix4x4f)_rotation;

            // 최종 로컬 변환 = 회전 * 현재 포즈
            Matrix4x4f finalLocalTransform = _rotationMatrix * _bone.BoneMatrixSet.LocalTransform;

            // 위치 복원 (회전만 적용, 위치는 유지)
            finalLocalTransform[3, 0] = _originalPositionLocal.x;
            finalLocalTransform[3, 1] = _originalPositionLocal.y;
            finalLocalTransform[3, 2] = _originalPositionLocal.z;

            // 본에 적용된 오일러각을 계산한다.
            if (_useAngleLimits)
            {
                // 1. 현재 LocalTransform을 오일러 각도로 분해
                _currentAngles = EulerConverter.MatrixToEuler(finalLocalTransform, _eulerOrder);

                // 2. 각도 제한 적용
                float clampedPitch = _currentAngles.Pitch.Clamp(-_maxPitchAngle, _maxPitchAngle);
                float clampedRoll = _currentAngles.Roll.Clamp(-_maxRollAngle, _maxRollAngle);
                float clampedYaw = _currentAngles.Yaw.Clamp(-_maxYawAngle, _maxYawAngle);

                _clampedAngles = new EulerAngle(clampedPitch, clampedRoll, clampedYaw);

                // 3. 각도가 실제로 변경되었는지 확인
                const float epsilon = 0.01f;
                bool anglesChanged =
                    Math.Abs(_currentAngles.Pitch - _clampedAngles.Pitch) > epsilon ||
                    Math.Abs(_currentAngles.Roll - _clampedAngles.Roll) > epsilon ||
                    Math.Abs(_currentAngles.Yaw - _clampedAngles.Yaw) > epsilon;

                Console.WriteLine(_currentAngles);

                if (anglesChanged)
                {
                    // 4. 스케일 추출 (원본에서)
                    _scaleX = finalLocalTransform.Column0.xyz().Length();
                    _scaleY = finalLocalTransform.Column1.xyz().Length();
                    _scaleZ = finalLocalTransform.Column2.xyz().Length();

                    // 5. 제한된 각도로 회전 행렬 생성
                    _constrainedRotation = EulerConverter.EulerToMatrix(_clampedAngles, _eulerOrder);

                    // 6. 스케일 재적용
                    _constrainedRotation = _constrainedRotation * Matrix4x4f.Scaled(_scaleX, _scaleY, _scaleZ);

                    // 7. 위치 복원
                    _constrainedRotation[3, 0] = _originalPositionLocal.x;
                    _constrainedRotation[3, 1] = _originalPositionLocal.y;
                    _constrainedRotation[3, 2] = _originalPositionLocal.z;

                    // 8. 제한된 변환 적용
                    finalLocalTransform = _constrainedRotation;
                    //_bone.BoneMatrixSet.LocalTransform = finalLocalTransform;
                }
            }

            // 본에 적용
            _bone.BoneMatrixSet.LocalTransform = finalLocalTransform;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }


        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 각도 제한을 적용하여 변환 행렬을 조정한다
        /// </summary>
        private Matrix4x4f ApplyAngleLimits(Matrix4x4f targetTransform, Vertex3f targetDirection)
        {
            /*
            // 목표 변환에서의 Forward 방향
            Vertex3f targetForward = Vertex3f.UnitX;
            if (_localForward == ForwardAxis.X) targetForward = _bone.BoneMatrixSet.LocalTransform.Column0.xyz().Normalized;
            if (_localForward == ForwardAxis.Y) targetForward = _bone.BoneMatrixSet.LocalTransform.Column1.xyz().Normalized;
            if (_localForward == ForwardAxis.Z) targetForward = _bone.BoneMatrixSet.LocalTransform.Column2.xyz().Normalized;

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
            */
            return Matrix4x4f.Identity;
        }

    }
}