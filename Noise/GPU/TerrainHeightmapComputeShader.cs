using Common;
using OpenGL;
using System.Drawing.Imaging;
using System.Drawing;
using System;
using ZetaExt;

namespace Noise
{
    public class TerrainHeightmapComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"Noise\GPU\comp\terrain_heightmap.comp";

        private int loc_width;
        private int loc_height;
        private int loc_scale;
        private int loc_seed;
        private int loc_offsetX;
        private int loc_offsetY;

        private int loc_octaves;
        private int loc_lacunarity;
        private int loc_gain;
        private int loc_terrainType;
        private int loc_heightScale;
        private int loc_roughness;

        public TerrainHeightmapComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_width = GetUniformLocation("u_Width");
            loc_height = GetUniformLocation("u_Height");
            loc_scale = GetUniformLocation("u_Scale");
            loc_seed = GetUniformLocation("u_Seed");
            loc_offsetX = GetUniformLocation("u_OffsetX");
            loc_offsetY = GetUniformLocation("u_OffsetY");

            loc_octaves = GetUniformLocation("u_Octaves");
            loc_lacunarity = GetUniformLocation("u_Lacunarity");
            loc_gain = GetUniformLocation("u_Gain");
            loc_terrainType = GetUniformLocation("u_TerrainType");
            loc_heightScale = GetUniformLocation("u_HeightScale");
            loc_roughness = GetUniformLocation("u_Roughness");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 지형 높이맵 파라미터 로드
        /// </summary>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="scale">전체 스케일 (작을수록 확대)</param>
        /// <param name="seed">랜덤 시드</param>
        /// <param name="offsetX">X 오프셋</param>
        /// <param name="offsetY">Y 오프셋</param>
        /// <param name="octaves">FBM 옥타브 수</param>
        /// <param name="lacunarity">FBM 주파수 배율</param>
        /// <param name="gain">FBM 진폭 배율</param>
        /// <param name="terrainType">지형 타입 (0=Terrain, 1=Canyon, 2=Volcanic, 3=Island, 4=Desert, 5=Mountain)</param>
        /// <param name="heightScale">높이 강도 (0.0 ~ 2.0)</param>
        /// <param name="roughness">거칠기 (0.0 ~ 1.0)</param>
        public void LoadParams(
            int width,
            int height,
            float scale = 1.0f,
            int seed = 0,
            float offsetX = 0.0f,
            float offsetY = 0.0f,
            int octaves = 6,
            float lacunarity = 2.0f,
            float gain = 0.5f,
            int terrainType = 0,
            float heightScale = 1.0f,
            float roughness = 0.5f)
        {
            LoadUniform1i(loc_width, width);
            LoadUniform1i(loc_height, height);
            LoadUniform1f(loc_scale, scale);
            LoadUniform1i(loc_seed, seed);
            LoadUniform1f(loc_offsetX, offsetX);
            LoadUniform1f(loc_offsetY, offsetY);

            LoadUniform1i(loc_octaves, octaves);
            LoadUniform1f(loc_lacunarity, lacunarity);
            LoadUniform1f(loc_gain, gain);
            LoadUniform1i(loc_terrainType, terrainType);
            LoadUniform1f(loc_heightScale, heightScale);
            LoadUniform1f(loc_roughness, roughness);
        }

        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }

        /// <summary>
        /// 높이맵 텍스처를 PNG로 저장
        /// </summary>
        public static Bitmap SaveToPNG(uint textureId, int width, int height)
        {
            Gl.Finish();
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            float[] pixels = new float[width * height];
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, pixels);

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float value = pixels[index];

                    byte gray = (byte)(value.Clamp(0.0f, 1.0f) * 255.0f);
                    Color color = Color.FromArgb(gray, gray, gray);
                    bitmap.SetPixel(x, height - 1 - y, color);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// 높이맵을 고도 컬러맵으로 저장
        /// </summary>
        public static Bitmap SaveWithElevationColorMap(uint textureId, int width, int height)
        {
            return SaveToPNGWithColorMap(textureId, width, height, (h) =>
            {
                // 고도별 색상
                if (h < 0.2f)
                    return Color.FromArgb(34, 139, 34);   // 낮은 지대 (초록)
                else if (h < 0.4f)
                    return Color.FromArgb(139, 90, 43);   // 언덕 (갈색)
                else if (h < 0.6f)
                    return Color.FromArgb(128, 128, 128); // 산 (회색)
                else if (h < 0.8f)
                    return Color.FromArgb(169, 169, 169); // 고산 (밝은 회색)
                else
                    return Color.FromArgb(255, 255, 255); // 눈 (흰색)
            });
        }

        private static Bitmap SaveToPNGWithColorMap(uint textureId, int width, int height, Func<float, Color> colorMap)
        {
            Gl.Finish();
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            float[] pixels = new float[width * height];
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, pixels);

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float value = pixels[index];
                    Color color = colorMap(value);
                    bitmap.SetPixel(x, height - 1 - y, color);
                }
            }

            return bitmap;
        }
    }
}