using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZetaEngine
{
    public class FontShader : ShaderProgram
    {        
        const string VERTEX_FILE = @"\Font\_vert.glsl";
        const string FRAGMENT_FILE = @"\Font\_frag.glsl";

        private int location_colour;
        private int location_translation;

        public FontShader() : base(EngineLoop.PROJECT_PATH + VERTEX_FILE,
            EngineLoop.PROJECT_PATH + FRAGMENT_FILE, "")
        { }
               
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
