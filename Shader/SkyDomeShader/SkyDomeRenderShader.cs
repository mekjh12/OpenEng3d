using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 스카이돔에 대기 텍스처를 렌더링하기 위한 셰이더 프로그램
    /// </summary>
    public class SkyDomeRenderShader : ShaderProgramBase
    {
        // 셰이더 파일 경로 상수
        private const string VERTEX_FILE = @"\Shader\SkyDomeShader\skydome.vert";
        private const string FRAGMENT_FILE = @"\Shader\SkyDomeShader\skydome.frag";

        // 유니폼 위치 캐싱
        private int loc_mvp;            // 모델-뷰-투영 행렬
        private int loc_skyTexture;     // 하늘 텍스처

        /// <summary>
        /// SkyDomeRenderShader 생성자
        /// 셰이더 파일을 로드하고 초기 컴파일 수행
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        public SkyDomeRenderShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        /// <summary>
        /// 셰이더 프로그램의 버텍스 속성 바인딩
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
        }

        /// <summary>
        /// 모든 유니폼 변수의 위치 가져오기
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            loc_mvp = GetUniformLocation("mvp");
            loc_skyTexture = GetUniformLocation("skyTexture");
        }

        // === Load 메서드들 ===
        public void LoadMVPMatrix(Matrix4x4f mvp)
        {
            LoadUniformMatrix4(loc_mvp, mvp);
        }

        /// <summary>
        /// 하늘 텍스처 바인딩
        /// </summary>
        public void LoadSkyTexture(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_skyTexture, (int)unit - (int)TextureUnit.Texture0);
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        /// <summary>
        /// 하늘 텍스처 바인딩 (TextureUnit.Texture0 기본값)
        /// </summary>
        public void LoadSkyTexture(uint textureId)
        {
            LoadSkyTexture(TextureUnit.Texture0, textureId);
        }
    }
}