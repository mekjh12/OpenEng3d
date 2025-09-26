using OpenGL;

namespace Animate
{
    public class DonkeyRig : AnimRig
    {

        public DonkeyRig(string filename,string hipBoneName, bool isLoadAnimation = true) : base(filename, hipBoneName, isLoadAnimation)
        {
            int index = 0;

            // 추가 본 부위 인덱스 딕셔너리 초기화
            index = Armature.AttachBone("Spine1", ATTACHMENT_SLOT.Back, Matrix4x4f.Translated(0, 20, 0));
            _dicIndices.Add(ATTACHMENT_SLOT.Back, index);
        }
    }
}
