using Model3d;
using Renderer;

namespace BillBoard
{
    /// <summary>
    /// ImpostorSettings를 ImpostorRenderData로 변환
    /// </summary>
    public static class ImpostorRenderDataFactory
    {
        public static ImpostorRenderData ToRenderData(
            this ImpostorSettings settings,
            UnifiedTexturedModel model,
            uint atlasTextureId,
            bool enableEdgeLine = false)
        {
            return new ImpostorRenderData
            {
                atlasTextureId = atlasTextureId,
                modelRadius = model.AABB.Radius,
                modelCenter = model.AABB.Center,
                modelMatrix = model.AABB.ModelMatrix,
                atlasSize = settings.AtlasSize,
                individualSize = settings.IndividualSize,
                horizontalFrames = settings.HorizontalAngles,
                verticalFrames = settings.VerticalAngles,
                enableEdgeLine = enableEdgeLine
            };
        }

        /// <summary>
        /// LODSystem에서 직접 변환 (더 간편)
        /// </summary>
        public static ImpostorRenderData ToRenderData(
            this ImpostorLODSystem lodSystem,
            string modelName,
            UnifiedTexturedModel model,
            bool enableEdgeLine = false)
        {
            var settings = lodSystem.GetImpostorSettings(modelName);
            var textureId = lodSystem.AtlasTexture(modelName);

            return settings.ToRenderData(model, textureId, enableEdgeLine);
        }
    }
}