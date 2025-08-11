using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// HorseBone 확장 메서드
    /// </summary>
    public static class HorseBoneExtensions
    {
        private static readonly Dictionary<string, HorseBone> StringToBoneMap =
            new Dictionary<string, HorseBone>();

        private static readonly Dictionary<HorseBone, string> BoneToStringMap =
            new Dictionary<HorseBone, string>();

        static HorseBoneExtensions()
        {
            // 말 뼈대 매핑 초기화
            var mappings = new Dictionary<HorseBone, string>
            {
                // 몸통
                { HorseBone.Pelvis, "horse_Pelvis" },
                { HorseBone.Spine1, "horse_Spine1" },
                { HorseBone.Spine2, "horse_Spine2" },
                { HorseBone.Spine3, "horse_Spine3" },
                { HorseBone.Neck1, "horse_Neck1" },
                { HorseBone.Neck2, "horse_Neck2" },
                { HorseBone.Neck3, "horse_Neck3" },
                { HorseBone.Head, "horse_Head" },

                // 꼬리
                { HorseBone.Tail1, "horse_Tail1" },
                { HorseBone.Tail2, "horse_Tail2" },
                { HorseBone.Tail3, "horse_Tail3" },

                // 앞다리
                { HorseBone.LeftShoulder, "horse_LeftShoulder" },
                { HorseBone.LeftUpperArm, "horse_LeftUpperArm" },
                { HorseBone.LeftForearm, "horse_LeftForearm" },
                { HorseBone.LeftFrontPastern, "horse_LeftFrontPastern" },
                { HorseBone.LeftFrontHoof, "horse_LeftFrontHoof" },

                { HorseBone.RightShoulder, "horse_RightShoulder" },
                { HorseBone.RightUpperArm, "horse_RightUpperArm" },
                { HorseBone.RightForearm, "horse_RightForearm" },
                { HorseBone.RightFrontPastern, "horse_RightFrontPastern" },
                { HorseBone.RightFrontHoof, "horse_RightFrontHoof" },

                // 뒷다리
                { HorseBone.LeftThigh, "horse_LeftThigh" },
                { HorseBone.LeftShin, "horse_LeftShin" },
                { HorseBone.LeftBackPastern, "horse_LeftBackPastern" },
                { HorseBone.LeftBackHoof, "horse_LeftBackHoof" },

                { HorseBone.RightThigh, "horse_RightThigh" },
                { HorseBone.RightShin, "horse_RightShin" },
                { HorseBone.RightBackPastern, "horse_RightBackPastern" },
                { HorseBone.RightBackHoof, "horse_RightBackHoof" }
            };

            foreach (var kvp in mappings)
            {
                BoneToStringMap[kvp.Key] = kvp.Value;
                StringToBoneMap[kvp.Value] = kvp.Key;
            }
        }

        public static string ToRigName(this HorseBone bone)
        {
            return BoneToStringMap.TryGetValue(bone, out string name) ? name : bone.ToString();
        }

        public static HorseBone? FromHorseName(this string boneName)
        {
            return StringToBoneMap.TryGetValue(boneName, out HorseBone bone) ? (HorseBone?)bone : null;
        }
    }
}
