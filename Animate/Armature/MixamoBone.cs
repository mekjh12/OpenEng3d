using System;
using System.Collections.Generic;

namespace Animate
{
    public static class MixamoBoneExtensions
    {
        // 역방향 매핑 (문자열 → Enum)
        private static readonly Dictionary<string, MixamoBone> StringToBoneMap =
            new Dictionary<string, MixamoBone>();

        static MixamoBoneExtensions()
        {
            // StringToBoneMap 딕셔너리 초기화

            // 몸통
            StringToBoneMap["mixamorig_Hips"] = MixamoBone.Hips;
            StringToBoneMap["mixamorig_Spine"] = MixamoBone.Spine;
            StringToBoneMap["mixamorig_Spine1"] = MixamoBone.Spine1;
            StringToBoneMap["mixamorig_Spine2"] = MixamoBone.Spine2;
            StringToBoneMap["mixamorig_Neck"] = MixamoBone.Neck;
            StringToBoneMap["mixamorig_Head"] = MixamoBone.Head;

            // 왼쪽 팔
            StringToBoneMap["mixamorig_LeftShoulder"] = MixamoBone.LeftShoulder;
            StringToBoneMap["mixamorig_LeftArm"] = MixamoBone.LeftArm;
            StringToBoneMap["mixamorig_LeftForeArm"] = MixamoBone.LeftForeArm;
            StringToBoneMap["mixamorig_LeftHand"] = MixamoBone.LeftHand;

            // 왼쪽 손가락 - 엄지
            StringToBoneMap["mixamorig_LeftHandThumb1"] = MixamoBone.LeftHandThumb1;
            StringToBoneMap["mixamorig_LeftHandThumb2"] = MixamoBone.LeftHandThumb2;
            StringToBoneMap["mixamorig_LeftHandThumb3"] = MixamoBone.LeftHandThumb3;

            // 왼쪽 손가락 - 검지
            StringToBoneMap["mixamorig_LeftHandIndex1"] = MixamoBone.LeftHandIndex1;
            StringToBoneMap["mixamorig_LeftHandIndex2"] = MixamoBone.LeftHandIndex2;
            StringToBoneMap["mixamorig_LeftHandIndex3"] = MixamoBone.LeftHandIndex3;

            // 왼쪽 손가락 - 중지
            StringToBoneMap["mixamorig_LeftHandMiddle1"] = MixamoBone.LeftHandMiddle1;
            StringToBoneMap["mixamorig_LeftHandMiddle2"] = MixamoBone.LeftHandMiddle2;
            StringToBoneMap["mixamorig_LeftHandMiddle3"] = MixamoBone.LeftHandMiddle3;

            // 왼쪽 손가락 - 약지
            StringToBoneMap["mixamorig_LeftHandRing1"] = MixamoBone.LeftHandRing1;
            StringToBoneMap["mixamorig_LeftHandRing2"] = MixamoBone.LeftHandRing2;
            StringToBoneMap["mixamorig_LeftHandRing3"] = MixamoBone.LeftHandRing3;

            // 왼쪽 손가락 - 새끼
            StringToBoneMap["mixamorig_LeftHandPinky1"] = MixamoBone.LeftHandPinky1;
            StringToBoneMap["mixamorig_LeftHandPinky2"] = MixamoBone.LeftHandPinky2;
            StringToBoneMap["mixamorig_LeftHandPinky3"] = MixamoBone.LeftHandPinky3;

            // 오른쪽 팔
            StringToBoneMap["mixamorig_RightShoulder"] = MixamoBone.RightShoulder;
            StringToBoneMap["mixamorig_RightArm"] = MixamoBone.RightArm;
            StringToBoneMap["mixamorig_RightForeArm"] = MixamoBone.RightForeArm;
            StringToBoneMap["mixamorig_RightHand"] = MixamoBone.RightHand;

            // 오른쪽 손가락 - 엄지
            StringToBoneMap["mixamorig_RightHandThumb1"] = MixamoBone.RightHandThumb1;
            StringToBoneMap["mixamorig_RightHandThumb2"] = MixamoBone.RightHandThumb2;
            StringToBoneMap["mixamorig_RightHandThumb3"] = MixamoBone.RightHandThumb3;

            // 오른쪽 손가락 - 검지
            StringToBoneMap["mixamorig_RightHandIndex1"] = MixamoBone.RightHandIndex1;
            StringToBoneMap["mixamorig_RightHandIndex2"] = MixamoBone.RightHandIndex2;
            StringToBoneMap["mixamorig_RightHandIndex3"] = MixamoBone.RightHandIndex3;

            // 오른쪽 손가락 - 중지
            StringToBoneMap["mixamorig_RightHandMiddle1"] = MixamoBone.RightHandMiddle1;
            StringToBoneMap["mixamorig_RightHandMiddle2"] = MixamoBone.RightHandMiddle2;
            StringToBoneMap["mixamorig_RightHandMiddle3"] = MixamoBone.RightHandMiddle3;

            // 오른쪽 손가락 - 약지
            StringToBoneMap["mixamorig_RightHandRing1"] = MixamoBone.RightHandRing1;
            StringToBoneMap["mixamorig_RightHandRing2"] = MixamoBone.RightHandRing2;
            StringToBoneMap["mixamorig_RightHandRing3"] = MixamoBone.RightHandRing3;

            // 오른쪽 손가락 - 새끼
            StringToBoneMap["mixamorig_RightHandPinky1"] = MixamoBone.RightHandPinky1;
            StringToBoneMap["mixamorig_RightHandPinky2"] = MixamoBone.RightHandPinky2;
            StringToBoneMap["mixamorig_RightHandPinky3"] = MixamoBone.RightHandPinky3;

            // 다리
            StringToBoneMap["mixamorig_LeftUpLeg"] = MixamoBone.LeftUpLeg;
            StringToBoneMap["mixamorig_LeftLeg"] = MixamoBone.LeftLeg;
            StringToBoneMap["mixamorig_LeftFoot"] = MixamoBone.LeftFoot;
            StringToBoneMap["mixamorig_LeftToeBase"] = MixamoBone.LeftToeBase;
            StringToBoneMap["mixamorig_RightUpLeg"] = MixamoBone.RightUpLeg;
            StringToBoneMap["mixamorig_RightLeg"] = MixamoBone.RightLeg;
            StringToBoneMap["mixamorig_RightFoot"] = MixamoBone.RightFoot;
            StringToBoneMap["mixamorig_RightToeBase"] = MixamoBone.RightToeBase;
        }

        /// <summary>
        /// 문자열을 Mixamo 뼈대 enum으로 변환
        /// </summary>
        public static MixamoBone? FromMixamoName(this string boneName)
        {
            return StringToBoneMap.TryGetValue(boneName, out MixamoBone bone) ? bone : (MixamoBone?)null;
        }

        /// <summary>
        /// 문자열을 MixamoBone으로 직접 변환 (실패시 예외 발생)
        /// </summary>
        public static MixamoBone? ToMixamoBone(this string boneName)
        {
            if (StringToBoneMap.TryGetValue(boneName, out MixamoBone bone))
            {
                return bone;
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Mixamo 리그의 표준 뼈대 구조를 나타내는 열거형
    /// </summary>
    public enum MixamoBone
    {
        // === 몸통 ===
        /// <summary>엉덩이 (루트 본)</summary>
        Hips,
        /// <summary>척추 하단</summary>
        Spine,
        /// <summary>척추 중단</summary>
        Spine1,
        /// <summary>척추 상단</summary>
        Spine2,
        /// <summary>목</summary>
        Neck,
        /// <summary>머리</summary>
        Head,

        // === 왼쪽 팔 ===
        /// <summary>왼쪽 어깨</summary>
        LeftShoulder,
        /// <summary>왼쪽 상완</summary>
        LeftArm,
        /// <summary>왼쪽 전완</summary>
        LeftForeArm,
        /// <summary>왼쪽 손</summary>
        LeftHand,

        // === 왼쪽 손가락 - 엄지 ===
        /// <summary>왼쪽 엄지 1번째 관절</summary>
        LeftHandThumb1,
        /// <summary>왼쪽 엄지 2번째 관절</summary>
        LeftHandThumb2,
        /// <summary>왼쪽 엄지 3번째 관절</summary>
        LeftHandThumb3,

        // === 왼쪽 손가락 - 검지 ===
        /// <summary>왼쪽 검지 1번째 관절</summary>
        LeftHandIndex1,
        /// <summary>왼쪽 검지 2번째 관절</summary>
        LeftHandIndex2,
        /// <summary>왼쪽 검지 3번째 관절</summary>
        LeftHandIndex3,

        // === 왼쪽 손가락 - 중지 ===
        /// <summary>왼쪽 중지 1번째 관절</summary>
        LeftHandMiddle1,
        /// <summary>왼쪽 중지 2번째 관절</summary>
        LeftHandMiddle2,
        /// <summary>왼쪽 중지 3번째 관절</summary>
        LeftHandMiddle3,

        // === 왼쪽 손가락 - 약지 ===
        /// <summary>왼쪽 약지 1번째 관절</summary>
        LeftHandRing1,
        /// <summary>왼쪽 약지 2번째 관절</summary>
        LeftHandRing2,
        /// <summary>왼쪽 약지 3번째 관절</summary>
        LeftHandRing3,

        // === 왼쪽 손가락 - 새끼 ===
        /// <summary>왼쪽 새끼 1번째 관절</summary>
        LeftHandPinky1,
        /// <summary>왼쪽 새끼 2번째 관절</summary>
        LeftHandPinky2,
        /// <summary>왼쪽 새끼 3번째 관절</summary>
        LeftHandPinky3,

        // === 오른쪽 팔 ===
        /// <summary>오른쪽 어깨</summary>
        RightShoulder,
        /// <summary>오른쪽 상완</summary>
        RightArm,
        /// <summary>오른쪽 전완</summary>
        RightForeArm,
        /// <summary>오른쪽 손</summary>
        RightHand,

        // === 오른쪽 손가락 - 엄지 ===
        /// <summary>오른쪽 엄지 1번째 관절</summary>
        RightHandThumb1,
        /// <summary>오른쪽 엄지 2번째 관절</summary>
        RightHandThumb2,
        /// <summary>오른쪽 엄지 3번째 관절</summary>
        RightHandThumb3,

        // === 오른쪽 손가락 - 검지 ===
        /// <summary>오른쪽 검지 1번째 관절</summary>
        RightHandIndex1,
        /// <summary>오른쪽 검지 2번째 관절</summary>
        RightHandIndex2,
        /// <summary>오른쪽 검지 3번째 관절</summary>
        RightHandIndex3,

        // === 오른쪽 손가락 - 중지 ===
        /// <summary>오른쪽 중지 1번째 관절</summary>
        RightHandMiddle1,
        /// <summary>오른쪽 중지 2번째 관절</summary>
        RightHandMiddle2,
        /// <summary>오른쪽 중지 3번째 관절</summary>
        RightHandMiddle3,

        // === 오른쪽 손가락 - 약지 ===
        /// <summary>오른쪽 약지 1번째 관절</summary>
        RightHandRing1,
        /// <summary>오른쪽 약지 2번째 관절</summary>
        RightHandRing2,
        /// <summary>오른쪽 약지 3번째 관절</summary>
        RightHandRing3,

        // === 오른쪽 손가락 - 새끼 ===
        /// <summary>오른쪽 새끼 1번째 관절</summary>
        RightHandPinky1,
        /// <summary>오른쪽 새끼 2번째 관절</summary>
        RightHandPinky2,
        /// <summary>오른쪽 새끼 3번째 관절</summary>
        RightHandPinky3,

        // === 다리 ===
        /// <summary>왼쪽 허벅지</summary>
        LeftUpLeg,
        /// <summary>왼쪽 정강이</summary>
        LeftLeg,
        /// <summary>왼쪽 발</summary>
        LeftFoot,
        /// <summary>왼쪽 발가락</summary>
        LeftToeBase,

        /// <summary>오른쪽 허벅지</summary>
        RightUpLeg,
        /// <summary>오른쪽 정강이</summary>
        RightLeg,
        /// <summary>오른쪽 발</summary>
        RightFoot,
        /// <summary>오른쪽 발가락</summary>
        RightToeBase
    }
}
