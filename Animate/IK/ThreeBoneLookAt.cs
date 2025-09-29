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

        // 각도 제한 설정
        private bool _useAngleLimits;
        private float _firstMaxYawAngle;
        private float _firstMaxPitchAngle;
        private float _secondMaxYawAngle;
        private float _secondMaxPitchAngle;
        private float _thirdMaxYawAngle;
        private float _thirdMaxPitchAngle;

        public bool UseAngleLimits => _useAngleLimits;


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

            // 각도 제한 기본값 (제한 없음)
            _useAngleLimits = false;
            _firstMaxYawAngle = 180f;
            _firstMaxPitchAngle = 180f;
            _secondMaxYawAngle = 180f;
            _secondMaxPitchAngle = 180f;
            _thirdMaxYawAngle = 180f;
            _thirdMaxPitchAngle = 180f;
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
        /// 각도 제한을 설정한다
        /// </summary>
        public void SetAngleLimits(
            float firstMaxYaw, float firstMaxPitch,
            float secondMaxYaw, float secondMaxPitch,
            float thirdMaxYaw, float thirdMaxPitch)
        {
            _useAngleLimits = true;
            _firstMaxYawAngle = Math.Max(0f, Math.Min(180f, firstMaxYaw));
            _firstMaxPitchAngle = Math.Max(0f, Math.Min(180f, firstMaxPitch));
            _secondMaxYawAngle = Math.Max(0f, Math.Min(180f, secondMaxYaw));
            _secondMaxPitchAngle = Math.Max(0f, Math.Min(180f, secondMaxPitch));
            _thirdMaxYawAngle = Math.Max(0f, Math.Min(180f, thirdMaxYaw));
            _thirdMaxPitchAngle = Math.Max(0f, Math.Min(180f, thirdMaxPitch));

            _firstBoneLookAt.SetAngleLimits(_firstMaxYawAngle, _firstMaxPitchAngle);
            _secondBoneLookAt.SetAngleLimits(_secondMaxYawAngle, _secondMaxPitchAngle);
            _thirdBoneLookAt.SetAngleLimits(_thirdMaxYawAngle, _thirdMaxPitchAngle);
        }

        /// <summary>
        /// 각도 제한을 해제한다
        /// </summary>
        public void DisableAngleLimits()
        {
            _useAngleLimits = false;
            _firstBoneLookAt.DisableAngleLimits();
            _secondBoneLookAt.DisableAngleLimits();
            _thirdBoneLookAt.DisableAngleLimits();
        }

        /// <summary>
        /// 특정 월드 위치를 바라보기 위한 회전 분석
        /// </summary>
        public void LookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
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