namespace Physics
{
    public interface IForceGenerator
    {
         void UpdateForce(RigidBody rigidBody, float duration);
    }
}
