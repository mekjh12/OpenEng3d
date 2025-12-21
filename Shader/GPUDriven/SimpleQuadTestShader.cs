using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// LOD1 테스트용 간단한 사각형 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 Quad로 확장합니다.
    /// batchID별로 다른 색상을 표시합니다 (0=빨강, 1=초록, 2=파랑)
    /// </summary>
    public class SimpleQuadTestShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\simple_quad_test.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\simple_quad_test.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\simple_quad_test.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_batchStartOffset;
        private int loc_currentBatchID;
        private int loc_quadSize;
        private int loc_cameraPosition;

        public SimpleQuadTestShader(string projectPath) : base()
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
            loc_quadSize = GetUniformLocation("quadSize");
            loc_cameraPosition = GetUniformLocation("u_cameraPosition");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
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

        public void LoadQuadSize(float size)
        {
            Gl.Uniform1(loc_quadSize, size);
        }
    }
}