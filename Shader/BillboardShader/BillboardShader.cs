using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 빌보드 렌더링을 위한 셰이더 클래스입니다.
    /// 항상 카메라를 바라보는 평면을 렌더링하며, 파티클이나 나무 등에 사용됩니다.
    /// </summary>
    public class BillboardShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\BillboardShader\billboard.vert";
        const string FRAGMENT_FILE = @"\Shader\BillboardShader\billboard.frag";
        const string GEOMETRY_FILE = @"\Shader\BillboardShader\billboard.gem.glsl";

        // 유니폼 위치 (캐싱)
        private int loc_proj;
        private int loc_view;
        private int loc_gCameraPos;
        private int loc_gColorMap;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_fogPlane;
        private int loc_atlasIndex;

        public BillboardShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_proj = GetUniformLocation("proj");
            loc_view = GetUniformLocation("view");
            loc_gCameraPos = GetUniformLocation("gCameraPos");
            loc_gColorMap = GetUniformLocation("gColorMap");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_fogPlane = GetUniformLocation("fogPlane");
            loc_atlasIndex = GetUniformLocation("atlasIndex");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 프로젝션 행렬 설정
        /// </summary>
        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_proj, 1, false, matrix);
        }

        /// <summary>
        /// 뷰 행렬 설정
        /// </summary>
        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        /// <summary>
        /// 카메라 위치 설정
        /// </summary>
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_gCameraPos, 1, position);
        }

        /// <summary>
        /// 컬러맵 텍스처 바인딩 및 설정
        /// </summary>
        public void LoadTexture(uint texture)
        {
            Gl.Uniform1i(loc_gColorMap, 1, (int)TextureUnit.Texture0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>
        /// 안개 색상 설정
        /// </summary>
        public void LoadFogColor(Vertex3f fogColor)
        {
            Gl.Uniform3f(loc_fogColor, 1, fogColor);
        }

        /// <summary>
        /// 안개 밀도 설정
        /// </summary>
        public void LoadFogDensity(float density)
        {
            Gl.Uniform1f(loc_fogDensity, 1, density);
        }

        /// <summary>
        /// 안개 평면 설정 (높이 기반 안개)
        /// </summary>
        public void LoadFogPlane(Vertex4f fogPlane)
        {
            Gl.Uniform4f(loc_fogPlane, 1, fogPlane);
        }

        /// <summary>
        /// 텍스처 아틀라스 인덱스 설정
        /// </summary>
        public void LoadAtlasIndex(int index)
        {
            Gl.Uniform1i(loc_atlasIndex, 1, index);
        }
    }
}