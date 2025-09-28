using OpenGL;
using System;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 2개 본 Look At IK - 1단계: 회전량 분석
    /// </summary>
    public class TwoBoneLookAt
    {
        private Bone _firstBone;      // 첫 번째 본 (목)
        private Bone _secondBone;     // 두 번째 본 (머리)

        private float _firstWeight;   // 첫 번째 본의 가중치 (0~1)

        // 두 번째 본용 SingleBoneLookAt (회전량 분석용)
        private SingleBoneLookAt _secondBoneLookAt;
        private SingleBoneLookAt _firstBoneLookAt;

        public TwoBoneLookAt(Bone firstBone, Bone secondBone, float firstWeight = 0.5f,
            Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _firstWeight = firstWeight;

            _firstBoneLookAt = new SingleBoneLookAt(firstBone, localForward, localUp);
            _secondBoneLookAt = new SingleBoneLookAt(secondBone, localForward, localUp);
        }

        /// <summary>
        /// 인간형 목+머리 Look At 생성
        /// </summary>
        public static TwoBoneLookAt CreateNeckHead(Armature armature, 
            string secondBoneName = "mixamorig_Head", 
            string firstBoneName = "mixamorig_Neck", 
            float neckWeight = 0.5f, 
            Vertex3f localForward = default,
            Vertex3f localUp = default)
        {
            localForward = (localForward == default ? Vertex3f.UnitY : localForward).Normalized;
            localUp = (localUp == default ? Vertex3f.UnitZ : localUp).Normalized;

            Bone firstBone = armature[firstBoneName];
            Bone secondBone = armature[secondBoneName];

            if (firstBone == null || secondBone == null)
                throw new ArgumentException("목 또는 머리 본을 찾을 수 없습니다.");

            return new TwoBoneLookAt(firstBone, secondBone, neckWeight, localForward, localUp);
        }

        /// <summary>
        /// 특정 월드 위치를 바라보기 위한 회전 분석
        /// </summary>
        public void AnalyzeLookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 1단계: 두 번째 본(머리)의 전체 회전 정보 계산
            SingleBoneLookAt.RotationInfo rotationInfo = _secondBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(목)의 회전량 계산 및 적용
            Quaternion rotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, rotation);

            // 3단계: 두 번째 본(머리)의 회전량 계산 및 적용
            _secondBoneLookAt.LookAt(worldTargetPosition, model, animator, worldUpHint);
        }
    }
}