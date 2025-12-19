using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU-Driven AABB Box 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 AABB 박스로 확장합니다.
    /// </summary>
    public class AABBInstanceShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\aabb_instance.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\aabb_instance.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\aabb_instance.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_batchStartOffset;
        private int loc_currentBatchID;
        private int loc_boxColor;
        private int loc_alpha;

        public AABBInstanceShader(string projectPath) : base()
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
            loc_batchStartOffset = GetUniformLocation("batchStartOffset");
            loc_currentBatchID = GetUniformLocation("currentBatchID");
            loc_boxColor = GetUniformLocation("boxColor");
            loc_alpha = GetUniformLocation("alpha");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
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

        public void LoadBoxColor(float r, float g, float b)
        {
            Gl.Uniform3(loc_boxColor, r, g, b);
        }

        public void LoadAlpha(float alpha)
        {
            Gl.Uniform1(loc_alpha, alpha);
        }
    }
}