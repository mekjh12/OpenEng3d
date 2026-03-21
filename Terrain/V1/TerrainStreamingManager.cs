using Common;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Terrain
{
    public class TerrainStreamingManager
    {
        // 지형 관련 변수들
        private TerrainStreamer _terrainHighStreamer;       // 지형 고해상도 스트리머
        private TerrainStreamer _terrainLowStreamer;        // 지형 저해상도 스트리머
        private TerrainStreamer _terrainNormalMapStreamer;  // 지형 노말맵 스트리머
        private Dictionary<(int, int), uint[]> _adjRegionTilesTextures;     // 주변 인접 텍스처들

        private TerrainStreamer _terrainFeatureStreamer;

        // 8방향 오프셋을 static readonly로
        private static readonly int[] _adjDx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly int[] _adjDy = { 0, 1, 1, 1, 0, -1, -1, -1 };

        // 속성
        public string CurrentRegionCoords => _terrainHighStreamer.CurrentRegionCoords;
        public int HighTileRadius => _terrainHighStreamer.TileRadius;
        public int LowTileRadius => _terrainLowStreamer.TileRadius;
        public int CurrentRegionX => _terrainLowStreamer.CurrentRegionX;
        public int CurrentRegionY => _terrainLowStreamer.CurrentRegionY;


        public TerrainStreamingManager(string folder)
        {
            _terrainHighStreamer = new TerrainStreamer(
                folder, tileRadius: 1, maxUploadsPerFrame: 1, tileFileSuffix: "", TileFormat.HeightmapHighFloat(), keepCpuData: true);

            _terrainLowStreamer = new TerrainStreamer(
                folder, tileRadius: 3, maxUploadsPerFrame: 1, tileFileSuffix: "_low", TileFormat.HeightmapLowFloat(), keepCpuData: true);

            _terrainNormalMapStreamer = new TerrainStreamer(
                folder, tileRadius: 3, maxUploadsPerFrame: 1, tileFileSuffix: "_normal", TileFormat.MapRGB(), keepCpuData: false);

            _terrainFeatureStreamer = new TerrainStreamer(
                folder, tileRadius: 3, maxUploadsPerFrame: 1, tileFileSuffix: "_feature", TileFormat.MapRGB(), keepCpuData: true);

            _adjRegionTilesTextures = new Dictionary<(int, int), uint[]>();

            /*
            // 강과 도로의 텍스처를 로딩한다.
            string[] filenames = new string[4] {
                "river_road_0_0", "river_road_1_0",
                 "river_road_0_1", "river_road_1_1"
            };
            _roadRivertileStreamer = new TileStreamer(filenames, folder);
            */

        }

        public void Update(float duration, float x, float y)
        {
            // 스트리머를 업데이트한다. 
            _terrainHighStreamer.UpdateStreaming(duration, x, y);
            _terrainLowStreamer.UpdateStreaming(duration, x, y);
            _terrainNormalMapStreamer.UpdateStreaming(duration, x, y);
            _terrainFeatureStreamer.UpdateStreaming(duration, x, y);

            // 각 스트리머는 내부적으로 로딩이 완료되었는지 체크한다.
            if (_terrainHighStreamer.IsLoadingComplete && 
                _terrainLowStreamer.IsLoadingComplete && 
                _terrainNormalMapStreamer.IsLoadingComplete && 
                _terrainFeatureStreamer.IsLoadingComplete)
            {
                Console.WriteLine("*** 로딩완료 " + _terrainLowStreamer.CurrentRegionX + "," + _terrainLowStreamer.CurrentRegionY);
                _terrainHighStreamer.IsLoadingComplete = false;
                _terrainLowStreamer.IsLoadingComplete = false;
                _terrainNormalMapStreamer.IsLoadingComplete = false;
                _terrainFeatureStreamer.IsLoadingComplete = false;

                // 주변 타일들의 텍스처 아이디를 업데이트한다.
                UpdateAdjTilesTextureIds();
            }
        }

        public float? SampleHeightWorld(float worldX, float worldY, float verticalScale)
        {
            _terrainHighStreamer.GetRegionCoord(worldX, worldY, out int rx, out int ry);

            int tileSize = Constants.TERRAIN_TILE_SIZE - 1;
            float u = ((worldX - rx * tileSize) / tileSize).Clamp(0f, 1f);
            float v = ((worldY - ry * tileSize) / tileSize).Clamp(0f, 1f);

            float? h = _terrainHighStreamer.SampleHeightByUV(rx, ry, u, v);

            if (h == null)
            {
                h = _terrainLowStreamer.SampleHeightByUV(rx, ry, u, v);
            }

            if (h == null)
            {
                return null;
            }

            return h.Value * verticalScale;
        }

        public uint GetRiverRoadTexture(int px, int py)
        {
            return _terrainFeatureStreamer.GetRegionTexture(px, py);
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

        public uint GetRegionNormalTexture(int regionX, int regionY)
        {
            return _terrainNormalMapStreamer.GetRegionTexture(regionX, regionY);
        }

        public uint[] GetAdjRegionTextures(int regionX, int regionY)
        {
            _adjRegionTilesTextures.TryGetValue((regionX, regionY), out uint[] result);
            return result;
        }

        public void Print()
        {
            _terrainLowStreamer.PrintRegion();
            _terrainHighStreamer.PrintRegion();
            _terrainNormalMapStreamer.PrintRegion();
            _terrainFeatureStreamer.PrintRegion();

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

            for (int dy = -_tileRadius; dy <= _tileRadius; dy++)
            {
                for (int dx = -_tileRadius; dx <= _tileRadius; dx++)
                {
                    int rx = _currentRegionX + dx;
                    int ry = _currentRegionY + dy;

                    uint[] textures = new uint[8];

                    for (int i = 0; i < 8; i++)
                    {
                        int nx = rx + _adjDx[i];
                        int ny = ry + _adjDy[i];

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

                    _adjRegionTilesTextures.Add((rx, ry), textures);
                }
            }
        }

    }
}
