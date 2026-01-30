using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 2D 텍스처를 풀스크린으로 표시하는 셰이더 클래스입니다.
    /// 노이즈 텍스처 시각화 등에 활용합니다.
    /// </summary>
    public class DisplayShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\DisplayShader\display.vert";
        const string FRAGMENT_FILE = @"\Shader\DisplayShader\display.frag";

        // 유니폼 위치 캐싱
        private int loc_noiseTexture;
        private int loc_heightMapTexture;
        private int loc_flip;
        private int loc_scaled;
        private int loc_useHeightMap;

        /// <summary>
        /// 디스플레이 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public DisplayShader(string projectPath) : base()
        {
            // 셰이더 초기화
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            // 컴파일 및 링크
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 유니폼 변수 위치를 가져옵니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            loc_noiseTexture = GetUniformLocation("noiseTexture");
            loc_heightMapTexture = GetUniformLocation("heightMapTexture");
            loc_flip = GetUniformLocation("flip");
            loc_scaled = GetUniformLocation("scaled");
            loc_useHeightMap = GetUniformLocation("useHeightMap");
        }

        public void LoadUseHeightMap(bool useHeightMap)
        {
            Gl.Uniform1(loc_useHeightMap, useHeightMap ? 1 : 0);
        }

        public void LoadScaled(float scaled)
        {
            Gl.Uniform1(loc_scaled, scaled);
        }

        public void LoadFlip(bool flip)
        {
            Gl.Uniform1(loc_flip, flip ? 1 : 0);
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
        }

        /// <summary>
        /// 노이즈 텍스처를 바인딩합니다.
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="texture">텍스처 ID</param>
        public void LoadNoiseTexture(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_noiseTexture, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadHeightMapTexture(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_heightMapTexture, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

    }
}