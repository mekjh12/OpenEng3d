using Common;

namespace Shader
{
    /// <summary>
    /// 컴퓨트 셰이더를 사용하는 HZB 밉맵 생성 셰이더 클래스
    /// </summary>
    public class HzbComputeShader : ShaderProgram<HzbComputeShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            inputSize,   // 입력 이미지 크기
            outputDepth,    // 현재 생성 중인 밉 레벨
            Count
        }

        const string COMPUTE_FILE = @"\Shader\TerrainShader\hzb_compute.comp";

        public HzbComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 BindAttributes가 필요 없음
        }
    }
}
