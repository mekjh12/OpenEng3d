using OpenGL;
using System;
using System.Collections.Generic;
using System.Text;
using Terrain;

namespace Renderer
{
    public class GrassTileManager
    {
        private const float TILE_SIZE = 10.0f;
        private const int TILES_RADIUS = 3;
        private const int MAX_POOL_SIZE = 500; // 최대 캐시 크기 (메모리 제한)

        private Dictionary<Vertex2i, GrassTile> _activeTiles;
        private Dictionary<Vertex2i, GrassTile> _tilePool;  // 좌표별 캐싱!
        private Queue<Vertex2i> _poolLRU;  // LRU 관리용
        private Vertex2i _lastCameraCell;
        TerrainData _terrainData;
        private bool _isTilesUpdated;

        private HashSet<Vertex2i> _requiredTiles;
        private List<Vertex2i> _tilesToRemove;

        private StringBuilder _activeTileNamesBuilder;
        private string _activeTileNames;

        public string ActiveTileNames => _activeTileNames;
        public uint PoolCount => (uint)_tilePool.Count;
        public bool IsTilesUpdated => _isTilesUpdated;

        public GrassTileManager(TerrainData terrainData)
        {
            _terrainData = terrainData;

            _activeTiles = new Dictionary<Vertex2i, GrassTile>();
            _tilePool = new Dictionary<Vertex2i, GrassTile>();
            _poolLRU = new Queue<Vertex2i>();
            _lastCameraCell = new Vertex2i(int.MaxValue, int.MaxValue);

            _requiredTiles = new HashSet<Vertex2i>();
            _tilesToRemove = new List<Vertex2i>();
            _activeTileNamesBuilder = new StringBuilder(2048);
        }

        private Vertex2i WorldToTileCoord(Vertex3f worldPos)
        {
            return new Vertex2i(
                (int)Math.Floor(worldPos.x / TILE_SIZE),
                (int)Math.Floor(worldPos.y / TILE_SIZE)
            );
        }

        public void Update(Vertex3f cameraPosition)
        {
            Vertex2i currentCell = WorldToTileCoord(cameraPosition);

            _isTilesUpdated = false;

            if (currentCell == _lastCameraCell)
                return;

            _lastCameraCell = currentCell;

            _isTilesUpdated = true;

            // 필요한 타일 목록 생성
            _requiredTiles.Clear();
            for (int dy = -TILES_RADIUS; dy <= TILES_RADIUS; dy++)
            {
                for (int dx = -TILES_RADIUS; dx <= TILES_RADIUS; dx++)
                {
                    Vertex2i tileCoord = new Vertex2i(
                        currentCell.x + dx,
                        currentCell.y + dy
                    );
                    _requiredTiles.Add(tileCoord);
                }
            }

            // 필요 없는 타일을 풀로 반환
            _tilesToRemove.Clear();
            foreach (var kvp in _activeTiles)
            {
                if (!_requiredTiles.Contains(kvp.Key))
                {
                    _tilesToRemove.Add(kvp.Key);
                }
            }

            // 제거할 타일 처리
            foreach (Vertex2i tile in _tilesToRemove)
            {
                GrassTile grassTile = _activeTiles[tile];

                // 좌표별로 풀에 저장 (GenerateGrass 결과 유지!)
                ReturnToPool(tile, grassTile);

                _activeTiles.Remove(tile);
            }

            // 새 타일 생성 (풀에서 재사용)
            foreach (Vertex2i coord in _requiredTiles)
            {
                if (!_activeTiles.ContainsKey(coord))
                {
                    _activeTiles[coord] = GetOrCreateTileFromPool(coord);
                }
            }
        }

        /// <summary>
        /// 풀에 반환 (LRU 관리)
        /// </summary>
        private void ReturnToPool(Vertex2i coord, GrassTile tile)
        {
            // 풀 크기 제한
            if (_tilePool.Count >= MAX_POOL_SIZE)
            {
                // 가장 오래된 타일 제거
                Vertex2i oldestCoord = _poolLRU.Dequeue();
                if (_tilePool.TryGetValue(oldestCoord, out GrassTile oldTile))
                {
                    oldTile.Dispose();  // 완전히 제거
                    _tilePool.Remove(oldestCoord);
                }
            }

            // 풀에 추가
            _tilePool[coord] = tile;
            _poolLRU.Enqueue(coord);
        }

        /// <summary>
        /// 풀에서 꺼내거나 새로 생성
        /// </summary>
        private GrassTile GetOrCreateTileFromPool(Vertex2i coord)
        {
            // 1단계: 같은 좌표가 풀에 있으면 그대로 재사용 (Reinitialize 불필요)
            if (_tilePool.TryGetValue(coord, out GrassTile cachedTile))
            {
                _tilePool.Remove(coord);
                return cachedTile;  // 이미 같은 좌표로 생성되어 있음!
            }

            // 2단계: 다른 좌표라도 풀에 타일이 있으면 재활용 (Reinitialize 필요)
            if (_tilePool.Count > 0)
            {
                // 아무 타일이나 가져와서 재초기화
                using (var enumerator = _tilePool.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var kvp = enumerator.Current;
                    GrassTile tile = kvp.Value;
                    _tilePool.Remove(kvp.Key);

                    tile.Reinitialize(coord, TILE_SIZE, _terrainData);
                    return tile;
                }
            }

            // 3단계: 풀이 완전히 비었으면 새로 생성
            return new GrassTile(coord, TILE_SIZE, _terrainData);
        }

        public IEnumerable<GrassTile> GetActiveTiles()
        {
            return _activeTiles.Values;
        }

        public void Dispose()
        {
            foreach (var tile in _activeTiles.Values)
            {
                tile.Dispose();
            }
            _activeTiles.Clear();

            foreach (var tile in _tilePool.Values)
            {
                tile.Dispose();
            }
            _tilePool.Clear();
            _poolLRU.Clear();
        }
    }
}