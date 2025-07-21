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
            HipIndex = Armature["mixamorig_Hips"].Index;
            RightHandIndex = Armature["mixamorig_RightHand"].Index;
            LeftHandIndex = Armature["mixamorig_LeftHand"].Index;
            HeadIndex = Armature["mixamorig_Head"].Index;
            LeftFootIndex = Armature["mixamorig_LeftFoot"].Index;
            RightFootIndex = Armature["mixamorig_RightFoot"].Index;

            Armature.AttachBone("mixamorig_Head", "mixamorig_Head_top", Matrix4x4f.Translated(0, 20, 0));
        }
    }
}
