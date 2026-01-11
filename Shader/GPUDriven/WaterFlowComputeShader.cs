using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// Water Flow Accumulation Compute Shader
    /// 높이맵 기반 물 흐름 시뮬레이션
    /// </summary>
    public class WaterFlowComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\water_flow.comp";

        // Uniform 위치 캐싱
        private int loc_deltaWater;
        private int loc_flowRate;
        private int loc_evaporationRate;
        private int loc_width;
        private int loc_height;
        private int loc_flatThreshold;      // ⭐ 추가
        private int loc_longRangeRadius;    // ⭐ 추가
        private int loc_maxWaterDepth;  // ⭐ 추가

        public WaterFlowComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_deltaWater = GetUniformLocation("u_DeltaWater");
            loc_flowRate = GetUniformLocation("u_FlowRate");
            loc_evaporationRate = GetUniformLocation("u_EvaporationRate");
            loc_width = GetUniformLocation("u_Width");
            loc_height = GetUniformLocation("u_Height");
            loc_flatThreshold = GetUniformLocation("u_FlatThreshold");       // ⭐ 추가
            loc_longRangeRadius = GetUniformLocation("u_LongRangeRadius");   // ⭐ 추가
            loc_maxWaterDepth = GetUniformLocation("u_MaxWaterDepth");  // ⭐
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 시뮬레이션 파라미터 로드
        /// </summary>
        public void LoadSimulationParams(
            float deltaWater,
            float flowRate,
            float evaporationRate,
            float maxWaterDepth = 50.0f)  // ⭐ 추가
        {
            LoadUniform1f(loc_deltaWater, deltaWater);
            LoadUniform1f(loc_flowRate, flowRate);
            LoadUniform1f(loc_evaporationRate, evaporationRate);
            LoadUniform1f(loc_maxWaterDepth, maxWaterDepth);  // ⭐
        }

        /// <summary>
        /// 텍스처 크기 로드
        /// </summary>
        public void LoadTextureSize(int width, int height)
        {
            LoadUniform1i(loc_width, width);
            LoadUniform1i(loc_height, height);
        }

        /// <summary>
        /// Compute Shader Dispatch
        /// </summary>
        public void Dispatch(int width, int height)
        {
            // 16x16 워크그룹 기준
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}