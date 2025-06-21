using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 최근 사용된 TerrainRegion을 캐싱하는 클래스입니다.
    /// LRU(Least Recently Used) 알고리즘을 사용하여 가장 최근에 사용된 리전을 우선적으로 보관합니다.
    /// </summary>
    /// <remarks>
    /// - 고정된 크기의 캐시를 사용하여 메모리 사용량을 제한합니다.
    /// - Dictionary를 사용하여 O(1) 시간 복잡도로 리전을 검색합니다.
    /// - LinkedList를 사용하여 LRU 순서를 추적합니다.
    /// - 사용자가 특정 지역을 왔다갔다 할 때 리전 데이터를 효율적으로 재사용할 수 있습니다.
    /// </remarks>
    public class RecentRegionCache
    {
        private readonly Dictionary<RegionCoord, TerrainRegion> _recentRegions;
        private readonly int _maxCacheSize;
        private readonly LinkedList<RegionCoord> _lruList; // Least Recently Used tracking

        /// <summary>
        /// RecentRegionCache의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="maxCacheSize">캐시할 최대 리전 수. 기본값은 12(9개 인접 리전 + 여유분)</param>
        public RecentRegionCache(int maxCacheSize = 12)
        {
            _maxCacheSize = maxCacheSize;
            _recentRegions = new Dictionary<RegionCoord, TerrainRegion>();
            _lruList = new LinkedList<RegionCoord>();
        }

        /// <summary>
        /// 주어진 좌표의 리전을 캐시에서 찾아 반환합니다.
        /// </summary>
        /// <param name="coord">찾을 리전의 좌표</param>
        /// <returns>캐시된 TerrainRegion 또는 찾지 못한 경우 null</returns>
        public TerrainRegion TryGetRegion(RegionCoord coord)
        {
            if (_recentRegions.TryGetValue(coord, out TerrainRegion region))
            {
                // Update LRU order
                _lruList.Remove(coord);
                _lruList.AddLast(coord);
                return region;
            }
            return null;
        }

        /// <summary>
        /// 리전을 캐시에 저장합니다. 캐시가 가득 찬 경우 가장 오래된 리전을 제거합니다.
        /// </summary>
        /// <param name="coord">리전의 좌표</param>
        /// <param name="region">캐시할 TerrainRegion 인스턴스</param>
        public void CacheRegion(RegionCoord coord, TerrainRegion region)
        {
            // If region already exists, just update LRU order
            if (_recentRegions.ContainsKey(coord))
            {
                _lruList.Remove(coord);
                _lruList.AddLast(coord);
                return;
            }

            // Remove oldest entry if cache is full
            if (_recentRegions.Count >= _maxCacheSize)
            {
                RegionCoord oldestCoord = _lruList.First.Value;
                _lruList.RemoveFirst();
                _recentRegions.Remove(oldestCoord);
            }

            // Add new region
            _recentRegions[coord] = region;
            _lruList.AddLast(coord);
        }

        /// <summary>
        /// 지정된 좌표의 리전을 캐시에서 제거하고 반환합니다.
        /// </summary>
        /// <param name="coord">제거할 리전의 좌표</param>
        /// <returns>제거된 TerrainRegion 또는 찾지 못한 경우 null</returns>
        public TerrainRegion RemoveRegion(RegionCoord coord)
        {
            if (_recentRegions.TryGetValue(coord, out TerrainRegion region))
            {
                _recentRegions.Remove(coord);
                _lruList.Remove(coord);
                return region;
            }
            return null;
        }

        /// <summary>
        /// 캐시를 완전히 비웁니다.
        /// </summary>
        public void Clear()
        {
            _recentRegions.Clear();
            _lruList.Clear();
        }
    }
}
