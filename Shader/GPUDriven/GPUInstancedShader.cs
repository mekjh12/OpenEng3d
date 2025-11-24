using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU Driven Instanced Rendering Shader
    /// SSBO에서 변환 행렬과 가시 인덱스를 읽어 렌더링합니다.
    /// </summary>
    public class GPUInstancedShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\instanced.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\instanced.frag";

        // SSBO 바인딩 포인트
        private const int TRANSFORM_BUFFER_BINDING = 0;
        private const int VISIBLE_INDICES_BINDING = 1;

        // 유니폼 위치
        private int loc_projection;
        private int loc_view;
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
            loc_projection = GetUniformLocation("proj");
            loc_view = GetUniformLocation("view");
            loc_texture = GetUniformLocation("uTexture");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aNormal");
            BindAttribute(2, "aTexCoord");
        }

        /// <summary>
        /// 투영 행렬을 설정합니다.
        /// </summary>
        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_projection, matrix);
        }

        /// <summary>
        /// 뷰 행렬을 설정합니다.
        /// </summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>
        /// 텍스처 유닛을 설정합니다.
        /// </summary>
        public void LoadTexture(int unit = 0)
        {
            Gl.Uniform1(loc_texture, unit);
        }

    }
}