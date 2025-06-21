using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 기본적인 Unlit 셰이더를 구현한 클래스입니다.
    /// 텍스처와 MVP 변환을 지원하며 조명 계산은 수행하지 않습니다.
    /// </summary>
    public class UnlitShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\UnlitShader\unlit.vert";
        const string FRAGMENT_FILE = @"\Shader\UnlitShader\unlit.frag";

        /// <summary>
        /// 셰이더에서 사용되는 유니폼 변수들을 정의합니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>Model-View-Projection 행렬</summary>
            mvp,
            /// <summary>모델 텍스처</summary> 
            modelTexture,
            /// <summary>유니폼 변수 개수</summary>
            Count
        }

        /// <summary>
        /// UnlitShader의 생성자입니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        /// <exception cref="FileNotFoundException">셰이더 파일을 찾을 수 없는 경우</exception>
        public UnlitShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
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
        }
    }
}