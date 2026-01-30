using Common;
using OpenGL;

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
        private int loc_heightMap;
        private int loc_waterBufferWrite;
        private int loc_waterBufferRead;

        private int loc_target;
        private int loc_radius;
        private int loc_mode;
        private int loc_flowVelocityConstant;
        private int loc_rainWaterAmount;
        private int loc_evaporationBaseRate;


        public WaterFlowComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_heightMap = GetUniformLocation("heightMap");
            loc_waterBufferWrite = GetUniformLocation("waterBufferWrite");
            loc_waterBufferRead = GetUniformLocation("waterBufferRead");

            loc_target = GetUniformLocation("target");
            loc_radius = GetUniformLocation("radius");
            loc_mode = GetUniformLocation("mode");
            loc_flowVelocityConstant = GetUniformLocation("flowVelocityConstant");
            loc_rainWaterAmount = GetUniformLocation("rainWaterAmount");
            loc_evaporationBaseRate = GetUniformLocation("evaporationBaseRate");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        public void SetEvaporationBaseRate(float rate)
        {
            Gl.Uniform1(loc_evaporationBaseRate, rate);
        }

        public void SetRainWaterAmount(float amount)
        {
            Gl.Uniform1(loc_rainWaterAmount, amount);
        }

        public void SetFlowVelocityConstant(float constant)
        {
            Gl.Uniform1(loc_flowVelocityConstant, constant);
        }

        public void SetMode(int mode)
        {
            Gl.Uniform1(loc_mode, mode);
        }

        public void LoadUniforms(Vertex2f target, float radius)
        {
            Gl.Uniform1(loc_radius, radius);
            Gl.Uniform2(loc_target, target.x, target.y);
        }

        public void BindBuffers(uint heightMap, uint readBuffer, uint writeBuffer, uint supportBuffer, uint fluxBuffer)
        {
            // 쉐이더 내부의 binding = 0, 1, 2와 일치해야 함
            Gl.BindImageTexture(0, heightMap, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.R32f);
            Gl.BindImageTexture(1, readBuffer, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba32f);
            Gl.BindImageTexture(2, writeBuffer, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba32f);
            Gl.BindImageTexture(3, supportBuffer, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba32f);
            Gl.BindImageTexture(4, fluxBuffer, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba32f);
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