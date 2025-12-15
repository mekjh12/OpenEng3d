using Common.Abstractions;
using OpenGL;

namespace BillBoard
{
    /// <summary>
    /// Billboard Cloud의 메타데이터를 저장
    /// 나무, 덤불, 바위, 복잡한 구조물 등 모든 객체에 사용 가능
    /// </summary>
    public class BillboardCloudData
    {
        // === Atlas UV 영역 정보 ===
        public struct AtlasRegion
        {
            public Vertex2f Offset;  // UV 시작점 (0~1)
            public Vertex2f Size;    // UV 크기 (0~1)

            public AtlasRegion(Vertex2f offset, Vertex2f size)
            {
                Offset = offset;
                Size = size;
            }
        }

        // === Atlas 정보 ===
        public Texture AtlasTexture { get; set; }
        public int AtlasWidth { get; set; }
        public int AtlasHeight { get; set; }

        // === 평면별 UV 영역 (4x2 고정) ===
        // [0~3]: 수직 평면 (0°, 45°, 90°, 135°)
        // [4~5]: 수평 평면 (상단, 하단)
        public AtlasRegion[] Regions { get; set; }

        // === 객체 크기 정보 ===
        public float ObjectWidth { get; set; }
        public float ObjectHeight { get; set; }
        public Vertex3f BoundsMin { get; set; }
        public Vertex3f BoundsMax { get; set; }

        // === 수평 평면 높이 (객체 높이에 대한 비율) ===
        public float HorizontalPlaneTopRatio { get; set; }     // 예: 0.9f
        public float HorizontalPlaneBottomRatio { get; set; }  // 예: 0.6f

        public BillboardCloudData()
        {
            Regions = new AtlasRegion[6];

            // 기본값
            HorizontalPlaneTopRatio = 0.9f;
            HorizontalPlaneBottomRatio = 0.6f;
        }
    }
}
