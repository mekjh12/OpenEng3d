using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    public class ColorShader : ShaderProgram<ColorShader.UNIFORM_NAME>
    {
        const string VERTEx_FILE = @"\Shader\ColorShader\color.vert";
        const string FRAGMENT_FILE = @"\Shader\ColorShader\color.frag";

        public enum UNIFORM_NAME
        {
            mvp,
            color, 
            Count
        }

        public ColorShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }
    }
}
