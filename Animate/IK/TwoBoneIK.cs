using Assimp;
using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

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
    public class TwoBoneIK
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------
        private const float EPSILON = 0.0001f;  // 부동소수점 비교용 작은 값
        private readonly Bone _upperBone;       // 상완 본 (어깨/엉덩이)
        private readonly Bone _lowerBone;       // 하완 본 (팔목/발)
        private readonly Bone _endBone;         // 끝 본 (손/발)

        private readonly SingleBoneLookAt _upperBoneLookAt; // 상완 본 LookAt 컨트롤러
        private readonly SingleBoneLookAt _lowerBoneLookAt; // 하완 본 LookAt 컨트롤러

        private float _upperLength;     // 상단 본 길이
        private float _lowerLength;     // 하단 본 길이
        private float _maxReach;        // 최대 도달 거리
        private float _minReach;        // 최소 도달 거리

        private bool _isLengthLoaded = false;

        // 계산용 임시 변수
        private Vertex3f _rootWorld;
        private Vertex3f _midWorld;
        private Vertex3f _endWorld;
        private Matrix4x4f _rootTransformWorld;



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

        public Vertex3f[] Solve2(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            CalculateJointWorldPosition(animator, modelMatrix, ref _rootTransformWorld);

            CalculateBoneLength(_rootWorld, _midWorld, _endWorld);

            // 최대/최소 도달 거리 계산
            _maxReach = _upperLength + _lowerLength;
            _minReach = Math.Abs(_upperLength - _lowerLength);

            // 3단계: 타겟 방향 및 거리(d) 계산
            Vertex3f targetWorld = targetPositionWorld; // IK 타겟 위치
            Vertex3f toTarget = targetWorld - _rootWorld;
            float distance = toTarget.Length();

            // 타겟 방향 정규화(d)
            Vertex3f targetDir = toTarget / distance;

            // 4단계: 도달 가능 범위 제한
            float clampedDistance = distance.Clamp(_minReach, _maxReach);

            // 타겟 방향 성분 제거 (사영)
            float dot = poleVector.Dot(targetDir);
            Vertex3f poleProj = poleVector - targetDir * dot;
            float poleProjLength = poleProj.Length();

            if (poleProjLength < EPSILON)
            {
                // 특이 케이스: 팔꿈치가 어깨-타겟 직선 상에 있음
                // 대체 벡터 사용
                Vertex3f fallback = Math.Abs(targetDir.Dot(Vertex3f.UnitY)) < 0.99f
                    ? Vertex3f.UnitY
                    : Vertex3f.UnitZ;

                float fallbackDot = fallback.Dot(targetDir);
                poleProj = fallback - targetDir * fallbackDot;
                poleProjLength = poleProj.Length();

                poleVector = poleProj / poleProjLength;
                Console.WriteLine("특이케이스" + DateTime.Now);
            }
            else
            {
                poleVector = poleProj / poleProjLength;
            }

            // 6단계: 팔꿈치 각도 계산 (코사인 법칙)
            float upperSq = _upperLength * _upperLength;
            float lowerSq = _lowerLength * _lowerLength;
            float distSq = clampedDistance * clampedDistance;

            float cosElbow = (upperSq + lowerSq - distSq) / (2f * _upperLength * _lowerLength);
            cosElbow = cosElbow.Clamp(-1f, 1f);
            float elbowAngle = (float)Math.Acos(cosElbow);

            // 7단계: 어깨 각도 계산
            float cosShoulder = (upperSq + distSq - lowerSq) / (2f * _upperLength * clampedDistance);
            cosShoulder = cosShoulder.Clamp(-1f, 1f);
            float shoulderAngle = (float)Math.Acos(cosShoulder);
            //shoulderAngle = forward.Dot(poleVector) > 0 ? shoulderAngle : -shoulderAngle;

            // 8단계: 팔꿈치 위치 계산
            float cosAlpha = (float)Math.Cos(shoulderAngle);
            float sinAlpha = (float)Math.Sin(shoulderAngle);
            Vertex3f newMidWorld = _rootWorld + (targetDir * cosAlpha + poleVector * sinAlpha) * _upperLength;

            // 9단계: 손목 위치 (검증용)
            Vertex3f midToTarget = targetWorld - newMidWorld;
            float midToTargetLength = midToTarget.Length();
            Vertex3f lowerDir = midToTargetLength > EPSILON ? midToTarget / midToTargetLength : targetDir;
            Vertex3f newEndWorld = newMidWorld + lowerDir * _lowerLength;

            _upperBoneLookAt.Solve(newMidWorld, modelMatrix, animator);
            _lowerBoneLookAt.Solve(targetWorld, modelMatrix, animator);

            return new Vertex3f[] { _rootWorld, _midWorld, _endWorld, newMidWorld, newEndWorld, poleVector };
        }

        /// <summary>
        /// 2본 IK를 해결하여 끝점이 목표 위치에 도달하도록 한다
        /// </summary>
        /// <param name="targetPositionWorld">목표 월드 위치</param>
        /// <param name="forward">목표 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <returns>계산된 관절 위치들 [Root, Mid, End, MidAfter]</returns>
        public Vertex3f[] Solve(Vertex3f targetPositionWorld, Vertex3f forward, Matrix4x4f modelMatrix, Animator animator)
        {
            CalculateJointWorldPosition(animator, modelMatrix, ref _rootTransformWorld);

            CalculateBoneLength(_rootWorld, _midWorld, _endWorld);

            // 최대/최소 도달 거리 계산
            _maxReach = _upperLength + _lowerLength;
            _minReach = Math.Abs(_upperLength - _lowerLength);

            // 3단계: 타겟 방향 및 거리(d) 계산
            Vertex3f targetWorld = targetPositionWorld; // IK 타겟 위치
            Vertex3f toTarget = targetWorld - _rootWorld;
            float distance = toTarget.Length();

            // 타겟 방향 정규화(d)
            Vertex3f targetDir = toTarget / distance;

            // 4단계: 도달 가능 범위 제한
            float clampedDistance = distance.Clamp(_minReach, _maxReach);

            // 5단계: 폴 벡터 계산 (애니메이션 기반)
            Vertex3f animElbowDir = _midWorld - _rootWorld;

            // 타겟 방향 성분 제거 (사영)
            float dot = animElbowDir.Dot(targetDir);
            Vertex3f poleProj = animElbowDir - targetDir * dot;
            float poleProjLength = poleProj.Length();

            Vertex3f poleVector;
            if (poleProjLength < EPSILON)
            {
                // 특이 케이스: 팔꿈치가 어깨-타겟 직선 상에 있음
                // 대체 벡터 사용
                Vertex3f fallback = Math.Abs(targetDir.Dot(Vertex3f.UnitY)) < 0.99f
                    ? Vertex3f.UnitY
                    : Vertex3f.UnitZ;

                float fallbackDot = fallback.Dot(targetDir);
                poleProj = fallback - targetDir * fallbackDot;
                poleProjLength = poleProj.Length();

                poleVector = poleProj / poleProjLength;
                Console.WriteLine("특이케이스" + DateTime.Now);
            }
            else
            {
                poleVector = poleProj / poleProjLength;
            }

            // 6단계: 팔꿈치 각도 계산 (코사인 법칙)
            float upperSq = _upperLength * _upperLength;
            float lowerSq = _lowerLength * _lowerLength;
            float distSq = clampedDistance * clampedDistance;

            float cosElbow = (upperSq + lowerSq - distSq) / (2f * _upperLength * _lowerLength);
            cosElbow = cosElbow.Clamp(-1f, 1f);
            float elbowAngle = (float)Math.Acos(cosElbow);

            // 7단계: 어깨 각도 계산
            float cosShoulder = (upperSq + distSq - lowerSq) / (2f * _upperLength * clampedDistance);
            cosShoulder = cosShoulder.Clamp(-1f, 1f);
            float shoulderAngle = (float)Math.Acos(cosShoulder);
            shoulderAngle = forward.Dot(poleVector) > 0 ? shoulderAngle : -shoulderAngle;

            // 8단계: 팔꿈치 위치 계산
            float cosAlpha = (float)Math.Cos(shoulderAngle);
            float sinAlpha = (float)Math.Sin(shoulderAngle);
            Vertex3f newMidWorld = _rootWorld + (targetDir * cosAlpha + poleVector * sinAlpha) * _upperLength;

            // 9단계: 손목 위치 (검증용)
            Vertex3f midToTarget = targetWorld - newMidWorld;
            float midToTargetLength = midToTarget.Length();
            Vertex3f lowerDir = midToTargetLength > EPSILON ? midToTarget / midToTargetLength : targetDir;
            Vertex3f newEndWorld = newMidWorld + lowerDir * _lowerLength;

            _upperBoneLookAt.Solve(newMidWorld, modelMatrix, animator);
            _lowerBoneLookAt.Solve(targetWorld, modelMatrix, animator);

            return new Vertex3f[] { _rootWorld, _midWorld, _endWorld, newMidWorld, newEndWorld, poleVector };
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

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