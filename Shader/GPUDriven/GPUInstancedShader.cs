using Common;
using Common.Abstractions;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// GPU Driven Instanced Rendering Shader
    /// SSBO에서 변환 행렬과 가시 인덱스를 읽어 렌더링합니다.
    /// </summary>
    public class GPUInstancedShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\instanced.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\instanced.frag";

        // SSBO 바인딩 포인트
        private const int TRANSFORM_BUFFER_BINDING = 0;
        private const int VISIBLE_INDICES_BINDING = 1;

        // 유니폼 위치
        private int loc_vp;
        private int loc_textureCount;
        private int[] loc_textures;  // ✅ 배열 위치
        private const int MAX_TEXTURES = 32;
        private int loc_batchStartOffset;


        public GPUInstancedShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("vp");
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");

            loc_textureCount = GetUniformLocation("textureCount");

            // ✅ 배열 위치 가져오기
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
        /// 뷰-투영 행렬을 설정합니다.
        /// </summary>
        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        /// <summary>
        /// ✅ 텍스처 배열 바인딩 (초기화 시 한 번만 호출)
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