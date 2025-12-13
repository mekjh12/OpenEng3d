using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Renderer
{
    public class UnifiedModelRenderer
    {
        private UnifiedTexturedModel _model;
        private const int MAX_TEXTURES = 32;

        public UnifiedModelRenderer(UnifiedTexturedModel model)
        {
            _model = model;
            CreateTextures();
        }

        /// <summary>
        /// 개별 텍스처 생성 (원본 크기 유지)
        /// </summary>
        private void CreateTextures()
        {
            if (_model.Textures.Count == 0)
            {
                Console.WriteLine("❌ 텍스처가 없습니다.");
                return;
            }

            if (_model.Textures.Count > MAX_TEXTURES)
            {
                Console.WriteLine($"⚠ 텍스처 개수 제한: {MAX_TEXTURES}개 (현재: {_model.Textures.Count})");
                Console.WriteLine($"처음 {MAX_TEXTURES}개만 사용됩니다.");
            }

            Console.WriteLine($"✅ 텍스처 생성 시작: {Math.Min(_model.Textures.Count, MAX_TEXTURES)}개");

            int loadCount = Math.Min(_model.Textures.Count, MAX_TEXTURES);

            for (int i = 0; i < loadCount; i++)
            {
                Texture tex = _model.Textures[i];

                Console.WriteLine($"  [{i}] 로딩: {tex.FileName}");
                uint textureID = LoadTexture(tex.FileName);

                _model.TextureIDs.Add(textureID);
                Console.WriteLine($"      TextureID: {textureID}");
            }

            Console.WriteLine($"✅ 텍스처 생성 완료!\n");
        }

        /// <summary>
        /// 개별 텍스처 로드 (원본 크기 유지)
        /// </summary>
        private uint LoadTexture(string filepath)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(filepath))
                {
                    BitmapData data = bmp.LockBits(
                        new Rectangle(0, 0, bmp.Width, bmp.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    byte[] pixels = new byte[bmp.Width * bmp.Height * 4];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                    bmp.UnlockBits(data);

                    // BGRA → RGBA
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        byte temp = pixels[i];
                        pixels[i] = pixels[i + 2];
                        pixels[i + 2] = temp;
                    }

                    uint texID = Gl.GenTexture();
                    Gl.BindTexture(TextureTarget.Texture2d, texID);

                    Gl.TexImage2D(
                        TextureTarget.Texture2d,
                        0,
                        InternalFormat.Rgba8,
                        bmp.Width,
                        bmp.Height,
                        0,
                        OpenGL.PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        pixels);

                    Gl.GenerateMipmap(TextureTarget.Texture2d);

                    Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    Gl.BindTexture(TextureTarget.Texture2d, 0);

                    Console.WriteLine($"      크기: {bmp.Width}x{bmp.Height}");
                    return texID;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 텍스처 로드 실패: {ex.Message}");
                throw;
            }
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