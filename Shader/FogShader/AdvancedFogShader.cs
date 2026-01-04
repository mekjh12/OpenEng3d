using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// Advanced Fog Shader (Post-Processing, G-Buffer 기반)
    /// Zero-allocation 설계
    /// - Distance Fog (거리 기반)
    /// - Height Fog (고도 기반) 
    /// - Layered Fog (층별 안개)
    /// </summary>
    public class AdvancedFogShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\FogShader\fog.vert";
        const string FRAGMENT_FILE = @"\Shader\FogShader\advanced_fog.frag";

        // ✅ 유니폼 위치 캐싱
        private int loc_colorTexture;
        private int loc_positionTexture;    // ✅ G-Buffer의 위치
        private int loc_depthTexture;       // ✅ G-Buffer의 선형 깊이

        // 카메라 정보
        private int loc_camPos;

        // Distance Fog
        private int loc_distanceFogDensity;
        private int loc_distanceFogStart;

        // Height Fog
        private int loc_heightFogColor;
        private int loc_heightFogDensity;
        private int loc_heightFogFalloff;
        private int loc_heightFogMin;
        private int loc_heightFogMax;

        // Layered Fog
        private int loc_enableLayeredFog;
        private int loc_layerHeight;
        private int loc_layerThickness;
        private int loc_layerDensity;

        // 안개 모드
        private int loc_fogMode;

        public AdvancedFogShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // G-Buffer 텍스처
            loc_colorTexture = GetUniformLocation("colorTexture");
            loc_positionTexture = GetUniformLocation("positionTexture");
            loc_depthTexture = GetUniformLocation("depthTexture");

            // 카메라
            loc_camPos = GetUniformLocation("camPos");

            // Distance Fog
            loc_distanceFogDensity = GetUniformLocation("distanceFogDensity");
            loc_distanceFogStart = GetUniformLocation("distanceFogStart");

            // Height Fog
            loc_heightFogColor = GetUniformLocation("heightFogColor");
            loc_heightFogDensity = GetUniformLocation("heightFogDensity");
            loc_heightFogFalloff = GetUniformLocation("heightFogFalloff");
            loc_heightFogMin = GetUniformLocation("heightFogMin");
            loc_heightFogMax = GetUniformLocation("heightFogMax");

            // Layered Fog
            loc_enableLayeredFog = GetUniformLocation("enableLayeredFog");
            loc_layerHeight = GetUniformLocation("layerHeight");
            loc_layerThickness = GetUniformLocation("layerThickness");
            loc_layerDensity = GetUniformLocation("layerDensity");

            // Mode
            loc_fogMode = GetUniformLocation("fogMode");
        }

        protected override void BindAttributes()
        {
            // 풀스크린 삼각형 - 속성 바인딩 없음
        }

        #region Texture Loading

        public void LoadColorTexture(TextureUnit textureUnit, uint textureId)
        {
            int textureIndex = (int)textureUnit - (int)TextureUnit.Texture0;
            LoadUniform1i(loc_colorTexture, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        public void LoadPositionTexture(TextureUnit textureUnit, uint textureId)
        {
            int textureIndex = (int)textureUnit - (int)TextureUnit.Texture0;
            LoadUniform1i(loc_positionTexture, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        public void LoadDepthTexture(TextureUnit textureUnit, uint textureId)
        {
            int textureIndex = (int)textureUnit - (int)TextureUnit.Texture0;
            LoadUniform1i(loc_depthTexture, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        #endregion

        #region Camera Parameters

        public void LoadCameraPosition(in Vertex3f position)
        {
            LoadUniform3f(loc_camPos, position);
        }

        #endregion

        #region Distance Fog

        public void LoadDistanceFogDensity(float density)
        {
            LoadUniform1f(loc_distanceFogDensity, density);
        }

        public void LoadDistanceFogStart(float start)
        {
            LoadUniform1f(loc_distanceFogStart, start);
        }

        #endregion

        #region Height Fog

        public void LoadHeightFogColor(in Vertex3f color)
        {
            LoadUniform3f(loc_heightFogColor, color);
        }

        public void LoadHeightFogDensity(float density)
        {
            LoadUniform1f(loc_heightFogDensity, density);
        }

        public void LoadHeightFogFalloff(float falloff)
        {
            LoadUniform1f(loc_heightFogFalloff, falloff);
        }

        public void LoadHeightFogRange(float min, float max)
        {
            LoadUniform1f(loc_heightFogMin, min);
            LoadUniform1f(loc_heightFogMax, max);
        }

        #endregion

        #region Layered Fog

        public void LoadEnableLayeredFog(bool enable)
        {
            LoadUniformBool(loc_enableLayeredFog, enable);
        }

        public void LoadLayerHeight(float height)
        {
            LoadUniform1f(loc_layerHeight, height);
        }

        public void LoadLayerThickness(float thickness)
        {
            LoadUniform1f(loc_layerThickness, thickness);
        }

        public void LoadLayerDensity(float density)
        {
            LoadUniform1f(loc_layerDensity, density);
        }

        #endregion

        #region Fog Mode

        /// <summary>
        /// 안개 모드 설정
        /// </summary>
        /// <param name="mode">0: Distance only, 1: Height only, 2: Combined</param>
        public void LoadFogMode(int mode)
        {
            LoadUniform1i(loc_fogMode, mode);
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// AdvancedFogParams 구조체를 한번에 로드
        /// </summary>
        public void LoadFogParams(in AdvancedFogParams fogParams)
        {
            // Distance Fog
            LoadUniform1f(loc_distanceFogDensity, fogParams.DistanceDensity);
            LoadUniform1f(loc_distanceFogStart, fogParams.DistanceStart);

            // Height Fog
            LoadUniform3f(loc_heightFogColor, fogParams.Color);
            LoadUniform1f(loc_heightFogDensity, fogParams.HeightDensity);
            LoadUniform1f(loc_heightFogFalloff, fogParams.HeightFalloff);
            LoadUniform1f(loc_heightFogMin, fogParams.HeightMin);
            LoadUniform1f(loc_heightFogMax, fogParams.HeightMax);

            // Layered Fog
            LoadUniformBool(loc_enableLayeredFog, fogParams.EnableLayers);
            LoadUniform1f(loc_layerHeight, fogParams.LayerHeight);
            LoadUniform1f(loc_layerThickness, fogParams.LayerThickness);
            LoadUniform1f(loc_layerDensity, fogParams.LayerDensity);
        }

        /// <summary>
        /// 모든 파라미터를 한번에 설정 (텍스처 제외)
        /// </summary>
        public void LoadAllFogParameters(
            in AdvancedFogParams fogParams,
            in Vertex3f camPos,
            int fogMode = 2)
        {
            // 카메라
            LoadUniform3f(loc_camPos, camPos);

            // Fog 파라미터
            LoadFogParams(fogParams);

            // Mode
            LoadUniform1i(loc_fogMode, fogMode);
        }

        #endregion
    }
}