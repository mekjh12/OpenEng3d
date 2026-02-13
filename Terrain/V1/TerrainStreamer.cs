using Common;
using Model3d;
using System;
using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 타일 기반 지형 스트리밍
    /// </summary>
    public class TerrainStreamer
    {
        private const float WORLD_POSITION_OFFSET = 0.0f;

        private AsyncHeightmapLoader _loader;
        private string _heightmapBasePath;
        private bool _isLowRes;
        private bool _completedLoad = false;

        private int _currentRegionX = 0;
        private int _currentRegionY = 0;
        private readonly int _tileRadius = 3;
        private readonly int _maxUploadsPerFrame = 1;

        private HashSet<string> _loadedRegions = new HashSet<string>();
        private HashSet<string> _newVisibleRegions = new HashSet<string>();
        private HashSet<string> _toUnloadRegions = new HashSet<string>();

        // ------------------------------------------------------------------------
        // 속성
        // ------------------------------------------------------------------------

        public string CurrentRegionCoords => $"{_currentRegionX}x{_currentRegionY}";
        public int CurrentRegionX => _currentRegionX;
        public int CurrentRegionY => _currentRegionY;
        public uint CurrentMapTextureId => GetCurrentTexture();
        public int TileRadius => _tileRadius;
        public bool CompletedLoad
        {
            get => _completedLoad; set => _completedLoad = value;
        }

        // ------------------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------------------

        public TerrainStreamer(string heightmapBasePath, int tileRadius, int maxUploadsPerFrame = 1, bool isLowRes = false)
        {
            _tileRadius = tileRadius;
            _isLowRes = isLowRes;
            _maxUploadsPerFrame = maxUploadsPerFrame;
            _heightmapBasePath = heightmapBasePath;
            _loader = new AsyncHeightmapLoader(isLowRes ? 129u: 1025u);
            _loader.Start();
        }

        /// <summary>
        /// 플레이어 위치 업데이트 (메인 스레드)
        /// </summary>
        public void Update(float duration, float worldX, float worldY)
        {
            // 월드 좌표 -> 리전 좌표
            GetRigionCoord(worldX, worldY, out int newRegionX, out int newRegionY);

            // 리전 변경 시 주변 타일 로드/언로드 처리
            if (newRegionX != _currentRegionX || newRegionY != _currentRegionY)
            {
                _currentRegionX = newRegionX;
                _currentRegionY = newRegionY;

                UpdateVisibleRegions();
            }

            // GPU 업로드 처리 (매 프레임)
            _loader.ProcessUploads(_maxUploadsPerFrame);

            // 주변 타일 업로드가 모두 완료되면
            if (_loader.CheckUploadCompleted())
            {
                _completedLoad = true;
                string low = _isLowRes ? "저해상도" : "고해상도";
                Console.WriteLine($"로딩완료 {low} {_currentRegionX}x{_currentRegionY}");
            }
        }

        /// <summary>
        /// 주변 + 현재 타일 로드/언로드
        /// </summary>
        private void UpdateVisibleRegions()
        {
            _newVisibleRegions.Clear();

            // 현재 + 주변
            for (int dy = -_tileRadius; dy <= _tileRadius; dy++)
            {
                for (int dx = -_tileRadius; dx <= _tileRadius; dx++)
                {
                    int rx = _currentRegionX + dx;
                    int ry = _currentRegionY + dy;
                    string key = GetRegionKey(rx, ry);

                    _newVisibleRegions.Add(key);

                    int priority = Math.Abs(dx) + Math.Abs(dy);
                    string filePath = GetHeightmapPath(rx, ry);
                    //Console.WriteLine($"{_isLowRes} {rx}x{ry}={filePath}");

                    _loader.RequestLoad(filePath, rx, ry, priority);
                }
            }

            // 재사용 (GC 압력 감소)
            _toUnloadRegions.Clear();
            foreach (string key in _loadedRegions)
            {
                if (!_newVisibleRegions.Contains(key))
                {
                    _toUnloadRegions.Add(key);
                }
            }

            foreach (string key in _toUnloadRegions)
            {
                var (x, y) = ParseRegionKey(key);
                _loader.UnloadTile(x, y);
            }

            // Swap (메모리 재사용)
            var temp = _loadedRegions;
            _loadedRegions = _newVisibleRegions;
            _newVisibleRegions = temp;

            Console.WriteLine($"[Streaming] 리전 이동: ({_currentRegionX}, {_currentRegionY}), 로드된 타일: {_loadedRegions.Count}");
        }

        // ------------------------------------------------------------------------
        // 기타 메소드
        // ------------------------------------------------------------------------

        /// <summary>
        /// 특정 리전의 텍스처 가져오기
        /// </summary>
        public uint GetRegionTexture(int regionX, int regionY)
        {
            uint? tid = _loader.GetTileTexture(regionX, regionY);
            return (uint)(tid == null ? 0 : tid);
        }

        public void PrintRegion()
        {
            Console.WriteLine("------------------------------");
            for (int i = 0; i < 9; i++)
            {
                string txt = "";
                for (int j = 0; j < 9; j++)
                {
                    uint idx = GetRegionTexture(j, i);
                    txt += $"{idx},";
                }
                Console.WriteLine(txt);
            }
        }

        /// <summary>
        /// 현재 플레이어 리전의 텍스처
        /// </summary>
        private uint GetCurrentTexture()
        {
            uint? tid = _loader.GetTileTexture(_currentRegionX, _currentRegionY);
            uint mapTextureId = (uint)(tid == null ? 0 : tid);
            return mapTextureId;
        }

        private string GetHeightmapPath(int x, int y)
        {
            string verb = _isLowRes ? "_low" : "";
            return $"{_heightmapBasePath}/tile_{x}_{y}{verb}.raw";
        }

        /// <summary>
        /// 월드 좌표 -> 리전 좌표
        /// </summary>
        public void GetRigionCoord(float worldX, float worldY, out int regionX, out int regionY)
        {
            int size = Constants.TERRAIN_TILE_SIZE - 1;
            regionX = (int)Math.Floor((worldX + WORLD_POSITION_OFFSET) / size);
            regionY = (int)Math.Floor((worldY + WORLD_POSITION_OFFSET) / size);
        }

        public string GetRegionKey(int x, int y) => $"{x}_{y}";

        private (int x, int y) ParseRegionKey(string key)
        {
            string[] parts = key.Split('_');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public void PrintStats()
        {
            _loader.PrintStats();
        }

        public void Cleanup()
        {
            _loader.Cleanup();
        }
    }
}