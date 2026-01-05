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
        const string VERTEX_FILE = @"\Shader\TerrainShader\common\terrain.vert";
        const string TCS_FILE = @"\Shader\TerrainShader\common\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\common\terrain.tes.glsl";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\new_terrain.frag";
        // ⭐ Geometry Shader 제거

        private int loc_heightScale;
        private int loc_model;
        private int loc_heightMap;
        private int loc_normalMap;  // ⭐ 추가

        private int loc_textureHeight0;
        private int loc_textureHeight1;
        private int loc_textureHeight2;
        private int loc_textureHeight3;
        private int loc_textureHeight4;
        private int loc_detailMap;
        private int loc_isDetailMap;

        private int loc_height0;
        private int loc_height1;
        private int loc_height2;
        private int loc_height3;
        private int loc_height4;
        private int loc_colorTexcoordScaling;

        public TerrainTessellationShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;
            // GeomFileName 제거

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

            loc_heightMap = GetUniformLocation("gHeightMap");
            loc_normalMap = GetUniformLocation("gNormalMap");

            loc_textureHeight0 = GetUniformLocation("gTextureHeight0");
            loc_textureHeight1 = GetUniformLocation("gTextureHeight1");
            loc_textureHeight2 = GetUniformLocation("gTextureHeight2");
            loc_textureHeight3 = GetUniformLocation("gTextureHeight3");
            loc_textureHeight4 = GetUniformLocation("gTextureHeight4");
            loc_detailMap = GetUniformLocation("gDetailMap");
            loc_isDetailMap = GetUniformLocation("gIsDetailMap");

            loc_height0 = GetUniformLocation("gHeight0");
            loc_height1 = GetUniformLocation("gHeight1");
            loc_height2 = GetUniformLocation("gHeight2");
            loc_height3 = GetUniformLocation("gHeight3");
            loc_height4 = GetUniformLocation("gHeight4");

            loc_colorTexcoordScaling = GetUniformLocation("gColorTexcoordScaling");
        }

        public void LoadHeightScale(float value)
        {
            Gl.Uniform1(loc_heightScale, value);
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        // ⭐ Normal Map 바인딩
        public void LoadHeightAndNormalMap(uint heightTexture, uint normalTexture)
        {
            // Height Map (Texture Unit 0)
            Gl.Uniform1(loc_heightMap, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, heightTexture);

            // Normal Map (Texture Unit 1)
            Gl.Uniform1(loc_normalMap, 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, normalTexture);
        }

        public void LoadTerrainTextures(uint tex0, uint tex1, uint tex2, uint tex3, uint tex4)
        {
            // Texture units 2-6에 바인딩
            Gl.Uniform1(loc_textureHeight0, 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, tex0);

            Gl.Uniform1(loc_textureHeight1, 3);
            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2d, tex1);

            Gl.Uniform1(loc_textureHeight2, 4);
            Gl.ActiveTexture(TextureUnit.Texture4);
            Gl.BindTexture(TextureTarget.Texture2d, tex2);

            Gl.Uniform1(loc_textureHeight3, 5);
            Gl.ActiveTexture(TextureUnit.Texture5);
            Gl.BindTexture(TextureTarget.Texture2d, tex3);

            Gl.Uniform1(loc_textureHeight4, 6);
            Gl.ActiveTexture(TextureUnit.Texture6);
            Gl.BindTexture(TextureTarget.Texture2d, tex4);
        }

        public void LoadDetailMap(uint texture)
        {
            Gl.Uniform1(loc_detailMap, 7);
            Gl.ActiveTexture(TextureUnit.Texture7);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadIsDetailMap(bool value)
        {
            Gl.Uniform1(loc_isDetailMap, value ? 1 : 0);
        }

        public void LoadHeightThresholds(float h0, float h1, float h2, float h3, float h4)
        {
            Gl.Uniform1(loc_height0, h0);
            Gl.Uniform1(loc_height1, h1);
            Gl.Uniform1(loc_height2, h2);
            Gl.Uniform1(loc_height3, h3);
            Gl.Uniform1(loc_height4, h4);
        }

        public void LoadColorTexcoordScaling(float scaling)
        {
            Gl.Uniform1(loc_colorTexcoordScaling, scaling);
        }
    }
}