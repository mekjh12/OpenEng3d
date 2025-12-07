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
        private int loc_texture;

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
            loc_texture = GetUniformLocation("uTexture");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aTexCoord");
            BindAttribute(2, "aNormal");
        }

        /// <summary>
        /// 뷰-투영 행렬을 설정합니다.
        /// </summary>
        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        /// <summary>
        /// 텍스처 유닛을 설정합니다.
        /// </summary>
        public void LoadTexture(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_texture, (uint)unit);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }
    }
}