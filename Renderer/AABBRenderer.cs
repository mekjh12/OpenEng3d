using Common;
using Common.Abstractions;
using OpenGL;
using Shader;

namespace Renderer
{
    public class AABBRenderer
    {
        ColorShader _shader;
        Camera _camera;

        public AABBRenderer(ColorShader shader, Camera camera)
        {
            _shader = shader;
            _camera = camera;
        }

        public void RenderAABB(AABB3f aabb, Vertex4f color = default)
        {
            if (color == default) color = new Vertex4f(0, 1, 0, 0.3f);

            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader.Bind();
            _shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            _shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, _camera.VPMatrix * aabb.ModelMatrix);
            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _shader.Unbind();

            Gl.Disable(EnableCap.Blend);
        }


    }
}
