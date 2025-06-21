using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    public class ComputeShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            InputDepth,
            OutputDepth,
            CurrentLevel,
            Count
        }

        const string COMPUTE_FILE = @"\Shader\ComputeShader\hiz.comp";

        public ComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }
    }
}
