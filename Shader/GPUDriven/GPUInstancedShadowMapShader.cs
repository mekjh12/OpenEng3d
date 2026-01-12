using Common;
using OpenGL;

namespace Shader
{
    public class GPUInstancedShadowMapShader: ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\instanced_shadow.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\instanced_shadow.frag";

        // SSBO 바인딩 포인트
        private const int TRANSFORM_BUFFER_BINDING = 0;
        private const int VISIBLE_INDICES_BINDING = 1;

        // 유니폼 위치
        private int loc_textureCount;
        private int[] loc_textures;
        private const int MAX_TEXTURES = 32;
        private int loc_batchStartOffset;
        private int loc_debugColor;
        private int loc_enableDebug;
        private int loc_gMaxDepthDistance;
        private int loc_lightView;
        private int loc_lightProj;


        public GPUInstancedShadowMapShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");

            loc_debugColor = GetUniformLocation("debugColor");
            loc_enableDebug = GetUniformLocation("enableDebug");
            loc_gMaxDepthDistance = GetUniformLocation("gMaxDepthDistance");

            loc_textureCount = GetUniformLocation("textureCount");
            loc_textures = new int[MAX_TEXTURES];
            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                loc_textures[i] = GetUniformLocation($"textures[{i}]");
            }

            loc_lightView = GetUniformLocation("lightView");
            loc_lightProj = GetUniformLocation("lightProj");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aTexCoord");
            BindAttribute(2, "aNormal");
            BindAttribute(3, "materialID");
        }

        public void LoadLightViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_lightView, matrix);
        }

        public void LoadLightProjMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_lightProj, matrix);
        }

        public void LoadMaxDepthDistance(float distance)
        {
            LoadUniform1f(loc_gMaxDepthDistance, distance);
        }

        public void LoadEnableDebug(bool enableDebug)
        {
            Gl.Uniform1(loc_enableDebug, enableDebug ? 1 : 0);
        }

        public void LoadDebugColor(Vertex4f color)
        {
            Gl.Uniform4f(loc_debugColor, 1, color);
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        /// <summary>
        /// 텍스처 배열 바인딩 (초기화 시 한 번만 호출)
        /// </summary>
        public void LoadTextureArray(uint[] textureIDs)
        {
            if (textureIDs == null || textureIDs.Length == 0) return;

            int count = System.Math.Min(textureIDs.Length, MAX_TEXTURES);

            Gl.Uniform1(loc_textureCount, count);

            for (int i = 0; i < count; i++)
            {
                Gl.ActiveTexture(TextureUnit.Texture0 + i);
                Gl.BindTexture(TextureTarget.Texture2d, textureIDs[i]);
                Gl.Uniform1(loc_textures[i], i);  // sampler에 텍스처 유닛 번호 전달
            }
        }
    }
}
