using OpenGL;
using ZetaExt;

namespace Physics
{
    /// <summary>
    /// 댐핑힘
    /// </summary>
    public class ParticleDrag : IParticleForceGenerator
    {
        /// <summary>
        /// 속도에 곱해지는 마찰 비례상수
        /// </summary>
        float _k1;

        /// <summary>
        /// 속도의 제곱에 곱해지는 마찰 비례상수
        /// </summary>
        float _k2; 

        public ParticleDrag(float k1, float k2)
        {
            _k1 = k1;
            _k2 = k2;
        }   

        public void UpdateForce(Particle particle, float duration)
        {
            Vertex3f force = particle.Velocity;
            float dragCoeff = force.Magnitude();
            dragCoeff = _k1 * dragCoeff + _k2 * dragCoeff * dragCoeff;

            force.Normalize();
            force *= -dragCoeff;
            particle.AddForce(force);
        }
    }
}
