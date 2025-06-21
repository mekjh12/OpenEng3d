using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    public class ScreenShader : ShaderProgram<ScreenShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            viewport_size,
            isPositionInBox,
            depthTexture1,
            screenTexture1,
            depthTexture2,
            screenTexture2,
            backgroundColor,
            Count
        }

        const string VERTEX_FILE = @"\Shader\ScreenShader\screen.vert";
        const string FRAGMENT_FILE = @"\Shader\ScreenShader\screen.frag";

        public ScreenShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            // 부모클래스 호출 후 반드시 후순위로 호출하여 컴파일해야 함.
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "aPos");
            base.BindAttribute(1, "aTexCoords");
        }


        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformName = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformName);
            }
        }

        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
    }
}
