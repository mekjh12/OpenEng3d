using OpenGL;
using Shader;

namespace Renderer
{
    public class WorldAxisRenderer
    {
        WorldAxisShader _shader;
        float _length;
        float _thick;

        public WorldAxisRenderer(string projectPath)
        {
            _shader = new WorldAxisShader(projectPath);
            _length = 5000.0f;
            _thick = 5.0f;
        }

        public void SetLength(float length)
        {
            _length = length;
        }

        public void SetThick(float thick)
        {
            _thick = thick;
        }
        
        public void Render(Matrix4x4f vpMatrix)
        {
            Gl.Disable(EnableCap.DepthTest);
            Gl.LineWidth(_thick);
            _shader.Bind();
            {
                _shader.LoadAxisLength(_length);
                _shader.LoadVPMatrix(vpMatrix);
                Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            }
            _shader.Unbind();
            Gl.LineWidth(1.0f);
        }
    }
}
