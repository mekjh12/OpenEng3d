using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    public class ZDepthShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            mvp,
            Count
        }

        #region Uniform 전달을 위한 기본 함수

        public void LoadTexture(UNIFORM_NAME textureUniformName, TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            base.LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        #endregion

        const string VERTEX_FILE = @"\Shader\ZDepthShader\zdepth.vert";
        const string FRAGMENT_FILE = @"\Shader\ZDepthShader\zdepth.frag";

        public ZDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }
    }
}
