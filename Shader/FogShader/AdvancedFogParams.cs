using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// Advanced Fog 파라미터 구조체 (Zero-allocation friendly)
    /// GLSL의 uniform 변수들과 대응
    /// </summary>
    public struct AdvancedFogParams
    {
        // Height Fog 색상
        public Vertex3f Color;

        // Distance fog
        public float DistanceDensity;
        public float DistanceStart;

        // Height fog
        public float HeightFalloff;
        public float HeightDensity;
        public float HeightMin;
        public float HeightMax;

        // Layered fog
        public bool EnableLayers;
        public float LayerHeight;
        public float LayerThickness;
        public float LayerDensity;

        /// <summary>
        /// 기본 안개 프리셋
        /// </summary>
        public static AdvancedFogParams CreateDefault()
        {
            return new AdvancedFogParams
            {
                Color = new Vertex3f(0.7f, 0.8f, 0.9f),
                DistanceDensity = 0.0003f,
                DistanceStart = 0f,
                HeightFalloff = 2.0f,
                HeightDensity = 0.5f,
                HeightMin = 0f,
                HeightMax = 500f,
                EnableLayers = false,
                LayerHeight = 200f,
                LayerThickness = 50f,
                LayerDensity = 0.3f
            };
        }

        /// <summary>
        /// 산악 지형용 안개 프리셋
        /// </summary>
        public static AdvancedFogParams CreateMountain()
        {
            return new AdvancedFogParams
            {
                Color = new Vertex3f(0.6f, 0.7f, 0.85f),
                DistanceDensity = 0.0002f,
                DistanceStart = 100f,
                HeightFalloff = 3.0f,
                HeightDensity = 0.7f,
                HeightMin = 0f,
                HeightMax = 800f,
                EnableLayers = true,
                LayerHeight = 300f,
                LayerThickness = 80f,
                LayerDensity = 0.4f
            };
        }

        /// <summary>
        /// 평원 지형용 안개 프리셋
        /// </summary>
        public static AdvancedFogParams CreatePlains()
        {
            return new AdvancedFogParams
            {
                Color = new Vertex3f(1, 0, 0),
                DistanceDensity = 0.001f,
                DistanceStart = 100f,
                HeightFalloff = 1.5f,
                HeightDensity = 0.8f,
                HeightMin = 0f,
                HeightMax = 200f,
                EnableLayers = false,
                LayerHeight = 0f,
                LayerThickness = 0f,
                LayerDensity = 0f
            };
        }

    }
}