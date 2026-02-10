using System;
using System.Collections.Generic;

namespace Terrain
{
    public class TerrainStreamingManager
    {
        // 지형 관련 변수들
        private TerrainStreamer _terrainHighStreamer;       // 지형 고해상도 스트리머
        private TerrainStreamer _terrainLowStreamer;        // 지형 저해상도 스트리머
        private Dictionary<string, uint[]> _adjRegionTilesTextures;     // 주변 인접 텍스처들

        // 속성
        public string CurrentRegionCoords => _terrainHighStreamer.CurrentRegionCoords;
        public int HighTileRadius => _terrainHighStreamer.TileRadius;
        public int LowTileRadius => _terrainLowStreamer.TileRadius;
        public int CurrentRegionX => _terrainLowStreamer.CurrentRegionX;
        public int CurrentRegionY => _terrainLowStreamer.CurrentRegionY;


        public TerrainStreamingManager(string folder)
        {
            _terrainHighStreamer = new TerrainStreamer(folder, 1, isLowRes: false);
            _terrainLowStreamer = new TerrainStreamer(folder, 3, isLowRes: true);
            _adjRegionTilesTextures = new Dictionary<string, uint[]>();
        }

        public void Update(float duration, float x, float y)
        {
            _terrainHighStreamer.Update(duration, x, y);
            _terrainLowStreamer.Update(duration, x, y);

            if (_terrainHighStreamer.CompletedLoad && _terrainLowStreamer.CompletedLoad)
            {
                Console.WriteLine("*** 로딩완료 " + _terrainLowStreamer.CurrentRegionX + "," + _terrainLowStreamer.CurrentRegionY);
                _terrainHighStreamer.CompletedLoad = false;
                _terrainLowStreamer.CompletedLoad = false;
                UpdateAdjTilesTextureIds();
            }
        }

        /// <summary>
        /// 특정 리전의 텍스처 가져오기
        /// </summary>
        public uint GetRegionTexture(int regionX, int regionY, int dx, int dy)
        {
            if (Math.Abs(dx)<=1 && Math.Abs(dy) <= 1)
            {
                return _terrainHighStreamer.GetRegionTexture(regionX, regionY);
            }
            else
            {
                return _terrainLowStreamer.GetRegionTexture(regionX, regionY);
            }
        }

        public uint[] GetAdjRegionTextures(int regionX, int regionY)
        {
            _adjRegionTilesTextures.TryGetValue($"{regionX}_{regionY}", out uint[] result);
            return result;
        }

        public void Print()
        {
            _terrainLowStreamer.PrintRegion();
            _terrainHighStreamer.PrintRegion();

            foreach (var key in _adjRegionTilesTextures)
            {
                string txt = $"{key.Key}=";
                foreach (var item in key.Value)
                {
                    txt += "," + item;
                }
                Console.WriteLine(txt);
            }
        }

        private void UpdateAdjTilesTextureIds()
        {
            int _tileRadius = _terrainLowStreamer.TileRadius;
            int _currentRegionX = _terrainLowStreamer.CurrentRegionX;
            int _currentRegionY = _terrainLowStreamer.CurrentRegionY;
            int highRadius = _terrainHighStreamer.TileRadius; // 1

            _adjRegionTilesTextures.Clear();

            // 8방향 오프셋: 동, 북동, 북, 북서, 서, 남서, 남, 남동
            int[] adjDx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] adjDy = { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (int dy = -_tileRadius; dy <= _tileRadius; dy++)
            {
                for (int dx = -_tileRadius; dx <= _tileRadius; dx++)
                {
                    int rx = _currentRegionX + dx;
                    int ry = _currentRegionY + dy;
                    string key = _terrainLowStreamer.GetRegionKey(rx, ry);

                    uint[] textures = new uint[8];

                    for (int i = 0; i < 8; i++)
                    {
                        int nx = rx + adjDx[i];
                        int ny = ry + adjDy[i];

                        // 인접 타일이 고해상도 범위 내인지 판별
                        int relX = nx - _currentRegionX;
                        int relY = ny - _currentRegionY;

                        if (Math.Abs(relX) <= highRadius && Math.Abs(relY) <= highRadius)
                        {
                            // 고해상도 우선, 없으면 저해상도 폴백
                            uint tex = _terrainHighStreamer.GetRegionTexture(nx, ny);
                            textures[i] = tex != 0 ? tex : _terrainLowStreamer.GetRegionTexture(nx, ny);
                        }
                        else
                        {
                            textures[i] = _terrainLowStreamer.GetRegionTexture(nx, ny);
                        }
                    }

                    _adjRegionTilesTextures.Add(key, textures);
                }
            }
        }

    }
}
