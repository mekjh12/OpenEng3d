using Animate;
using OpenGL;
using System;
using ZetaExt;

namespace Animates
{
    internal class ThreeBoneRoll
    {
        private readonly Bone _firstBone;   // 상완 (최상위 본)
        private readonly Bone _secondBone;  // 하완 (중간 본)
        private readonly Bone _thirdBone;   // 손 (끝 본)
        private readonly LocalSpaceAxis _rollAxis;  // 회전 축 (기본값: Y축)
        private readonly LocalSpaceAxis _forward;   // 전방 축 (기본값: Z축)
        private readonly SingleBoneRoll _firstBoneRoll;   // 첫 번째 본의 단일 롤 솔버
        private readonly SingleBoneRoll _secondBoneRoll;  // 두 번째 본의 단일 롤 솔버
        private readonly SingleBoneRoll _thirdBoneRoll;   // 세 번째 본의 단일 롤 솔버

        private float _previousAngle = 0f;      // 이전 프레임의 각도 (라디안)
        private float _accumulatedAngle = 0f;   // 누적된 회전 각도 (180도 경계 보정됨)

        public ThreeBoneRoll(Bone firstBone, Bone secondBone, Bone thirdBone, LocalSpaceAxis rollAxis = LocalSpaceAxis.Y, LocalSpaceAxis forward = LocalSpaceAxis.Z)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _thirdBone = thirdBone;
            _rollAxis = rollAxis;
            _forward = forward;
            if (_rollAxis == _forward)
            {
                throw new ArgumentException("두 축이 동일할 수 없습니다.");
            }
            _firstBoneRoll = new SingleBoneRoll(_firstBone, _rollAxis, _forward);
            _secondBoneRoll = new SingleBoneRoll(_secondBone, _rollAxis, _forward);
            _thirdBoneRoll = new SingleBoneRoll(_thirdBone, _rollAxis, _forward);
        }

        /// <summary>
        /// 세 본의 롤을 자연스럽게 분배한다.
        /// 끝 본이 먼저 비틀어지고, 중간 본과 상위 본이 가중치만큼 따라 비틀어진다.
        /// </summary>
        /// <param name="target">목표 지점 (월드 좌표)</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="firstWeight">상완이 하완을 따라가는 비율 (0~1)</param>
        /// <param name="secondWeight">하완이 손을 따라가는 비율 (0~1)</param>
        /// <param name="thirdWeight">손의 롤 가중치</param>
        public void Solve(Vertex3f target, Matrix4x4f modelMatrix, Animator animator,
            float firstWeight = 0.33f, float secondWeight = 0.66f, float thirdWeight = 1.0f)
        {
            // 세 번째 본(손)을 먼저 target 방향으로 roll
            Matrix4x4f thirdRotation = _thirdBoneRoll.Solve(target, modelMatrix, animator, out float angle3, thirdWeight, true);

            // 각도 불연속 보정: -π와 π 사이의 경계를 넘을 때 발생하는 점프 방지
            float angleDiff = angle3 - _previousAngle;
            if (angleDiff > Math.PI)
                _accumulatedAngle -= 2 * (float)Math.PI;  // 180도 → -180도 점프 시 보정
            else if (angleDiff < -Math.PI)
                _accumulatedAngle += 2 * (float)Math.PI;  // -180도 → 180도 점프 시 보정

            _accumulatedAngle += angleDiff;
            _previousAngle = angle3;

            // 두 번째 본(하완)이 세 번째 본을 따라 비틀어짐
            Matrix4x4f secondFollowRotation = GetRotationMatrix(_accumulatedAngle.ToDegree() * thirdWeight * secondWeight);
            Matrix4x4f secondTransform = _secondBone.BoneMatrixSet.LocalTransform * secondFollowRotation;
            _secondBone.UpdateBone(ref secondTransform, animator, true);

            // 첫 번째 본(상완)이 두 번째 본을 따라 비틀어짐 (누적 가중치 적용)
            Matrix4x4f firstFollowRotation = GetRotationMatrix(_accumulatedAngle.ToDegree() * thirdWeight * secondWeight * firstWeight);
            Matrix4x4f firstTransform = _firstBone.BoneMatrixSet.LocalTransform * firstFollowRotation;
            _firstBone.UpdateBone(ref firstTransform, animator, true);

            // 두 번째 본이 첫 번째 본을 따라 비틀어진 것을 역회전으로 상쇄
            Matrix4x4f secondCounterRotation = firstFollowRotation.Inversed();
            secondTransform = secondCounterRotation * _secondBone.BoneMatrixSet.LocalTransform;
            _secondBone.UpdateBone(ref secondTransform, animator, true);

            // 세 번째 본이 두 번째 본을 따라 비틀어진 것을 역회전으로 상쇄 (형태 유지)
            Matrix4x4f thirdCounterRotation = secondFollowRotation.Inversed();
            Matrix4x4f thirdTransform = thirdCounterRotation * _thirdBone.BoneMatrixSet.LocalTransform;
            _thirdBone.UpdateBone(ref thirdTransform, animator, true);
        }

        /// <summary>
        /// Roll 축에 따라 적절한 회전 행렬을 반환한다.
        /// </summary>
        private Matrix4x4f GetRotationMatrix(float angleDegree)
        {
            if (_rollAxis == LocalSpaceAxis.Y)
                return Matrix4x4f.RotatedY(angleDegree);
            if (_rollAxis == LocalSpaceAxis.X)
                return Matrix4x4f.RotatedX(angleDegree);
            if (_rollAxis == LocalSpaceAxis.Z)
                return Matrix4x4f.RotatedZ(angleDegree);
            throw new ArgumentException($"지원하지 않는 Roll 축: {_rollAxis}");
        }
    }
}