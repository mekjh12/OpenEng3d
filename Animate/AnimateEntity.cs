using Common.Abstractions;
using Model3d;

namespace Animate
{
    public class AnimateEntity : Entity
    {

        public AnimateEntity(string name, RawModel3d rawModel3D) : base(name, "animateEntity", rawModel3D)
        {
        }

        public AnimateEntity(string name, RawModel3d[] rawModel3D) : base(name, "animateEntity", rawModel3D)
        {
        }

    }
}
