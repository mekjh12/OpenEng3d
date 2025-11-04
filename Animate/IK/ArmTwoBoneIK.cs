using OpenGL;
using ZetaExt;

namespace Animate
{
    public class ArmTwoBoneIK : TwoBoneIK
    {
        Vertex3f _poleVector3 = default;
        bool _isLeft = false;

        public ArmTwoBoneIK(Bone upperBone, Bone lowerBone, Bone endBone, bool isLeft) :
            base(upperBone, lowerBone, endBone)
        {
            _isLeft = isLeft;
        }

        protected override Vertex3f SolveInternal(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            Vertex3f localTarget = modelMatrix.Inversed().Multiply(targetPositionWorld);

            if (_isLeft)
            {
                _poleVector3.x = -localTarget.y;
                _poleVector3.y = localTarget.x;
                _poleVector3.z = 0.0f;
            }
            else
            {
                _poleVector3.x = localTarget.y;
                _poleVector3.y = -localTarget.x;
                _poleVector3.z = 0.0f;
            }

            return _poleVector3;
        }
    }
}