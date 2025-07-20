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
        public void RenderAxes(Matrix4x4f model, Matrix4x4f[] boneTransforms, Matrix4x4f viewProjection, float axisLength = 10.0f, float lineWidth = 1.0f)
        {
            if (boneTransforms == null || boneTransforms.Length == 0) return;

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
            foreach (var transform in boneTransforms)
            {
                Matrix4x4f finalTransform = viewProjection * model * transform * scaleMatrix;
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
    }
}
