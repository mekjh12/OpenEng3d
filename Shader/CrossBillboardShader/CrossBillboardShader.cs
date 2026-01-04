using Common;
using OpenGL;
using System.Drawing.Drawing2D;
using System.Xml.Linq;

namespace Shader
{
    public class CrossBillboardShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\CrossBillboardShader\crossBillboard.vert";
        const string GEOMETRY_FILE = @"\Shader\CrossBillboardShader\crossBillboard.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\CrossBillboardShader\crossBillboard.frag";

        private int loc_atlasTexture;
        private int loc_normalTexture;
        private int loc_enableEdgeLine;
        private int loc_aabbMin;
        private int loc_aabbMax;
        private int loc_model;

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
            loc_atlasTexture = GetUniformLocation("atlasTexture");
            loc_normalTexture = GetUniformLocation("normalTexture");
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");
            loc_aabbMin = GetUniformLocation("aabbMin");
            loc_aabbMax = GetUniformLocation("aabbMax");
            loc_model = GetUniformLocation("model");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "instancePosition");
            base.BindAttribute(1, "instanceScale");
        }

        public void LoadAABB(Vertex3f aabbMin, Vertex3f aabbMax)
        {
            Gl.Uniform3(loc_aabbMin, aabbMin);
            Gl.Uniform3(loc_aabbMax, aabbMax);
        }

        public void EnableEdgeLine(bool enable)
        {
            Gl.Uniform1(loc_enableEdgeLine, enable ? 1 : 0);
        }

        public void LoadAtlasTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_atlasTexture, 0);
        }

        public void LoadNormalTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_normalTexture, 1);
        }

        public void LoadModelMatrix(Matrix4x4f model)
        {
            LoadUniformMatrix4(loc_model, model);
        }
    }
}