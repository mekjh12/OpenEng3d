using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 2개 본 IK 시스템
    /// <br/>
    /// 두 개의 본으로 이루어진 체인의 끝점이 목표 위치에 도달하도록 한다.
    /// 주로 팔(어깨+팔꿈치+손), 다리(엉덩이+무릎+발)에 사용된다.
    /// 
    /// <code>
    /// 사용 예시:
    /// var armIK = TwoBoneIKFactory.CreateLeftArmIK(armature);
    /// armIK.Solve(targetPosition, modelMatrix, animator);
    /// </code>
    /// </summary>
    public abstract class TwoBoneIK
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------
        protected const float EPSILON = 0.0001f;  // 부동소수점 비교용 작은 값
        private readonly Bone _upperBone;       // 상완 본 (어깨/엉덩이)
        private readonly Bone _lowerBone;       // 하완 본 (팔목/발)
        private readonly Bone _endBone;         // 끝 본 (손/발)

        private readonly SingleBoneLookAt _upperBoneLookAt; // 상완 본 LookAt 컨트롤러
        private readonly SingleBoneLookAt _lowerBoneLookAt; // 하완 본 LookAt 컨트롤러

        protected float _upperLength;     // 상단 본 길이
        protected float _lowerLength;     // 하단 본 길이
        protected float _maxReach;        // 최대 도달 거리
        protected float _minReach;        // 최소 도달 거리

        private bool _isLengthLoaded = false;

        // 계산용 임시 변수
        protected Vertex3f _rootWorld;
        protected Vertex3f _midWorld;
        protected Vertex3f _endWorld;
        protected Matrix4x4f _rootTransformWorld;
        protected Vertex3f _prevTargetWorld;
        protected Vertex3f _targetDir;
        protected Vertex3f _targetWorld;
        protected Vertex3f _newMidWorld;
        protected Vertex3f _newEndWorld;
        protected Vertex3f _poleVector;

        protected float _shoulderAngle;
        protected float _elbowAngle;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public float UpperLength => _upperLength;
        public float LowerLength => _lowerLength;
        public float MaxReach => _maxReach;
        public float MinReach => _minReach;
        public Bone UpperBone => _upperBone;
        public Bone LowerBone => _lowerBone;
        public Bone EndBone => _endBone;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// TwoBoneIK 생성자
        /// </summary>
        /// <param name="upperBone">상완 본 (상완/허벅지)</param>
        /// <param name="lowerBone">하완 본 (하완/종아리)</param>
        /// <param name="endBone">끝 본 (손/발)</param>
        public TwoBoneIK(Bone upperBone, Bone lowerBone, Bone endBone)
        {
            // 본 유효성 검사
            _upperBone = upperBone ?? throw new ArgumentNullException(nameof(upperBone));
            _lowerBone = lowerBone ?? throw new ArgumentNullException(nameof(lowerBone));
            _endBone = endBone ?? throw new ArgumentNullException(nameof(endBone));

            // LookAt 컨트롤러 초기화
            _upperBoneLookAt = new SingleBoneLookAt(_upperBone, LocalSpaceAxis.Y, LocalSpaceAxis.Z);
            _lowerBoneLookAt = new SingleBoneLookAt(_lowerBone, LocalSpaceAxis.Y, LocalSpaceAxis.Z);
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        public void Solve(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            PrepareSolve(targetPositionWorld, modelMatrix, animator);

            // 자식 클래스가 구현할 중간 로직
            _poleVector = SolveInternal(targetPositionWorld, poleVector, modelMatrix, animator);

            // 폴벡터와 d벡터로 직교 성분 계산
            float dot = _poleVector.Dot(_targetDir);
            _poleVector = _poleVector - _targetDir * dot;
            float poleProjLength = _poleVector.Length();
            _poleVector /= poleProjLength;

            FinishedSolve(_poleVector, modelMatrix, animator);

            //return new Vertex3f[] { _rootWorld, _midWorld, _endWorld, _newMidWorld, _newEndWorld, _poleVector };
        }

        /// <summary>
        /// IK 해결을 위한 내부 로직을 구현합니다.
        /// <code>
        /// 구현 단계:
        /// 1. 폴 벡터 방향 결정 (조건에 따라)
        /// 2. 정규화된 폴 벡터 반환
        /// </code>
        /// </summary>
        /// <param name="targetPositionWorld">월드 공간의 목표 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터 인스턴스</param>
        /// <returns>계산된 폴 벡터 (정규화됨)</returns>
        protected abstract Vertex3f SolveInternal(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator);

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        protected void PrepareSolve(Vertex3f targetPositionWorld, Matrix4x4f modelMatrix, Animator animator)
        {
            if (_targetWorld == targetPositionWorld) return;

            // 관절의 월드 위치 계산
            CalculateJointWorldPosition(animator, modelMatrix, ref _rootTransformWorld);

            // 본 길이 계산
            CalculateBoneLength(_rootWorld, _midWorld, _endWorld);

            // 최대/최소 도달 거리 계산
            _maxReach = _upperLength + _lowerLength;
            _minReach = Math.Abs(_upperLength - _lowerLength);

            // 타겟 방향 및 거리(d) 계산
            _targetWorld = targetPositionWorld; // IK 타겟 위치
            Vertex3f toTarget = _targetWorld - _rootWorld;
            float distance = toTarget.Length();

            // 타겟 방향 정규화(d)
            _targetDir = toTarget / distance;

            // 도달 가능 범위 제한
            float clampedDistance = distance.Clamp(_minReach, _maxReach);

            // 팔꿈치 각도 계산 (코사인 법칙)
            float upperSq = _upperLength * _upperLength;
            float lowerSq = _lowerLength * _lowerLength;
            float distSq = clampedDistance * clampedDistance;

            float cosElbow = (upperSq + lowerSq - distSq) / (2f * _upperLength * _lowerLength);
            cosElbow = cosElbow.Clamp(-1f, 1f);
            _elbowAngle = (float)Math.Acos(cosElbow);

            // 어깨 각도 계산
            float cosShoulder = (upperSq + distSq - lowerSq) / (2f * _upperLength * clampedDistance);
            cosShoulder = cosShoulder.Clamp(-1f, 1f);
            _shoulderAngle = (float)Math.Acos(cosShoulder);
        }

        protected void FinishedSolve(Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            // 팔꿈치 위치 계산
            float cosAlpha = (float)Math.Cos(_shoulderAngle);
            float sinAlpha = (float)Math.Sin(_shoulderAngle);
            _newMidWorld = _rootWorld + (_targetDir * cosAlpha + poleVector * sinAlpha) * _upperLength;

            // 손목 위치 (검증용)
            Vertex3f midToTarget = _targetWorld - _newMidWorld;
            float midToTargetLength = midToTarget.Length();
            Vertex3f lowerDir = midToTargetLength > EPSILON ? midToTarget / midToTargetLength : _targetDir;
            _newEndWorld = _newMidWorld + lowerDir * _lowerLength;

            // 본 회전 적용
            _upperBoneLookAt.Solve(_newMidWorld, modelMatrix, animator);
            _lowerBoneLookAt.Solve(_targetWorld, modelMatrix, animator);
        }

        private void CalculateJointWorldPosition(Animator animator, Matrix4x4f modelMatrix, ref Matrix4x4f rootTransformWorld)
        {
            rootTransformWorld = animator.GetAnimatedWorldTransform(_upperBone, modelMatrix);
            _rootWorld = rootTransformWorld.Position;
            _midWorld = animator.GetAnimatedWorldTransform(_lowerBone, modelMatrix).Position;
            _endWorld = animator.GetAnimatedWorldTransform(_endBone, modelMatrix).Position;
        }

        private void CalculateBoneLength(Vertex3f rootWorld, Vertex3f midWorld, Vertex3f endWorld)
        {
            // 상완과 하완의 길이를 구한다.
            if (!_isLengthLoaded)
            {
                _upperLength = (midWorld - rootWorld).Length();
                _lowerLength = (endWorld - midWorld).Length();
                _isLengthLoaded = true;
                if (_upperLength < 0.001f || _lowerLength < 0.001f)
                    throw new ArgumentException("본의 길이가 너무 짧다.");
            }
        }
    }
}