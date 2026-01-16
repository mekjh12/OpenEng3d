using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU-Driven 빌보드 인스턴스 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 카메라 방향 사각형으로 확장
    /// </summary>
    public class BillboardInstancedShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.geom";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\billboard_instance.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_cameraPosition;
        private int loc_batchStartOffset;
        private int loc_fogTexture;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_alphaThreshold;

        public BillboardInstancedShader(string projectPath) : base()
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
            loc_cameraPosition = GetUniformLocation("cameraPosition");
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");
            loc_fogTexture = GetUniformLocation("fogTexture");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_alphaThreshold = GetUniformLocation("alphaThreshold");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        public void LoadCameraPosition(float x, float y, float z)
        {
            Gl.Uniform3(loc_cameraPosition, x, y, z);
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadFogTexture(int textureUnit)
        {
            Gl.Uniform1(loc_fogTexture, textureUnit);
        }

        public void LoadFogColor(float r, float g, float b)
        {
            Gl.Uniform3(loc_fogColor, r, g, b);
        }

        public void LoadFogDensity(float density)
        {
            Gl.Uniform1(loc_fogDensity, density);
        }

        public void LoadAlphaThreshold(float threshold)
        {
            Gl.Uniform1(loc_alphaThreshold, threshold);
        }
    }
}