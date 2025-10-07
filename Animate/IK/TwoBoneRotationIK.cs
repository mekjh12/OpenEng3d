using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animate.IK
{
    public class TwoBoneRotationIK
    {
        Bone _boneRoot;
        Bone _boneEnd;
        SingleBoneRotationIK _boneRootIK;
        SingleBoneRotationIK _boneEndIK;
        float _rootWeight = 0.5f;

        public TwoBoneRotationIK(Bone boneRoot, Bone boneEnd, float rootWeight = 0.5f)
        {
            _boneRoot = boneRoot ?? throw new ArgumentNullException(nameof(boneRoot));
            _boneEnd = boneEnd ?? throw new ArgumentNullException(nameof(boneEnd));
            if (boneEnd.Parent != boneRoot)
                throw new ArgumentException("boneEnd must be a child of boneRoot");
            _boneRootIK = new SingleBoneRotationIK(boneRoot);
            _boneEndIK = new SingleBoneRotationIK(boneEnd);
            _rootWeight = rootWeight;
        }

        public static TwoBoneRotationIK Create(Bone boneRoot, Bone boneEnd, float rootWeight = 0.5f)
        {
            return new TwoBoneRotationIK(boneRoot, boneEnd, rootWeight);
        }

        public Vertex3f[] Solve(Vertex3f targetPositionWorld, Matrix4x4f modelMatrix, Animator animator)
        {
            float angle = _boneEndIK.Solve(targetPositionWorld, modelMatrix, animator);
            Matrix4x4f Rot = Matrix4x4f.RotatedY(angle * _rootWeight);

            _boneRoot.BoneMatrixSet.LocalTransform = _boneRoot.BoneMatrixSet.LocalTransform * Rot;
            _boneRoot.UpdateAnimatorTransforms(animator, isSelfIncluded: true);

            _boneEndIK.Solve(targetPositionWorld, modelMatrix, animator);

            return new Vertex3f[] {  };
        }
    }
}
