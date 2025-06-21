namespace GlWindow
{
    public interface IRenderable
    {
        void _update(int deltaTime);

        void _render(int deltaTime);

        void _init2d(int w, int h);

        void _init3d(int w, int h);
    }
}
