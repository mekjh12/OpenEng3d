using OpenGL;
using Shader;
using Common;

namespace Shader
{    
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다.
    /// 높이맵 기반의 지형을 동적 LOD로 처리합니다.
    /// </summary>
    public class TerrainImposterShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 셰이더의 유니폼 변수 식별자입니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>모델-뷰-투영 변환 행렬</summary>
            mvp,
            /// <summary>디테일맵 사용 여부</summary>
            gIsDetailMap,
            /// <summary>반전된 광원 방향</summary>
            gReversedLightDir,
            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        /// <summary>프래그먼트 셰이더 파일 경로</summary>
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\terrain.uniform.frag";
        /// <summary>지오메트리 셰이더 파일 경로 (미사용)</summary>
        const string GEOMETRY_FILE = "";
        /// <summary>테셀레이션 컨트롤 셰이더 파일 경로</summary>
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.uniform.tcs.glsl";
        /// <summary>테셀레이션 평가 셰이더 파일 경로</summary>
        const string TES_FILE = @"\Shader\TerrainShader\terrain.uniform.tes.glsl";

        /// <summary>
        /// 지형 테셀레이션 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public TerrainImposterShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            TcsFilename = projectPath + TCS_FILE;
            TesFilename = projectPath + TES_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");    // 위치
            base.BindAttribute(1, "texCoord");    // 텍스처 좌표
            base.BindAttribute(2, "color");       // 색상
        }

        /// <summary>
        /// 셰이더의 모든 유니폼 변수 위치를 가져옵니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        #region Uniform 로딩 함수
        /// <summary>텍스처 유니폼을 로드합니다.</summary>
        public void LoadTexture(UNIFORM_NAME textureUniformName, TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            base.LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>실수형 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);
        /// <summary>정수형 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);
        /// <summary>불리언형 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);
        /// <summary>4차원 벡터 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, Vertex4f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        /// <summary>3차원 벡터 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        /// <summary>2차원 벡터 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        /// <summary>4x4 행렬 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        /// <summary>3x3 행렬 유니폼을 로드합니다.</summary>
        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        #endregion

    }
}
