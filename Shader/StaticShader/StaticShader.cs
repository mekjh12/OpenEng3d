using Common;

namespace Shader
{
    /// <summary>
    /// 정적 셰이더 프로그램을 관리하는 클래스입니다.
    /// MVP 변환, 텍스처링, 포그 효과 등 기본적인 렌더링 기능을 제공하며,
    /// 버텍스와 프래그먼트 셰이더를 로드하고 컴파일하여 OpenGL 프로그램으로 관리합니다.
    /// </summary>
    /// <remarks>
    /// ShaderProgram을 상속받아 구현되었으며, 셰이더 파일은 지정된 경로에서 로드됩니다.
    /// 유니폼 변수들은 UNIFORM_NAME 열거형을 통해 관리되며, 각 변수는 셰이더 내에서 특정 용도로 사용됩니다.
    /// </remarks>
    public class StaticShader : ShaderProgram<StaticShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 셰이더 프로그램에서 사용되는 유니폼 변수들의 식별자를 정의하는 열거형입니다.
        /// 각 유니폼은 셰이더 내에서 특정한 렌더링 속성을 제어하는데 사용됩니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>모델-뷰-프로젝션 결합 행렬 (Model-View-Projection Matrix)</summary>
            mvp,
            /// <summary>월드 공간 변환을 위한 모델 행렬</summary>
            model,
            /// <summary>카메라 변환을 위한 뷰 행렬</summary>
            view,
            /// <summary>투영 변환을 위한 프로젝션 행렬</summary>
            proj,

            /// <summary>화면 뷰포트의 크기 (width, height)</summary>
            viewport_size,

            /// <summary>뷰 공간에서의 카메라 위치</summary>
            viewPos,

            /// <summary>텍스처 매핑 사용 여부를 결정하는 플래그</summary>
            isTextured,

            /// <summary>정점 별 색상 속성 사용 여부를 결정하는 플래그</summary>
            isAttribColored,

            /// <summary>객체의 기본 색상 값</summary>
            color,

            /// <summary>월드 공간에서의 카메라 위치</summary>
            camPos,

            /// <summary>안개 효과에 사용 유무</summary>
            isFogEnable,

            /// <summary>안개 효과에 사용될 색상 값</summary>
            fogColor,

            /// <summary>안개 효과의 농도 값</summary>
            fogDensity,

            /// <summary>안개 평면의 방정식 계수 (a, b, c, d)</summary>
            fogPlane,

            /// <summary>3D 모델에 적용될 텍스처 유닛</summary>
            modelTexture,

            /// <summary>깊이 정보를 저장하는 텍스처 유닛</summary>
            depthTexture,

            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        /// <summary>버텍스 셰이더 파일의 상대 경로</summary>
        const string VERTEX_FILE = @"\Shader\StaticShader\static.vert";

        /// <summary>프래그먼트 셰이더 파일의 상대 경로</summary>
        const string FRAGMENT_FILE = @"\Shader\StaticShader\static.frag";

        /// <summary>
        /// StaticShader 클래스의 생성자입니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 기본 경로</param>
        public StaticShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더 프로그램의 애트리뷰트를 바인딩합니다.
        /// 정점 위치, 텍스처 좌표, 색상 데이터를 각각의 위치에 바인딩합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "textureCoords");
            base.BindAttribute(2, "color");
        }

        /// <summary>
        /// 모든 유니폼 변수의 위치를 가져옵니다.
        /// UNIFORM_NAME 열거형에 정의된 모든 유니폼 변수의 위치를 조회합니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }
    }
}