using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// MixamoBone 확장 메서드
    /// </summary>
    public static class MixamoBoneExtensions
    {
        private static readonly Dictionary<string, MixamoBone> StringToBoneMap =
            new Dictionary<string, MixamoBone>();

        private static readonly Dictionary<MixamoBone, string> BoneToStringMap =
            new Dictionary<MixamoBone, string>();

        static MixamoBoneExtensions()
        {
            // 인간 뼈대 매핑 초기화
            var mappings = new Dictionary<MixamoBone, string>
            {
                // 몸통
                { MixamoBone.Hips, "mixamorig_Hips" },
                { MixamoBone.Spine, "mixamorig_Spine" },
                { MixamoBone.Spine1, "mixamorig_Spine1" },
                { MixamoBone.Spine2, "mixamorig_Spine2" },
                { MixamoBone.Neck, "mixamorig_Neck" },
                { MixamoBone.Head, "mixamorig_Head" },

                // 왼쪽 팔
                { MixamoBone.LeftShoulder, "mixamorig_LeftShoulder" },
                { MixamoBone.LeftArm, "mixamorig_LeftArm" },
                { MixamoBone.LeftForeArm, "mixamorig_LeftForeArm" },
                { MixamoBone.LeftHand, "mixamorig_LeftHand" },

                // 왼쪽 손가락들
                { MixamoBone.LeftHandThumb1, "mixamorig_LeftHandThumb1" },
                { MixamoBone.LeftHandThumb2, "mixamorig_LeftHandThumb2" },
                { MixamoBone.LeftHandThumb3, "mixamorig_LeftHandThumb3" },
                { MixamoBone.LeftHandIndex1, "mixamorig_LeftHandIndex1" },
                { MixamoBone.LeftHandIndex2, "mixamorig_LeftHandIndex2" },
                { MixamoBone.LeftHandIndex3, "mixamorig_LeftHandIndex3" },
                { MixamoBone.LeftHandMiddle1, "mixamorig_LeftHandMiddle1" },
                { MixamoBone.LeftHandMiddle2, "mixamorig_LeftHandMiddle2" },
                { MixamoBone.LeftHandMiddle3, "mixamorig_LeftHandMiddle3" },
                { MixamoBone.LeftHandRing1, "mixamorig_LeftHandRing1" },
                { MixamoBone.LeftHandRing2, "mixamorig_LeftHandRing2" },
                { MixamoBone.LeftHandRing3, "mixamorig_LeftHandRing3" },
                { MixamoBone.LeftHandPinky1, "mixamorig_LeftHandPinky1" },
                { MixamoBone.LeftHandPinky2, "mixamorig_LeftHandPinky2" },
                { MixamoBone.LeftHandPinky3, "mixamorig_LeftHandPinky3" },

                // 오른쪽 팔
                { MixamoBone.RightShoulder, "mixamorig_RightShoulder" },
                { MixamoBone.RightArm, "mixamorig_RightArm" },
                { MixamoBone.RightForeArm, "mixamorig_RightForeArm" },
                { MixamoBone.RightHand, "mixamorig_RightHand" },

                // 오른쪽 손가락들
                { MixamoBone.RightHandThumb1, "mixamorig_RightHandThumb1" },
                { MixamoBone.RightHandThumb2, "mixamorig_RightHandThumb2" },
                { MixamoBone.RightHandThumb3, "mixamorig_RightHandThumb3" },
                { MixamoBone.RightHandIndex1, "mixamorig_RightHandIndex1" },
                { MixamoBone.RightHandIndex2, "mixamorig_RightHandIndex2" },
                { MixamoBone.RightHandIndex3, "mixamorig_RightHandIndex3" },
                { MixamoBone.RightHandMiddle1, "mixamorig_RightHandMiddle1" },
                { MixamoBone.RightHandMiddle2, "mixamorig_RightHandMiddle2" },
                { MixamoBone.RightHandMiddle3, "mixamorig_RightHandMiddle3" },
                { MixamoBone.RightHandRing1, "mixamorig_RightHandRing1" },
                { MixamoBone.RightHandRing2, "mixamorig_RightHandRing2" },
                { MixamoBone.RightHandRing3, "mixamorig_RightHandRing3" },
                { MixamoBone.RightHandPinky1, "mixamorig_RightHandPinky1" },
                { MixamoBone.RightHandPinky2, "mixamorig_RightHandPinky2" },
                { MixamoBone.RightHandPinky3, "mixamorig_RightHandPinky3" },

                // 다리
                { MixamoBone.LeftUpLeg, "mixamorig_LeftUpLeg" },
                { MixamoBone.LeftLeg, "mixamorig_LeftLeg" },
                { MixamoBone.LeftFoot, "mixamorig_LeftFoot" },
                { MixamoBone.LeftToeBase, "mixamorig_LeftToeBase" },
                { MixamoBone.RightUpLeg, "mixamorig_RightUpLeg" },
                { MixamoBone.RightLeg, "mixamorig_RightLeg" },
                { MixamoBone.RightFoot, "mixamorig_RightFoot" },
                { MixamoBone.RightToeBase, "mixamorig_RightToeBase" }
            };

            foreach (var kvp in mappings)
            {
                BoneToStringMap[kvp.Key] = kvp.Value;
                StringToBoneMap[kvp.Value] = kvp.Key;
            }
        }

        public static string ToRigName(this MixamoBone bone)
        {
            return BoneToStringMap.TryGetValue(bone, out string name) ? name : bone.ToString();
        }

        public static MixamoBone? FromMixamoName(this string boneName)
        {
            return StringToBoneMap.TryGetValue(boneName, out MixamoBone bone) ? (MixamoBone?)bone : null;
        }

        public static MixamoBone? ToMixamoBone(this string boneName)
        {
            return StringToBoneMap.TryGetValue(boneName, out MixamoBone bone) ? (MixamoBone?)bone : null;
        }
    }

}

