using Common;
using OpenGL;
using Shader;
using System;
using System.IO;
using System.Windows.Forms;

namespace Ui2d
{
    public class FontShader : ShaderProgram<Enum>
    {        
        const string VERTEX_FILE = @"\font.vert";
        const string FRAGMENT_FILE = @"\font.frag";

        private int location_colour;
        private int location_translation;

        public FontShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        public void LoadColour(Vertex4f colour)
        {
            base.LoadVector(location_colour, colour);
        }

        public void LoadTranslation(Vertex2f translation)
        {
            base.LoadVector(location_translation, translation);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "in_position");
            base.BindAttribute(1, "textureCoords");
        }

        protected override void GetAllUniformLocations()
        {
            location_colour = base.GetUniformLocation("colour");
            location_translation = base.GetUniformLocation("translation");
        }
    }
}
