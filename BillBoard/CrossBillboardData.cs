using Common.Abstractions;
using OpenGL;

namespace BillBoard
{
    /// <summary>
    /// Billboard Cloud의 메타데이터를 저장
    /// 나무, 덤불, 바위, 복잡한 구조물 등 모든 객체에 사용 가능
    /// </summary>
    public class CrossBillboardData
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

        // === 평면별 UV 영역 (3x1 고정) ===
        // [0~3]: 수직 평면 (0°, 60°, 120°)
        public AtlasRegion[] Regions { get; set; }

        // === 객체 크기 정보 ===
        public float ObjectWidth { get; set; }
        public float ObjectHeight { get; set; }
        public Vertex3f BoundsMin { get; set; }
        public Vertex3f BoundsMax { get; set; }


        public CrossBillboardData()
        {
            Regions = new AtlasRegion[3];
        }
    }
}
