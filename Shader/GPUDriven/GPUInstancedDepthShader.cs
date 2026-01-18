using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU 인스턴싱 깊이 전용 셰이더 (Temporal Z-PrePass용)
    /// 텍스처, 라이팅 없이 깊이만 출력
    /// </summary>
    public class GPUInstancedDepthShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\gpu_InstancedDepth.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\gpu_InstancedDepth.frag";
        private const int MAX_TEXTURES = 32;

        private int loc_batchStartOffset;
        private int loc_textureCount;
        private int[] loc_textures;

        public GPUInstancedDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");

            loc_textureCount = GetUniformLocation("textureCount");
            loc_textures = new int[MAX_TEXTURES];
            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                loc_textures[i] = GetUniformLocation($"textures[{i}]");
            }
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aTexCoord");
            BindAttribute(2, "aNormal");
            BindAttribute(3, "materialID");
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