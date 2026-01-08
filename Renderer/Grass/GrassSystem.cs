using Common.Abstractions;
using OpenGL;
using System;
using Terrain;

namespace Renderer
{
    public class GrassSystem
    {
        private GrassTileManager _tileManager;
        private GrassRenderer _renderer;
        private TerrainData _terrainData;

        // ---------------------------------
        // 속성
        // ---------------------------------

        public string ActiveTileNames => _tileManager.ActiveTileNames;
        public uint PoolCount => _tileManager.PoolCount;

        // ---------------------------------
        // 생성자
        // ---------------------------------

        public GrassSystem(string projectPath)
        {
            Initialize(projectPath);
        }

        private void Initialize(string projectPath)
        {
            // 렌더러 초기화
            _renderer = new GrassRenderer(projectPath);
        }

        public void SetTerrainData(TerrainData terrainData)
        {
            _terrainData = terrainData;
            _tileManager = new GrassTileManager(_terrainData);
            Console.WriteLine("풀렌더링 시스템 초기화!");
        }

        public void Update(Camera camera)
        {
            // 타일 매니저 업데이트
            _tileManager?.Update(camera.PivotPosition);

            // 타일 업데이트 + GPU 업로드
            _renderer.UpdateGrassData(_tileManager, camera);
        }

        public void Render(Camera camera, Vertex3f sunDirection)
        {
            _renderer.Render(camera, sunDirection);
        }

        public void Dispose()
        {
        }
    }
}
