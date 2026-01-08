using Common.Abstractions;
using Geometry;
using OpenGL;
using System;

namespace Renderer
{
    /// <summary>
    /// GPU-Driven 풀 시스템
    /// - TerrainData 불필요, 높이맵 텍스처만 사용
    /// - GrassTileManager 불필요, 카메라 기반 자동 타일링
    /// </summary>
    public class GrassSystemGPUDriven
    {
        private GrassRendererGPUDriven _renderer;

        // 지형 텍스처 ID만 저장
        private uint _heightmapTexture;
        private uint _normalMapTexture;

        public GrassSystemGPUDriven(string projectPath)
        {
            _renderer = new GrassRendererGPUDriven(projectPath);
            Console.WriteLine("[GrassSystemGPUDriven] Initialized");
        }

        /// <summary>
        /// 지형 높이맵과 노멀맵 텍스처 설정
        /// </summary>
        public void SetHeightmapTextures(uint heightmapTexture, uint normalMapTexture)
        {
            _heightmapTexture = heightmapTexture;
            _normalMapTexture = normalMapTexture;
            Console.WriteLine("[GrassSystemGPUDriven] Heightmap textures set");
        }

        public void Update(Camera camera, Polyhedron viewFrustum)
        {
            _renderer.Update(camera, viewFrustum);
        }

        public void Render(Camera camera, Vertex3f sunDirection)
        {
            _renderer.Render(camera, sunDirection, _heightmapTexture, _normalMapTexture);
        }

        public void Dispose()
        {
            _renderer?.Dispose();
        }
    }
}