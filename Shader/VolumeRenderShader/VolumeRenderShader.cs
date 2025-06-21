using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 볼륨 렌더링을 위한 셰이더 클래스
    /// 구름, 연기 같은 반투명 볼륨을 레이 마칭 기법으로 렌더링
    /// </summary>
    public class VolumeRenderShader : ShaderProgram<VolumeRenderShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 변환 행렬 유니폼
            model,              // 모델 변환 행렬
            view,               // 뷰 변환 행렬
            proj,               // 투영 변환 행렬

            // 레이 마칭 유니폼
            viewport_size,      // 뷰포트 크기
            focal_length,       // 초점 거리
            aspect_ratio,       // 화면 비율
            gamma,              // 감마 보정 값
            step_length,        // 레이 마칭 스텝 길이
            ray_origin,         // 레이 시작점
            volume,             // 볼륨 텍스처

            // 총 유니폼 개수
            Count
        }

        const string VERTEX_FILE = @"\Shader\VolumeRenderShader\vr.vert";
        const string FRAGMENT_FILE = @"\Shader\VolumeRenderShader\vr.frag";

        public VolumeRenderShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            _vertFilename = projectPath + VERTEX_FILE;
            _fragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            // 유니폼 변수 이름을 이용하여 위치 찾기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }
    }
}
