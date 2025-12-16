using Common.Abstractions;
using Common;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model3d;

namespace Renderer
{
    public class AABBRenderer
    {
        BaseModel3d Cube = Loader3d.LoadCube();

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
            Gl.BindVertexArray(Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _shader.Unbind();

            Gl.Disable(EnableCap.Blend);
        }


    }
}
