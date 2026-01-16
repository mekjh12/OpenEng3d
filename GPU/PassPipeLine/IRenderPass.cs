using Common.Abstractions;

namespace GPUDriven
{
    public interface IRenderPass
    {
        void Initialize(Camera camera, ModelBatchManager batchManager);
        void RenderBatchLod0(uint batchID, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod1(uint batchID, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod2(uint batchID, string batchName, int cmdStartIndex, Camera camera);
        void RenderBatchLod3(uint batchID, string batchName, int cmdStartIndex, Camera camera);

    }
}
