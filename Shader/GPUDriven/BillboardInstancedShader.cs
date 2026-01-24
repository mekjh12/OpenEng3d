using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU-Driven 빌보드 인스턴스 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 카메라 방향 사각형으로 확장
    /// </summary>
    public class BillboardInstancedShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.geom";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.frag";

        private int loc_batchStartOffset;
        private int loc_fogTexture;
        private int loc_structureTexture;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_alphaThreshold;
        private int loc_width;
        private int loc_height;

        public BillboardInstancedShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");
            loc_fogTexture = GetUniformLocation("fogTexture");
            loc_structureTexture = GetUniformLocation("structureTexture");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_alphaThreshold = GetUniformLocation("alphaThreshold");
            loc_width = GetUniformLocation("width");
            loc_height = GetUniformLocation("height");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadScreenSize(int width, int height)
        {
            Gl.Uniform1(loc_width, width);
            Gl.Uniform1(loc_height, height);
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadFogTexture(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_fogTexture, (int)unit - (int)TextureUnit.Texture0);
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        public void LoadStructureTexture(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_structureTexture, (int)unit - (int)TextureUnit.Texture0);
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        public void LoadFogColor(float r, float g, float b)
        {
            Gl.Uniform3(loc_fogColor, r, g, b);
        }

        public void LoadFogDensity(float density)
        {
            Gl.Uniform1(loc_fogDensity, density);
        }

        public void LoadAlphaThreshold(float threshold)
        {
            Gl.Uniform1(loc_alphaThreshold, threshold);
        }
    }
}