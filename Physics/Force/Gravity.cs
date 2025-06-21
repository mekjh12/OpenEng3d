using OpenGL;

namespace Physics
{
    /// <summary>
    /// 중력힘 발생기
    /// </summary>
    public class Gravity : BaseForce, IForceGenerator
    {
        /// <summary>
        /// 중력은 기본 -Z방향
        /// </summary>
        Vertex3f _gravity = new Vertex3f(0, 0, -10.0f);

        public Gravity()
        {

        }

        public Gravity(Vertex3f gravity)
        {
            _gravity = gravity;
        }   

        public override void UpdateForce(RigidBody rigidBody, float duration)
        {
            if (!rigidBody.HasFiniteMass) return;
            rigidBody.AddForce(_gravity * rigidBody.Mass);
        }
    }
}
