namespace Physics
{
    public abstract class BaseForce : IForceGenerator
    {
        public abstract void UpdateForce(RigidBody rigidBody, float duration);

        public static BaseForce Gravity => new Gravity();
    }
}
