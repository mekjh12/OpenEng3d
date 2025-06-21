using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    ///     지형 렌더링을 위한 테셀레이션 셰이더
    /// </summary>
    /// <remarks>
    ///     높이맵 기반의 지형을 동적 LOD(Level of Detail)로 처리
    ///     테셀레이션을 통한 적응형 지형 세분화 구현
    /// </remarks>
    public class TerrainChunkShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        /// <summary>
        ///     셰이더의 유니폼 변수 식별자
        /// </summary>
        /// <remarks>
        ///     각 유니폼 변수는 셰이더 프로그램에서 사용되는 전역 변수를 나타냄
        /// </remarks>
        public enum UNIFORM_NAME
        {
            /// <summary>고해상도 높이맵</summary>
            heightMapHighRes,
            /// <summary>저해상도 높이맵</summary>
            heightMapLowRes,
            /// <summary>해상도 전환 블렌딩 계수</summary>
            blendFactor,
            /// <summary>높이 스케일 팩터</summary>
            heightScale,
            /// <summary>청크 분할 개수</summary>
            chunkSeperateCount,
            /// <summary>청크 크기</summary>
            chunkSize,
            /// <summary>청크 좌표</summary>
            chunkCoord,
            /// <summary>청크 오프셋</summary>
            chunkOffset,
            /// <summary>모델 변환 행렬</summary>
            model,
            /// <summary>투영 변환 행렬</summary>
            proj,
            /// <summary>뷰 변환 행렬</summary>
            view,
            /// <summary>카메라 위치</summary>
            camPos,
            /// <summary>지형 기본 색상</summary>
            color,
            /// <summary>텍스처 사용 여부</summary>
            isTextured,
            /// <summary>상세 맵 사용 여부</summary>
            gIsDetailMap,
            /// <summary>상세 맵</summary>
            gDetailMap,
            /// <summary>식생 텍스처 맵</summary>
            gVegetationMap,
            /// <summary>반전된 광원 방향 벡터</summary>
            gReversedLightDir,
            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        /// <summary>셰이더 파일 경로 상수</summary>
        private const string VERTEX_FILE = @"\Shader\TerrainShader\chunk.vert";
        private const string TCS_FILE = @"\Shader\TerrainShader\chunk.tcs.glsl";
        private const string TES_FILE = @"\Shader\TerrainShader\chunk.tes.glsl";
        private const string GEOMETRY_FILE = "";
        private const string FRAGMENT_FILE = @"\Shader\TerrainShader\chunk.frag";

        /// <summary>
        ///     지형 테셀레이션 셰이더를 초기화
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        /// <remarks>
        ///     필요한 모든 셰이더 파일을 로드하고 컴파일하여 셰이더 프로그램을 생성
        /// </remarks>
        public TerrainChunkShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            TcsFilename = projectPath + TCS_FILE;
            TesFilename = projectPath + TES_FILE;
            InitCompileShader();
        }

        /// <summary>
        ///     셰이더의 입력 애트리뷰트를 바인딩
        /// </summary>
        /// <remarks>
        ///     정점 위치 데이터를 location 0에 바인딩
        /// </remarks>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");    // 위치
        }

        /// <summary>
        ///     셰이더의 모든 유니폼 변수 위치를 가져옴
        /// </summary>
        /// <remarks>
        ///     UNIFORM_NAME enum에 정의된 모든 유니폼 변수의 위치를 초기화
        /// </remarks>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        #region Uniform 로딩 함수
        /// <summary>
        ///     텍스처 유니폼을 로드
        /// </summary>
        /// <param name="textureUniformName">텍스처 유니폼 이름</param>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="texture">텍스처 ID</param>
        public void LoadTexture(UNIFORM_NAME textureUniformName, TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            base.LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>유니폼 변수 로드 메서드들</summary>
        /// <param name="uniform">유니폼 변수 이름</param>
        /// <param name="value">설정할 값</param>
        public void LoadUniform(UNIFORM_NAME uniform, float value) => base.LoadFloat(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, int value) => base.LoadInt(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, bool value) => base.LoadBoolean(_location[uniform.ToString()], value);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex4f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex3f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex3i vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Vertex2f vec) => base.LoadVector(_location[uniform.ToString()], vec);
        public void LoadUniform(UNIFORM_NAME uniform, Matrix4x4f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        public void LoadUniform(UNIFORM_NAME uniform, Matrix3x3f mat) => base.LoadMatrix(_location[uniform.ToString()], mat);
        #endregion
    }
}