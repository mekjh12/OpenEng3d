using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ZetaExt;
using Common;

namespace Shader
{
    /// <summary>
    /// 
    /// </summary>
    public class NoiseShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            mvp,
            model, view, proj,
            viewport_size,
            amplitude,
            frequency,
            octaves,
            persistence,
            u_time,
            focal_length,
            aspect_ratio,
            gamma,
            interpolationMode,
            ray_origin,
            isInBox,
            seed,
            boundSize,
            centerPosition,
            divisor,
            numCells,
            noiseType,
            stepLength,
            debug,
            cloudtype,
            highQuality,
            cloudSize,
            cloudPosition,
            Count
        }

        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);

        const string VERTEX_FILE = @"\Shader\NoiseShader\noise3.vert";
        const string FRAGMENT_FILE = @"\Shader\NoiseShader\noise3-3.frag";
        
        uint _ssboSize;
        uint _ssbo;
        uint _locSsbo;
        uint _ssboBindIndex = 0;

        uint _lowFrequency3DTexture;
        uint _highFrequency3DTexture;
        uint _weathermap;
        uint _curlNoise;

        public NoiseShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            // 부모클래스 호출 후 반드시 후순위로 호출하여 컴파일해야 함.
            InitCompileShader(); 
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        public void BindSSBO()
        {
            Gl.ShaderStorageBlockBinding(_programID, _ssbo, _locSsbo);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, _ssboBindIndex, _ssbo);
            Gl.BindBufferRange(BufferTarget.ShaderStorageBuffer, _ssboBindIndex, _ssbo, IntPtr.Zero, _ssboSize);
        }

        public void LoadSSBO(float[] points)
        {
            _ssboSize = (uint)(points.Length * sizeof(float));
            _ssbo = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _ssbo);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, _ssboSize, points, BufferUsage.DynamicCopy);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, _ssboSize, points);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, _ssboBindIndex, _ssbo);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            _locSsbo = Gl.GetProgramResourceIndex(_programID, ProgramInterface.ShaderStorageBlock, "shader_data");
        }

        public void LoadTextureCloud(string PROJECT_PATH)
        {
            _lowFrequency3DTexture = LoadTexture3d(TgaDecoder.FromFile(PROJECT_PATH + @"\FormTools\bin\Debug\textures\LowFrequency3DTexture.tga"), 128);
            _highFrequency3DTexture = LoadTexture3d(TgaDecoder.FromFile(PROJECT_PATH + @"\FormTools\bin\Debug\textures\HighFrequency3DTexture.tga"), 32);
            _weathermap = LoadTexture2d((Bitmap)Bitmap.FromFile(PROJECT_PATH + @"\FormTools\bin\Debug\textures\weathermap.png"));
            _curlNoise = LoadTexture2d((Bitmap)Bitmap.FromFile(PROJECT_PATH + @"\FormTools\bin\Debug\textures\curlNoise.png"));
        }

        private uint LoadTexture3d(Bitmap bmp, int resolution)
        {
            uint sampler3d = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, sampler3d);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, Gl.REPLACE);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, Gl.REPLACE);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, Gl.REPLACE);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // bgra 순으로 읽어야 정상적으로 읽힘
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.Rgba, resolution, resolution, resolution, 0, OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            Gl.BindTexture(TextureTarget.Texture3d, 0);

            return sampler3d;
        }

        private uint LoadTexture2d(Bitmap bmp)
        {
            uint sampler2d = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, sampler2d);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPLACE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPLACE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapR, Gl.REPLACE);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, bmp.Width, bmp.Height, 0, OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return sampler2d;
        }

        public void BindCloudTexture()
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture3d, _lowFrequency3DTexture);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture3d, _highFrequency3DTexture);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, _weathermap);
            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2d, _curlNoise);
        }

    }
}
