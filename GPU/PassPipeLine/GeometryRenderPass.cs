using BillBoard;
using Common.Abstractions;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;

namespace GPUDriven
{
    public class GeometryRenderPass : RenderPassPipeLine, IRenderPass
    {
        private GPUInstancedShader _instancedShader;                        // 메시 렌더링 셰이더
        private GPUDrivenImpostorShader _gpuDrivenImpostorShader;           // GPU 드리븐 임포스터 렌더링 셰이더
        private Dictionary<int, Vertex3i> _dicTextures;

        // 최적화용
        private BatchDescriptor _batchTemp;

        // ------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------

        public GeometryRenderPass(string name, string projPath) : base(name, projPath)
        {
            // 기본 세이더 초기화
            _instancedShader = new GPUInstancedShader(projPath);
            _gpuDrivenImpostorShader = new GPUDrivenImpostorShader(projPath);
        }

        public override void Initialize(Camera camera, ModelBatchManager batchManager,
                   float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f)
        {
            // 기본 초기화(반드시 호출)
            base.Initialize(camera, batchManager, distance0, distance1, distance2);
        }

        /// <summary>
        /// 통합 Indirect Command Buffer 생성 및 초기화
        /// LOD0, LOD1, LOD2, LOD3 커맨드를 순차적으로 배치
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
                BufferSubDataDrawArraysIndirectCommand(batch.VertexCount, ref commandOffset);

                // LOD1 커맨드 (DrawArraysIndirect)
                BufferSubDataDrawArraysIndirectCommand((uint)batch.Model_LOD1.VertexCount, ref commandOffset);

                // LOD2 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                BufferSubDataDrawArraysIndirectCommand(1, ref commandOffset);

                // LOD3 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                BufferSubDataDrawArraysIndirectCommand(1, ref commandOffset);
            }
        }

        /// <summary>
        /// 렌더링: 섀도우맵 생성
        /// </summary>
        /// <param name="shadowMap">세도우맵</param>
        /// <param name="camera">카메라</param>
        /// <param name="sunLightDirection">태양이 지표면을 바라보는 벡터</param>
        /// <param name="lightViewWidth">광원뷰 행렬을 위한 크기 </param>
        /// <param name="isClearBuffer">세도우맵을 지우고 렌더링을 시작할지 결정 여부</param>
        public void RenderShadowMap(ShadowMap shadowMap, Camera camera, Vertex3f sunLightDirection, float lightViewWidth = 50.0f, bool isClearBuffer = false)
        {
            // 렌더 스테이트 설정
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);

            if (_batchManager == null) return;

            // 세도우맵 바인딩 및 광원 뷰 행렬 계산
            Vertex3f terrainCenter = new Vertex3f(0, 0, 0);
            shadowMap.Bind();

            // 지우기 옵션(이전 단계에서 세도우맵이 그려진 경우에 지우지 말고 덮어쓰기)
            if (isClearBuffer) shadowMap.Clear();

            // 광원 뷰 및 투영 행렬 업데이트
            float terrainSize = Math.Max(lightViewWidth, 100);
            shadowMap.Update(sunLightDirection, camera.PivotPosition, terrainSize);

            // 각 배치별 렌더링
            for (uint batchID = 0; batchID < _batchedModelCount; batchID++)
            {
                _batchTemp = _batchManager.GetBatch(batchID);
                string batchName = _batchTemp.ModelName;
                int cmdStartIndex = _batchCommandStartIndices[batchID];
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, (uint)_indirectCommandBuffer);

                _gpuInstancedShadowMapShader.Bind();
                {
                    // LOD0
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

                    _gpuInstancedShadowMapShader.LoadBatchStartOffset(_batchTemp.StartIndex);
                    _gpuInstancedShadowMapShader.LoadTextureArray(_batchTemp.Model.TextureIDArray);
                    _gpuInstancedShadowMapShader.LoadEnableDebug(false);
                    _gpuInstancedShadowMapShader.LoadMaxDepthDistance(10000.0f);
                    _gpuInstancedShadowMapShader.LoadLightViewMatrix(shadowMap.LightViewMatrix);
                    _gpuInstancedShadowMapShader.LoadLightProjMatrix(shadowMap.LightProjMatrix);
                    DrawArraysIndirect(_batchTemp.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Triangles);

                    // LOD1
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

                    _gpuInstancedShadowMapShader.LoadEnableDebug(_isDebugLOD1);
                    _gpuInstancedShadowMapShader.LoadDebugColor(COLOR_RED4);
                    DrawArraysIndirect(_batchTemp.VAO, cmdStartIndex, 1, _visibleIndicesSSBO_LOD1, PrimitiveType.Triangles);
                }
                _gpuInstancedShadowMapShader.Unbind();
            }

            // 세도우맵 바인딩 해제
            shadowMap.Unbind();
        }

        public override void RenderBatchLod0(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
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
                _instancedShader.LoadDebugColor(COLOR_RED4);
                DrawArraysIndirect(batch.Model_LOD1.VaoID, cmdStartIndex, 1, _visibleIndicesSSBO_LOD1, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();
        }

        public override void RenderBatchLod2(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            
        }

        ImpostorBaseInfo _baseInfo;

        public override void RenderBatchLod3(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera)
        {
            //Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            _baseInfo = _batchManager.ImpostorManager.GetBaseInfo(batch.ModelID);

            _gpuDrivenImpostorShader.Bind();
            {
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD3);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _batchManager.ImpostorBaseInfoSSBO);

                _gpuDrivenImpostorShader.LoadImpostorAtlas(_baseInfo.AlbedoTextureID);
                _gpuDrivenImpostorShader.LoadNormalAtlas(_baseInfo.NormalTextureID);
                _gpuDrivenImpostorShader.LoadBatchStartOffset(batch.StartIndex);
                _gpuDrivenImpostorShader.LoadMaxDepthDistance(10000.0f);
                _gpuDrivenImpostorShader.LoadCameraPosition(camera.Position);

                DrawArraysIndirect(_point.VAO, cmdStartIndex, lodIndex: 3, ssboIndex: _visibleIndicesSSBO_LOD3);
            }
            _gpuDrivenImpostorShader.Unbind();
        }

    }
}
