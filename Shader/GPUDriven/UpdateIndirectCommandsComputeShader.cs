using Common;
using Common.Abstractions;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// Indirect Command Buffer를 GPU에서 업데이트하는 Compute Shader
    /// visibleCounts를 읽어서 각 command의 instanceCount 필드를 채웁니다.
    /// </summary>
    public class UpdateIndirectCommandsComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\UpdateIndirectCommands.comp";

        // 유니폼 위치
        private int loc_numBatches;
        private int loc_batchCommandStartIndices;

        public UpdateIndirectCommandsComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_numBatches = GetUniformLocation("numBatches");
            loc_batchCommandStartIndices = GetUniformLocation("batchCommandStartIndices");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 attribute 없음
        }

        /// <summary>
        /// 총 배치 개수
        /// </summary>
        public void LoadNumBatches(uint numBatches)
        {
            Gl.Uniform1(loc_numBatches, numBatches);
        }

        /// <summary>
        /// 각 배치의 command 시작 인덱스 배열
        /// </summary>
        public void LoadBatchCommandStartIndices(uint[] indices)
        {
            Gl.Uniform1(loc_batchCommandStartIndices, indices);
        }
    }
}