using OpenGL;

namespace Animate
{
    public class PrimateRig : AniRig
    {
        public int HipIndex { get; private set; }
        public int RightHandIndex { get; private set; }
        public int LeftHandIndex { get; private set; }
        public int HeadIndex { get; private set; }
        public int LeftFootIndex { get; private set; }
        public int RightFootIndex { get; private set; }        

        public PrimateRig(string filename, bool isLoadAnimation = true) : base(filename, isLoadAnimation)
        {
            // 애니메이션 리그 생성자
            HipIndex = Armature["mixamorig_Hips"].Index;
            RightHandIndex = Armature["mixamorig_RightHand"].Index;
            LeftHandIndex = Armature["mixamorig_LeftHand"].Index;
            HeadIndex = Armature["mixamorig_Head"].Index;
            LeftFootIndex = Armature["mixamorig_LeftFoot"].Index;
            RightFootIndex = Armature["mixamorig_RightFoot"].Index;

            int index = 0;
            
            // 추가 본 부위 인덱스 딕셔너리 초기화
            index = Armature.AttachBone("mixamorig_Head", ATTACHMENT_SLOT.Head, Matrix4x4f.Translated(0, 20, 0));
            _dicIndices.Add(ATTACHMENT_SLOT.Head, index);
            index = Armature.AttachBone("mixamorig_LeftHand", ATTACHMENT_SLOT.LeftHand, Matrix4x4f.Translated(0, 8, 5));
            _dicIndices.Add(ATTACHMENT_SLOT.LeftHand, index);
            index = Armature.AttachBone("mixamorig_RightHand", ATTACHMENT_SLOT.RightHand, Matrix4x4f.Translated(0, 8, 5));
            _dicIndices.Add(ATTACHMENT_SLOT.RightHand, index);
        }
    }
}
