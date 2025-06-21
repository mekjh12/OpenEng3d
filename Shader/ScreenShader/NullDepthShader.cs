using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    public class NullDepthShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            Count
        }

        const string vertex_sources = @"
        #version 420 core
        layout (location = 0) in vec2 aPos;

        void main()
        {
            gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0); 
        }  
        ";

        const string fragment_sources = @"
        #version 420 core
        layout (location = 0) out float fragColor;
        layout (location = 1) out float depthColor;

        void main()
        {
            gl_FragDepth = 1.0f;
            fragColor = 0.5f;
            depthColor = 1.0f;
        }
        ";


        public NullDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            string vertFileName = projectPath + "\\vert.tmp";
            File.WriteAllText(vertFileName, vertex_sources);
            VertFileName = vertFileName;

            string fragFileName = projectPath + "\\frag.tmp";
            File.WriteAllText(fragFileName, fragment_sources);
            FragFilename = fragFileName;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "aPos");
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
