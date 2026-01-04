using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 크로스 빌보드 평면 법선 벡터 디버그 셰이더
    /// </summary>
    public class CrossBillboardNormalShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\crossbillboard_normal.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\crossbillboard_normal.geom.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\crossbillboard_normal.frag";

        private int loc_batchStartOffset;
        private int loc_currentBatchID;
        private int loc_normalLength;

        public CrossBillboardNormalShader(string projectPath) : base()
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
            loc_normalLength = GetUniformLocation("normalLength");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadCurrentBatchID(uint batchID)
        {
            Gl.Uniform1(loc_currentBatchID, batchID);
        }

        public void LoadNormalLength(float length)
        {
            Gl.Uniform1(loc_normalLength, length);
        }
    }
}