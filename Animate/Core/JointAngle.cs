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
        private Bone _bone;                                 // 제어할 본 (부모 본은 bone.Parent로 접근 가능)

        // 회전 제한 (오일러 각도 기준)
        private Vertex2f _pitch;                            // (pitchMin, pitchMax)
        private Vertex2f _yaw;                              // (yawMin, yawMax)
        private Vertex2f _roll;                             // (rollMin, rollMax)

        // 현재 각도 캐시
        private Vertex3f _currentAngles;                    // (pitch, yaw, roll) - 도 단위
        private EulerOrder _eulerOrder;                     // 오일러 각 계산 순서

        public Vertex3f CurrentAngles => _currentAngles;    // 현재 관절 각도 (pitch, yaw, roll) - 도 단위
        public Bone Bone => _bone;                          // 제어 대상 본

        public JointAngle(Bone bone, EulerOrder eulerOrder,
                         float pitchMin = -180f, float pitchMax = 180f,
                         float yawMin = -180f, float yawMax = 180f,
                         float rollMin = -90f, float rollMax = 90f)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _pitch = new Vertex2f(pitchMin, pitchMax);
            _yaw = new Vertex2f(yawMin, yawMax);
            _roll = new Vertex2f(rollMin, rollMax);
            _eulerOrder = eulerOrder;
        }

        public static JointAngle CreateJointAngle(Bone bone, EulerOrder eulerOrder,
                                                     float pitchMin = -180f, float pitchMax = 180f,
                                                     float yawMin = -180f, float yawMax = 180f,
                                                     float rollMin = -90f, float rollMax = 90f)
        {
            return new JointAngle(bone, eulerOrder, pitchMin, pitchMax, yawMin, yawMax, rollMin, rollMax);
        }

        /// <summary>
        /// LocalTransform으로부터 부모 본 대비 상대 각도를 추출하고 제한 적용
        /// </summary>
        public void UpdateAndConstrain()
        {
            // 1. LocalTransform = 부모 본 공간에서의 변환
            Matrix4x4f localTransform = _bone.BoneMatrixSet.LocalTransform;

            // 2. 오일러 각도 추출 (설정된 순서대로)
            _currentAngles = EulerConverter.MatrixToEuler(localTransform, _eulerOrder);

            Console.WriteLine("원본=" + _currentAngles);

            // 3. 각도 제한 적용
            Vertex3f clampedAngles = ClampRotation(_currentAngles.x, _currentAngles.y, _currentAngles.z);

            // 4. 각도가 변경되었으면 LocalTransform 업데이트
            if (!IsApproximatelyEqual(_currentAngles, clampedAngles))
            {
                _currentAngles = clampedAngles;
                ApplyClampedRotation();
                Console.WriteLine("변경=" + clampedAngles);
            }

            Console.WriteLine("-------------------");
        }

        /// <summary>
        /// 각도 제한 적용
        /// </summary>
        public Vertex3f ClampRotation(float pitch, float yaw, float roll)
        {
            float clampedPitch = pitch.Clamp(_pitch.x, _pitch.y);
            float clampedYaw = yaw.Clamp(_yaw.x, _yaw.y);
            float clampedRoll = roll.Clamp(_roll.x, _roll.y);

            return new Vertex3f(clampedPitch, clampedYaw, clampedRoll);
        }

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

            // 개별 회전 행렬 생성
            Matrix4x4f rotX = Matrix4x4f.RotatedX(-_currentAngles.x);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(-_currentAngles.y);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(-_currentAngles.z);

            // EulerOrder에 따라 회전 조합 (오른쪽부터 적용)
            Matrix4x4f rotation = CombineRotations(rotX, rotY, rotZ);

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
        /// EulerOrder에 따라 회전 행렬들을 올바른 순서로 조합
        /// </summary>
        private Matrix4x4f CombineRotations(Matrix4x4f rotX, Matrix4x4f rotY, Matrix4x4f rotZ)
        {
            switch (_eulerOrder)
            {
                case EulerOrder.XYZ:
                    return rotZ * rotY * rotX;  // Z * Y * X (오른쪽부터)

                case EulerOrder.XZY:
                    return rotY * rotZ * rotX;  // Y * Z * X

                case EulerOrder.YXZ:
                    return rotZ * rotX * rotY;  // Z * X * Y

                case EulerOrder.YZX:
                    return rotX * rotZ * rotY;  // X * Z * Y

                case EulerOrder.ZXY:
                    return rotY * rotX * rotZ;  // Y * X * Z

                case EulerOrder.ZYX:
                    return rotX * rotY * rotZ;  // X * Y * Z

                default:
                    throw new ArgumentException($"지원하지 않는 EulerOrder: {_eulerOrder}");
            }
        }

        /// <summary>
        /// 두 각도 벡터가 거의 같은지 확인 (부동소수점 오차 고려)
        /// </summary>
        private bool IsApproximatelyEqual(Vertex3f a, Vertex3f b, float epsilon = 0.01f)
        {
            return Math.Abs(a.x - b.x) < epsilon &&
                   Math.Abs(a.y - b.y) < epsilon &&
                   Math.Abs(a.z - b.z) < epsilon;
        }

        /// <summary>
        /// 현재 각도를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"[{_eulerOrder}] pitch(X)={_currentAngles.x:F1}°, " +
                $"yaw(Y)={_currentAngles.y:F1}°, " +
                $"roll(Z)={_currentAngles.z:F1}°";
        }
    }
}