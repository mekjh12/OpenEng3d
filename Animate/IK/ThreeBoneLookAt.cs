using OpenGL;
using System;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 3개 본 Look At IK 시스템
    /// <br/>
    /// 세 개의 본(가슴+목+머리 등)이 목표 위치를 바라보도록 회전을 분산시키는 IK다.
    /// 각 본에 가중치를 적용하여 자연스러운 회전 분배가 가능하다.
    /// 
    /// <code>
    /// 사용 예시:
    /// var chestNeckHead = ThreeBoneLookAt.CreateNeckHead(armature, 
    ///     firstWeight: 0.2f, secondWeight: 0.3f);
    /// chestNeckHead.SetAngleLimits(
    ///     firstMaxYaw: 20f, firstMaxPitch: 15f,   // 가슴 제한
    ///     secondMaxYaw: 40f, secondMaxPitch: 30f, // 목 제한
    ///     thirdMaxYaw: 70f, thirdMaxPitch: 60f    // 머리 제한
    /// );
    /// chestNeckHead.LookAt(targetPosition, modelMatrix, animator);
    /// </code>
    /// </summary>
    public class ThreeBoneLookAt
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Bone _firstBone;    // 첫 번째 본 (가슴)
        private Bone _secondBone;   // 두 번째 본 (목)
        private Bone _thirdBone;    // 세 번째 본 (머리)

        private float _firstWeight;   // 첫 번째 본의 가중치 (0~1)
        private float _secondWeight;  // 두 번째 본의 가중치 (0~1)

        // 각 본의 OneBoneLookAt (회전 계산 및 적용용)
        private OneBoneLookAt _firstBoneLookAt;
        private OneBoneLookAt _secondBoneLookAt;
        private OneBoneLookAt _thirdBoneLookAt;

        // 각도 제한 설정
        private bool _useAngleLimits;              // 각도 제한 사용 여부
        private float _firstMaxYawAngle;           // 첫 번째 본 최대 좌우 회전 (도)
        private float _firstMaxPitchAngle;         // 첫 번째 본 최대 상하 회전 (도)
        private float _secondMaxYawAngle;          // 두 번째 본 최대 좌우 회전 (도)
        private float _secondMaxPitchAngle;        // 두 번째 본 최대 상하 회전 (도)
        private float _thirdMaxYawAngle;           // 세 번째 본 최대 좌우 회전 (도)
        private float _thirdMaxPitchAngle;         // 세 번째 본 최대 상하 회전 (도)

        // 부드러운 전환
        private float _smoothSpeed = 5.0f;
        private bool _isInitialized = false;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>각도 제한 사용 여부</summary>
        public bool UseAngleLimits => _useAngleLimits;

        /// <summary>부드러운 전환 속도 설정 (기본: 5.0)</summary>
        public float SmoothSpeed
        {
            get => _smoothSpeed;
            set => _smoothSpeed = Math.Max(0.1f, value);
        }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// ThreeBoneLookAt 생성자
        /// </summary>
        /// <param name="firstBone">첫 번째 본 (가슴 등)</param>
        /// <param name="secondBone">두 번째 본 (목 등)</param>
        /// <param name="threeBone">세 번째 본 (머리 등)</param>
        /// <param name="firstWeight">첫 번째 본의 회전 가중치 (0~1)</param>
        /// <param name="secondWeight">두 번째 본의 회전 가중치 (0~1)</param>
        /// <param name="localForward">로컬 전방 벡터 (기본: Z축)</param>
        /// <param name="localUp">로컬 상향 벡터 (기본: Y축)</param>
        public ThreeBoneLookAt(Bone firstBone, Bone secondBone, Bone threeBone,
            float firstWeight = 0.2f, float secondWeight = 0.3f,
            Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _thirdBone = threeBone;

            _firstWeight = firstWeight;
            _secondWeight = secondWeight;

            _firstBoneLookAt = new OneBoneLookAt(firstBone, localForward, localUp);
            _secondBoneLookAt = new OneBoneLookAt(secondBone, localForward, localUp);
            _thirdBoneLookAt = new OneBoneLookAt(threeBone, localForward, localUp);

            // 각도 제한 기본값 (제한 없음)
            _useAngleLimits = false;
            _firstMaxYawAngle = 180f;
            _firstMaxPitchAngle = 180f;
            _secondMaxYawAngle = 180f;
            _secondMaxPitchAngle = 180f;
            _thirdMaxYawAngle = 180f;
            _thirdMaxPitchAngle = 180f;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 인간형 가슴+목+머리 Look At 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <param name="firstBoneName">첫 번째 본 이름 (기본: 가슴)</param>
        /// <param name="secondBoneName">두 번째 본 이름 (기본: 목)</param>
        /// <param name="threeBoneName">세 번째 본 이름 (기본: 머리)</param>
        /// <param name="firstWeight">첫 번째 본의 회전 가중치 (0~1)</param>
        /// <param name="secondWeight">두 번째 본의 회전 가중치 (0~1)</param>
        /// <param name="localForward">로컬 전방 벡터</param>
        /// <param name="localUp">로컬 상향 벡터</param>
        /// <returns>생성된 ThreeBoneLookAt 인스턴스</returns>
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

            if (firstBone == null || secondBone == null || threeBone == null)
                throw new ArgumentException("목 또는 머리 또는 가슴 본을 찾을 수 없다.");

            return new ThreeBoneLookAt(firstBone, secondBone, threeBone, firstWeight, secondWeight, localForward, localUp);
        }

        /// <summary>
        /// 각도 제한을 설정한다
        /// </summary>
        /// <param name="firstMaxYaw">첫 번째 본 최대 좌우 회전 (도, 0~180)</param>
        /// <param name="firstMaxPitch">첫 번째 본 최대 상하 회전 (도, 0~180)</param>
        /// <param name="secondMaxYaw">두 번째 본 최대 좌우 회전 (도, 0~180)</param>
        /// <param name="secondMaxPitch">두 번째 본 최대 상하 회전 (도, 0~180)</param>
        /// <param name="thirdMaxYaw">세 번째 본 최대 좌우 회전 (도, 0~180)</param>
        /// <param name="thirdMaxPitch">세 번째 본 최대 상하 회전 (도, 0~180)</param>
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

            // 각 SingleBoneLookAt에 각도 제한 적용
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
        /// 특정 월드 위치를 바라보도록 세 본의 회전을 계산하고 적용한다
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="model">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트 (기본: Z축)</param>
        public void LookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 1단계: 세 번째 본(머리)의 전체 회전 정보 계산
            OneBoneLookAt.RotationInfo rotationInfo = _thirdBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(가슴)의 회전량 계산 및 적용 (첫 번째 가중치만큼만 회전)
            Quaternion firstRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, firstRotation);

            // 3단계: 두 번째 본(목)의 회전량 계산 및 적용 (두 번째 가중치만큼만 회전)
            Quaternion secondRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _secondWeight);
            _secondBoneLookAt.Rotate(animator, secondRotation);

            // 4단계: 세 번째 본(머리)의 회전량 계산 및 적용 (나머지 회전 담당)
            _thirdBoneLookAt.SolveWorldTarget(worldTargetPosition, model, animator, worldUpHint);
        }

        /// <summary>
        /// 부드러운 전환을 적용하여 LookAt 수행
        /// </summary>
        public void SmoothLookAt(Vertex3f worldTargetPosition, Matrix4x4f model,
                                 Animator animator, float deltaTime,
                                 Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 각 본의 SingleBoneLookAt에 SmoothSpeed 전달
            _firstBoneLookAt.SmoothSpeed = _smoothSpeed;
            _secondBoneLookAt.SmoothSpeed = _smoothSpeed;
            _thirdBoneLookAt.SmoothSpeed = _smoothSpeed;

            // 1단계: 세 번째 본(머리)의 전체 회전 정보 계산
            OneBoneLookAt.RotationInfo rotationInfo =
                _thirdBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(가슴)의 회전량 계산 및 적용 (첫 번째 가중치만큼만 회전)
            Quaternion firstRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, firstRotation);

            // 3단계: 두 번째 본(목)의 회전량 계산 및 적용 (두 번째 가중치만큼만 회전)
            Quaternion secondRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _secondWeight);
            _secondBoneLookAt.Rotate(animator, secondRotation);

            // 4단계: 세 번째 본(머리)의 회전량 계산 및 적용 (부드럽게)
            _thirdBoneLookAt.SolveSmooth(worldTargetPosition, model, animator, deltaTime, worldUpHint);
        }
    }
}