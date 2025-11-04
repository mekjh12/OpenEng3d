using Animate;
using OpenGL;
using System;
using ZetaExt;

namespace Animates
{
    internal class TwoBoneRoll
    {
        Bone _firstBone;
        Bone _secondBone;
        LocalSpaceAxis _rollAxis;

        SingleBoneRoll _firstBoneRoll;
        SingleBoneRoll _secondBoneRoll;

        public TwoBoneRoll(Bone firstBone, Bone secondBone, LocalSpaceAxis rollAxis = LocalSpaceAxis.Y)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _rollAxis = rollAxis;

            _firstBoneRoll = new SingleBoneRoll(_firstBone, _rollAxis);
            _secondBoneRoll = new SingleBoneRoll(_secondBone, _rollAxis);
        }

        public void Solve(Vertex3f target, Matrix4x4f modelMatrix, Animator animator, float firstWeight = 0.5f, float secondWeight = 1.0f)
        {
            Matrix4x4f rotation = _firstBoneRoll.Solve(target, modelMatrix, animator, firstWeight);
            rotation = _secondBone.BoneMatrixSet.LocalTransform * rotation.Inversed();

            _secondBone.UpdateBone(ref rotation, animator, true);
            _secondBoneRoll.Solve(target, modelMatrix, animator, secondWeight);
        }

    }
}
