using Common;
using OpenGL;
using System;

namespace Shader
{
    public class FlowAccumulationComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\flow_accumulation.comp";

        private int loc_width;
        private int loc_height;
        private int loc_maxIterations;
        private int loc_minSlopeThreshold;
        private int loc_searchRadius;
        private int loc_minStartHeight;      // ⭐ 추가
        private int loc_tracesPerPoint;      // ⭐ 추가

        public FlowAccumulationComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_width = GetUniformLocation("u_Width");
            loc_height = GetUniformLocation("u_Height");
            loc_maxIterations = GetUniformLocation("u_MaxIterations");
            loc_minSlopeThreshold = GetUniformLocation("u_MinSlopeThreshold");
            loc_searchRadius = GetUniformLocation("u_SearchRadius");
            loc_minStartHeight = GetUniformLocation("u_MinStartHeight");      // ⭐ 추가
            loc_tracesPerPoint = GetUniformLocation("u_TracesPerPoint");      // ⭐ 추가
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 파라미터 로드
        /// </summary>
        public void LoadParams(
            int width,
            int height,
            int maxIterations = 1000,
            float minSlopeThreshold = 0.0001f,
            int searchRadius = 5,
            float minStartHeight = 0.6f,      // ⭐ 추가
            int tracesPerPoint = 5)           // ⭐ 추가
        {
            LoadUniform1i(loc_width, width);
            LoadUniform1i(loc_height, height);
            LoadUniform1i(loc_maxIterations, maxIterations);
            LoadUniform1f(loc_minSlopeThreshold, minSlopeThreshold);
            LoadUniform1i(loc_searchRadius, searchRadius);
            LoadUniform1f(loc_minStartHeight, minStartHeight);      // ⭐ 추가
            LoadUniform1i(loc_tracesPerPoint, tracesPerPoint);      // ⭐ 추가
        }

        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}