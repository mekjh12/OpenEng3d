using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU Driven Impostor Rendering Shader
    /// Geometry Shader를 사용해 Point를 카메라를 향하는 빌보드로 확장합니다.
    /// G-Buffer 출력으로 Deferred Rendering 파이프라인과 통합됩니다.
    /// </summary>
    public class GPUDrivenImpostorShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\gpu_impostor.frag";

        // 유니폼 위치
        private int loc_batchStartOffset;
        private int loc_cameraPosition;
        private int loc_aabbSphereRadius;

        // 아틀라스 관련
        private int loc_impostorAtlas;
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
            loc_aabbSphereRadius = GetUniformLocation("aabbSphereRadius");

            loc_impostorAtlas = GetUniformLocation("impostorAtlas");
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

        // AABB
        public void LoadAABBSphereRadius(float radius)
        {
            Gl.Uniform1(loc_aabbSphereRadius, radius);
        }

        // 텍스처 아틀라스
        public void LoadImpostorAtlas(uint textureId)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(loc_impostorAtlas, 0);
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