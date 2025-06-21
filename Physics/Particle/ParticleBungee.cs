using OpenGL;
using ZetaExt;

namespace Physics
{
    /// <summary>
    /// 고무줄 힘 발생기
    /// </summary>
    public class ParticleBungee : IParticleForceGenerator
    {
        Particle _other = null;
        float _springConstant = 1.0f;
        float _restLength = 0.0f;

        public ParticleBungee(Particle other, float springConstant, float restLength)
        {
            _other = other;
            _springConstant = springConstant;
            _restLength = restLength;
        }   

        public void UpdateForce(Particle particle, float duration)
        {
            if (!particle.HasFiniteMass) return;

            // 스프링 벡터 계산
            Vertex3f force = particle.Position - _other.Position;
            float magitude = force.Magnitude();

            // 고무줄이 압축되었는지 검사
            if (magitude < _restLength) return;

            // 힘의 크기 계산
            magitude = MathF.Abs(magitude - _restLength);
            magitude *= _springConstant;

            // 최종 힘을 계산하여 입자에 적용
            force.Normalize();
            force *= -magitude;
            particle.AddForce(force);
        }
    }
}
