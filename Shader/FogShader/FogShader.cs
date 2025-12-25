using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 선형 깊이 버퍼 기반 안개 효과를 구현한 Post-Processing 셰이더입니다.
    /// Linear, Exponential, Exponential Squared 세 가지 안개 모드를 지원합니다.
    /// </summary>
    public class FogShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\FogShader\fog.vert";
        const string FRAGMENT_FILE = @"\Shader\FogShader\fog.frag";

        // 유니폼 위치 (캐싱)
        private int loc_colorTexture;
        private int loc_linearDepthTexture;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_fogStart;
        private int loc_fogEnd;
        private int loc_maxDistance;
        private int loc_fogType;

        /// <summary>
        /// FogShader의 생성자입니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        public FogShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 모든 유니폼 위치를 가져옵니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            loc_colorTexture = GetUniformLocation("colorTexture");
            loc_linearDepthTexture = GetUniformLocation("linearDepthTexture");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_fogStart = GetUniformLocation("fogStart");
            loc_fogEnd = GetUniformLocation("fogEnd");
            loc_maxDistance = GetUniformLocation("maxDistance");
            loc_fogType = GetUniformLocation("fogType");
        }

        /// <summary>
        /// 셰이더 속성들을 바인딩합니다.
        /// 이 셰이더는 버텍스 데이터 없이 gl_VertexID만 사용하므로 바인딩 불필요
        /// </summary>
        protected override void BindAttributes()
        {
            // 풀스크린 삼각형은 gl_VertexID로 생성하므로 속성 바인딩 없음
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 원본 색상 텍스처를 바인딩합니다. (MRT location 0)
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="textureId">텍스처 ID</param>
        public void LoadColorTexture(TextureUnit textureUnit, uint textureId)
        {
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(loc_colorTexture, (int)textureUnit - (int)TextureUnit.Texture0);
        }

        /// <summary>
        /// 선형 깊이 텍스처를 바인딩합니다. (MRT location 1)
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="textureId">텍스처 ID</param>
        public void LoadLinearDepthTexture(TextureUnit textureUnit, uint textureId)
        {
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(loc_linearDepthTexture, (int)textureUnit - (int)TextureUnit.Texture0);
        }

        /// <summary>
        /// 안개 색상을 설정합니다.
        /// </summary>
        /// <param name="color">안개 색상 (RGB)</param>
        public void LoadFogColor(Vertex3f color)
        {
            Gl.Uniform3(loc_fogColor, color.x, color.y, color.z);
        }

        /// <summary>
        /// 안개 밀도를 설정합니다. (Exponential/Exponential Squared 모드에서 사용)
        /// </summary>
        /// <param name="density">안개 밀도 (권장값: 0.0001 ~ 0.001)</param>
        public void LoadFogDensity(float density)
        {
            Gl.Uniform1(loc_fogDensity, density);
        }

        /// <summary>
        /// 안개 시작/끝 거리를 설정합니다. (Linear 모드에서 사용)
        /// </summary>
        /// <param name="start">안개 시작 거리</param>
        /// <param name="end">안개 끝 거리</param>
        public void LoadFogRange(float start, float end)
        {
            Gl.Uniform1(loc_fogStart, start);
            Gl.Uniform1(loc_fogEnd, end);
        }

        /// <summary>
        /// 정규화에 사용된 최대 거리를 설정합니다.
        /// 셰이더에서 vViewPos.z / maxDistance로 정규화했다면 같은 값 사용
        /// </summary>
        /// <param name="maxDistance">최대 거리 (기본값: 10000.0)</param>
        public void LoadMaxDistance(float maxDistance)
        {
            Gl.Uniform1(loc_maxDistance, maxDistance);
        }

        /// <summary>
        /// 안개 타입을 설정합니다.
        /// </summary>
        /// <param name="type">0: Linear, 1: Exponential, 2: Exponential Squared</param>
        public void LoadFogType(int type)
        {
            Gl.Uniform1(loc_fogType, type);
        }
    }
}