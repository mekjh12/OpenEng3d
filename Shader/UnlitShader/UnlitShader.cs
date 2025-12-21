using Common;
using OpenGL;
using System.Drawing.Drawing2D;

namespace Shader
{
    /// <summary>
    /// 기본적인 Unlit 셰이더를 구현한 클래스입니다.
    /// 텍스처와 MVP 변환을 지원하며 조명 계산은 수행하지 않습니다.
    /// </summary>
    public class UnlitShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\UnlitShader\unlit.vert";
        const string FRAGMENT_FILE = @"\Shader\UnlitShader\unlit.frag";

        // 유니폼 위치 (캐싱)
        private int loc_mvp;
        private int loc_modelTexture;
        private int loc_textureCount;
        private int[] loc_textures;  // ✅ 배열 위치
        private const int MAX_TEXTURES = 32;
        private int loc_modelView;  // ✅ 추가

        /// <summary>
        /// UnlitShader의 생성자입니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        public UnlitShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_mvp = GetUniformLocation("mvp");
            loc_textureCount = GetUniformLocation("textureCount");
            loc_modelView = GetUniformLocation("modelView");  // ✅ 추가

            // ✅ 배열 위치 가져오기
            loc_textures = new int[MAX_TEXTURES];
            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                loc_textures[i] = GetUniformLocation($"textures[{i}]");
            }
        }

        /// <summary>
        /// 셰이더 속성들을 바인딩합니다.
        /// position: 정점 위치 (location = 0)
        /// textureCoords: 텍스처 좌표 (location = 1)
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "textureCoords");
            base.BindAttribute(3, "materialID");
        }

        // === Load 메서드들 ===
        // ✅ 추가: ModelView 행렬 로드
        public void LoadModelView(Matrix4x4f modelView)
        {
            Gl.UniformMatrix4f(loc_mvp, 1, false, modelView);
        }

        /// <summary>
        /// Model-View-Projection 행렬 설정
        /// </summary>
        public void LoadMVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_mvp, 1, false, matrix);
        }

        /// <summary>
        /// ✅ 텍스처 배열 바인딩 (초기화 시 한 번만 호출)
        /// </summary>
        public void LoadTextureArray(uint[] textureIDs)
        {
            int count = System.Math.Min(textureIDs.Length, MAX_TEXTURES);

            Gl.Uniform1(loc_textureCount, count);

            for (int i = 0; i < count; i++)
            {
                Gl.ActiveTexture(TextureUnit.Texture0 + i);
                Gl.BindTexture(TextureTarget.Texture2d, textureIDs[i]);
                Gl.Uniform1(loc_textures[i], i);  // sampler에 텍스처 유닛 번호 전달
            }
        }

        /// <summary>
        /// 모델 텍스처 바인딩
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛 (0 = GL_TEXTURE0)</param>
        public void LoadModelTexture(TextureUnit textureUnit, uint texture)
        {
            Gl.Uniform1(loc_modelTexture, (int)TextureUnit.Texture0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}