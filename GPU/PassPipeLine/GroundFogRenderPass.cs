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
        private BillboardInstancedShader _billboardShader;
        private GPUInstancedShader _instancedShader;

        private CrossBillboardAtlasGenerator _generator;  // 크로스 빌보드 아틀라스 생성기
        private CrossBillboardData[] _billboardData;      // 배치별 크로스 빌보드 데이터
        private uint _fogTextureID;

        public GroundFogRenderPass(string name, string projPath) : base(name, projPath)
        {
            if (!ShaderManager.Instance.HasShader("BillboardInstancedShader"))
                ShaderManager.Instance.AddShader(new BillboardInstancedShader(StrRes.PROJECT_PATH));
            if (!ShaderManager.Instance.HasShader("GPUInstancedShader"))
                ShaderManager.Instance.AddShader(new GPUInstancedShader(StrRes.PROJECT_PATH));

            _billboardShader = ShaderManager.Instance.GetShader<BillboardInstancedShader>();
            _instancedShader = ShaderManager.Instance.GetShader<GPUInstancedShader>();
        }

        public override void Initialize(Camera camera, ModelBatchManager batchManager,
            float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f)
        {
            // 기본 초기화(반드시 호출)
            base.Initialize(camera, batchManager, distance0, distance1, distance2);

            // 
        }

        public override void CreateUnifiedIndirectBuffer()
        {

        }

        public void SetFogTexture(uint textureID)
        {
            _fogTextureID = textureID;
        }

        public override void RenderBatchLod0(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            /*
            // 알파 블렌딩 설정
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.DepthMask(false);
            Gl.Disable(EnableCap.CullFace);

            _billboardShader.Bind();
            {
                // VP 행렬
                _billboardShader.LoadVPMatrix(camera.VPMatrix);

                // 카메라 위치
                _billboardShader.LoadCameraPosition(
                    camera.Position.x,
                    camera.Position.y,
                    camera.Position.z
                );

                // 배치 오프셋
                _billboardShader.LoadBatchStartOffset(_batch.StartIndex);

                // 노이즈 텍스처
                _billboardShader.LoadFogTexture((int)_fogTextureID);

                // 연무 파라미터
                _billboardShader.LoadFogColor(0.8f, 0.85f, 0.9f);
                _billboardShader.LoadFogDensity(0.6f);
                _billboardShader.LoadAlphaThreshold(0.05f);

                // SSBO 바인딩
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

                // Point 렌더링 (Geometry Shader가 확장)
                DrawArraysIndirect(_batch.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Points);
            }
            _billboardShader.Unbind();

            // 상태 복원
            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);

            */

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            {
                _instancedShader.LoadBatchStartOffset(batch.StartIndex);
                _instancedShader.LoadTextureArray(batch.Model.TextureIDArray);
                _instancedShader.LoadEnableDebug(false);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                DrawArraysIndirect(batch.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();
        }

        public override void RenderBatchLod1(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            {
                _instancedShader.LoadBatchStartOffset(batch.StartIndex);
                _instancedShader.LoadTextureArray(batch.Model.TextureIDArray);
                _instancedShader.LoadEnableDebug(false);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                DrawArraysIndirect(batch.VAO, cmdStartIndex, 1, _visibleIndicesSSBO_LOD1, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();
        }

        public override void RenderBatchLod2(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD2);

            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            {
                _instancedShader.LoadBatchStartOffset(batch.StartIndex);
                _instancedShader.LoadTextureArray(batch.Model.TextureIDArray);
                _instancedShader.LoadEnableDebug(false);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                DrawArraysIndirect(batch.VAO, cmdStartIndex, 2, _visibleIndicesSSBO_LOD2, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();

            /*
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _aabbSSBO);

            CrossBillboardData crossBillboardData = _billboardData[batchID];

            // LOD2: 크로스 빌보드 렌더링
            Gl.Disable(EnableCap.CullFace);
            _crossBillboardInstanceShader.Bind();
            {
                _crossBillboardInstanceShader.LoadCurrentBatchID(batchID);
                _crossBillboardInstanceShader.LoadBatchStartOffset(batch.StartIndex);
                _crossBillboardInstanceShader.LoadAtlasTexture(crossBillboardData.AtlasTexture.TextureID);
                _crossBillboardInstanceShader.LoadMaxDepthDistance(10000.0f);
                _crossBillboardInstanceShader.UseTexture(true);
                DrawArraysIndirect(_point.VAO, cmdStartIndex, 2, _visibleIndicesSSBO_LOD2);
            }
            _crossBillboardInstanceShader.Unbind();
            */
        }

        public override void RenderBatchLod3(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            
        }
    }
}
