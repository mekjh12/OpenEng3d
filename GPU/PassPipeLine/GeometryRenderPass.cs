using BillBoard;
using Common.Abstractions;
using OpenGL;
using Renderer;
using Shader;

namespace GPUDriven
{
    public class GeometryRenderPass : RenderPassPipeLine, IRenderPass
    {
        private GPUInstancedShader _instancedShader;                        // 메시 렌더링 셰이더
        private GPUDrivenImpostorShader _gpuDrivenImpostorShader;           // GPU 드리븐 임포스터 렌더링 셰이더
        private CrossBillboardInstanceShader _crossBillboardInstanceShader; // 크로스 빌보드 렌더링 셰이더
        private UnlitShader _unlitShader;                                   // 임포스터 생성용 셰이더

        // 크로스 빌보드 관련
        private CrossBillboardAtlasGenerator _generator;  // 크로스 빌보드 아틀라스 생성기
        private CrossBillboardData[] _billboardData;      // 배치별 크로스 빌보드 데이터

        // 임포스터 관련
        protected ImpostorRenderData _renderData;       // 임포스터 렌더링 데이터
        private ImpostorAssets _impostor;               // 임포스터 에셋 관리자

        // ------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------

        public GeometryRenderPass(string name, string projPath) : base(name, projPath)
        {
            // 기본 세이더 초기화
            _instancedShader = new GPUInstancedShader(projPath);
            _crossBillboardInstanceShader = new CrossBillboardInstanceShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _gpuDrivenImpostorShader = new GPUDrivenImpostorShader(projPath);
        }

        public override void Initialize(Camera camera, ModelBatchManager batchManager)
        {
            // 기본 초기화(반드시 호출)
            base.Initialize(camera, batchManager);

            // 크로스 빌보드 아틀라스 생성
            _generator = new CrossBillboardAtlasGenerator();
            _billboardData = new CrossBillboardData[_batchManager.ActualBatchCount];
            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);
                _billboardData[i] = _generator.GenerateAtlas(_unlitShader, batch.Model);
            }
                        
            // 임포스터 에셋 초기화
            _impostor = new ImpostorAssets(_unlitShader, camera);

            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);
                _impostor.CreateImpostorModel(
                    ImpostorSettings.CreateSettings(batch.ModelName, 64, 8, 6),
                    batch.Model);
            }
        }


        public override void RenderBatchLod0(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            {
                _instancedShader.LoadBatchStartOffset(_batch.StartIndex);
                _instancedShader.LoadTextureArray(_batch.Model.TextureIDArray);
                _instancedShader.LoadEnableDebug(false);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                DrawArraysIndirect(_batch.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();
        }

        public override void RenderBatchLod1(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            {
                _instancedShader.LoadBatchStartOffset(_batch.StartIndex);
                _instancedShader.LoadTextureArray(_batch.Model.TextureIDArray);
                _instancedShader.LoadEnableDebug(false);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                _instancedShader.LoadDebugColor(COLOR_RED4);
                DrawArraysIndirect(_batch.VAO, cmdStartIndex, 1, _visibleIndicesSSBO_LOD1, PrimitiveType.Triangles);
            }
            _instancedShader.Unbind();
        }

        public override void RenderBatchLod2(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _aabbSSBO);

            CrossBillboardData crossBillboardData = _billboardData[batchID];

            // LOD2: 크로스 빌보드 렌더링
            Gl.Disable(EnableCap.CullFace);
            _crossBillboardInstanceShader.Bind();
            {
                _crossBillboardInstanceShader.LoadCurrentBatchID(batchID);
                _crossBillboardInstanceShader.LoadBatchStartOffset(_batch.StartIndex);
                _crossBillboardInstanceShader.LoadAtlasTexture(crossBillboardData.AtlasTexture.TextureID);
                _instancedShader.LoadMaxDepthDistance(10000.0f);
                _crossBillboardInstanceShader.UseTexture(true);
                DrawArraysIndirect(_point.VAO, cmdStartIndex, 2, _visibleIndicesSSBO_LOD2);
            }
            _crossBillboardInstanceShader.Unbind();
        }

        public override void RenderBatchLod3(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
            _renderData = _impostor.GetImpostorRenderData(batchName);
            _unifiedTexturedModel = _impostor.UnifiedTexturedModel(batchName);

            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            _gpuDrivenImpostorShader.Bind();
            {
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD3);

                _gpuDrivenImpostorShader.LoadImpostorAtlas(_renderData.AtlasTextureId);
                _gpuDrivenImpostorShader.LoadAtlasSize(_renderData.atlasSize);
                _gpuDrivenImpostorShader.LoadIndividualSize(_renderData.individualSize);
                _gpuDrivenImpostorShader.LoadFrameCounts(_renderData.horizontalFrames, _renderData.verticalFrames);
                _gpuDrivenImpostorShader.LoadMaxDepthDistance(10000.0f);
                _gpuDrivenImpostorShader.LoadAABBSphereRadius(_unifiedTexturedModel.AABB.Radius);
                _gpuDrivenImpostorShader.LoadCameraPosition(camera.Position);
                _gpuDrivenImpostorShader.LoadEnableEdgeLine(false);
                _gpuDrivenImpostorShader.LoadBatchStartOffset(_batch.StartIndex);
                DrawArraysIndirect(_point.VAO, cmdStartIndex, 3, _visibleIndicesSSBO_LOD3);
            }
            _gpuDrivenImpostorShader.Unbind();
        }

    }
}
