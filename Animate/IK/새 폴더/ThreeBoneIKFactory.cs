using OpenGL;
using System;

namespace Animate
{
    /// <summary>
    /// ThreeBoneIK 생성을 위한 정적 헬퍼 클래스
    /// </summary>
    public static class ThreeBoneIKFactory
    {
        // -----------------------------------------------------------------------
        // 척추 IK 생성
        // -----------------------------------------------------------------------

        /// <summary>
        /// 척추 IK 생성 (Mixamo 리그 기준)
        /// Spine → Spine1 → Spine2 → Neck
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>척추 IK</returns>
        public static ThreeBoneIK CreateSpineIK(Armature armature)
        {
            Bone spine = armature["mixamorig_Spine"];
            Bone spine1 = armature["mixamorig_Spine1"];
            Bone spine2 = armature["mixamorig_Spine2"];
            Bone neck = armature["mixamorig_Neck"];

            ValidateBones(spine, spine1, spine2, neck, "척추");
            return new ThreeBoneIK(spine, spine1, spine2, neck);
        }

        // -----------------------------------------------------------------------
        // 팔 IK 생성 (어깨 포함)
        // -----------------------------------------------------------------------

        /// <summary>
        /// 왼쪽 팔 전체 IK 생성 (어깨 → 상완 → 전완 → 손)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 팔 전체 IK</returns>
        public static ThreeBoneIK CreateLeftFullArmIK(Armature armature)
        {
            Bone shoulder = armature["mixamorig_LeftArm"];
            Bone upperArm = armature["mixamorig_LeftForeArm"];
            Bone forearm = armature["mixamorig_LeftHand"];
            Bone hand = armature["mixamorig_LeftHandMiddle1"];

            ValidateBones(shoulder, upperArm, forearm, hand, "왼쪽 팔 전체");
            return new ThreeBoneIK(shoulder, upperArm, forearm, hand);
        }

        /// <summary>
        /// 오른쪽 팔 전체 IK 생성 (어깨 → 상완 → 전완 → 손)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 팔 전체 IK</returns>
        public static ThreeBoneIK CreateRightFullArmIK(Armature armature)
        {
            Bone shoulder = armature["mixamorig_RightShoulder"];
            Bone upperArm = armature["mixamorig_RightArm"];
            Bone forearm = armature["mixamorig_RightForeArm"];
            Bone hand = armature["mixamorig_RightHand"];

            ValidateBones(shoulder, upperArm, forearm, hand, "오른쪽 팔 전체");
            return new ThreeBoneIK(shoulder, upperArm, forearm, hand);
        }

        // -----------------------------------------------------------------------
        // 다리 IK 생성 (발목까지)
        // -----------------------------------------------------------------------

        /// <summary>
        /// 왼쪽 다리 전체 IK 생성 (엉덩이 → 무릎 → 발목 → 발)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 다리 전체 IK</returns>
        public static ThreeBoneIK CreateLeftFullLegIK(Armature armature)
        {
            Bone hip = armature["mixamorig_LeftUpLeg"];
            Bone knee = armature["mixamorig_LeftLeg"];
            Bone ankle = armature["mixamorig_LeftFoot"];
            Bone toe = armature["mixamorig_LeftToeBase"];

            ValidateBones(hip, knee, ankle, toe, "왼쪽 다리 전체");
            return new ThreeBoneIK(hip, knee, ankle, toe);
        }

        /// <summary>
        /// 오른쪽 다리 전체 IK 생성 (엉덩이 → 무릎 → 발목 → 발)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 다리 전체 IK</returns>
        public static ThreeBoneIK CreateRightFullLegIK(Armature armature)
        {
            Bone hip = armature["mixamorig_RightUpLeg"];
            Bone knee = armature["mixamorig_RightLeg"];
            Bone ankle = armature["mixamorig_RightFoot"];
            Bone toe = armature["mixamorig_RightToeBase"];

            ValidateBones(hip, knee, ankle, toe, "오른쪽 다리 전체");
            return new ThreeBoneIK(hip, knee, ankle, toe);
        }

        // -----------------------------------------------------------------------
        // 손가락 IK 생성
        // -----------------------------------------------------------------------

        /// <summary>
        /// 왼쪽 검지 IK 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 검지 IK</returns>
        public static ThreeBoneIK CreateLeftIndexFingerIK(Armature armature)
        {
            Bone finger1 = armature["mixamorig_LeftHandIndex1"];
            Bone finger2 = armature["mixamorig_LeftHandIndex2"];
            Bone finger3 = armature["mixamorig_LeftHandIndex3"];
            Bone finger4 = armature["mixamorig_LeftHandIndex4"];

            ValidateBones(finger1, finger2, finger3, finger4, "왼쪽 검지");
            return new ThreeBoneIK(finger1, finger2, finger3, finger4);
        }

        /// <summary>
        /// 오른쪽 검지 IK 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 검지 IK</returns>
        public static ThreeBoneIK CreateRightIndexFingerIK(Armature armature)
        {
            Bone finger1 = armature["mixamorig_RightHandIndex1"];
            Bone finger2 = armature["mixamorig_RightHandIndex2"];
            Bone finger3 = armature["mixamorig_RightHandIndex3"];
            Bone finger4 = armature["mixamorig_RightHandIndex4"];

            ValidateBones(finger1, finger2, finger3, finger4, "오른쪽 검지");
            return new ThreeBoneIK(finger1, finger2, finger3, finger4);
        }

        /// <summary>
        /// 왼쪽 중지 IK 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 중지 IK</returns>
        public static ThreeBoneIK CreateLeftMiddleFingerIK(Armature armature)
        {
            Bone finger1 = armature["mixamorig_LeftHandMiddle1"];
            Bone finger2 = armature["mixamorig_LeftHandMiddle2"];
            Bone finger3 = armature["mixamorig_LeftHandMiddle3"];
            Bone finger4 = armature["mixamorig_LeftHandMiddle4"];

            ValidateBones(finger1, finger2, finger3, finger4, "왼쪽 중지");
            return new ThreeBoneIK(finger1, finger2, finger3, finger4);
        }

        /// <summary>
        /// 오른쪽 중지 IK 생성
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 중지 IK</returns>
        public static ThreeBoneIK CreateRightMiddleFingerIK(Armature armature)
        {
            Bone finger1 = armature["mixamorig_RightHandMiddle1"];
            Bone finger2 = armature["mixamorig_RightHandMiddle2"];
            Bone finger3 = armature["mixamorig_RightHandMiddle3"];
            Bone finger4 = armature["mixamorig_RightHandMiddle4"];

            ValidateBones(finger1, finger2, finger3, finger4, "오른쪽 중지");
            return new ThreeBoneIK(finger1, finger2, finger3, finger4);
        }

        // -----------------------------------------------------------------------
        // 꼬리 IK 생성 (동물 리그용)
        // -----------------------------------------------------------------------

        /// <summary>
        /// 꼬리 IK 생성 (동물 리그 기준)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>꼬리 IK</returns>
        public static ThreeBoneIK CreateTailIK(Armature armature)
        {
            Bone tail1 = armature["Tail1"];
            Bone tail2 = armature["Tail2"];
            Bone tail3 = armature["Tail3"];
            Bone tailEnd = armature["TailEnd"];

            ValidateBones(tail1, tail2, tail3, tailEnd, "꼬리");
            return new ThreeBoneIK(tail1, tail2, tail3, tailEnd);
        }

        // -----------------------------------------------------------------------
        // 유효성 검증
        // -----------------------------------------------------------------------

        /// <summary>
        /// 4개 본의 유효성을 검증한다
        /// </summary>
        private static void ValidateBones(Bone bone1, Bone bone2, Bone bone3, Bone bone4, string chainName)
        {
            if (bone1 == null)
                throw new ArgumentException($"{chainName}의 첫 번째 본을 찾을 수 없다.");
            if (bone2 == null)
                throw new ArgumentException($"{chainName}의 두 번째 본을 찾을 수 없다.");
            if (bone3 == null)
                throw new ArgumentException($"{chainName}의 세 번째 본을 찾을 수 없다.");
            if (bone4 == null)
                throw new ArgumentException($"{chainName}의 네 번째 본을 찾을 수 없다.");
        }
    }
}