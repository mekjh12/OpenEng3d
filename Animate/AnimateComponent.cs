using Common.Abstractions;

namespace Animate
{
    public class AnimateComponent: IAnimateComponent
    {

        protected bool _isOnlyOneJointWeight = false;
        protected int _boneIndexOnlyOneJoint;

        public int BoneIndexOnlyOneJoint
        {
            get => _boneIndexOnlyOneJoint;
            set => _boneIndexOnlyOneJoint = value;
        }

        public bool IsOnlyOneJointWeight
        {
            get => _isOnlyOneJointWeight;
            set => _isOnlyOneJointWeight = value;
        }
    }
}
