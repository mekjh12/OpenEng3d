using Noise;
using OpenGL;
using System;
using System.Collections.Generic;
using Terrain;
using ZetaExt;

namespace Renderer
{
    public class GrassTile
    {
        private Vertex2i _coord;
        private float _tileSize;
        private List<GrassInstance> _instances;
        private TerrainData _terrainData;

        private const float GRASS_SPACING = 0.1f;
        private const float JITTER_RATIO = 0.4f;
        //private const float MIN_SLOPE = 0.0f;
        //private const float MAX_SLOPE = 0.7f;

        public Vertex2i Coord => _coord;


        public GrassTile(Vertex2i coord, float tileSize, TerrainData terrainData)
        {
            _instances = new List<GrassInstance>();
            Reinitialize(coord, tileSize, terrainData);
        }

        /// <summary>
        /// 풀에서 재사용할 때 호출
        /// </summary>
        public void Reinitialize(Vertex2i coord, float tileSize, TerrainData terrainData)
        {
            _terrainData = terrainData;
            _coord = coord;
            _tileSize = tileSize;
            _instances.Clear();
            GenerateGrass();
        }

        private void GenerateGrass()
        {
            float tileWorldX = _coord.x * _tileSize;
            float tileWorldY = _coord.y * _tileSize;

            Random rand = new Random(_coord.GetHashCode());

            int gridCountX = (int)(_tileSize / GRASS_SPACING);
            int gridCountY = (int)(_tileSize / GRASS_SPACING);

            for (int iy = 0; iy < gridCountY; iy++)
            {
                for (int ix = 0; ix < gridCountX; ix++)
                {
                    float gridCenterX = ix * GRASS_SPACING + GRASS_SPACING * 0.5f;
                    float gridCenterY = iy * GRASS_SPACING + GRASS_SPACING * 0.5f;

                    float jitterX = ((float)rand.NextDouble() - 0.5f) *
                                    2.0f * JITTER_RATIO * GRASS_SPACING;
                    float jitterY = ((float)rand.NextDouble() - 0.5f) *
                                    2.0f * JITTER_RATIO * GRASS_SPACING;

                    float localX = gridCenterX;// + jitterX;
                    float localY = gridCenterY;// + jitterY;

                    float worldX = tileWorldX + localX;
                    float worldY = tileWorldY + localY;

                    float worldZ = _terrainData.GetTerrainHeight(worldX, worldY);
                    Vertex3f normal = SampleTerrainNormal(worldX, worldY);

                    //float slope = 1.0f - normal.z;

                    //if (rand.NextDouble() > GetDensityMultiplier(worldX, worldY)) continue;

                    GrassInstance grass = new GrassInstance
                    {
                        Position = new Vertex3f(worldX, worldY, worldZ),
                        Rotation = (float)(rand.NextDouble() * Math.PI * 2),
                        Scale = 0.8f + (float)rand.NextDouble() * 0.4f
                    };

                    _instances.Add(grass);
                }
            }
        }

        private float GetDensityMultiplier(float worldX, float worldY)
        {
            float noise = SimplexNoise.Generate(worldX * 0.05f, worldY * 0.05f);
            noise = (noise + 1.0f) * 0.5f;

            if (noise < 0.5f) return 0.0f;

            return (noise - 0.5f) * 2.0f;
        }

        private float SampleTerrainHeight(float worldX, float worldY)
        {
            return _terrainData.GetTerrainHeight(worldX, worldY);
        }

        private Vertex3f SampleTerrainNormal(float worldX, float worldY)
        {
            float delta = 0.1f;
            float h0 = SampleTerrainHeight(worldX, worldY);
            float hx = SampleTerrainHeight(worldX + delta, worldY);
            float hy = SampleTerrainHeight(worldX, worldY + delta);

            Vertex3f dx = new Vertex3f(delta, 0, hx - h0);
            Vertex3f dy = new Vertex3f(0, delta, hy - h0);

            return dx.Cross(dy).Normalized;
        }

        public void GetGrassInstances(ref List<GrassInstanceData> result)
        {
            foreach (var grass in _instances)
            {
                result.Add(new GrassInstanceData
                {
                    Position = grass.Position,
                    Rotation = grass.Rotation
                });
            }
        }

        /// <summary>
        /// 풀로 반환할 때 호출 (데이터만 정리)
        /// </summary>
        public void Clear()
        {
            _instances.Clear();
        }

        /// <summary>
        /// 완전히 정리할 때만 호출
        /// </summary>
        public void Dispose()
        {
            _instances.Clear();
            _instances = null;
        }
    }
}