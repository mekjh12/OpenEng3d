using OpenGL;
using System.Drawing.Drawing2D;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 관성 텐서 시각화를 위한 셰이더 클래스입니다.
    /// 물체의 회전 관성을 색상으로 표현하여 디버깅에 활용합니다.
    /// </summary>
    public class InertiaShader : ShaderProgramBase
    {
        const string VERTEX_SOURCES = @"
        #version 420 core
        in vec3 position;
        out vec4 worldPosition;
        out vec3 center;
        uniform mat4 model;
        uniform mat4 proj;
        uniform mat4 view;
        void main(void)
        {
            center = model[3].xyz;
            worldPosition = model * vec4(position, 1.0);
            gl_Position = proj * view * worldPosition;
        }
        ";

        const string FRAGMENT_SOURCES = @"
        #version 420 core
        uniform mat3 inverseInertia;
        uniform vec3 axis;
        in vec3 center;
        in vec4 worldPosition;
        out vec4 FragColor;
        void main(void)
        {
            vec3 relPoint = normalize(worldPosition.xyz - center);
            vec3 torque = inverseInertia * relPoint;
            float dot = dot(relPoint, torque);
            FragColor = vec4(dot, 0, 0, 1.0f);
        }
        ";

        // 유니폼 위치 (캐싱)
        private int loc_model;
        private int loc_view;
        private int loc_proj;
        private int loc_inverseInertia;
        private int loc_axis;

        public InertiaShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 임시 파일로 셰이더 소스 저장
            string vertFileName = Path.Combine(projectPath, "vert.tmp");
            File.WriteAllText(vertFileName, VERTEX_SOURCES);
            VertFileName = vertFileName;

            string fragFileName = Path.Combine(projectPath, "frag.tmp");
            File.WriteAllText(fragFileName, FRAGMENT_SOURCES);
            FragFileName = fragFileName;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = GetUniformLocation("model");
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
            loc_inverseInertia = GetUniformLocation("inverseInertia");
            loc_axis = GetUniformLocation("axis");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 모델 행렬 설정
        /// </summary>
        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_model, 1, false, matrix);
        }

        /// <summary>
        /// 뷰 행렬 설정
        /// </summary>
        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        /// <summary>
        /// 프로젝션 행렬 설정
        /// </summary>
        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_proj, 1, false, matrix);
        }

        /// <summary>
        /// 역 관성 텐서 행렬 설정 (3x3 행렬)
        /// </summary>
        public void LoadInverseInertia(Matrix3x3f matrix)
        {
            Gl.UniformMatrix3f(loc_inverseInertia, 1, false, matrix);
        }

        /// <summary>
        /// 회전 축 설정
        /// </summary>
        public void LoadRotationAxis(Vertex3f axis)
        {
            Gl.Uniform3f(loc_axis, 1, axis);
        }

        /// <summary>
        /// 텍스처 바인딩 (필요시 확장용)
        /// </summary>
        public void LoadTexture(string uniformName, TextureUnit textureUnit, uint texture)
        {
            int location = GetUniformLocation(uniformName);
            Gl.Uniform1i(location, 1, (int)(textureUnit - TextureUnit.Texture0));
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}