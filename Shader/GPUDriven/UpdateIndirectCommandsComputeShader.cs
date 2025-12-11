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

        // SSBO 바인딩 포인트
        private const int INDIRECT_COMMANDS_BINDING = 10;
        private const int VISIBLE_COUNTS_LOD0_BINDING = 11;
        private const int VISIBLE_COUNTS_LOD1_BINDING = 12;

        // 유니폼 위치
        private int loc_numBatches;
        private int loc_batchCommandStartIndices;
        private int loc_numModelsPerBatch;

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
            loc_numModelsPerBatch = GetUniformLocation("numModelsPerBatch");
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

        /// <summary>
        /// 각 배치의 모델 개수 배열
        /// </summary>
        public void LoadNumModelsPerBatch(uint[] counts)
        {
            Gl.Uniform1(loc_numModelsPerBatch, counts);
        }
    }
}