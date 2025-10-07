using OpenGL;
using System;

namespace Animate
{
    /// <summary>
    /// TwoBoneIK 생성을 위한 정적 헬퍼 클래스
    /// </summary>
    public static class TwoBoneIKFactory
    {
        // -----------------------------------------------------------------------
        // 팔 IK 생성
        // -----------------------------------------------------------------------

        public static TwoBoneIK CreateHeadIK(Armature armature)
        {
            Bone neck = armature["mixamorig_Neck"];
            Bone head = armature["mixamorig_Head"];
            Bone headEnd = armature["mixamorig_HeadTop_End"];

            ValidateBones(neck, head, headEnd, "머리");
            return new TwoBoneIK(neck, head, headEnd);
        }

        /// <summary>
        /// 왼쪽 팔 IK 생성 (Mixamo 리그 기준)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 팔 IK</returns>
        public static TwoBoneIK CreateLeftArmIK(Armature armature)
        {
            Bone shoulder = armature["mixamorig_LeftArm"];
            Bone elbow = armature["mixamorig_LeftForeArm"];
            Bone hand = armature["mixamorig_LeftHand"];

            ValidateBones(shoulder, elbow, hand, "왼쪽 팔");
            return new TwoBoneIK(shoulder, elbow, hand);
        }

        /// <summary>
        /// 오른쪽 팔 IK 생성 (Mixamo 리그 기준)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 팔 IK</returns>
        public static TwoBoneIK CreateRightArmIK(Armature armature)
        {
            Bone shoulder = armature["mixamorig_RightArm"];
            Bone elbow = armature["mixamorig_RightForeArm"];
            Bone hand = armature["mixamorig_RightHand"];

            ValidateBones(shoulder, elbow, hand, "오른쪽 팔");
            return new TwoBoneIK(shoulder, elbow, hand);
        }

        // -----------------------------------------------------------------------
        // 다리 IK 생성
        // -----------------------------------------------------------------------

        /// <summary>
        /// 왼쪽 다리 IK 생성 (Mixamo 리그 기준)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>왼쪽 다리 IK</returns>
        public static TwoBoneIK CreateLeftLegIK(Armature armature)
        {
            Bone hip = armature["mixamorig_LeftUpLeg"];
            Bone knee = armature["mixamorig_LeftLeg"];
            Bone foot = armature["mixamorig_LeftFoot"];

            ValidateBones(hip, knee, foot, "왼쪽 다리");
            return new TwoBoneIK(hip, knee, foot);
        }

        /// <summary>
        /// 오른쪽 다리 IK 생성 (Mixamo 리그 기준)
        /// </summary>
        /// <param name="armature">골격 구조</param>
        /// <returns>오른쪽 다리 IK</returns>
        public static TwoBoneIK CreateRightLegIK(Armature armature)
        {
            Bone hip = armature["mixamorig_RightUpLeg"];
            Bone knee = armature["mixamorig_RightLeg"];
            Bone foot = armature["mixamorig_RightFoot"];

            ValidateBones(hip, knee, foot, "오른쪽 다리");
            return new TwoBoneIK(hip, knee, foot);
        }

        // -----------------------------------------------------------------------
        // 유효성 검증
        // -----------------------------------------------------------------------

        /// <summary>
        /// 본들의 유효성을 검증한다
        /// </summary>
        private static void ValidateBones(Bone root, Bone end, Bone tip, string chainName)
        {
            if (root == null)
                throw new System.ArgumentException($"{chainName}의 루트 본을 찾을 수 없다.");
            if (end == null)
                throw new System.ArgumentException($"{chainName}의 끝 본을 찾을 수 없다.");
            if (tip == null)
                throw new System.ArgumentException($"{chainName}의 끝점 본을 찾을 수 없다.");
        }
    }
}