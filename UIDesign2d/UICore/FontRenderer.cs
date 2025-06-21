using OpenGL;
using System;
using System.Collections.Generic;

namespace Ui2d
{
    /// <summary>
    /// 폰트 렌더러
    /// </summary>
    public class FontRenderer
    {
        FontShader shader;

        public FontRenderer(string path)
        {
            this.shader = new FontShader(path);
        }

        public void CleanUp()
        {
            shader.CleanUp();
        }

        public void Render(FontFamily fontFamily, Text text, Vertex2f position)
        {
            if (text == null) return;
            if (text.Mesh == null) return;

            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            shader.Bind();

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, (uint)fontFamily.TextureAtlas);

            Gl.BindVertexArray((uint)text.Mesh.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            shader.LoadColour(text.Color);
            shader.LoadTranslation(position);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, text.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.DisableVertexAttribArray(1);
            Gl.BindVertexArray(0);

            shader.Unbind();
            Gl.Disable(EnableCap.Blend);
        }

        public void Render(Text text, Vertex2f position)
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            shader.Bind();

            foreach (KeyValuePair<string, FontFamily> item in FontFamilySet.Fonts)
            {
                FontFamily font = item.Value;
                //if (font == text.FontFamily)
                {
                    Gl.ActiveTexture(TextureUnit.Texture0);
                    Gl.BindTexture(TextureTarget.Texture2d, (uint)font.TextureAtlas);

                    Gl.BindVertexArray((uint)text.Mesh.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.EnableVertexAttribArray(1);
                    shader.LoadColour(text.Color);
                    shader.LoadTranslation(position);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, text.VertexCount);
                    Gl.DisableVertexAttribArray(0);
                    Gl.DisableVertexAttribArray(1);
                    Gl.BindVertexArray(0);
                }
            }

            shader.Unbind();
            Gl.Disable(EnableCap.Blend);
        }
    }
}
