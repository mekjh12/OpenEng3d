using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 발자국 분석을 위한 간결한 유틸리티 클래스
    /// 모션의 발자국 거리만 계산합니다.
    /// </summary>
    public class FootStepAnalyzer
    {
        // 상수
        private const string LEFT_FOOT_NAME = "mixamorig_LeftToeBase";
        private const string RIGHT_FOOT_NAME = "mixamorig_RightToeBase";

        // 결과 데이터
        public struct FootStepResult
        {
            public float FootStepDistance;  // 발자국 거리
            public float Speed;             // 속도 (방향성 포함)
            public MovementType Type;       // 이동 타입
        }

        // 이동 타입
        public enum MovementType
        {
            Forward,    // 전진
            Backward,   // 후진
            Left,       // 좌측 이동
            Right,      // 우측 이동
            Stationary  // 제자리
        }

        /// <summary>
        /// 모션의 발자국 거리를 분석합니다.
        /// </summary>
        /// <param name="motion">분석할 모션</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="rootBone">루트 본</param>
        /// <returns>발자국 분석 결과</returns>
        public static FootStepResult AnalyzeFootStep(Motion motion, Animator animator, Bone rootBone)
        {
            var result = new FootStepResult();

            if (motion.KeyFrameCount < 2)
            {
                return result; // 키프레임이 부족하면 기본값 반환
            }

            // 본 인덱스 찾기
            int leftFootIndex = FindBoneIndex(animator, LEFT_FOOT_NAME);
            int rightFootIndex = FindBoneIndex(animator, RIGHT_FOOT_NAME);

            if (leftFootIndex < 0 || rightFootIndex < 0)
            {
                //Console.WriteLine($"[경고] {motion.Name}: 발 본을 찾을 수 없습니다.");
                return result;
            }

            // 발자국 위치 수집
            var leftFootPositions = CollectFootPositions(motion, animator, rootBone, leftFootIndex);
            var rightFootPositions = CollectFootPositions(motion, animator, rootBone, rightFootIndex);

            // 발자국 거리 계산
            float leftDistance = CalculateFootDistance(leftFootPositions);
            float rightDistance = CalculateFootDistance(rightFootPositions);

            result.FootStepDistance = Math.Max(leftDistance, rightDistance);

            // 모션 이름 기반 방향성 판단
            result.Type = DetermineMovementType(motion.Name);

            // 방향성을 고려한 속도 계산
            float baseSpeed = motion.PeriodTime > 0 ? (rightDistance + leftDistance) / motion.PeriodTime : 0f;
            result.Speed = ApplyDirectionalSpeed(baseSpeed, result.Type);

            // 로그 출력
            LogResult(motion.Name, result);

            return result;
        }

        /// <summary>
        /// 본 인덱스를 찾습니다.
        /// </summary>
        private static int FindBoneIndex(Animator animator, string boneName)
        {
            for (int i = 0; i < animator.BoneTraversalOrder.Length; i++)
            {
                if (animator.BoneTraversalOrder[i].Name == boneName)
                {
                    return animator.BoneTraversalOrder[i].Index;
                }
            }
            return -1;
        }

        /// <summary>
        /// 발 위치를 수집합니다.
        /// </summary>
        private static List<Vertex2f> CollectFootPositions(Motion motion, Animator animator, Bone rootBone, int footIndex)
        {
            var positions = new List<Vertex2f>();
            var sortedKeyframes = motion.Keyframes.OrderBy(kvp => kvp.Key).ToArray();

            foreach (var kvp in sortedKeyframes)
            {
                float time = kvp.Key;
                UpdateAnimationTransforms(motion, animator, rootBone, time);

                var footMatrix = animator.GetRootTransform(animator.BoneTraversalOrder[footIndex]);
                positions.Add(footMatrix.Position.xy());
            }

            return positions;
        }

        /// <summary>
        /// 발의 이동 거리를 계산합니다.
        /// </summary>
        private static float CalculateFootDistance(List<Vertex2f> positions)
        {
            if (positions.Count < 2) return 0f;

            var min = new Vertex2f(float.MaxValue, float.MaxValue);
            var max = new Vertex2f(float.MinValue, float.MinValue);

            foreach (var pos in positions)
            {
                min = Vertex2f.Min(min, pos);
                max = Vertex2f.Max(max, pos);
            }

            return (max - min).Norm();
        }

        /// <summary>
        /// 모션 이름을 기반으로 이동 타입을 판단합니다.
        /// </summary>
        private static MovementType DetermineMovementType(string motionName)
        {
            string lowerName = motionName.ToLower();

            // 후진 키워드 확인 (우선순위 높음)
            if (lowerName.Contains("back") || lowerName.Contains("backward") ||
                lowerName.Contains("reverse") || lowerName.Contains("retreat"))
            {
                return MovementType.Backward;
            }

            // 좌우 이동 키워드 확인
            if (lowerName.Contains("left") || lowerName.Contains("strafe") && lowerName.Contains("left"))
            {
                return MovementType.Left;
            }

            if (lowerName.Contains("right") || lowerName.Contains("strafe") && lowerName.Contains("right"))
            {
                return MovementType.Right;
            }

            // 전진 키워드 확인
            if (lowerName.Contains("walk") || lowerName.Contains("run") || lowerName.Contains("jog") ||
                lowerName.Contains("sprint") || lowerName.Contains("forward") || lowerName.Contains("march") ||
                lowerName.Contains("jump"))
            {
                return MovementType.Forward;
            }

            // 기본값은 제자리
            return MovementType.Stationary;
        }

        /// <summary>
        /// 이동 타입에 따라 방향성을 적용한 속도를 계산합니다.
        /// </summary>
        private static float ApplyDirectionalSpeed(float baseSpeed, MovementType type)
        {
            switch (type)
            {
                case MovementType.Forward:
                    return baseSpeed;           // 양의 속도
                case MovementType.Backward:
                    return -baseSpeed;          // 음의 속도
                case MovementType.Left:
                case MovementType.Right:
                    return baseSpeed;           // 좌우 이동은 양의 속도 (방향은 타입으로 구분)
                case MovementType.Stationary:
                default:
                    return 0f;                  // 제자리는 속도 0
            }
        }

        /// <summary>
        /// 분석 결과를 로그로 출력합니다.
        /// </summary>
        private static void LogResult(string motionName, FootStepResult result)
        {
            string direction;
            switch (result.Type)
            {
                case MovementType.Forward:
                    direction = "전진";
                    break;
                case MovementType.Backward:
                    direction = "후진";
                    break;
                case MovementType.Left:
                    direction = "좌측이동";
                    break;
                case MovementType.Right:
                    direction = "우측이동";
                    break;
                case MovementType.Stationary:
                    direction = "제자리";
                    break;
                default:
                    direction = "알수없음";
                    break;
            }

            //Console.WriteLine($"[{motionName}] 발자국거리: {result.FootStepDistance:F3}, " +
            //                $"속도: {result.Speed:F3}, 방향: {direction}");
        }
        private static void UpdateAnimationTransforms(Motion motion, Animator animator, Bone rootBone, float time)
        {
            var currentPose = new Dictionary<string, Matrix4x4f>();
            if (motion.InterpolatePoseAtTime(time, ref currentPose))
            {
                var queue = new Queue<Bone>();
                queue.Enqueue(rootBone);

                while (queue.Count > 0)
                {
                    Bone bone = queue.Dequeue();
                    if (bone.Index < 0) continue;

                    Matrix4x4f parentTransform = bone.IsRoot ?
                        Matrix4x4f.Identity :
                        animator.GetRootTransform(bone.Parent);

                    Matrix4x4f localTransform = currentPose.ContainsKey(bone.Name) ?
                        currentPose[bone.Name] :
                        bone.BoneMatrixSet.LocalBindTransform;

                    Matrix4x4f rootTransform = parentTransform * localTransform;
                    animator.SetRootTransform(bone.Index, rootTransform);

                    foreach (var child in bone.Children)
                        queue.Enqueue(child);
                }
            }
        }
    }
}