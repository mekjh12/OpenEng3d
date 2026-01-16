using Common.Abstractions;
using OpenGL;
using Shader;
using Ui3d;

namespace GPUDriven
{
    public class GroundFogRenderPass : RenderPassPipeLine, IRenderPass
    {
        private BillboardInstancedShader _billboardShader;
        private uint _fogTextureID;

        public GroundFogRenderPass(string name, string projPath) : base(name, projPath)
        {
            _billboardShader = new BillboardInstancedShader(projPath);
        }

        public override void Initialize(Camera camera, ModelBatchManager batchManager)
        {
            // 기본 초기화(반드시 호출)
            base.Initialize(camera, batchManager);
        }

        public void SetFogTexture(uint textureID)
        {
            _fogTextureID = textureID;
        }

        public override void RenderBatchLod0(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
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
        }

        public override void RenderBatchLod1(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {
            
        }

        public override void RenderBatchLod2(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {

        }

        public override void RenderBatchLod3(uint batchID, string batchName, int cmdStartIndex, Camera camera)
        {

        }
    }
}
