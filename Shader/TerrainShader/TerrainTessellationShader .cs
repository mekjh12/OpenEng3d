using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 높이맵 기반의 지형을 동적 LOD로 처리합니다.
    /// </summary>
    public class TerrainTessellationShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\terrain.frag";
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\terrain.tes.glsl";

        // ✅ 유니폼 위치 캐싱
        private int loc_heightScale;
        private int loc_model;
        private int loc_proj;
        private int loc_view;
        private int loc_camPos;
        private int loc_color;
        private int loc_isTextured;
        private int loc_gIsDetailMap;
        private int loc_gVegetationMap;
        private int loc_isFogEnable;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_fogPlane;

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
            loc_model = GetUniformLocation("model");
            loc_proj = GetUniformLocation("proj");
            loc_view = GetUniformLocation("view");
            loc_camPos = GetUniformLocation("camPos");
            loc_color = GetUniformLocation("color");
            loc_isTextured = GetUniformLocation("isTextured");
            loc_gIsDetailMap = GetUniformLocation("gIsDetailMap");
            loc_gVegetationMap = GetUniformLocation("gVegetationMap");
            loc_isFogEnable = GetUniformLocation("isFogEnable");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_fogPlane = GetUniformLocation("fogPlane");
        }

        #region Uniform 로딩 함수

        public void LoadHeightScale(float value)
        {
            Gl.Uniform1(loc_heightScale, value);
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_proj, matrix);
        }

        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        public void LoadCameraPosition(in Vertex3f position)
        {
            Gl.Uniform3(loc_camPos, position.x, position.y, position.z);
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

        public void LoadIsFogEnable(bool value)
        {
            Gl.Uniform1(loc_isFogEnable, value ? 1 : 0);
        }

        public void LoadFogColor(in Vertex3f color)
        {
            Gl.Uniform3(loc_fogColor, color.x, color.y, color.z);
        }

        public void LoadFogDensity(float value)
        {
            Gl.Uniform1(loc_fogDensity, value);
        }

        public void LoadFogPlane(in Vertex2f plane)
        {
            Gl.Uniform2(loc_fogPlane, plane.x, plane.y);
        }

        public void LoadTexture(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_isTextured, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadVegetationMap(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_gVegetationMap, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        #endregion
    }
}