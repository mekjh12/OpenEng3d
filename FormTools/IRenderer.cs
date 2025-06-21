using Common.Abstractions;
using OpenGL;

namespace FormTools
{
    public interface IRenderer
    {
        void Initialize(string projectPath);

        void UpdateFrame(float deltaTime, int width, int height, Camera camera);
        
        void RenderFrame(float deltaTime, int width, int height, Vertex4f backcolor, Camera camera);

        void Init2d(int w, int h);

        void Init3d(int w, int h);

        void InitializeGlControl();

        void Dispose();
    }
}
