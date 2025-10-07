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

        private readonly Bone _upperBone;       // 상완 본 (어깨/엉덩이)
        private readonly Bone _lowerBone;       // 하완 본 (팔목/발)
        private readonly Bone _endBone;         // 끝 본 (손/발)

        private float _upperLength;  // 상단 본 길이
        private float _lowerLength;  // 하단 본 길이

        private bool _isLengthLoaded = false;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>상단 본 길이</summary>
        public float UpperLength => _upperLength;

        /// <summary>하단 본 길이</summary>
        public float LowerLength => _lowerLength;

        /// <summary>최대 도달 거리</summary>
        public float MaxReach => _upperLength + _lowerLength;

        public Bone UpperBone => _upperBone;
        public Bone LowerBone => _lowerBone;
        public Bone EndBone => _endBone;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// TwoBoneIK 생성자
        /// </summary>
        /// <param name="upperBone">상완 본 (팔꿈치/무릎)</param>
        /// <param name="lowerBone">하완 본 (손/발)</param>
        /// <param name="endBone">끝 본 (손/발)</param>
        public TwoBoneIK(Bone upperBone, Bone lowerBone, Bone endBone)
        {
            // 본 유효성 검사
            _upperBone = upperBone ?? throw new ArgumentNullException(nameof(upperBone));
            _lowerBone = lowerBone ?? throw new ArgumentNullException(nameof(lowerBone));
            _endBone = endBone ?? throw new ArgumentNullException(nameof(endBone));
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

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

        /// <summary>
        /// 2본 IK를 해결하여 끝점이 목표 위치에 도달하도록 한다
        /// </summary>
        /// <param name="targetPositionWorld">목표 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <returns>계산된 관절 위치들 [Root, Mid, End, MidAfter]</returns>
        public Vertex3f[] Solve(Vertex3f targetPositionWorld, Matrix4x4f modelMatrix, Animator animator)
        {
            // ===============================================================================
            // Two-Bone IK 알고리즘 (코사인 법칙 기반)
            // ===============================================================================
            // [목표]
            // 월드공간에서 3개 관절(루트-중간-끝)로 구성된 체인에서 끝점이 목표 위치에 도달하도록
            // 상단 본과 하단 본을 회전시킨다.
            //
            // [핵심 원리]
            // 1. 루트-중간-목표가 이루는 삼각형에 코사인 법칙 적용
            //    - 변 a: 상단 본 길이 (고정)
            //    - 변 b: 루트→목표 거리 (입력)
            //    - 변 c: 하단 본 길이 (고정)
            //    
            // 2. cos(θ) = (b² + a² - c²) / (2ab)
            //    - θ: 상단 본이 회전해야 할 각도
            //
            // 3. 회전축 계산: 현재방향 × 목표방향 (외적)
            //    - 이 축을 중심으로 회전하면 중간 관절이 목표 쪽으로 이동
            //
            // 4. 단계별 적용:
            //    - 상단 본: 새로운 중간 관절 위치를 향하도록 회전
            //    - 하단 본: 최종 목표 위치를 향하도록 회전
            // ===============================================================================

            // 1단계: 현재 포즈의 관절 월드 좌표 계산
            Matrix4x4f rootTransformWorld = animator.GetAnimatedWorldTransform(_upperBone, modelMatrix);
            Vertex3f rootWorld = rootTransformWorld.Position;
            Vertex3f midWorld = animator.GetAnimatedWorldTransform(_lowerBone, modelMatrix).Position;
            Vertex3f endWorld = animator.GetAnimatedWorldTransform(_endBone, modelMatrix).Position;

            // 2단계: 본 길이 초기화 (최초 1회만)
            CalculateBoneLength(rootWorld, midWorld, endWorld);

            // 3단계: 도달 가능 범위 체크
            Vertex3f toTarget = targetPositionWorld - rootWorld;
            float targetDistance = toTarget.Length();
            Vertex3f targetDirection = toTarget.Normalized;

            float maxReach = _upperLength + _lowerLength;
            float minReach = Math.Abs(_upperLength - _lowerLength);

            if (targetDistance > maxReach)
            {
                targetDistance = maxReach * 0.99f;
                targetPositionWorld = rootWorld + targetDirection * targetDistance;
            }
            else if (targetDistance < minReach)
            {
                targetDistance = minReach;
                targetPositionWorld = rootWorld + targetDirection * targetDistance;
            }

            // 4단계: 현재 중간 관절 방향 (폴 방향)
            Vertex3f currentPoleDirection = (midWorld - rootWorld).Normalized;

            // 5단계: 회전축 계산 (안전한 처리)
            Vertex3f rotationAxis = currentPoleDirection.Cross(targetDirection);
            if (rotationAxis.Length() < 0.001f)
            {
                // 평행한 경우: IK가 이미 해결되었거나 180도 반대 방향
                return new Vertex3f[] { rootWorld, midWorld, endWorld, midWorld };
            }
            rotationAxis = rotationAxis.Normalized;

            // 6단계: 각도 계산
            float dotProduct = currentPoleDirection.Dot(targetDirection).Clamp(-1f, 1f);
            float currentAngle = (float)Math.Acos(dotProduct) * (180f / (float)Math.PI);
            float requiredAngle = CalculateRootBoneAngle(targetDistance) * (180f / (float)Math.PI);
            float rotationAngle = currentAngle - requiredAngle;

            // 6단계: 회전 적용하여 새로운 중간 관절 위치 계산
            Matrix4x4f rotationMatrix = Quaternion.CreateRotationMatrix(rotationAxis, rotationAngle);
            Vertex3f newMidDirection = rotationMatrix.MultiplyDirection(currentPoleDirection);
            Vertex3f midAfterWorld = newMidDirection * _upperLength + rootWorld;

            // 7단계: 상단 본(어깨/엉덩이) 회전 적용
            Vertex3f upperBoneUp = animator.GetRootTransform(_upperBone.Parent).Column2.xyz().Normalized;
            RotateBone(animator, modelMatrix, _upperBone, midAfterWorld - rootWorld, upperBoneUp);

            // 8단계: 하단 본(팔꿈치/무릎) 회전 적용
            Vertex3f lowerBoneUp = animator.GetRootTransform(_upperBone).Column2.xyz().Normalized;
            RotateBone(animator, modelMatrix, _lowerBone, targetPositionWorld - midAfterWorld, lowerBoneUp);

            return new Vertex3f[] { rootWorld, midWorld, endWorld };
        }

        /// <summary>
        /// 지정된 본을 특정 방향으로 회전시킨다
        /// </summary>
        /// <param name="animator">애니메이터</param>
        /// <param name="model">모델 변환 행렬</param>
        /// <param name="bone">회전시킬 본</param>
        /// <param name="forwardDirection">회전할 방향 벡터 (월드 공간)</param>
        /// <param name="upHint">상향 벡터 힌트 (월드 공간)</param>
        private void RotateBone(Animator animator, Matrix4x4f model, Bone bone,
                                Vertex3f forwardDirection, Vertex3f upHint)
        {
            // 방향 벡터 유효성 검사
            if (forwardDirection.Length() < 0.001f)
                return;

            // 본의 현재 월드 변환 계산
            Matrix4x4f boneWorldTransform = model * animator.GetRootTransform(bone);
            Vertex3f boneWorldPosition = boneWorldTransform.Position;

            // 목표 방향으로의 Look-At 변환 계산
            Vertex3f targetDirection = forwardDirection.Normalized;
            Vertex3f targetPosition = boneWorldPosition + targetDirection;

            Matrix4x4f rotationTransform = CreateLookAtWorldTransform(
                boneWorldTransform,
                boneWorldPosition,
                targetPosition,
                upHint);

            // 로컬 변환에 회전 적용
            bone.BoneMatrixSet.LocalTransform *= rotationTransform;

            // 관절 각도 제한이 설정되어 있으면 적용한다
            bone.JointAngle?.UpdateAndConstrain();

            // 본과 자식들의 변환 업데이트
            bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// 이전 본의 월드 변환 행렬의 스케일을 유지하면서 
        /// 새로운 방향(Look-At)을 가진 월드 변환 행렬을 생성한다
        /// </summary>
        /// <param name="previousWorldTransform">이전 본의 월드 변환 행렬</param>
        /// <param name="newPosition">새로운 위치 (월드 공간)</param>
        /// <param name="lookAtTarget">바라볼 목표 지점 (월드 공간)</param>
        /// <param name="upHint">상향 벡터 힌트 (월드 공간)</param>
        /// <returns>새로운 월드 변환 행렬</returns>
        private Matrix4x4f CreateLookAtWorldTransform(Matrix4x4f previousWorldTransform,
                                                       Vertex3f newPosition,
                                                       Vertex3f lookAtTarget,
                                                       Vertex3f upHint)
        {
            // 기존 스케일 추출 (각 축의 길이)
            float scaleX = previousWorldTransform.Column0.xyz().Length();
            float scaleY = previousWorldTransform.Column1.xyz().Length();
            float scaleZ = previousWorldTransform.Column2.xyz().Length();

            // 새로운 좌표계 구성 (월드 공간)
            Vertex3f forward = (lookAtTarget - newPosition).Normalized;
            Vertex3f upNormalized = upHint.Normalized;
            Vertex3f right = forward.Cross(upNormalized).Normalized;

            // forward와 up이 거의 평행한 경우 처리
            if (right.Length() < 0.001f)
            {
                // 대체 up 벡터 선택 (forward와 가장 수직인 축)
                Vertex3f alternativeUp = Math.Abs(forward.Dot(Vertex3f.UnitX)) < 0.9f
                    ? Vertex3f.UnitX
                    : Vertex3f.UnitY;
                right = forward.Cross(alternativeUp).Normalized;
            }

            Vertex3f up = right.Cross(forward).Normalized;

            // 스케일 적용된 축 벡터
            Vertex3f axisX = right * scaleX;
            Vertex3f axisY = forward * scaleY;
            Vertex3f axisZ = up * scaleZ;

            // 새로운 월드 변환 행렬 생성
            Matrix4x4f newWorldTransform = Matrix4x4f.Identity.Frame(axisX, axisY, axisZ, newPosition);
            return previousWorldTransform.Inversed() * newWorldTransform;
        }

        private Quaternion CreateRotationFromTo(Vertex3f from, Vertex3f to)
        {
            from = from.Normalized;
            to = to.Normalized;

            float dot = from.Dot(to);

            // 벡터가 거의 같은 방향인 경우
            if (dot > 0.999f)
            {
                return Quaternion.Identity;
            }

            // 벡터가 거의 반대 방향인 경우
            if (dot < -0.999f)
            {
                var axis = Math.Abs(from.Dot(Vertex3f.UnitX)) < 0.9f
                    ? from.Cross(Vertex3f.UnitX).Normalized
                    : from.Cross(Vertex3f.UnitY).Normalized;
                var perpAxis = from.Cross(axis).Normalized;
                return new Quaternion(perpAxis, 180f);
            }

            // 일반적인 경우
            Vertex3f rotationAxis = from.Cross(to).Normalized;
            float rotationAngle = (float)(Math.Acos(dot) * 180d / Math.PI);

            Quaternion q = new Quaternion(rotationAxis, rotationAngle);
            q.Normalize();
            return q;
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 중간 본의 굽힘 각도 계산 (코사인 법칙)
        /// </summary>
        /// <returns>라디안 단위의 굽힘 각도</returns>
        private float CalculateMidBoneAngle(float targetDistance)
        {
            float upper2 = _upperLength * _upperLength;
            float lower2 = _lowerLength * _lowerLength;
            float target2 = targetDistance * targetDistance;

            // 코사인 법칙: cos(θ) = (a² + b² - c²) / (2ab)
            float cosAngle = (upper2 + lower2 - target2) / (2f * _upperLength * _lowerLength);
            cosAngle = cosAngle.Clamp(-1f, 1f);

            // 굽힘 각도 = π - 내각
            return (float)Math.PI - (float)Math.Acos(cosAngle);
        }

        /// <summary>
        /// 루트 본의 회전 각도 계산 (코사인 법칙)
        /// </summary>
        /// <returns>라디안 단위의 회전 각도 (0~π)</returns>
        private float CalculateRootBoneAngle(float targetDistance)
        {
            if (targetDistance < 0.01f)
                return 0f;

            float upper2 = _upperLength * _upperLength;
            float lower2 = _lowerLength * _lowerLength;
            float target2 = targetDistance * targetDistance;

            // 코사인 법칙: cos(rootAngle) = (target² + upper² - lower²) / (2·target·upper)
            float cosRootAngle = (target2 + upper2 - lower2) / (2f * targetDistance * _upperLength);
            cosRootAngle = cosRootAngle.Clamp(-1f, 1f);

            return (float)Math.Acos(cosRootAngle);
        }


    }
}