using OpenGL;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Model3d
{
    public class Loader
    {
        public static List<uint> s_textures = new List<uint>();

        /// <summary>
        /// 텍스처 파일을 로드하여 GPU에 바로 올린다.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static uint LoadTexture2(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Debug.WriteLine($"can't find a file of {fileName}");
                return 0;
            }

            uint id = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, id);

            Bitmap bmp = new Bitmap(fileName);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height)
                                            , ImageLockMode.ReadOnly
                                            , System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.TexImage2D(TextureTarget.Texture2d
                            , 0
                            , InternalFormat.Rgba
                            , data.Width
                            , data.Height
                            , 0
                            , OpenGL.PixelFormat.Bgra
                            , PixelType.UnsignedByte
                            , data.Scan0);

            bmp.UnlockBits(data);

            Gl.TexParameter(TextureTarget.Texture2d
                            , TextureParameterName.TextureWrapS
                            , (int)TextureWrapMode.Clamp);
            Gl.TexParameter(TextureTarget.Texture2d
                            , TextureParameterName.TextureWrapT
                            , (int)TextureWrapMode.Clamp);

            Gl.TexParameter(TextureTarget.Texture2d
                            , TextureParameterName.TextureMinFilter
                            , (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d
                            , TextureParameterName.TextureMagFilter
                            , (int)TextureMagFilter.Linear);

            return id;
        }
    }
}
