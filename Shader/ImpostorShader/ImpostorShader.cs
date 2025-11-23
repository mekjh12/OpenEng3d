using OpenGL;
using System.IO;
using System;
using Common;

namespace Shader
{
    /// <summary>
    /// 임포스터(게임에서 멀리 있는 오브젝트를 최적화하여 렌더링하는 기법) 쉐이더를 구현하는 클래스
    /// </summary>
    public class ImpostorShader : ShaderProgram<ImpostorShader.UNIFORM_NAME>
    {
        // 쉐이더 파일 경로 상수 정의 (Path.Combine 사용하여 크로스 플랫폼 호환성 확보)
        private static readonly string VERTEx_FILE = "Shader/ImpostorShader/impostor.vert";
        private static readonly string FRAGMENT_FILE = "Shader/ImpostorShader/impostor.frag";
        private static readonly string GEOMETRY_FILE = "Shader/ImpostorShader/impostor.gem.glsl";

        // Uniform 변수들을 용도별로 그룹화한 열거형
        public enum UNIFORM_NAME
        {
            // 텍스처 아틀라스 관련
            atlasOffset,    // 아틀라스 내 오프셋 위치
            atlasSize,      // 아틀라스 전체 크기
            impostorAtlas,  // 임포스터 텍스처 아틀라스
            enableEdgeLine, // 테두리 렌더링 유무

            // 변환 행렬 관련
            model,          // 모델 변환 행렬
            vp,             // View-Projection 행렬

            // 위치 관련
            worldPosition,      // 월드 공간에서의 위치
            cameraPosition,     // 카메라 위치

            // 크기 관련
            individualSize, // 개별 임포스터의 크기

            // 경계 상자(AABB) 관련
            aabbSizeModel,          // 모델의 최소 경계점
            aabbCenterEntity,       // 물체 박스의 중심점

            Count          // 전체 Uniform 개수
        }

        /// <summary>
        /// ImpostorShader 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        public ImpostorShader(string projectPath) : base()
        {
            _name = GetType().Name;

            // Path.Combine을 사용하여 OS에 독립적인 경로 생성
            VertFileName = Path.Combine(projectPath, VERTEx_FILE);
            GeomFileName = Path.Combine(projectPath, GEOMETRY_FILE);
            FragFilename = Path.Combine(projectPath, FRAGMENT_FILE);

            InitCompileShader();
        }

        /// <summary>
        /// 버텍스 쉐이더 속성 바인딩
        /// </summary>
        protected override void BindAttributes()
        {
            // 현재는 위치만 바인딩. 필요시 UV나 노말 등 추가 가능
            base.BindAttribute(0, "position");
        }
    }
}