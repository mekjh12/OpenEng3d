using Animate;
using OpenGL;
using System;
using ZetaExt;

namespace Animates
{
    internal class TwoBoneRoll
    {
        Bone _firstBone;  // 상완 (부모 본)
        Bone _secondBone; // 하완 (자식 본)
        LocalSpaceAxis _rollAxis;  // 회전 축 (기본값: Y축)
        LocalSpaceAxis _forward;   // 전방 축 (기본값: Z축)
        SingleBoneRoll _firstBoneRoll;   // 첫 번째 본의 단일 롤 솔버
        SingleBoneRoll _secondBoneRoll;  // 두 번째 본의 단일 롤 솔버

        private float _previousAngle = 0f;      // 이전 프레임의 각도 (라디안)
        private float _accumulatedAngle = 0f;   // 누적된 회전 각도 (180도 경계 보정됨)

        public TwoBoneRoll(Bone firstBone, Bone secondBone, LocalSpaceAxis rollAxis = LocalSpaceAxis.Y, LocalSpaceAxis forward = LocalSpaceAxis.Z)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _rollAxis = rollAxis;
            _forward = forward;
            if (_rollAxis == _forward)
            {
                throw new ArgumentException("두 축이 동일할 수 없습니다.");
            }
            _firstBoneRoll = new SingleBoneRoll(_firstBone, _rollAxis, _forward);
            _secondBoneRoll = new SingleBoneRoll(_secondBone, _rollAxis, _forward);
        }

        /// <summary>
        /// 두 본의 롤을 자연스럽게 분배한다.
        /// 하완이 먼저 비틀어지고, 상완이 가중치만큼 따라 비틀어진다.
        /// </summary>
        /// <param name="target">목표 지점 (월드 좌표)</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="firstWeight">상완이 하완을 따라가는 비율 (0~1)</param>
        /// <param name="secondWeight">하완의 롤 가중치</param>
        public void Solve(Vertex3f target, Matrix4x4f modelMatrix, Animator animator, float firstWeight = 0.5f, float secondWeight = 1.0f)
        {
            // 하완을 먼저 target 방향으로 roll
            Matrix4x4f secondRotation = _secondBoneRoll.Solve(target, modelMatrix, animator, out float angle2, secondWeight, true);

            // 각도 불연속 보정: -π와 π 사이의 경계를 넘을 때 발생하는 점프 방지
            float angleDiff = angle2 - _previousAngle;
            if (angleDiff > Math.PI)
                _accumulatedAngle -= 2 * (float)Math.PI;  // 180도 → -180도 점프 시 보정
            else if (angleDiff < -Math.PI)
                _accumulatedAngle += 2 * (float)Math.PI;  // -180도 → 180도 점프 시 보정

            _accumulatedAngle += angleDiff;
            _previousAngle = angle2;

            // 상완이 하완을 따라 비틀어짐 (누적 각도 사용)
            Matrix4x4f firstFollowRotation = GetRotationMatrix(_accumulatedAngle.ToDegree() * secondWeight * firstWeight);
            Matrix4x4f firstTransform = _firstBone.BoneMatrixSet.LocalTransform * firstFollowRotation;
            _firstBone.UpdateBone(ref firstTransform, animator, true);

            // 하완이 상완을 따라 비틀어진 것을 역회전으로 상쇄 (형태 유지)
            Matrix4x4f secondCounterRotation = firstFollowRotation.Inversed();
            Matrix4x4f secondTransform = secondCounterRotation * _secondBone.BoneMatrixSet.LocalTransform;
            _secondBone.UpdateBone(ref secondTransform, animator, true);
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