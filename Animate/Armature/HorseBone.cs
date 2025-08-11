namespace Animate
{
    /// <summary>
    /// 말 뼈대 구조 (사족보행)
    /// </summary>
    public enum HorseBone
    {
        // === 몸통 ===
        Pelvis, Spine1, Spine2, Spine3, Neck1, Neck2, Neck3, Head,

        // === 꼬리 ===
        Tail1, Tail2, Tail3,

        // === 앞다리 ===
        LeftShoulder, LeftUpperArm, LeftForearm, LeftFrontPastern, LeftFrontHoof,
        RightShoulder, RightUpperArm, RightForearm, RightFrontPastern, RightFrontHoof,

        // === 뒷다리 ===
        LeftThigh, LeftShin, LeftBackPastern, LeftBackHoof,
        RightThigh, RightShin, RightBackPastern, RightBackHoof
    }

}
