using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 물리 기반 대기 산란을 구현하는 셰이더 프로그램입니다.
    /// Rayleigh와 Mie 산란을 모두 고려하여 현실적인 하늘을 렌더링합니다.
    /// </summary>
    /// <remarks>
    /// 이 셰이더는 다음과 같은 주요 기능을 제공합니다:
    /// - 물리적으로 정확한 대기 산란 시뮬레이션
    /// - 태양 위치에 따른 동적 하늘 색상 변화
    /// - 다중 산란 근사를 위한 광선 샘플링
    /// - HDR 렌더링과 톤 매핑
    /// </remarks>
    public class AtmosphereShader : ShaderProgram<AtmosphereShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 대기 산란 셰이더에서 사용되는 유니폼 변수들을 정의합니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            // 3D 변환 관련 유니폼
            /// <summary>모델 변환 행렬</summary>
            model,
            /// <summary>모델-뷰-프로젝션 결합 행렬</summary>
            mvp,

            // 카메라와 광원 위치 관련 유니폼
            /// <summary>관찰자(카메라)의 위치</summary>
            viewPos,
            /// <summary>태양의 위치 (광원 방향)</summary>
            sunPos,

            // 샘플링 관련 유니폼
            /// <summary>시점 광선의 샘플링 수</summary>
            viewSamples,
            /// <summary>광원 광선의 샘플링 수</summary>
            lightSamples,

            // 물리적 매개변수 관련 유니폼
            /// <summary>태양 광원의 강도</summary>
            I_sun,
            /// <summary>행성의 반지름 (미터 단위)</summary>
            R_e,
            /// <summary>대기권의 반지름 (미터 단위)</summary>
            R_a,

            // 산란 계수 관련 유니폼
            /// <summary>Rayleigh 산란 계수</summary>
            beta_R,
            /// <summary>Mie 산란 계수</summary>
            beta_M,
            /// <summary>Rayleigh 산란의 스케일 높이</summary>
            H_R,
            /// <summary>Mie 산란의 스케일 높이</summary>
            H_M,

            // 렌더링 매개변수 관련 유니폼
            /// <summary>Mie 산란의 이방성 계수 - 매질의 비등방성을 정의</summary>
            g,
            /// <summary>톤 매핑 적용 강도</summary>
            toneMappingFactor,

            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        // 셰이더 파일 경로 상수
        /// <summary>버텍스 셰이더 파일의 상대 경로</summary>
        const string VERTEX_FILE = @"\Shader\AtmosphereShader\atmosphere.vert";
        /// <summary>프래그먼트 셰이더 파일의 상대 경로</summary>
        const string FRAGMENT_FILE = @"\Shader\AtmosphereShader\atmosphere.frag";

        /// <summary>
        /// AtmosphereShader 클래스의 생성자입니다.
        /// 셰이더 파일을 로드하고 초기 컴파일을 수행합니다.
        /// </summary>
        /// <param name="projectPath">프로젝트의 루트 경로</param>
        public AtmosphereShader(string projectPath) : base()
        {
            // 셰이더 이름을 현재 클래스 이름으로 설정
            _name = this.GetType().Name;

            // 셰이더 파일의 전체 경로 설정
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            // 셰이더 컴파일 및 초기화
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더 프로그램의 버텍스 속성들을 바인딩합니다.
        /// 각 버텍스 속성에 대한 위치와 이름을 연결합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            // 위치, 법선, 텍스처 좌표를 각각 인덱스와 함께 바인딩
            base.BindAttribute(0, "position");
            //base.BindAttribute(1, "normal");
            //base.BindAttribute(2, "texCoord");
        }

        /// <summary>
        /// 모든 유니폼 변수의 위치를 가져옵니다.
        /// UNIFORM_NAME 열거형에 정의된 모든 유니폼 변수의 위치를 조회합니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            // 모든 유니폼 변수에 대해 반복하며 위치 정보 가져오기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }
    }
}