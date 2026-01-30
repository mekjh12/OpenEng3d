using BillBoard;
using Common;
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using Ui3d;

namespace GPUDriven
{
    public class GroundFogRenderPass : RenderPassPipeLine, IRenderPass
    {
        private GroundFogCrossBillboardShader _fogShader;
        private GPUInstancedShader _instancedShader;

        private uint _fogTextureID;

        public GroundFogRenderPass(string name, string projPath) : base(name, projPath)
        {
            if (!ShaderManager.Instance.HasShader("GroundFogCrossBillboardShader"))
                ShaderManager.Instance.AddShader(new GroundFogCrossBillboardShader(StrRes.PROJECT_PATH));
            if (!ShaderManager.Instance.HasShader("GPUInstancedShader"))
                ShaderManager.Instance.AddShader(new GPUInstancedShader(StrRes.PROJECT_PATH));

            _fogShader = ShaderManager.Instance.GetShader<GroundFogCrossBillboardShader>();
            _instancedShader = ShaderManager.Instance.GetShader<GPUInstancedShader>();
        }

        public override void Initialize(Camera camera, ModelBatchManager batchManager,
            float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f)
        {
            // 기본 초기화(반드시 호출)
            base.Initialize(camera, batchManager, distance0, distance1, distance2);
        }

        /// <summary>
        /// 통합 Indirect Command Buffer 생성 및 초기화, LOD0, LOD1, LOD2, LOD3 커맨드를 순차적으로 배치
        /// </summary>
        public override void CreateUnifiedIndirectBuffer()
        {
            int bufferSize = (int)(_batchedModelCount * BYTES_PER_BATCH);

            if (_indirectCommandBuffer < 0)
                _indirectCommandBuffer = (int)Gl.GenBuffer();

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, (uint)_indirectCommandBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, (uint)bufferSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, BINDING_INDIRECT_COMMAND, (uint)_indirectCommandBuffer);  // SSBO 바인딩(13)

            int commandOffset = 0;
            for (uint b = 0; b < _batchedModelCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                // LOD0 커맨드 (DrawArraysIndirect)
                BufferSubDataDrawArraysIndirectCommand(1, ref commandOffset);

                // LOD1 커맨드 (DrawArraysIndirect)
                BufferSubDataDrawArraysIndirectCommand((uint)batch.Model.VertexCount, ref commandOffset);

                // LOD2 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                BufferSubDataDrawArraysIndirectCommand(1, ref commandOffset);

                // LOD3 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                BufferSubDataDrawArraysIndirectCommand(1, ref commandOffset);
            }
        }



        public void SetFogTexture(uint textureID)
        {
            _fogTextureID = textureID;
        }

        public override void RenderBatchLod0(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

            // 알파 블렌딩 설정
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.DepthMask(false);
            Gl.Disable(EnableCap.CullFace);

            _fogShader.Bind();
            {
                // 카메라 가로, 세로
                _fogShader.LoadScreenSize(camera.Width, camera.Height);

                // 배치 오프셋
                _fogShader.LoadBatchStartOffset(batch.StartIndex);

                // 안개 텍스처 바인딩 (텍스처 유닛 0)
                _fogShader.LoadFogTexture( TextureUnit.Texture0, _fogTextureID);

                // 스트럭처 텍스처 바인딩 (텍스처 유닛 1)
                _fogShader.LoadStructureTexture( TextureUnit.Texture1, ShareBuffer.StructureBufferId);

                // 안개 파라미터
                _fogShader.LoadFogColor(0.8f, 0.85f, 0.9f);
                _fogShader.LoadFogDensity(0.6f);
                _fogShader.LoadAlphaThreshold(0.05f);

                // Point 렌더링 (Geometry Shader가 3개 쿼드로 확장)
                DrawArraysIndirect(batch.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Points);
            }
            _fogShader.Unbind();

            // 상태 복원
            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.CullFace);
        }

        public override void RenderBatchLod1(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
           

        }

        public override void RenderBatchLod2(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            

        }

        public override void RenderBatchLod3(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            
        }
    }
}
