using Common;
using OpenGL;
using System.Xml.Linq;

namespace Shader
{
    public class CrossBillboardShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\CrossBillboardShader\crossBillboard.vert";
        const string GEOMETRY_FILE = @"\Shader\CrossBillboardShader\crossBillboard.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\CrossBillboardShader\crossBillboard.frag";

        private int loc_vp;
        private int loc_objectWidth;
        private int loc_objectHeight;
        private int loc_atlasTexture;
        private int loc_enableEdgeLine;

        public CrossBillboardShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("vp");
            loc_objectWidth = GetUniformLocation("objectWidth");
            loc_objectHeight = GetUniformLocation("objectHeight");
            loc_atlasTexture = GetUniformLocation("atlasTexture");
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "instancePosition");
            base.BindAttribute(1, "instanceScale");
        }

        public void EnableEdgeLine(bool enable)
        {
            Gl.Uniform1(loc_enableEdgeLine, enable ? 1 : 0);
        }

        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_vp, 1, false, matrix);
        }

        public void LoadObjectSize(float width, float height)
        {
            Gl.Uniform1(loc_objectWidth, width);
            Gl.Uniform1(loc_objectHeight, height);
        }

        public void LoadAtlasTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_atlasTexture, 0);
        }
    }
}