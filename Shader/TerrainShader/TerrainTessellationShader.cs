using OpenGL;
using Common;
using System.Drawing.Drawing2D;

namespace Shader
{
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 높이맵 기반의 지형을 동적 LOD로 처리하며, 단층(Fault) 시뮬레이션을 지원합니다.
    /// </summary>
    public class TerrainTessellationShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\TerrainShader\common\terrain.vert";
        const string TCS_FILE = @"\Shader\TerrainShader\common\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\common\terrain.tes.glsl";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\new_terrain.frag";

        private int loc_heightScale;
        private int loc_normalMatrix;
        private int loc_model;
        private int loc_heightMap;
        private int loc_normalMap;
        private int loc_gIsDetailMap;

        private int loc_textureHeight0;
        private int loc_textureHeight1;
        private int loc_textureHeight2;
        private int loc_textureHeight3;
        private int loc_textureHeight4;
        private int loc_detailMap;
        private int loc_isDetailMap;
        private int loc_rockTexture;

        private int loc_height0;
        private int loc_height1;
        private int loc_height2;
        private int loc_height3;
        private int loc_height4;
        private int loc_colorTexcoordScaling;

        // --- 단층(Fault) 관련 유니폼 로케이션 ---
        private int loc_faultMap;
        private int loc_faultMapScale;
        private int loc_faultDisplacementScale;
        private int loc_faultZoneWidth;
        private int loc_faultZoneIntensity;

        private int loc_onFunc;

        public TerrainTessellationShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;

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
            loc_normalMatrix = GetUniformLocation("normalMatrix");
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
            loc_rockTexture = GetUniformLocation("gRockTexture");

            loc_height0 = GetUniformLocation("gHeight0");
            loc_height1 = GetUniformLocation("gHeight1");
            loc_height2 = GetUniformLocation("gHeight2");
            loc_height3 = GetUniformLocation("gHeight3");
            loc_height4 = GetUniformLocation("gHeight4");

            loc_gIsDetailMap = GetUniformLocation("gIsDetailMap");
            loc_colorTexcoordScaling = GetUniformLocation("gColorTexcoordScaling");

            // --- 단층(Fault) 유니폼 로케이션 초기화 ---
            loc_faultMap = GetUniformLocation("gFaultMap");
            loc_faultMapScale = GetUniformLocation("gFaultMapScale");
            loc_faultDisplacementScale = GetUniformLocation("gFaultDisplacementScale");
            loc_faultZoneWidth = GetUniformLocation("gFaultZoneWidth");
            loc_faultZoneIntensity = GetUniformLocation("gFaultZoneIntensity");

            loc_onFunc = GetUniformLocation("onFunc");
        }

        // --- 기존 Load 메서드 생략 (유지됨) ---

        public void LoadHeightScale(float value) { Gl.Uniform1(loc_heightScale, value); }
        public void LoadModelMatrix(Matrix4x4f model) { LoadUniformMatrix4(loc_model, model); }
        public void LoadNormalMatrix(in Matrix3x3f matrix) { LoadUniformMatrix3(loc_normalMatrix, matrix); }

        public void LoadHeightAndNormalMap(uint heightTexture, uint normalTexture)
        {
            Gl.Uniform1(loc_heightMap, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, heightTexture);

            Gl.Uniform1(loc_normalMap, 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, normalTexture);
        }

        public void LoadEnableFunc(bool enable)
        {
            Gl.Uniform1(loc_onFunc, enable ? 1 : 0);
        }

        // --- 단층(Fault) 관련 Load 메서드 ---

        /// <summary>
        /// 단층 맵을 바인딩합니다. (Texture Unit 9 사용)
        /// </summary>
        public void LoadFaultMap(uint texture)
        {
            Gl.Uniform1(loc_faultMap, 9);
            Gl.ActiveTexture(TextureUnit.Texture9);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>
        /// 단층 활성화 여부 및 세부 파라미터를 설정합니다.
        /// </summary>
        public void LoadFaultParameters(float scale, float displacement, float width, float intensity)
        {
            Gl.Uniform1(loc_faultMapScale, scale);
            Gl.Uniform1(loc_faultDisplacementScale, displacement);
            Gl.Uniform1(loc_faultZoneWidth, width);
            Gl.Uniform1(loc_faultZoneIntensity, intensity);
        }

        public void LoadRockTexture(uint texture)
        {
            // RockTexture는 기존 8번 유닛 유지
            Gl.Uniform1(loc_rockTexture, 8);
            Gl.ActiveTexture(TextureUnit.Texture8);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadTerrainTextures(uint tex0, uint tex1, uint tex2, uint tex3, uint tex4)
        {
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

        public void LoadIsDetailMap(bool value) { Gl.Uniform1(loc_isDetailMap, value ? 1 : 0); }

        public void LoadHeightThresholds(float h0, float h1, float h2, float h3, float h4)
        {
            Gl.Uniform1(loc_height0, h0);
            Gl.Uniform1(loc_height1, h1);
            Gl.Uniform1(loc_height2, h2);
            Gl.Uniform1(loc_height3, h3);
            Gl.Uniform1(loc_height4, h4);
        }

        public void LoadColorTexcoordScaling(float scaling) { Gl.Uniform1(loc_colorTexcoordScaling, scaling); }
    }
}