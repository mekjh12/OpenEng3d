using OpenGL;

namespace BillBoard
{
    /// <summary>
    /// 상단: 수직 평면 4개 (0°, 45°, 90°, 135°)
    /// </summary>
    public static class CrossBillboardAtlasLayout
    {
        public const int VerticalPlaneCount = 3;

        public static readonly float[] VerticalAngles = { 0f, 60f, 120f };

        public static CrossBillboardData.AtlasRegion[] CalculateRegions()
        {
            var regions = new CrossBillboardData.AtlasRegion[VerticalAngles.Length];

            // 수직 4개 (상단 줄)
            for (int i = 0; i < VerticalPlaneCount; i++)
            {
                regions[i] = new CrossBillboardData.AtlasRegion(
                    new Vertex2f(i * 0.25f, 0f),
                    new Vertex2f(0.25f, 0.5f)
                );
            }

            return regions;
        }
    }
}
