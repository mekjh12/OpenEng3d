using OpenGL;
using ZetaExt;

namespace Physics
{
    /// <summary>
    /// 고정 스프링 힘 발생기
    /// </summary>
    public class ParticleAnchoredSpring : IParticleForceGenerator
    {
        Vertex3f _anchor = Vertex3f.Zero;
        float _springConstant = 1.0f;
        float _restLength = 0.0f;

        public ParticleAnchoredSpring(Vertex3f anchor, float springConstant, float restLength)
        {
            _anchor = anchor;
            _springConstant = springConstant;
            _restLength = restLength;
        }   

        public void UpdateForce(Particle particle, float duration)
        {
            if (!particle.HasFiniteMass) return;

            // 힘의 크기 계산
            Vertex3f force = particle.Position - _anchor;
            float magitude = force.Magnitude();
            magitude = MathF.Abs(magitude - _restLength);
            magitude *= _springConstant;

            // 최종 힘을 계산하여 입자에 적용
            force.Normalize();
            force *= -magitude;
            particle.AddForce(force);
        }

        public void SetAnchor(Vertex3f anchor)
        {
            _anchor = anchor;
        }
    }
}
