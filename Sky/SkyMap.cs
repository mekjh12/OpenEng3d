using OpenGL;
using Camera3d;
using Model3d;
using Shader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZetaExt;
using Common.Abstractions;

namespace Sky
{
    public class SkyMap
    {
        RawModel3d _model;
        uint _textureId;
        Camera _camera;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="faceTextures"></param>
        public SkyMap(string[] faceTextures)
        {
            // outer cube
            _model = Loader3d.LoadSkyBox();
            _textureId = LoadCubeMap(faceTextures);
        }

        /// <summary>
        /// 6개의 파일로부터 스카이맵 텍스처인 큐브맵을 만든다.<br/>
        /// Right,Left,Front,Back,Top,Bottom 순이다.<br/>
        /// </summary>
        /// <param name="faceTextures"></param>
        /// <returns></returns>
        public uint LoadCubeMap(string[] faceTextures)
        {
            uint textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.TextureCubeMap, textureId);
            for (int i = 0; i < faceTextures.Length; i++)
            {
                string filename = faceTextures[i];
                if (File.Exists(filename))
                {
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(filename);
                    BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
                    bitmap.UnlockBits(data);
                }
            }

            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            return textureId;
        }

        /// <summary>
        /// 초기화한다.
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// 업데이트한다.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="deltaTime"></param>
        public void Update(Camera camera, int deltaTime)
        {
            _camera = camera;
        }

        public void ShutDown()
        {

        }
    }
}
