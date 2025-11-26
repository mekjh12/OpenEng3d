using OpenGL;
using System.Drawing.Drawing2D;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 무한 그리드 렌더링을 위한 셰이더 클래스입니다.
    /// 3D 뷰포트에서 바닥면에 무한히 펼쳐지는 그리드를 렌더링합니다.
    /// </summary>
    public class InfiniteGridShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\InfiniteGridShader\grid.vert";
        const string FRAGMENT_FILE = @"\Shader\InfiniteGridShader\grid.frag";

        // 유니폼 위치 (캐싱)
        private int loc_gVP;
        private int loc_gCameraFocusWorldPos;
        private int loc_gCameraWorldPos;
        private int loc_viewportSize;
        private int loc_focalLength;
        private int loc_aspectRatio;
        private int loc_view;

        public InfiniteGridShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_gVP = GetUniformLocation("gVP");
            loc_gCameraFocusWorldPos = GetUniformLocation("gCameraFocusWorldPos");
            loc_gCameraWorldPos = GetUniformLocation("gCameraWorldPos");
            loc_viewportSize = GetUniformLocation("viewport_size");
            loc_focalLength = GetUniformLocation("focal_length");
            loc_aspectRatio = GetUniformLocation("aspect_ratio");
            loc_view = GetUniformLocation("view");
        }

        protected override void BindAttributes()
        {
            // 무한 그리드는 풀스크린 쿼드로 렌더링되므로 별도 애트리뷰트 불필요
        }

        // === Load 메서드들 ===

        /// <summary>
        /// View-Projection 행렬 설정
        /// </summary>
        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_gVP, 1, false, matrix);
        }

        /// <summary>
        /// 뷰 행렬 설정
        /// </summary>
        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        /// <summary>
        /// 카메라 포커스 위치 설정 (카메라가 바라보는 지점)
        /// </summary>
        public void LoadCameraFocusPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_gCameraFocusWorldPos, 1, position);
        }

        /// <summary>
        /// 카메라 월드 위치 설정
        /// </summary>
        public void LoadCameraWorldPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_gCameraWorldPos, 1, position);
        }

        /// <summary>
        /// 뷰포트 크기 설정 (픽셀 단위)
        /// </summary>
        public void LoadViewportSize(Vertex2f size)
        {
            Gl.Uniform2f(loc_viewportSize, 1, size);
        }

        /// <summary>
        /// 카메라 초점 거리 설정
        /// </summary>
        public void LoadFocalLength(float focalLength)
        {
            Gl.Uniform1f(loc_focalLength, 1, focalLength);
        }

        /// <summary>
        /// 화면 종횡비 설정
        /// </summary>
        public void LoadAspectRatio(float aspectRatio)
        {
            Gl.Uniform1f(loc_aspectRatio, 1, aspectRatio);
        }
    }
}