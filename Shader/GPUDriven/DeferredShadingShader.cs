using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// Deferred Rendering의 라이팅 패스 셰이더
    /// G-Buffer를 읽어서 Ambient + Diffuse 라이팅 + Shadow 계산
    /// </summary>
    public class DeferredShadingShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\post.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\deferred_shading.frag";

        private int loc_lightView;
        private int loc_lightProj;

        public DeferredShadingShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // G-Buffer 텍스처들은 셰이더 내부에서 고정 바인딩 사용
            loc_lightView = GetUniformLocation("lightView");
            loc_lightProj = GetUniformLocation("lightProj");
        }

        protected override void BindAttributes()
        {
            // 풀스크린 쿼드는 geometry shader에서 생성
        }

        /// <summary>
        /// G-Buffer 텍스처들을 바인딩
        /// </summary>
        public void LoadGBufferTextures(
            uint albedoTexture,
            uint positionTexture,
            uint normalTexture,
            uint depthTexture)
        {
            // Albedo
            SetInt("gAlbedo", 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, albedoTexture);

            // Position
            SetInt("gPosition", 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, positionTexture);

            // Normal
            SetInt("gNormal", 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, normalTexture);

            // Depth
            SetInt("gDepth", 3);
            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2d, depthTexture);
        }

        /// <summary>
        /// Shadow Map 텍스처 바인딩
        /// </summary>
        public void LoadShadowMap(uint shadowMapTexture)
        {
            SetInt("gShadowMap", 4);
            Gl.ActiveTexture(TextureUnit.Texture4);
            Gl.BindTexture(TextureTarget.Texture2d, shadowMapTexture);
        }

        /// <summary>
        /// Light Space 행렬 로드
        /// </summary>
        public void LoadLightMatrices(in Matrix4x4f lightView, in Matrix4x4f lightProj)
        {
            LoadUniformMatrix4(loc_lightView, lightView);
            LoadUniformMatrix4(loc_lightProj, lightProj);
        }
    }
}