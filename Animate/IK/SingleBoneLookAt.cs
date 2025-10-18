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

        // 계산용 임시 변수 추가
        private EulerAngle _currentAngles;
        private EulerAngle _clampedAngles;
        private Matrix4x4f _constrainedRotation;
        private float _scaleX, _scaleY, _scaleZ;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public LocalSpaceAxis LocalForward => _localForward;
        public float SmoothSpeed { get => _smoothSpeed; set => _smoothSpeed = Math.Max(0.1f, value); }
        public Bone Bone => _bone;

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
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public void Solve(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator)
        {
            // 부모 본에 대한 로컬 공간으로의 변환 행렬
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

            // 쿼터니언 생성 및 회전 행렬 변환
            _rotation = new Quaternion(_rotationAxis, angleDeg);
            _rotationMatrix = (Matrix4x4f)_rotation;

            // 최종 로컬 변환 = 회전 * 현재 포즈
            Matrix4x4f finalLocalTransform = _rotationMatrix * _bone.BoneMatrixSet.LocalTransform;

            // 위치 복원 (회전만 적용, 위치는 유지)
            finalLocalTransform[3, 0] = _originalPositionLocal.x;
            finalLocalTransform[3, 1] = _originalPositionLocal.y;
            finalLocalTransform[3, 2] = _originalPositionLocal.z;

            // ★ 제약 적용 ★
            if (_bone.HasConstraint && _bone.JointConstraint.Enabled)
            {
                finalLocalTransform = _bone.JointConstraint.ApplyConstraint(finalLocalTransform);
            }

            // 본에 적용
            _bone.BoneMatrixSet.LocalTransform = finalLocalTransform;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }
    }
}