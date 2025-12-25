using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// SSBO 기반 GPU Driven 크로스 빌보드 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 3개의 수직 평면으로 확장합니다.
    /// 텍스처 아틀라스를 사용해 각 평면에 다른 텍스처를 적용합니다.
    /// </summary>
    public class CrossBillboardInstanceShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\crossBillboardInstance.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_view;
        private int loc_batchStartOffset;
        private int loc_currentBatchID;
        private int loc_atlasTexture;
        private int loc_useTexture;  // 텍스처 사용 여부 (디버깅용)

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
            loc_vp = GetUniformLocation("vp");
            loc_view = GetUniformLocation("view");
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");
            loc_currentBatchID = GetUniformLocation("currentBatchID");
            loc_atlasTexture = GetUniformLocation("atlasTexture");
            loc_useTexture = GetUniformLocation("useTexture");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

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

        public void UseTexture(bool enable)
        {
            Gl.Uniform1(loc_useTexture, enable ? 1 : 0);
        }
    }
}