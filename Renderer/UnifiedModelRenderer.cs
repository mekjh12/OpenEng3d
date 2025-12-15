using Model3d;
using OpenGL;
using Shader;
using System;

namespace Renderer
{
    public class UnifiedModelRenderer
    {
        private UnifiedTexturedModel _model;
        private const int MAX_TEXTURES = 32;

        public UnifiedModelRenderer(UnifiedTexturedModel model)
        {
            _model = model;
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public void Render(UnlitShader shader, Matrix4x4f mvp)
        {
            if (_model.EnableCullFace)
            {
                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(_model.CullFaceMode);
            }
            else
            {
                Gl.Disable(EnableCap.CullFace);
            }

            shader.Bind();

            // ✅ 텍스처 배열 바인딩 (한 번만!)
            shader.LoadTextureArray(_model.TextureIDs.ToArray());

            shader.LoadMVPMatrix(mvp);

            Gl.BindVertexArray(_model.VaoID);

            Gl.DrawElements(
                PrimitiveType.Triangles,
                _model.IndexCount,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero);

            Gl.BindVertexArray(0);
            shader.Unbind();

            Gl.Disable(EnableCap.CullFace);
        }

        public void Dispose()
        {
            foreach (uint texID in _model.TextureIDs)
            {
                Gl.DeleteTextures(texID);
            }

            if (_model.VaoID != 0)
                Gl.DeleteVertexArrays(_model.VaoID);
        }
    }
}