namespace Physics
{
    public interface IParticleForceGenerator
    {
         void UpdateForce(Particle particle, float duration);
    }
}
