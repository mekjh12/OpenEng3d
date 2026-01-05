using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형의 법선 벡터를 라인으로 시각화하는 셰이더
    /// Geometry Shader에서 삼각형 법선을 계산하고 RGB 라인으로 표시
    /// Vertex 0: 빨강, Vertex 1: 녹색, Vertex 2: 파랑
    /// </summary>
    public class TerrainNormalLineShader : ShaderProgramBase
    {
        // 기존 지형 셰이더 파일 재사용
        const string VERTEX_FILE = @"\Shader\TerrainShader\common\terrain.vert";
        const string TCS_FILE = @"\Shader\TerrainShader\common\terrain.tcs.glsl";
        const string TES_FILE = @"\Shader\TerrainShader\common\terrain.tes.glsl";

        // 법선 라인 전용 셰이더
        const string GEOM_FILE = @"\Shader\TerrainShader\terrain_normal_line.geom.glsl";
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\terrain_normal_line.frag";

        private int loc_heightScale;
        private int loc_model;
        private int loc_normalLength;

        public TerrainNormalLineShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;
            GeomFileName = projectPath + GEOM_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
            base.BindAttribute(2, "color");
        }

        protected override void GetAllUniformLocations()
        {
            loc_heightScale = GetUniformLocation("heightScale");
            loc_model = GetUniformLocation("model");
            loc_normalLength = GetUniformLocation("normalLength");
        }

        #region Uniform 로딩 함수
        public void LoadHeightScale(float value)
        {
            Gl.Uniform1(loc_heightScale, value);
        }

        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        public void LoadNormalLength(float length)
        {
            Gl.Uniform1(loc_normalLength, length);
        }
        #endregion
    }
}