using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// SSBO 기반 GPU Driven 크로스 빌보드 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 3개의 수직 평면으로 확장합니다.
    /// G-Buffer 출력으로 Deferred Rendering 파이프라인과 통합됩니다.
    /// </summary>
    public class CrossBillboardInstanceShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.frag";

        // 유니폼 위치
        private int loc_batchStartOffset;
        private int loc_currentBatchID;
        private int loc_atlasTexture;
        private int loc_normalTexture;
        private int loc_useTexture;
        private int loc_gMaxDepthDistance;

        public CrossBillboardInstanceShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");
            loc_currentBatchID = GetUniformLocation("currentBatchID");
            loc_atlasTexture = GetUniformLocation("atlasTexture");
            loc_normalTexture = GetUniformLocation("normalTexture");
            loc_useTexture = GetUniformLocation("useTexture");
            loc_gMaxDepthDistance = GetUniformLocation("gMaxDepthDistance");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        // 배치 관리
        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadCurrentBatchID(uint batchID)
        {
            Gl.Uniform1(loc_currentBatchID, batchID);
        }

        public void LoadAtlasTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_atlasTexture, 0);
        }

        public void LoadNormalTexture(uint textureID)
        {
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.Uniform1(loc_normalTexture, 1);
        }

        // 렌더링 모드
        public void UseTexture(bool enable)
        {
            Gl.Uniform1(loc_useTexture, enable ? 1 : 0);
        }

        // 깊이 정규화 거리 설정
        public void LoadMaxDepthDistance(float distance)
        {
            Gl.Uniform1(loc_gMaxDepthDistance, distance);
        }
    }
}