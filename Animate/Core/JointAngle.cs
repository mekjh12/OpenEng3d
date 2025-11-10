using FastMath;
using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 본의 관절 각도 제한 클래스<br/>
    /// - 본의 LocalTransform에서 부모 본 대비 상대 각도를 추출하고 제한 적용<br/>
    /// - 오일러 순서에 따라 동적으로 처리
    /// </summary>
    public class JointAngle
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private Bone _bone;                     // 제어할 본 (부모 본은 bone.Parent로 접근 가능)

        // 회전 제한 (오일러 각도 기준, 도 단위)
        private Vertex2f _pitchLimit;           // (min, max)
        private Vertex2f _yawLimit;             // (min, max)
        private Vertex2f _rollLimit;            // (min, max)

        // 현재 각도 캐시
        private EulerAngle _currentAngles;      // 현재 관절 각도
        private EulerOrder _eulerOrder;         // 오일러 각 계산 순서

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>현재 관절 각도 (pitch, roll, yaw) - 도 단위</summary>
        public EulerAngle CurrentAngles => _currentAngles;

        /// <summary>제어 대상 본</summary>
        public Bone Bone => _bone;

        /// <summary>오일러 각도 순서</summary>
        public EulerOrder EulerOrder => _eulerOrder;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 관절 각도 제한 객체 생성
        /// </summary>
        /// <param name="bone">제어할 본</param>
        /// <param name="eulerOrder">오일러 각도 순서</param>
        /// <param name="pitchMin">Pitch 최소값 (도)</param>
        /// <param name="pitchMax">Pitch 최대값 (도)</param>
        /// <param name="rollMin">Roll 최소값 (도)</param>
        /// <param name="rollMax">Roll 최대값 (도)</param>
        /// <param name="yawMin">Yaw 최소값 (도)</param>
        /// <param name="yawMax">Yaw 최대값 (도)</param>
        public JointAngle(Bone bone, EulerOrder eulerOrder,
                         float pitchMin = -180f, float pitchMax = 180f,
                         float rollMin = -90f, float rollMax = 90f,
                         float yawMin = -180f, float yawMax = 180f)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _pitchLimit = new Vertex2f(pitchMin, pitchMax);
            _rollLimit = new Vertex2f(rollMin, rollMax);
            _yawLimit = new Vertex2f(yawMin, yawMax);
            _eulerOrder = eulerOrder;
            _currentAngles = EulerAngle.Zero;
        }

        // -----------------------------------------------------------------------
        // 정적 생성 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 관절 각도 제한 객체 생성 (정적 팩토리 메서드)
        /// </summary>
        public static JointAngle CreateJointAngle(Bone bone, EulerOrder eulerOrder,
                                                   float pitchMin = -180f, float pitchMax = 180f,
                                                   float rollMin = -90f, float rollMax = 90f,
                                                   float yawMin = -180f, float yawMax = 180f)
        {
            return new JointAngle(bone, eulerOrder, pitchMin, pitchMax, rollMin, rollMax, yawMin, yawMax);
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// LocalTransform으로부터 부모 본 대비 상대 각도를 추출하고 제한 적용
        /// </summary>
        public void UpdateAndConstrain()
        {
            // 1. LocalTransform = 부모 본 공간에서의 변환
            Matrix4x4f localTransform = _bone.BoneMatrixSet.LocalTransform;

            // 2. 오일러 각도 추출 (설정된 순서대로)
            EulerAngle extractedAngles = EulerConverter.MatrixToEuler(localTransform, _eulerOrder);

            Console.WriteLine($"[{_bone.Name}] 원본 각도: {extractedAngles}");

            // 3. 각도 제한 적용
            EulerAngle clampedAngles = ClampRotation(extractedAngles);

            // 4. 각도가 변경되었으면 LocalTransform 업데이트
            if (!IsApproximatelyEqual(_currentAngles, clampedAngles))
            {
                _currentAngles = clampedAngles;
                ApplyClampedRotation();
                Console.WriteLine($"[{_bone.Name}] 제한 적용: {clampedAngles}");
            }

            Console.WriteLine("-------------------");
        }

        /// <summary>
        /// 각도 제한을 적용한 새로운 EulerAngle 반환
        /// </summary>
        public EulerAngle ClampRotation(EulerAngle angle)
        {
            float clampedPitch = angle.Pitch.Clamp(_pitchLimit.x, _pitchLimit.y);
            float clampedRoll = angle.Roll.Clamp(_rollLimit.x, _rollLimit.y);
            float clampedYaw = angle.Yaw.Clamp(_yawLimit.x, _yawLimit.y);

            return new EulerAngle(clampedPitch, clampedRoll, clampedYaw);
        }

        /// <summary>
        /// 각도 제한 범위 설정
        /// </summary>
        public void SetLimits(float pitchMin, float pitchMax,
                            float rollMin, float rollMax,
                            float yawMin, float yawMax)
        {
            _pitchLimit = new Vertex2f(pitchMin, pitchMax);
            _rollLimit = new Vertex2f(rollMin, rollMax);
            _yawLimit = new Vertex2f(yawMin, yawMax);
        }

        /// <summary>
        /// Pitch 제한 범위만 설정
        /// </summary>
        public void SetPitchLimit(float min, float max)
        {
            _pitchLimit = new Vertex2f(min, max);
        }

        /// <summary>
        /// Roll 제한 범위만 설정
        /// </summary>
        public void SetRollLimit(float min, float max)
        {
            _rollLimit = new Vertex2f(min, max);
        }

        /// <summary>
        /// Yaw 제한 범위만 설정
        /// </summary>
        public void SetYawLimit(float min, float max)
        {
            _yawLimit = new Vertex2f(min, max);
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 제한된 각도를 LocalTransform에 재적용 (EulerOrder에 따라)
        /// </summary>
        private void ApplyClampedRotation()
        {
            Matrix4x4f localTransform = _bone.BoneMatrixSet.LocalTransform;
            Vertex3f position = localTransform.Position;  // 위치 보존

            // 스케일 추출
            float scaleX = localTransform.Column0.xyz().Length();
            float scaleY = localTransform.Column1.xyz().Length();
            float scaleZ = localTransform.Column2.xyz().Length();

            // 제한된 각도로 회전 행렬 생성
            Matrix4x4f rotation = EulerConverter.EulerToMatrix(_currentAngles, _eulerOrder);

            // 스케일 재적용
            Matrix4x4f scale = Matrix4x4f.Scaled(scaleX, scaleY, scaleZ);
            Matrix4x4f result = rotation * scale;

            // 위치 복원
            result[3, 0] = position.x;
            result[3, 1] = position.y;
            result[3, 2] = position.z;

            _bone.BoneMatrixSet.LocalTransform = result;
        }

        /// <summary>
        /// 두 오일러 각도가 거의 같은지 확인 (부동소수점 오차 고려)
        /// </summary>
        private bool IsApproximatelyEqual(EulerAngle a, EulerAngle b, float epsilon = 0.01f)
        {
            return MathFast.Abs(a.Pitch - b.Pitch) < epsilon &&
                   MathFast.Abs(a.Roll - b.Roll) < epsilon &&
                   MathFast.Abs(a.Yaw - b.Yaw) < epsilon;
        }

        // -----------------------------------------------------------------------
        // ToString
        // -----------------------------------------------------------------------

        /// <summary>
        /// 현재 각도와 제한 범위를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"JointAngle[{_bone.Name}, {_eulerOrder}]\n" +
                   $"  Current: {_currentAngles}\n" +
                   $"  Limits: Pitch[{_pitchLimit.x:F1}°~{_pitchLimit.y:F1}°], " +
                   $"Roll[{_rollLimit.x:F1}°~{_rollLimit.y:F1}°], " +
                   $"Yaw[{_yawLimit.x:F1}°~{_yawLimit.y:F1}°]";
        }
    }
}