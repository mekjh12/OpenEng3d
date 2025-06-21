namespace Ui2d
{
    interface IControlRenderable
    {
        void Update(int deltaTime);

        void Render(UIShader uiShader, FontRenderer fontRenderer);

        void Init();

        void Start();

        void Stop();

        void Resume();

    }
}
