using System;
using System.Runtime.CompilerServices;

namespace Terrain
{
    /// <summary>
    /// 지형 리전의 2D 좌표를 나타내는 불변 구조체
    /// </summary>
    public readonly struct RegionCoord : IEquatable<RegionCoord>
    {
        public int X { get; }
        public int Y { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegionCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        #region 연산자 오버로딩

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegionCoord operator +(RegionCoord a, RegionCoord b)
            => new RegionCoord(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegionCoord operator -(RegionCoord a, RegionCoord b)
            => new RegionCoord(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RegionCoord a, RegionCoord b)
            => a.X == b.X && a.Y == b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RegionCoord a, RegionCoord b)
            => !(a == b);

        #endregion

        #region IEquatable<RegionCoord> 구현

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RegionCoord other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is RegionCoord other && Equals(other);

        public override int GetHashCode()
        {
            // 더 나은 해시 함수 (충돌 감소)
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                return hash;
            }
        }

        #endregion

        public override string ToString() => $"({X}, {Y})";

        #region 정적 방향 배열

        /// <summary>자신을 포함한 인접 9개 리전</summary>
        public static readonly RegionCoord[] AdjacentWithCenter = new RegionCoord[9]
        {
            new RegionCoord(0, 0),   // 중앙
            new RegionCoord(1, 0),   // 동
            new RegionCoord(1, 1),   // 북동
            new RegionCoord(0, 1),   // 북
            new RegionCoord(-1, 1),  // 북서
            new RegionCoord(-1, 0),  // 서
            new RegionCoord(-1, -1), // 남서
            new RegionCoord(0, -1),  // 남
            new RegionCoord(1, -1),  // 남동
        };

        /// <summary>인접 8개 리전 (자신 제외)</summary>
        public static readonly RegionCoord[] Adjacent = new RegionCoord[8]
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

        /// <summary>외곽 16개 리전 (거리 2)</summary>
        public static readonly RegionCoord[] Outer = new RegionCoord[16]
        {
            // 동쪽 라인
            new RegionCoord(2, -2), new RegionCoord(2, -1),
            new RegionCoord(2, 0),  new RegionCoord(2, 1),
            new RegionCoord(2, 2),
            
            // 북쪽 라인
            new RegionCoord(1, 2),  new RegionCoord(0, 2),
            new RegionCoord(-1, 2),
            
            // 서쪽 라인
            new RegionCoord(-2, 2), new RegionCoord(-2, 1),
            new RegionCoord(-2, 0), new RegionCoord(-2, -1),
            new RegionCoord(-2, -2),
            
            // 남쪽 라인
            new RegionCoord(-1, -2), new RegionCoord(0, -2),
            new RegionCoord(1, -2),
        };

        #endregion

        #region 유틸리티 메서드

        /// <summary>맨해튼 거리 계산</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ManhattanDistance(RegionCoord other)
            => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>체비셰프 거리 계산 (대각선 포함)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ChebyshevDistance(RegionCoord other)
            => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

        #endregion
    }
}