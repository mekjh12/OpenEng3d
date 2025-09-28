using OpenGL;
using System;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 2개 본 Look At IK - 1단계: 회전량 분석
    /// </summary>
    public class ThreeBoneLookAt
    {
        private Bone _firstBone;    // 첫 번째 본 (가슴)
        private Bone _secondBone;   // 두 번째 본 (목)
        private Bone _thirdBone;    // 세 번째 본 (머리)

        private float _firstWeight;   // 첫 번째 본의 가중치 (0~1)
        private float _secondWeight;  // 두 번째 본의 가중치 (0~1)

        // 두 번째 본용 SingleBoneLookAt (회전량 분석용)
        private SingleBoneLookAt _secondBoneLookAt;
        private SingleBoneLookAt _firstBoneLookAt;
        private SingleBoneLookAt _thirdBoneLookAt;


        public ThreeBoneLookAt(Bone firstBone, Bone secondBone, Bone threeBone,
            float firstWeight = 0.2f, float secondWeight = 0.3f,
            Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _thirdBone = threeBone;
            
            _firstWeight = firstWeight;
            _secondWeight = secondWeight;

            _firstBoneLookAt = new SingleBoneLookAt(firstBone, localForward, localUp);
            _secondBoneLookAt = new SingleBoneLookAt(secondBone, localForward, localUp);
            _thirdBoneLookAt = new SingleBoneLookAt(threeBone, localForward, localUp);
        }

        /// <summary>
        /// 인간형 목+머리 Look At 생성
        /// </summary>
        public static ThreeBoneLookAt CreateNeckHead(Armature armature,
            string firstBoneName = "mixamorig_Spine1",
            string secondBoneName = "mixamorig_Neck",
            string threeBoneName = "mixamorig_Head",
            float firstWeight = 0.2f,
            float secondWeight = 0.3f,

            Vertex3f localForward = default,
            Vertex3f localUp = default)
        {
            localForward = (localForward == default ? Vertex3f.UnitZ : localForward).Normalized;
            localUp = (localUp == default ? Vertex3f.UnitY : localUp).Normalized;

            Bone firstBone = armature[firstBoneName];
            Bone secondBone = armature[secondBoneName];
            Bone threeBone = armature[threeBoneName];

            if (firstBone == null || secondBone == null || threeBone == null )
                throw new ArgumentException("목 또는 머리 또는 가슴 본을 찾을 수 없습니다.");

            return new ThreeBoneLookAt(firstBone, secondBone, threeBone, firstWeight, secondWeight, localForward, localUp);
        }

        /// <summary>
        /// 특정 월드 위치를 바라보기 위한 회전 분석
        /// </summary>
        public void AnalyzeLookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 1단계: 세 번째 본(머리)의 전체 회전 정보 계산
            SingleBoneLookAt.RotationInfo rotationInfo = _thirdBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(목)의 회전량 계산 및 적용
            Quaternion firstRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, firstRotation);

            // 3단계: 두 번째 본(목)의 회전량 계산 및 적용
            Quaternion secondRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _secondWeight);
            _secondBoneLookAt.Rotate(animator, secondRotation);

            // 4단계: 세 번째 본(머리)의 회전량 계산 및 적용
            _thirdBoneLookAt.LookAt(worldTargetPosition, model, animator, worldUpHint);
        }
    }
}