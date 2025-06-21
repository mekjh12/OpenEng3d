using Model3d;
using System;
using System.Collections.Generic;
using System.IO;

namespace Terrain
{
    public class SimpleTerrainRegionCache
    {
        private Dictionary<RegionCoord, SimpleTerrainRegion> _cache;
        private float _size;
        private string _heightmapBasePath;

        public int Count => _cache.Count;
        public SimpleTerrainRegion this[RegionCoord coord] => _cache.ContainsKey(coord)? _cache[coord]: null;

        public SimpleTerrainRegionCache(string heightmapBasePath, float size)
        {
            _cache = new Dictionary<RegionCoord, SimpleTerrainRegion>();
            _heightmapBasePath = heightmapBasePath;
            _size = size;
        }

        public bool ContainsKey(RegionCoord regionCoord)
        {
            return _cache.ContainsKey(regionCoord);
        }

        public void AddRegion(RegionCoord coord, RawModel3d rawModel3d)
        {
            if (!_cache.ContainsKey(coord) )
            {
                SimpleTerrainRegion region = new SimpleTerrainRegion(coord, rawModel3d, _size);

                string heightmapLowResFileName = _heightmapBasePath + $"simple\\region{coord.X}x{coord.Y}.png";
                if (!File.Exists(heightmapLowResFileName)) return;

                _ = region.LoadTerrainFromFile(heightmapLowResFileName, TerrainConstants.DEFAULT_VERTICAL_SCALE);

                _cache.Add(coord, region);
            }
            else
            {
                throw new Exception("RegionCoord가 이미 등록되어 있습니다.");
            }
        }
    }
}
