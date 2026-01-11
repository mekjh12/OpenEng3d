using OpenGL;
using Common;
using System;

namespace Shader
{
    /// <summary>
    /// 태양 관점의 Shadow Map 생성을 위한 경량 셰이더입니다.
    /// 고정된 낮은 테셀레이션 레벨을 사용하여 빠르게 깊이맵을 생성합니다.
    /// </summary>
    public class TerrainShadowMapShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\TerrainShader\shadowmap\shadowmap.vert";
        const string TCS_FILE = @"\Shader\TerrainShader\shadowmap\shadowmap.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\shadowmap\shadowmap.tes.glsl";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\shadowmap\shadowmap.frag";

        // 유니폼 위치 캐싱
        private int loc_model;
        private int loc_heightScale;
        private int loc_gHeightMap;
        private int loc_lightView;
        private int loc_lightProj;

        public TerrainShadowMapShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = GetUniformLocation("model");
            loc_heightScale = GetUniformLocation("heightScale");
            loc_gHeightMap = GetUniformLocation("gHeightMap");
            loc_lightProj = GetUniformLocation("lightProj");
            loc_lightView = GetUniformLocation("lightView");
        }

        protected override void BindAttributes()
        {
            // 테셀레이션 사용으로 비활성화
        }

        public void LoadLightViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_lightView, matrix);
        }

        public void LoadLightProjMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_lightProj, matrix);
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        public void LoadHeightScale(float scale)
        {
            LoadUniform1f(loc_heightScale, scale);
        }

        public void LoadHeightMap(uint texture)
        {
            LoadUniform1i(loc_gHeightMap, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}