using Common;
using OpenGL;
using System.Xml.Linq;

namespace Shader
{
    public class BillboardCloudShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\BillboardCloudShader\billboardcloud.vert";
        const string GEOMETRY_FILE = @"\Shader\BillboardCloudShader\billboardcloud.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\BillboardCloudShader\billboardcloud.frag";

        private int loc_vp;
        private int loc_objectWidth;
        private int loc_objectHeight;
        private int loc_horizontalTopRatio;
        private int loc_horizontalBottomRatio;
        private int loc_atlasTexture;

        public BillboardCloudShader(string projectPath) : base()
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
            loc_horizontalTopRatio = GetUniformLocation("horizontalTopRatio");
            loc_horizontalBottomRatio = GetUniformLocation("horizontalBottomRatio");
            loc_atlasTexture = GetUniformLocation("atlasTexture");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "instancePosition");
            base.BindAttribute(1, "instanceScale");
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

        public void LoadHorizontalRatios(float topRatio, float bottomRatio)
        {
            Gl.Uniform1(loc_horizontalTopRatio, topRatio);
            Gl.Uniform1(loc_horizontalBottomRatio, bottomRatio);
        }

        public void LoadAtlasTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_atlasTexture, 0);
        }
    }
}