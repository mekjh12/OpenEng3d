using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// GPU Driven Normal Vector 렌더링 셰이더
    /// Geometry Shader를 사용해 각 정점의 법선 벡터를 라인으로 시각화합니다.
    /// </summary>
    public class NormalVectorShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\normal_vector.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\normal_vector.geom.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\normal_vector.frag";

        // SSBO 바인딩 포인트
        private const int TRANSFORM_BUFFER_BINDING = 0;
        private const int VISIBLE_INDICES_BINDING = 1;

        // 유니폼 위치
        private int loc_vp;
        private int loc_view;
        private int loc_batchStartOffset;
        private int loc_normalLength;
        private int loc_normalColor;

        public NormalVectorShader(string projectPath) : base()
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
            loc_normalLength = GetUniformLocation("normalLength");
            loc_normalColor = GetUniformLocation("normalColor");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aTexCoord");
            BindAttribute(2, "aNormal");
            BindAttribute(3, "materialID");
        }

        public void LoadBatchStartOffset(uint offset)
        {
            Gl.Uniform1(loc_batchStartOffset, (int)offset);
        }

        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        public void LoadNormalLength(float length)
        {
            Gl.Uniform1(loc_normalLength, length);
        }

        public void LoadNormalColor(float r, float g, float b)
        {
            Gl.Uniform3(loc_normalColor, r, g, b);
        }
    }
}