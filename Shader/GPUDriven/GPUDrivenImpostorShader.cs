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
        private int loc_aabbSphereRadius;
        private int loc_modelMatrix;

        // 아틀라스 관련
        private int loc_impostorAtlas;
        private int loc_atlasSize;
        private int loc_individualSize;
        private int loc_horizontalFrames;
        private int loc_verticalFrames;

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
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");
            loc_modelMatrix = GetUniformLocation("model");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_modelMatrix, matrix);
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
        }

        public void LoadAABBSphereRadius(float radius)
        {
            Gl.Uniform1(loc_aabbSphereRadius, radius);
        }

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

        public void LoadEnableEdgeLine(bool enable, float lineWidth = 1.0f)
        {
            Gl.Uniform1i(loc_enableEdgeLine, 1, enable ? 1 : 0);
            if (enable)
                Gl.LineWidth(lineWidth);
        }
    }
}