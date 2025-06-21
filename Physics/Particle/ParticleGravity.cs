using OpenGL;

namespace Physics
{
    /// <summary>
    /// 중력힘 발생기
    /// </summary>
    public class ParticleGravity : IParticleForceGenerator
    {
        /// <summary>
        /// 중력은 기본 -Z방향
        /// </summary>
        Vertex3f _gravity = new Vertex3f(0, 0, -10.0f);

        public ParticleGravity()
        {

        }

        public ParticleGravity(Vertex3f gravity)
        {
            _gravity = gravity;
        }   

        public void UpdateForce(Particle particle, float duration)
        {
            if (!particle.HasFiniteMass) return;
            particle.AddForce(_gravity * particle.Mass);
        }
    }
}
