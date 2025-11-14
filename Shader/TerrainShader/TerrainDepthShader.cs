using OpenGL;
using Shader;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형의 깊이맵 생성을 위한 셰이더입니다.
    /// 테셀레이션을 사용하여 지형의 높이맵을 처리하고 깊이값만 계산합니다.
    /// </summary>
    public class TerrainDepthShader : ShaderProgram<TerrainDepthShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 셰이더의 유니폼 변수 식별자입니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>높이 스케일</summary>
            heightScale,
            /// <summary>모델 변환 행렬</summary>
            model,
            /// <summary>뷰 변환 행렬</summary>
            view,
            /// <summary>투영 변환 행렬</summary>
            proj,
            /// <summary>지형의 높이맵 텍스처</summary>
            gHeightMap,
            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        /// <summary>지오메트리 셰이더 파일 경로 (미사용)</summary>
        const string GEOMETRY_FILE = "";
        /// <summary>테셀레이션 컨트롤 셰이더 파일 경로</summary>
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.tcs.glsl";
        /// <summary>테셀레이션 평가 셰이더 파일 경로</summary>
        const string TES_FILE = @"\Shader\TerrainShader\terrain.tes.glsl";
        /// <summary>프래그먼트 셰이더 파일 경로 (빈 셰이더)</summary>
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\null.frag";

        /// <summary>
        /// 지형 깊이맵 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public TerrainDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            TcsFilename = projectPath + TCS_FILE;
            TesFilename = projectPath + TES_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 유니폼 변수 위치를 가져옵니다.
        /// 변환 행렬과 높이맵 텍스처의 위치를 조회합니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// 테셀레이션 셰이더에서 처리하므로 버텍스 셰이더의 애트리뷰트는 비활성화됩니다.
        /// </summary>
        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        #region Uniform 전달을 위한 기본 함수

        public void LoadTexture(UNIFORM_NAME textureUniformName, TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            base.LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);

        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        #endregion

    }
}
