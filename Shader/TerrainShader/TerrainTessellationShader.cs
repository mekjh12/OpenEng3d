using OpenGL;
using Common;
using System.Drawing.Drawing2D;
using System;

namespace Shader
{
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 높이맵 기반의 지형을 동적 LOD로 처리하며, 단층(Fault) 시뮬레이션을 지원합니다.
    /// 
    /// 텍스처 유닛 할당:
    /// - Unit 0: 고해상도 높이맵 (heightMapHighRes)
    /// - Unit 1: 저해상도 높이맵 (heightMapLowRes)
    /// - Unit 2: 노말맵 (normalMap)
    /// - Unit 3~10: 인접 청크 높이맵 8개 (adjacentHeightMap0~7: R, RU, U, LU, L, LD, D, RD)
    /// - Unit 11: 지형 텍스처 0 (gTextureHeight0)
    /// - Unit 12: 지형 텍스처 1 (gTextureHeight1)
    /// - Unit 13: 지형 텍스처 2 (gTextureHeight2)
    /// - Unit 14: 지형 텍스처 3 (gTextureHeight3)
    /// - Unit 15: 지형 텍스처 4 (gTextureHeight4)
    /// - Unit 16: 디테일맵 (detailMap)
    /// - Unit 17: 암석 텍스처 (rockTexture)
    /// - Unit 18: 단층맵 (faultMap)
    /// - Unit 19: 강 맵 (riverMap)
    /// - Unit 20: 이끼 암석 텍스처 (mossRockTexture)
    /// </summary>
    public class TerrainTessellationShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\TerrainShader\common\terrain.vert";
        const string TCS_FILE = @"\Shader\TerrainShader\common\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\common\terrain.tes.glsl";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\new_terrain.frag";

        private int loc_time;

        private int loc_heightScale;
        private int loc_normalMatrix;
        private int loc_model;
        private int loc_normalMap;
        private int loc_gIsDetailMap;

        // 지형 높이맵 텍스처
        private int loc_heightMapLowRes;
        private int loc_heightMapHighRes;
        private int loc_blendFactor;
        private int[] loc_adjacentHeightMaps = new int[8];

        // --- 지형 텍스처 유니폼 로케이션 ---
        private int loc_textureHeight0;
        private int loc_textureHeight1;
        private int loc_textureHeight2;
        private int loc_textureHeight3;
        private int loc_textureHeight4;

        private int loc_detailMap;
        private int loc_isDetailMap;
        private int loc_rockTexture;
        private int loc_rockMap;
        private int loc_riverMap;
        private int loc_mossRockTexture;

        // --- 높이 임계값 유니폼 로케이션 ---
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
            loc_time = GetUniformLocation("gTime");

            loc_heightScale = GetUniformLocation("heightScale");
            loc_normalMatrix = GetUniformLocation("normalMatrix");
            loc_model = GetUniformLocation("model");

            loc_heightMapHighRes = GetUniformLocation("heightMapHighRes");
            loc_heightMapLowRes = GetUniformLocation("heightMapLowRes");
            loc_blendFactor = GetUniformLocation("blendFactor");

            for (int i = 0; i < 8; i++)
                loc_adjacentHeightMaps[i] = GetUniformLocation($"adjacentHeightMap{i}");

            loc_normalMap = GetUniformLocation("gNormalMap");

            loc_textureHeight0 = GetUniformLocation("gTextureHeight0");
            loc_textureHeight1 = GetUniformLocation("gTextureHeight1");
            loc_textureHeight2 = GetUniformLocation("gTextureHeight2");
            loc_textureHeight3 = GetUniformLocation("gTextureHeight3");
            loc_textureHeight4 = GetUniformLocation("gTextureHeight4");
            loc_detailMap = GetUniformLocation("gDetailMap");
            loc_isDetailMap = GetUniformLocation("gIsDetailMap");
            loc_rockTexture = GetUniformLocation("gRockTexture");
            loc_riverMap = GetUniformLocation("gRiverMap");

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

            loc_mossRockTexture = GetUniformLocation("gMossRockTexture");
        }

        public void LoadTime(float time)
        {
            Gl.Uniform1(loc_time, time);
        }

        public void LoadHeightScale(float value) { Gl.Uniform1(loc_heightScale, value); }
        public void LoadModelMatrix(Matrix4x4f model) { LoadUniformMatrix4(loc_model, model); }
        public void LoadNormalMatrix(in Matrix3x3f matrix) { LoadUniformMatrix3(loc_normalMatrix, matrix); }

        // Unit 0: 고해상도 높이맵
        public void LoadHeightHighResolutionMap(uint heightTexture)
        {
            Gl.Uniform1(loc_heightMapHighRes, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, heightTexture);
        }

        // Unit 1: 저해상도 높이맵
        public void LoadHeightLowResolutionMap(uint heightTexture)
        {
            Gl.Uniform1(loc_heightMapLowRes, 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, heightTexture);
        }

        public void LoadBlendFactor(float blendFactor)
        {
            Gl.Uniform1(loc_blendFactor, blendFactor);
        }

        // Unit 2: 노말맵
        public void LoadNormalMap(uint normalMapTexture)
        {
            Gl.Uniform1(loc_normalMap, 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, normalMapTexture);
        }

        /// <summary>
        /// Unit 3~10: 인접 청크의 높이맵 텍스처들을 로드합니다.
        /// 순서: R, RU, U, LU, L, LD, D, RD (시계방향)
        /// </summary>
        public void LoadAdjacentHeightMaps(uint[] adjacentHeightMaps)
        {
            if (adjacentHeightMaps == null) return;
            if (adjacentHeightMaps.Length != 8) return;
                //throw new ArgumentException("8개의 인접 높이맵이 필요합니다.");

            for (int i = 0; i < 8; i++)
            {
                int textureUnit = 3 + i;
                Gl.ActiveTexture((TextureUnit)(TextureUnit.Texture0 + textureUnit));
                Gl.BindTexture(TextureTarget.Texture2d, adjacentHeightMaps[i]);
                Gl.Uniform1(loc_adjacentHeightMaps[i], textureUnit);
            }
        }

        public void LoadEnableFunc(bool enable)
        {
            Gl.Uniform1(loc_onFunc, enable ? 1 : 0);
        }

        // Unit 11~15: 지형 텍스처
        public void LoadTerrainTextures(uint tex0, uint tex1, uint tex2, uint tex3, uint tex4)
        {
            Gl.Uniform1(loc_textureHeight0, 11);
            Gl.ActiveTexture(TextureUnit.Texture11);
            Gl.BindTexture(TextureTarget.Texture2d, tex0);

            Gl.Uniform1(loc_textureHeight1, 12);
            Gl.ActiveTexture(TextureUnit.Texture12);
            Gl.BindTexture(TextureTarget.Texture2d, tex1);

            Gl.Uniform1(loc_textureHeight2, 13);
            Gl.ActiveTexture(TextureUnit.Texture13);
            Gl.BindTexture(TextureTarget.Texture2d, tex2);

            Gl.Uniform1(loc_textureHeight3, 14);
            Gl.ActiveTexture(TextureUnit.Texture14);
            Gl.BindTexture(TextureTarget.Texture2d, tex3);

            Gl.Uniform1(loc_textureHeight4, 15);
            Gl.ActiveTexture(TextureUnit.Texture15);
            Gl.BindTexture(TextureTarget.Texture2d, tex4);
        }

        // Unit 16: 디테일맵
        public void LoadDetailMap(uint texture)
        {
            Gl.Uniform1(loc_detailMap, 16);
            Gl.ActiveTexture(TextureUnit.Texture16);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadIsDetailMap(bool value) { Gl.Uniform1(loc_isDetailMap, value ? 1 : 0); }

        // Unit 17: 암석 텍스처
        public void LoadRockTexture(uint texture)
        {
            Gl.Uniform1(loc_rockTexture, 17);
            Gl.ActiveTexture(TextureUnit.Texture17);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        // Unit 18: 단층맵
        public void LoadFaultMap(uint texture)
        {
            Gl.Uniform1(loc_faultMap, 18);
            Gl.ActiveTexture(TextureUnit.Texture18);
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

        // Unit 19: 강 맵
        public void LoadRiverMap(uint texture)
        {
            Gl.Uniform1(loc_riverMap, 19);
            Gl.ActiveTexture(TextureUnit.Texture19);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        // Unit 20: 이끼 암석 텍스처
        public void LoadMossRockTexture(uint texture)
        {
            Gl.Uniform1(loc_mossRockTexture, 20);
            Gl.ActiveTexture(TextureUnit.Texture20);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadHeightThresholds(float h0, float h1, float h2, float h3, float h4)
        {
            Gl.Uniform1(loc_height0, h0);
            Gl.Uniform1(loc_height1, h1);
            Gl.Uniform1(loc_height2, h2);
            Gl.Uniform1(loc_height3, h3);
            Gl.Uniform1(loc_height4, h4);
        }

        public void LoadColorTexCoordScaling(float scaling) { Gl.Uniform1(loc_colorTexcoordScaling, scaling); }
    }
}