using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 높이맵 기반의 지형을 동적 LOD로 처리합니다.
    /// Geometry Shader에서 정확한 삼각형 법선을 계산합니다.
    /// </summary>
    public class TerrainTessellationShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\terrain.frag";
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\terrain.tes.glsl";
        const string GEOM_FILE = @"\Shader\TerrainShader\terrain.geom.glsl";

        private int loc_heightScale;
        private int loc_model;
        private int loc_color;
        private int loc_isTextured;
        private int loc_gIsDetailMap;

        public TerrainTessellationShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;
            GeomFileName = projectPath + GEOM_FILE;  // ⭐ 추가
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
            base.BindAttribute(2, "color");
        }

        protected override void GetAllUniformLocations()
        {
            loc_heightScale = GetUniformLocation("heightScale");
            loc_model = GetUniformLocation("model");
            loc_color = GetUniformLocation("color");
            loc_isTextured = GetUniformLocation("isTextured");
            loc_gIsDetailMap = GetUniformLocation("gIsDetailMap");
        }

        public void LoadHeightScale(float value)
        {
            Gl.Uniform1(loc_heightScale, value);
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        public void LoadColor(in Vertex4f color)
        {
            Gl.Uniform4(loc_color, color.x, color.y, color.z, color.w);
        }

        public void LoadIsTextured(bool value)
        {
            Gl.Uniform1(loc_isTextured, value ? 1 : 0);
        }

        public void LoadIsDetailMap(bool value)
        {
            Gl.Uniform1(loc_gIsDetailMap, value ? 1 : 0);
        }

        public void LoadTexture(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_isTextured, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}