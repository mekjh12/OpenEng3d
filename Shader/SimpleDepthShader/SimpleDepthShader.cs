using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 깊이맵 생성을 위한 간단한 셰이더입니다.
    /// 정점의 위치만을 처리하여 깊이값을 계산합니다.
    /// </summary>
    public class SimpleDepthShader : ShaderProgram<SimpleDepthShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 셰이더의 유니폼 변수 식별자입니다.
        /// </summary>
        public enum UNIFORM_NAME
        {
            /// <summary>모델 변환 행렬</summary>
            model,
            /// <summary>뷰 변환 행렬</summary>
            view,
            /// <summary>투영 변환 행렬</summary>
            proj,
            /// <summary>유니폼 변수의 총 개수</summary>
            Count
        }

        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\SimpleDepthShader\simple.vert";
        /// <summary>프래그먼트 셰이더 파일 경로 (빈 셰이더)</summary>
        const string FRAGMENT_FILE = @"\Shader\SimpleDepthShader\null.frag";

        /// <summary>
        /// 간단한 깊이맵 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public SimpleDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// 위치(position) 애트리뷰트만 사용합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

    }
}