using Common.Abstractions;
using OpenGL;
using Shader;

namespace Animate
{
    public static class Render3d
    {
        public static void RenderOBB(ColorShader shader, OBBMat obb, Camera camera)
        {
            if (obb == null) return;

            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, obb.Color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * obb.ModelMatrix);
            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }

        public static void RenderOBB(ColorShader shader, Matrix4x4f obb, Vertex4f color, Camera camera)
        {
            if (obb == null) return;

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.Bind();
            shader.LoadUniform(ColorShader.UNIFORM_NAME.color, color);
            shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * obb);
            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();

            Gl.Disable(EnableCap.Blend);
        }
    }
}
