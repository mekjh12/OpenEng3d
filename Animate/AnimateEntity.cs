using Common.Abstractions;
using Model3d;

namespace Animate
{
    public class AnimateEntity : Entity, IAnimateComponent
    {
        private readonly AnimateComponent _animateComponent;

        public AnimateEntity(string name, RawModel3d rawModel3D) : base(name, "animateEntity", rawModel3D)
        {
            _animateComponent = new AnimateComponent();  // 생성자에서 초기화 필요
        }

        public AnimateEntity(string name, RawModel3d[] rawModel3D) : base(name, "animateEntity", rawModel3D)
        {
            _animateComponent = new AnimateComponent();  // 생성자에서 초기화 필요
        }

        // 인터페이스 속성 직접 위임
        public int BoneIndexOnlyOneJoint
        {
            get => ((IAnimateComponent)_animateComponent).BoneIndexOnlyOneJoint; 
            set => ((IAnimateComponent)_animateComponent).BoneIndexOnlyOneJoint = value;
        }

        public bool IsOnlyOneJointWeight
        {
            get => ((IAnimateComponent)_animateComponent).IsOnlyOneJointWeight; 
            set => ((IAnimateComponent)_animateComponent).IsOnlyOneJointWeight = value;
        }
    }
}
