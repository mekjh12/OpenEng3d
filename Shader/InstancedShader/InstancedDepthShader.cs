using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// TexturedModel의 인스턴스를 Hi-Z 버퍼에 깊이 렌더링하는 셰이더
    /// GPU 인스턴싱을 사용하여 여러 인스턴스를 한 번에 처리합니다.
    /// </summary>
    public class InstancedDepthShader : ShaderProgramBase
    {
        const string VERTEx_FILE = @"\Shader\InstancedShader\instanced_depth.vert";
        const string FRAGMENT_FILE = @"\Shader\InstancedShader\instanced_depth.frag";

        // 유니폼 위치 캐싱
        private int loc_view;
        private int loc_proj;
        private int loc_modelTexture;  // ✅ 텍스처 유니폼 추가

        // SSBO 바인딩 포인트
        private const int INSTANCE_BUFFER_BINDING = 0;

        public InstancedDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
            loc_modelTexture = GetUniformLocation("modelTexture");  // ✅
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "position");
            BindAttribute(1, "texcoord");  // ✅ 텍스처 좌표 바인딩
        }

        /// <summary>
        /// 뷰 행렬을 설정합니다.
        /// </summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>
        /// 투영 행렬을 설정합니다.
        /// </summary>
        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_proj, matrix);
        }

        /// <summary>
        /// 텍스처를 설정합니다.
        /// </summary>
        public void LoadTexture(TextureUnit textureUnit, uint textureId)
        {
            int unit = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_modelTexture, unit);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        /// <summary>
        /// 렌더링 상태를 설정합니다.
        /// </summary>
        public void SetupRenderState()
        {
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(true);
            Gl.Disable(EnableCap.CullFace);
            Gl.ColorMask(false, false, false, false);  // 깊이만 렌더링
        }

        /// <summary>
        /// 렌더링 상태를 복원합니다.
        /// </summary>
        public void RestoreRenderState()
        {
            Gl.ColorMask(true, true, true, true);
            Gl.Enable(EnableCap.CullFace);
        }
    }
}