using Common;
using Common.Abstractions;
using OpenGL;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// 모델의 법선 벡터를 시각화하는 셰이더
    /// Geometry Shader를 사용해 각 정점의 법선을 라인으로 표시합니다.
    /// </summary>
    public class ModelNormalShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\UnlitShader\model_normal.vert";
        const string GEOMETRY_FILE = @"\Shader\UnlitShader\model_normal.geom.glsl";
        const string FRAGMENT_FILE = @"\Shader\UnlitShader\model_normal.frag";

        // 유니폼 위치
        private int loc_mvp;
        private int loc_mv;
        private int loc_normalMatrix;
        private int loc_normalLength;
        private int loc_normalColor;
        private int loc_projection;

        public ModelNormalShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_mvp = GetUniformLocation("mvp");
            loc_mv = GetUniformLocation("mv");
            loc_normalMatrix = GetUniformLocation("normalMatrix");
            loc_normalLength = GetUniformLocation("normalLength");
            loc_normalColor = GetUniformLocation("normalColor");
            loc_projection = GetUniformLocation("projection");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "position");
            BindAttribute(1, "textureCoords");
            BindAttribute(2, "normal");
            BindAttribute(3, "materialID");
        }

        /// <summary>
        /// 모든 변환 행렬을 한 번에 설정합니다. (권장)
        /// </summary>
        public void LoadTransforms(Matrix4x4f projection, Matrix4x4f view, Matrix4x4f model)
        {
            Matrix4x4f vp = projection * view;
            Matrix4x4f mvp = vp * model;
            Matrix4x4f modelView = view * model;

            LoadUniformMatrix4(loc_projection, projection);
            LoadUniformMatrix4(loc_mvp, mvp);
            LoadUniformMatrix4(loc_mv, modelView);
            LoadUniformMatrix3(loc_normalMatrix, CalculateNormalMatrix(modelView));
        }

        /// <summary>
        /// Projection 행렬만 개별 설정 (레거시)
        /// </summary>
        public void LoadProjectionMatrix(Matrix4x4f projection)
        {
            LoadUniformMatrix4(loc_projection, projection);
        }

        /// <summary>
        /// Model-View-Projection 설정 (레거시)
        /// 주의: LoadProjectionMatrix()를 별도로 호출해야 함
        /// </summary>
        public void LoadModelViewProjection(Matrix4x4f vp, Matrix4x4f view, Matrix4x4f model)
        {
            Matrix4x4f mvp = vp * model;
            Matrix4x4f modelView = view * model;

            LoadUniformMatrix4(loc_mvp, mvp);
            LoadUniformMatrix4(loc_mv, modelView);
            LoadUniformMatrix3(loc_normalMatrix, CalculateNormalMatrix(modelView));
        }

        public void LoadNormalLength(float length)
        {
            Gl.Uniform1(loc_normalLength, length);
        }

        public void LoadNormalColor(float r, float g, float b)
        {
            Gl.Uniform3(loc_normalColor, r, g, b);
        }

        private Matrix3x3f CalculateNormalMatrix(Matrix4x4f modelView)
        {
            // 1. ModelView의 3x3 회전 부분 추출
            Matrix3x3f mv3x3 = modelView.Rot3x3f();

            // 2. 역행렬 계산
            Matrix3x3f inverse = mv3x3.Inverse;

            // 3. 전치
            Matrix3x3f normalMatrix = inverse.Transposed;

            return normalMatrix;
        }
    }
}