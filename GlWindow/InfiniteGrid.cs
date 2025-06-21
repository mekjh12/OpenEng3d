using Common.Abstractions;
using OpenGL;
using Shader;

namespace GlWindow
{
    public class InfiniteGrid
    {
        InfiniteGridShader _shader;
        float _width;
        float _height;

        public InfiniteGrid() { }

        public void Init(string projectPath, float width, float height)
        {
            _shader = new InfiniteGridShader(projectPath);
            _width = width;
            _height = height;
        }

        public void Update(int deltaTime)
        {

        }

        public void Render(Camera camera)
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);

            // 쉐이더를 시작한다.
            _shader.Bind();

            _shader.LoadVPMatrix(camera.VPMatrix);
            _shader.LoadCameraPosition(Vertex3f.Zero);
            _shader.LoadOrbitCameraPosition(camera.Position);

            _shader.LoadFocalLength(camera.FocalLength);
            _shader.LoadViewportSize(new Vertex2f(_width, _height));
            _shader.LoadAspectRatio(camera.AspectRatio);
            _shader.LoadViewMatrix(camera.ViewMatrix);

            Gl.BindVertexArray(0);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            _shader.Unbind();

            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);
        }

    }
}
