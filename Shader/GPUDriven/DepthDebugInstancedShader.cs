using OpenGL;
using Common;

namespace Shader
{
    public class DepthDebugInstancedShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\depth_debug_instanced.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\depth_debug_instanced.frag";

        private int loc_vpMatrix;
        private int loc_nearPlane;  // ✅ 추가
        private int loc_farPlane;   // ✅ 추가
        private int loc_batchStartOffset;  // ← 추가

        public DepthDebugInstancedShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vpMatrix = GetUniformLocation("vpMatrix");
            loc_nearPlane = GetUniformLocation("nearPlane");  // ✅
            loc_farPlane = GetUniformLocation("farPlane");    // ✅
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");  // ← 추가
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
            base.BindAttribute(2, "normal");
        }

        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vpMatrix, matrix);
        }

        public void LoadCameraNearFar(float near, float far)  // ✅
        {
            LoadUniform1f(loc_nearPlane, near);
            LoadUniform1f(loc_farPlane, far);
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }
    }
}