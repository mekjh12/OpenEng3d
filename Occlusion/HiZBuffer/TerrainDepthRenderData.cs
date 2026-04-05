using OpenGL;

namespace Occlusion
{
    /// <summary>
    /// RenderTerrainDepth에 필요한 최소 데이터만 담는 구조체
    /// </summary>
    public struct TerrainDepthRenderData
    {
        public uint VAO;
        public uint IBO;
        public int Count;

        // 타일별 (heightMapTextureId, worldMatrix) 쌍
        public (uint HeightMapTextureId, Matrix4x4f WorldMatrix)[] Tiles; // 최대 9개
    }
}
