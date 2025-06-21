using Geometry;

namespace Terrain
{
    public class RegionCoord
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public RegionCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static RegionCoord operator + (RegionCoord a, RegionCoord b)
        {
            return new RegionCoord(a.X + b.X, a.Y + b.Y);
        }

        public static RegionCoord operator -(RegionCoord a, RegionCoord b)
        {
            return new RegionCoord(a.X - b.X, a.Y - b.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj is RegionCoord other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public override string ToString()
        {
            return $"{X}:{Y}";
        }

        public static readonly RegionCoord[] ADJACENT_REGION_COORDS = new RegionCoord[9]
        {
            new RegionCoord(0, 0),   // 자신
            new RegionCoord(1, 0),   // 동
            new RegionCoord(1, 1),   // 북동
            new RegionCoord(0, 1),   // 북
            new RegionCoord(-1, 1),  // 북서
            new RegionCoord(-1, 0),  // 서
            new RegionCoord(-1, -1), // 남서
            new RegionCoord(0, -1),  // 남
            new RegionCoord(1, -1),  // 남동
         };

        public static readonly RegionCoord[] ADJACENT_REGION_COORDS_EXCEPT_CENTER = new RegionCoord[8]
        {
            new RegionCoord(1, 0),   // 동
            new RegionCoord(1, 1),   // 북동
            new RegionCoord(0, 1),   // 북
            new RegionCoord(-1, 1),  // 북서
            new RegionCoord(-1, 0),  // 서
            new RegionCoord(-1, -1), // 남서
            new RegionCoord(0, -1),  // 남
            new RegionCoord(1, -1),  // 남동
         };

        public static readonly RegionCoord[] OUTER_REGION_COORDS = new RegionCoord[16]
        {
            // 동쪽 가장자리
            new RegionCoord(2, -2),  // 남동쪽 모서리
            new RegionCoord(2, -1),  // 남동쪽
            new RegionCoord(2, 0),   // 동쪽
            new RegionCoord(2, 1),   // 북동쪽
            new RegionCoord(2, 2),   // 북동쪽 모서리
    
            // 북쪽 가장자리
            new RegionCoord(1, 2),   // 북동쪽
            new RegionCoord(0, 2),   // 북쪽
            new RegionCoord(-1, 2),  // 북서쪽
    
            // 서쪽 가장자리
            new RegionCoord(-2, 2),  // 북서쪽 모서리
            new RegionCoord(-2, 1),  // 북서쪽
            new RegionCoord(-2, 0),  // 서쪽
            new RegionCoord(-2, -1), // 남서쪽
            new RegionCoord(-2, -2), // 남서쪽 모서리
    
            // 남쪽 가장자리
            new RegionCoord(-1, -2), // 남서쪽
            new RegionCoord(0, -2),  // 남쪽
            new RegionCoord(1, -2),  // 남동쪽
        };
    }
}
