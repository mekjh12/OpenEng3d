namespace Animate
{
    /// <summary>
    /// Mixamo 리그의 표준 뼈대 구조 (인간)
    /// </summary>
    public enum MixamoBone
    {
        // === 몸통 ===
        Hips, Spine, Spine1, Spine2, Neck, Head,

        // === 왼쪽 팔 ===
        LeftShoulder, LeftArm, LeftForeArm, LeftHand,

        // === 왼쪽 손가락 ===
        LeftHandThumb1, LeftHandThumb2, LeftHandThumb3,
        LeftHandIndex1, LeftHandIndex2, LeftHandIndex3,
        LeftHandMiddle1, LeftHandMiddle2, LeftHandMiddle3,
        LeftHandRing1, LeftHandRing2, LeftHandRing3,
        LeftHandPinky1, LeftHandPinky2, LeftHandPinky3,

        // === 오른쪽 팔 ===
        RightShoulder, RightArm, RightForeArm, RightHand,

        // === 오른쪽 손가락 ===
        RightHandThumb1, RightHandThumb2, RightHandThumb3,
        RightHandIndex1, RightHandIndex2, RightHandIndex3,
        RightHandMiddle1, RightHandMiddle2, RightHandMiddle3,
        RightHandRing1, RightHandRing2, RightHandRing3,
        RightHandPinky1, RightHandPinky2, RightHandPinky3,

        // === 다리 ===
        LeftUpLeg, LeftLeg, LeftFoot, LeftToeBase,
        RightUpLeg, RightLeg, RightFoot, RightToeBase
    }
}
