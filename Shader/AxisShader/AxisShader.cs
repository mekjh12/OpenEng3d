using Common;
using OpenGL;

namespace Shader
{
    public class AxisShader : ShaderProgram<AxisShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\AxisShader\axis.vert";
        const string FRAGMENT_FILE = @"\Shader\AxisShader\axis.frag";

        public enum UNIFORM_NAME
        {
            mvp,    // Model-View-Projection 행렬만 필요
            Count
        }

        public AxisShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "color");
        }

        /// <summary>
        /// 여러 객체의 좌표축을 배치 렌더링
        /// </summary>
        public void RenderAxes(Matrix4x4f worldTransform, Matrix4x4f[] transforms, Matrix4x4f viewProjection, float axisLength = 10.0f, float lineWidth = 1.0f)
        {
            if (transforms == null || transforms.Length == 0) return;

            // 렌더링 상태 설정
            Gl.Disable(EnableCap.DepthTest);
            Gl.LineWidth(lineWidth);

            Bind();
            Gl.BindVertexArray(AxisGeometry.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);

            // 스케일 행렬
            Matrix4x4f scaleMatrix = Matrix4x4f.Scaled(axisLength, axisLength, axisLength);

            // 각 객체의 좌표축 렌더링 (각각 1번의 draw call)
            foreach (var transform in transforms)
            {
                Matrix4x4f finalTransform = viewProjection * worldTransform * transform * scaleMatrix;
                LoadUniform(UNIFORM_NAME.mvp, finalTransform);

                // 한 번의 draw call로 3축 모두 렌더링
                Gl.DrawArrays(PrimitiveType.Lines, 0, 6);
            }

            // 정리
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            Unbind();

            // 원래 상태 복원
            Gl.Enable(EnableCap.DepthTest);
            Gl.LineWidth(1.0f);
        }

        /// <summary>
        /// 좌표축을 렌더링합니다 (한 번의 draw call로 3축 모두)
        /// </summary>
        /// <param name="transform">객체의 변환 행렬</param>
        /// <param name="viewProjection">뷰-프로젝션 행렬</param>
        /// <param name="axisLength">축 길이</param>
        /// <param name="lineWidth">선 두께</param>
        public void RenderAxis(Matrix4x4f transform, Matrix4x4f viewProjection, float axisLength = 10.0f, float lineWidth = 2.0f)
        {
            // 렌더링 상태 설정
            Gl.Disable(EnableCap.DepthTest);
            Gl.LineWidth(lineWidth);

            Bind();
            Gl.BindVertexArray(AxisGeometry.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1); // 색상 속성 활성화

            // 스케일 행렬 (축 길이 조정)
            Matrix4x4f scaleMatrix = Matrix4x4f.Scaled(axisLength, axisLength, axisLength);

            // 최종 변환 행렬 = ViewProjection * Transform * Scale
            Matrix4x4f finalTransform = viewProjection * transform * scaleMatrix;

            // 한 번의 draw call로 모든 축 렌더링
            LoadUniform(AxisShader.UNIFORM_NAME.mvp, finalTransform);
            Gl.DrawArrays(PrimitiveType.Lines, 0, 6); // 6개 점 = 3축

            // 정리
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            Unbind();

            // 원래 상태 복원
            Gl.Enable(EnableCap.DepthTest);
            Gl.LineWidth(1.0f);
        }
    }
}
