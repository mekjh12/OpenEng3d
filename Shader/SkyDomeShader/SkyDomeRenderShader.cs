using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 스카이돔에 대기 텍스처를 렌더링하기 위한 셰이더 프로그램입니다.
    /// </summary>
    public class SkyDomeRenderShader : ShaderProgram<SkyDomeRenderShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 스카이돔 셰이더의 유니폼 변수들을 정의합니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>모델 변환 행렬</summary>
            model,
            /// <summary>뷰 변환 행렬</summary>
            view,
            /// <summary>투영 변환 행렬</summary>
            proj,
            /// <summary>하늘 텍스처</summary>
            skyTexture,
            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        // 셰이더 파일 경로 상수
        const string VERTEX_FILE = @"\Shader\SkyDomeShader\skydome.vert";
        const string FRAGMENT_FILE = @"\Shader\SkyDomeShader\skydome.frag";

        /// <summary>
        /// SkyDomeShader 클래스의 생성자입니다.
        /// 셰이더 파일을 로드하고 초기 컴파일을 수행합니다.
        /// </summary>
        /// <param name="projectPath">프로젝트의 루트 경로</param>
        public SkyDomeRenderShader(string projectPath) : base()
        {
            // 셰이더 이름을 현재 클래스 이름으로 설정
            _name = this.GetType().Name;

            // 셰이더 파일의 전체 경로 설정
            _vertFilename = projectPath + VERTEX_FILE;
            _fragFilename = projectPath + FRAGMENT_FILE;

            // 셰이더 컴파일 및 초기화
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더 프로그램의 버텍스 속성들을 바인딩합니다.
        /// 각 버텍스 속성에 대한 위치와 이름을 연결합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            // 위치와 텍스처 좌표를 각각 인덱스와 함께 바인딩
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
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