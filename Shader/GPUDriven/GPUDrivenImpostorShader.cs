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

        // ✅ 크기 관련 (기존 aabbSphereRadius 대체)
        private int loc_billboardWidth;   // BoundingSphereRadius × 2
        private int loc_billboardHeight;  // ActualHeight

        // 아틀라스 관련
        private int loc_impostorAtlas;
        private int loc_normalAtlas;
        private int loc_atlasSize;
        private int loc_individualSize;
        private int loc_horizontalFrames;
        private int loc_verticalFrames;

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

            // ✅ 새로운 유니폼
            loc_billboardWidth = GetUniformLocation("billboardWidth");
            loc_billboardHeight = GetUniformLocation("billboardHeight");

            loc_impostorAtlas = GetUniformLocation("impostorAtlas");
            loc_normalAtlas = GetUniformLocation("normalAtlas");
            loc_atlasSize = GetUniformLocation("atlasSize");
            loc_individualSize = GetUniformLocation("individualSize");
            loc_horizontalFrames = GetUniformLocation("horizontalFrames");
            loc_verticalFrames = GetUniformLocation("verticalFrames");

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

        // ✅ 빌보드 크기 (메타데이터에서 로드한 값 사용)
        public void LoadBillboardSize(float width, float height)
        {
            Gl.Uniform1(loc_billboardWidth, width);
            Gl.Uniform1(loc_billboardHeight, height);
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

        public void LoadAtlasSize(float size)
        {
            Gl.Uniform1(loc_atlasSize, size);
        }

        public void LoadIndividualSize(float size)
        {
            Gl.Uniform1(loc_individualSize, size);
        }

        public void LoadFrameCounts(int horizontal, int vertical)
        {
            Gl.Uniform1(loc_horizontalFrames, horizontal);
            Gl.Uniform1(loc_verticalFrames, vertical);
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