namespace Animate
{
    public interface IComponent
    {
        void Initialize();
        void Update(float deltaTime);
        void Dispose();
    }
}
