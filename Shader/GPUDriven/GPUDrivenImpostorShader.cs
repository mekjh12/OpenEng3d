using Common;
using OpenGL;

namespace Shader
{
    public class GPUDrivenImpostorShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.frag";

        // 유니폼 위치
        private int loc_batchStartOffset;
        private int loc_cameraPosition;

        // 아틀라스 관련
        private int loc_impostorAtlas;
        private int loc_normalAtlas;

        // G-Buffer 관련
        private int loc_gMaxDepthDistance;

        // 디버깅
        private int loc_enableEdgeLine;

        public GPUDrivenImpostorShader(string projectPath) : base()
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
            loc_cameraPosition = GetUniformLocation("cameraPosition");

            loc_impostorAtlas = GetUniformLocation("impostorAtlas");
            loc_normalAtlas = GetUniformLocation("normalAtlas");

            loc_gMaxDepthDistance = GetUniformLocation("gMaxDepthDistance");
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");
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

        // 카메라
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
        }

        // 텍스처 아틀라스
        public void LoadImpostorAtlas(uint textureId)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(loc_impostorAtlas, 0);
        }

        public void LoadNormalAtlas(uint textureId)
        {
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(loc_normalAtlas, 1);
        }

        // G-Buffer
        public void LoadMaxDepthDistance(float distance)
        {
            Gl.Uniform1(loc_gMaxDepthDistance, distance);
        }

        // 디버깅
        public void LoadEnableEdgeLine(bool enable, float lineWidth = 1.0f)
        {
            Gl.Uniform1(loc_enableEdgeLine, enable ? 1 : 0);
            if (enable)
                Gl.LineWidth(lineWidth);
        }
    }
}