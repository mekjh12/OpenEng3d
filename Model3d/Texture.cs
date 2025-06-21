using OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Model3d
{
    public class Texture
    {
        [Flags]
        public enum TextureMapType
        {
            Diffuse = 1,
            Specular = 2,
            Normal = 4,
            Displace = 8,
        }

        private string _fileName; // 다른 텍스처와 비교하여 중복저장을 막기 위한 변수

        private uint _textureID;
        private uint _normalMapID;
        private uint _specularMapID;
        private uint _depthMapID;

        private int _width;
        private int _height;
        private TextureMapType _textureType;

        public TextureMapType TextureType
        {
            get => _textureType;
            set => _textureType = value;
        }

        public string FileName
        {
            get => _fileName;
            set => _fileName = value;
        }

        public uint SpecularMapID
        {
            get => _specularMapID;
            set => _specularMapID = value;
        }

        public uint DiffuseMapID
        {
            get => _textureID;
            set => _textureID = value;
        }

        public uint TextureID
        {
            get => _textureID;
            set => _textureID = value;
        }


        public uint NormalMapID
        {
            get => _normalMapID;
            set => _normalMapID = value;
        }

        public uint DepthMapID
        {
            get => _depthMapID;
            set => _depthMapID = value;
        }

        public Texture(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public Texture(Bitmap bitmap)
        {
            _textureType = TextureMapType.Diffuse;
            _width = bitmap.Width;
            _height = bitmap.Height;

            _textureID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _textureID);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                 OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }

        public void Bind(TextureUnit textureUnit)
        {
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, _textureID);
        }

        public void Clear()
        {
            List<uint> ids = new List<uint>();
            if (_textureID > 0) ids.Add(_textureID);
            if (_normalMapID > 0) ids.Add(_normalMapID);
            if (_specularMapID > 0) ids.Add(_specularMapID);
            if (_depthMapID > 0) ids.Add(_depthMapID);
            Gl.DeleteTextures(ids.ToArray());
        }

        /// <summary>
        /// 텍스처를 생성합니다. 타입에 대한 기본정보가 없으면 기본 디퓨즈맵으로 설정한다.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="typeName"></param>
        public Texture(string fileName, TextureMapType textureMapType = TextureMapType.Diffuse)
        {
            _fileName = fileName;

            //string filename = EngineLoop.PROJECT_PATH + "\\Res\\" + fileName;
            string fn = Path.GetFileNameWithoutExtension(fileName);
            _textureType = textureMapType;

            if (_textureType.HasFlag(TextureMapType.Diffuse))
            {
                Bitmap bitmap = (Bitmap)Bitmap.FromFile(fileName);
                _width = bitmap.Width;
                _height = bitmap.Height;

                _textureID = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _textureID);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                     OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.LinearMipmapLinear);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureLodBias, -0.4f);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                Gl.GenerateMipmap(TextureTarget.Texture2d);

                bitmap.UnlockBits(data);
            }

            if (_textureType.HasFlag(TextureMapType.Normal))
            {
                string normalMapFileName = fileName.Replace(fn, fn + "_normal");
                if (File.Exists(normalMapFileName))
                {
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(normalMapFileName);
                    _width = bitmap.Width;
                    _height = bitmap.Height;

                    _normalMapID = Gl.GenTexture();
                    Gl.BindTexture(TextureTarget.Texture2d, _normalMapID);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.LinearMipmapLinear);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                    Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureLodBias, -0.4f);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                    Gl.GenerateMipmap(TextureTarget.Texture2d);
                    bitmap.UnlockBits(data);
                    Debug.WriteLine($"normalmap created. {fileName}");
                }
                else
                {
                    _textureType &= ~TextureMapType.Normal; // 노멀 플래그를 제거한다.
                    Debug.WriteLine($"{fn} 노멀맵파일이 없어 노멀맵 플래그를 제거합니다.");
                }
            }

            if (_textureType.HasFlag(TextureMapType.Specular))
            {
                string specularMapFileName = fileName.Replace(fn, fn + "_specular");
                if (File.Exists(specularMapFileName))
                {
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(specularMapFileName);
                    _width = bitmap.Width;
                    _height = bitmap.Height;

                    _specularMapID = Gl.GenTexture();
                    Gl.BindTexture(TextureTarget.Texture2d, _specularMapID);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.LinearMipmapLinear);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                    //Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureLodBias, -0.4f);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                    Gl.GenerateMipmap(TextureTarget.Texture2d);
                    bitmap.UnlockBits(data);

                    Debug.WriteLine($"specularmap created. {fileName}");
                }
                else
                {
                    _textureType &= ~TextureMapType.Specular; // 스펙큘러 플래그를 제거한다.
                    Debug.WriteLine($"{fn} 스펙큘러맵파일이 없어 스펙큘러 플래그를 제거합니다.");
                }
            }

            if (_textureType.HasFlag(TextureMapType.Displace))
            {
                string depthMapFileName = fileName.Replace(fn, fn + "_disp");
                if (File.Exists(depthMapFileName))
                {
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(depthMapFileName);
                    _width = bitmap.Width;
                    _height = bitmap.Height;

                    _depthMapID = Gl.GenTexture();
                    Gl.BindTexture(TextureTarget.Texture2d, _depthMapID);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0,
                         OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.LinearMipmapLinear);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                    //Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureLodBias, -0.4f);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                    Gl.GenerateMipmap(TextureTarget.Texture2d);
                    bitmap.UnlockBits(data);

                    Debug.WriteLine($"depthmap created. {fileName}");
                }
                else
                {
                    _textureType &= ~TextureMapType.Displace; // 깊이맵 플래그를 제거한다.
                    Debug.WriteLine($"{fn} 깊이맵 파일이 없어 깊이맵 플래그를 제거합니다.");
                }
            }
        }
    }
}
