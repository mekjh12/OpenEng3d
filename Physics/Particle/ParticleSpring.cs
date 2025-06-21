using OpenGL;
using ZetaExt;

namespace Physics
{
    /// <summary>
    /// 기본적 스프링 힘 발생기
    /// </summary>
    public class ParticleSpring : IParticleForceGenerator
    {
        Particle _other = null;
        float _springConstant = 1.0f;
        float _restLength = 0.0f;

        public ParticleSpring(Particle other, float springConstant, float restLength)
        {
            _other = other;
            _springConstant = springConstant;
            _restLength = restLength;
        }   

        public void UpdateForce(Particle particle, float duration)
        {
            if (!particle.HasFiniteMass) return;

            // 힘의 크기 계산
            Vertex3f force = particle.Position - _other.Position;
            float magitude = force.Magnitude();
            magitude = MathF.Abs(magitude - _restLength);
            magitude *= _springConstant;

            // 최종 힘을 계산하여 입자에 적용
            force.Normalize();
            force *= -magitude;
            particle.AddForce(force);
        }
    }
}
