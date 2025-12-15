namespace BillBoard
{
    /// <summary>
    /// 4x2 레이아웃으로 Atlas 영역 계산
    /// 상단: 수직 평면 4개 (0°, 45°, 90°, 135°)
    /// 하단: 수평 평면 2개 (상단, 하단)
    /// </summary>
    public static class BillboardCloudAtlasLayout
    {
        public const int VerticalPlaneCount = 4;
        public const int HorizontalPlaneCount = 2;
        public const int TotalPlaneCount = 6;

        public static readonly float[] VerticalAngles = { 0f, 45f, 90f, 135f };

        public static BillboardCloudData.AtlasRegion[] CalculateRegions()
        {
            var regions = new BillboardCloudData.AtlasRegion[TotalPlaneCount];

            // 수직 4개 (상단 줄)
            for (int i = 0; i < VerticalPlaneCount; i++)
            {
                regions[i] = new BillboardCloudData.AtlasRegion(
                    new Vertex2f(i * 0.25f, 0f),
                    new Vertex2f(0.25f, 0.5f)
                );
            }

            // 수평 2개 (하단 줄)
            for (int i = 0; i < HorizontalPlaneCount; i++)
            {
                regions[VerticalPlaneCount + i] = new BillboardCloudData.AtlasRegion(
                    new Vertex2f(i * 0.25f, 0.5f),
                    new Vertex2f(0.25f, 0.5f)
                );
            }

            return regions;
        }
    }
}
