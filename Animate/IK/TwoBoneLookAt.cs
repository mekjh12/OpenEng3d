using OpenGL;
using System;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 2개 본 Look At IK 시스템
    /// <br/>
    /// 두 개의 본(목+머리 등)이 목표 위치를 바라보도록 회전을 분산시키는 IK다.
    /// 첫 번째 본에 가중치를 적용하여 자연스러운 회전 분배가 가능하다.
    /// 
    /// <code>
    /// 사용 예시:
    /// var neckHeadLookAt = TwoBoneLookAt.CreateNeckHead(armature, neckWeight: 0.5f);
    /// neckHeadLookAt.SetAngleLimits(
    ///     firstMaxYaw: 30f, firstMaxPitch: 20f,  // 목 제한
    ///     secondMaxYaw: 60f, secondMaxPitch: 45f  // 머리 제한
    /// );
    /// neckHeadLookAt.LookAt(targetPosition, modelMatrix, animator);
    /// </code>
    /// </summary>
    public class TwoBoneLookAt
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Bone _firstBone;      // 첫 번째 본 (목)
        private Bone _secondBone;     // 두 번째 본 (머리)
        private float _firstWeight;   // 첫 번째 본의 가중치 (0~1)

        // 각 본의 OneBoneLookAt (회전 계산 및 적용용)
        private OneBoneLookAt _firstBoneLookAt;
        private OneBoneLookAt _secondBoneLookAt;

        // 각도 제한 설정
        private bool _useAngleLimits;              // 각도 제한 사용 여부
        private float _firstMaxYawAngle;           // 첫 번째 본 최대 좌우 회전 (도)
        private float _firstMaxPitchAngle;         // 첫 번째 본 최대 상하 회전 (도)
        private float _secondMaxYawAngle;          // 두 번째 본 최대 좌우 회전 (도)
        private float _secondMaxPitchAngle;        // 두 번째 본 최대 상하 회전 (도)

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
        /// TwoBoneLookAt 생성자
        /// </summary>
        /// <param name="firstBone">첫 번째 본 (목 등)</param>
        /// <param name="secondBone">두 번째 본 (머리 등)</param>
        /// <param name="firstWeight">첫 번째 본의 회전 가중치 (0~1)</param>
        /// <param name="localForward">로컬 전방 벡터 (기본: Y축)</param>
        /// <param name="localUp">로컬 상향 벡터 (기본: Z축)</param>
        public TwoBoneLookAt(Bone firstBone, Bone secondBone, float firstWeight = 0.5f,
            Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _firstWeight = firstWeight;

            _firstBoneLookAt = new OneBoneLookAt(firstBone, localForward, localUp);
            _secondBoneLookAt = new OneBoneLookAt(secondBone, localForward, localUp);

            // 각도 제한 기본값 (제한 없음)
            _useAngleLimits = false;
            _firstMaxYawAngle = 180f;
            _firstMaxPitchAngle = 180f;
            _secondMaxYawAngle = 180f;
            _secondMaxPitchAngle = 180f;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 인간형 목+머리 Look At 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <param name="secondBoneName">두 번째 본 이름 (기본: 머리)</param>
        /// <param name="firstBoneName">첫 번째 본 이름 (기본: 목)</param>
        /// <param name="neckWeight">목의 회전 가중치 (0~1)</param>
        /// <param name="localForward">로컬 전방 벡터</param>
        /// <param name="localUp">로컬 상향 벡터</param>
        /// <returns>생성된 TwoBoneLookAt 인스턴스</returns>
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
                throw new ArgumentException("목 또는 머리 본을 찾을 수 없다.");

            return new TwoBoneLookAt(firstBone, secondBone, neckWeight, localForward, localUp);
        }

        /// <summary>
        /// 각도 제한을 설정한다
        /// </summary>
        /// <param name="firstMaxYaw">첫 번째 본 최대 좌우 회전 (도, 0~180)</param>
        /// <param name="firstMaxPitch">첫 번째 본 최대 상하 회전 (도, 0~180)</param>
        /// <param name="secondMaxYaw">두 번째 본 최대 좌우 회전 (도, 0~180)</param>
        /// <param name="secondMaxPitch">두 번째 본 최대 상하 회전 (도, 0~180)</param>
        public void SetAngleLimits(float firstMaxYaw, float firstMaxPitch,
                           float secondMaxYaw, float secondMaxPitch)
        {
            _useAngleLimits = true;
            _firstMaxYawAngle = Math.Max(0f, Math.Min(180f, firstMaxYaw));
            _firstMaxPitchAngle = Math.Max(0f, Math.Min(180f, firstMaxPitch));
            _secondMaxYawAngle = Math.Max(0f, Math.Min(180f, secondMaxYaw));
            _secondMaxPitchAngle = Math.Max(0f, Math.Min(180f, secondMaxPitch));

            // 각 OneBoneLookAt에 각도 제한 적용
            _firstBoneLookAt.SetAngleLimits(_firstMaxYawAngle, _firstMaxPitchAngle);
            _secondBoneLookAt.SetAngleLimits(_secondMaxYawAngle, _secondMaxPitchAngle);
        }

        /// <summary>
        /// 각도 제한을 해제한다
        /// </summary>
        public void DisableAngleLimits()
        {
            _useAngleLimits = false;
            _firstBoneLookAt.DisableAngleLimits();
            _secondBoneLookAt.DisableAngleLimits();
        }

        /// <summary>
        /// 특정 월드 위치를 바라보도록 두 본의 회전을 계산하고 적용한다
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="model">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트 (기본: Z축)</param>
        public void LookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 1단계: 두 번째 본(머리)의 전체 회전 정보 계산
            OneBoneLookAt.RotationInfo rotationInfo = _secondBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(목)의 회전량 계산 및 적용 (가중치만큼만 회전)
            Quaternion rotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, rotation);

            // 3단계: 두 번째 본(머리)의 회전량 계산 및 적용 (나머지 회전 담당)
            _secondBoneLookAt.SolveWorldTarget(worldTargetPosition, model, animator, worldUpHint);
        }

        /// <summary>
        /// 부드러운 전환을 적용하여 LookAt 수행
        /// </summary>
        public void SmoothLookAt(Vertex3f worldTargetPosition, Matrix4x4f model,
                                 Animator animator, float deltaTime,
                                 Vertex3f worldUpHint = default)
        {
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 각 본의 OneBoneLookAt에 SmoothSpeed 전달
            _firstBoneLookAt.SmoothSpeed = _smoothSpeed;
            _secondBoneLookAt.SmoothSpeed = _smoothSpeed;

            // 1단계: 두 번째 본(머리)의 전체 회전 정보 계산
            OneBoneLookAt.RotationInfo rotationInfo =
                _secondBoneLookAt.CalculateRotation(worldTargetPosition, model, animator, worldUpHint);

            // 2단계: 첫 번째 본(목)의 회전량 계산 및 적용 (가중치만큼만 회전)
            Quaternion firstRotation = Quaternion.Identity.Interpolate(rotationInfo.Quaternion, _firstWeight);
            _firstBoneLookAt.Rotate(animator, firstRotation);

            // 3단계: 두 번째 본(머리)의 회전량 계산 및 적용 (부드럽게)
            _secondBoneLookAt.SolveSmooth(worldTargetPosition, model, animator, deltaTime, worldUpHint);
        }
    }
}