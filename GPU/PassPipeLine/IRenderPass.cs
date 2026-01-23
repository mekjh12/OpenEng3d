using Common.Abstractions;

namespace GPUDriven
{
    public interface IRenderPass
    {
        void Initialize(Camera camera, ModelBatchManager batchManager, float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f);
        void RenderBatchLod0(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod1(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod2(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod3(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);

    }
}
