using OpenGL;
using System.IO;
using System;
using Common;

namespace Shader
{
    public class VegetationBillboardShader : ShaderProgram<VegetationBillboardShader.UNIFORM_NAME>
    {
        // 쉐이더 파일 경로 상수 정의 (Path.Combine 사용하여 크로스 플랫폼 호환성 확보)
        private static readonly string VERTEX_FILE = "Shader/VegetationBillboardShader/vegetation.vert";
        private static readonly string FRAGMENT_FILE = "Shader/VegetationBillboardShader/vegetation.frag";
        private static readonly string GEOMETRY_FILE = "Shader/VegetationBillboardShader/vegetation.gem.glsl";

        // Uniform 변수들을 용도별로 그룹화한 열거형
        public enum UNIFORM_NAME
        {
            gColorMap,
            gCameraPos,
            treeSize,
            vp,

            Count          // 전체 Uniform 개수
        }

        /// <summary>
        /// ImpostorShader 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        public VegetationBillboardShader(string projectPath) : base()
        {
            _name = GetType().Name;

            // Path.Combine을 사용하여 OS에 독립적인 경로 생성
            VertFileName = Path.Combine(projectPath, VERTEX_FILE);
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