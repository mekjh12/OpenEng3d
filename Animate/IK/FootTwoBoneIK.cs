using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    public class FootTwoBoneIK : TwoBoneIK
    {
        Vertex3f _poleVector3 = default;


        public FootTwoBoneIK(Bone upperBone, Bone lowerBone, Bone endBone) :
            base(upperBone, lowerBone, endBone)
        {

        }

        protected override Vertex3f SolveInternal(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            Vertex3f targetVector =  (targetPositionWorld - _rootWorld).Normalized;
            Vertex3f localTarget = modelMatrix.Inversed().Multiply(targetPositionWorld);

            return localTarget -  Vertex3f.UnitY * 10f;
            _poleVector3.y = localTarget.z;
            _poleVector3.z = -localTarget.y;
            _poleVector3.x = 0.0f;

            return _poleVector3;

            /*
            Vertex3f localTarget = modelMatrix.Inversed().Multiply(targetPositionWorld);

            _poleVector3.y = -localTarget.z;
            _poleVector3.z = localTarget.y;
            _poleVector3.x = 0.0f;

            return _poleVector3;
            */
        }
    }
}
