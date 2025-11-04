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

        private readonly Bone _bone;                    // 제어할 본
        private readonly LocalSpaceAxis _localForward;  // 본의 로컬 전방 벡터 (바라보는 방향)
        private readonly LocalSpaceAxis _localUp;       // 본의 로컬 업 벡터

        // 각도 제한 설정 추가
        private bool _useAngleLimits;                   // 각도 제한 사용 여부
        private float _maxYawAngle;                     // 최대 좌우 회전 각도 (도)
        private float _maxPitchAngle;                   // 최대 상하 회전 각도 (도)

        // 멤버 변수 추가
        private float _smoothSpeed = 5.0f;              // 전환 속도 (값이 클수록 빠름)
        private Quaternion _currentRotation;            // 현재 회전 (내부 상태)
        private bool _isInitialized = false;            // 초기화 여부

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

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public LocalSpaceAxis LocalForward => _localForward;
        public float SmoothSpeed { get => _smoothSpeed; set => _smoothSpeed = Math.Max(0.1f, value); }
        public Bone Bone => _bone;
        public Quaternion Rotation { get => _rotation; }

        /// <summary>각도 제한 사용 여부</summary>
        public bool UseAngleLimits => _useAngleLimits;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public SingleBoneLookAt(Bone bone,
            LocalSpaceAxis localForward = LocalSpaceAxis.Y,
            LocalSpaceAxis localUp = LocalSpaceAxis.Z)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));

            // 로컬 전방 벡터 설정
            _localForward = localForward;
            _localUp = localUp;

            // 각도 제한 기본값 (제한 없음)
            _useAngleLimits = false;
            _maxYawAngle = 180f;
            _maxPitchAngle = 180f;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드 - 각도 제한 설정
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

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public void Calculate(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator)
        {
            // 부모 본에 대한 로컬 공간으로의 변환 행렬
            _bone.WorldToParentLocal(modelMatrix, animator, ref _worldToParentLocal);

            // 월드 타겟 위치를 부모 본의 로컬 공간으로 변환
            Vertex3NoGC.Transform(_worldToParentLocal, worldTargetPosition, ref _targetPositionLocal);

            // 현재 포즈에서 본의 조인트 위치 가져오기
            _originalPositionLocal = _bone.BoneMatrixSet.LocalTransform.Position;

            // 본에서 타겟으로의 방향 벡터 계산
            Vertex3NoGC.Subtract(_targetPositionLocal, _originalPositionLocal, ref _targetLocal);

            // 타겟 방향 정규화
            Vertex3NoGC.Normalize(ref _targetLocal);

            // 현재 포즈에서 본의 로컬 전방 벡터 계산
            if (_localForward == LocalSpaceAxis.X)
                Matrix4x4NoGC.NormalizeColumn0(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);
            if (_localForward == LocalSpaceAxis.Y)
                Matrix4x4NoGC.NormalizeColumn1(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);
            if (_localForward == LocalSpaceAxis.Z)
                Matrix4x4NoGC.NormalizeColumn2(_bone.BoneMatrixSet.LocalTransform, ref _forwardLocal);

            // 회전축 계산 (외적)
            Vertex3NoGC.Cross(_forwardLocal, _targetLocal, ref _rotationAxis);

            // 회전축 정규화
            Vertex3NoGC.Normalize(ref _rotationAxis);

            // 회전 각도 계산
            float angleDeg = Vertex3NoGC.AngleBetween(_forwardLocal, _targetLocal);

            // 각도 제한 적용
            if (_useAngleLimits)
            {
                angleDeg = ApplyAngleLimits(angleDeg);
            }

            // 쿼터니언 생성 및 회전 행렬 변환
            _rotation = new Quaternion(_rotationAxis, angleDeg);
            _rotationMatrix = (Matrix4x4f)_rotation;
        }

        public void Solve(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator, bool isSelfIncluded = true)
        {
            Calculate(worldTargetPosition, modelMatrix, animator);

            // 최종 로컬 변환 = 회전 * 현재 포즈
            Matrix4x4f finalLocalTransform = _rotationMatrix * _bone.BoneMatrixSet.LocalTransform;

            // 위치 복원 (회전만 적용, 위치는 유지)
            finalLocalTransform[3, 0] = _originalPositionLocal.x;
            finalLocalTransform[3, 1] = _originalPositionLocal.y;
            finalLocalTransform[3, 2] = _originalPositionLocal.z;

            // 뼈대에 최종 로컬 변환 적용
            _bone.UpdateBone(ref finalLocalTransform, animator, isSelfIncluded);
        }

        public void Rotate(Matrix4x4f rotationMatrix, Animator animator, bool isSelfIncluded = true)
        {
            // 최종 로컬 변환 = 회전 * 현재 포즈
            Matrix4x4f finalLocalTransform = rotationMatrix * _bone.BoneMatrixSet.LocalTransform;

            // 위치 복원 (회전만 적용, 위치는 유지)
            finalLocalTransform[3, 0] = _originalPositionLocal.x;
            finalLocalTransform[3, 1] = _originalPositionLocal.y;
            finalLocalTransform[3, 2] = _originalPositionLocal.z;

            // 뼈대에 최종 로컬 변환 적용
            _bone.UpdateBone(ref finalLocalTransform, animator, isSelfIncluded);
        }

        // -----------------------------------------------------------------------
        // 내부 메서드 - 각도 제한 적용
        // -----------------------------------------------------------------------

        /// <summary>
        /// 각도 제한을 적용한다
        /// </summary>
        /// <param name="angle">계산된 회전 각도 (도)</param>
        /// <returns>제한된 회전 각도 (도)</returns>
        private float ApplyAngleLimits(float angle)
        {
            // 최대 각도 중 작은 값 사용
            float maxAngle = Math.Min(_maxYawAngle, _maxPitchAngle);

            // 각도를 제한 범위 내로 클램프
            return Math.Min(angle, maxAngle);
        }
    }
}