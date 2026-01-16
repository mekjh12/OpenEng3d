using Common;
using OpenGL;
using System.Drawing.Imaging;
using System.Drawing;
using System;
using ZetaExt;

namespace Noise
{
    public class WorleyNoiseComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"Noise\GPU\comp\worley_noise.comp";

        private int loc_width;
        private int loc_height;
        private int loc_cellSize;
        private int loc_jitter;
        private int loc_distanceType;
        private int loc_noiseType;
        private int loc_seed;
        private int loc_offsetX;
        private int loc_offsetY;

        // FBM 및 Cloud 모드 추가
        private int loc_octaves;
        private int loc_lacunarity;
        private int loc_gain;
        private int loc_cloudMode;

        public WorleyNoiseComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_width = GetUniformLocation("u_Width");
            loc_height = GetUniformLocation("u_Height");
            loc_cellSize = GetUniformLocation("u_CellSize");
            loc_jitter = GetUniformLocation("u_Jitter");
            loc_distanceType = GetUniformLocation("u_DistanceType");
            loc_noiseType = GetUniformLocation("u_NoiseType");
            loc_seed = GetUniformLocation("u_Seed");
            loc_offsetX = GetUniformLocation("u_OffsetX");
            loc_offsetY = GetUniformLocation("u_OffsetY");

            // FBM 및 Cloud 모드
            loc_octaves = GetUniformLocation("u_Octaves");
            loc_lacunarity = GetUniformLocation("u_Lacunarity");
            loc_gain = GetUniformLocation("u_Gain");
            loc_cloudMode = GetUniformLocation("u_CloudMode");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// Worley 노이즈 파라미터 로드
        /// </summary>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="cellSize">셀 크기 (작을수록 더 많은 셀)</param>
        /// <param name="jitter">포인트 랜덤성 (0.0 ~ 1.0, 0이면 규칙적, 1이면 완전 랜덤)</param>
        /// <param name="distanceType">거리 계산 방식 (0=Euclidean, 1=Manhattan, 2=Chebyshev)</param>
        /// <param name="noiseType">노이즈 타입 (0=F1, 1=F2, 2=F2-F1)</param>
        /// <param name="seed">랜덤 시드</param>
        /// <param name="offsetX">X 오프셋</param>
        /// <param name="offsetY">Y 오프셋</param>
        /// <param name="octaves">FBM 옥타브 수</param>
        /// <param name="lacunarity">FBM 주파수 배율 (기본 2.0)</param>
        /// <param name="gain">FBM 진폭 배율 (기본 0.5)</param>
        /// <param name="cloudMode">모드 (0=Worley, 1=Worley-FBM, 2=Cloud)</param>
        public void LoadParams(
            int width,
            int height,
            float cellSize = 20.0f,
            float jitter = 1.0f,
            int distanceType = 0,
            int noiseType = 0,
            int seed = 0,
            float offsetX = 0.0f,
            float offsetY = 0.0f,
            int octaves = 4,
            float lacunarity = 2.0f,
            float gain = 0.5f,
            int cloudMode = 0)
        {
            LoadUniform1i(loc_width, width);
            LoadUniform1i(loc_height, height);
            LoadUniform1f(loc_cellSize, cellSize);
            LoadUniform1f(loc_jitter, jitter);
            LoadUniform1i(loc_distanceType, distanceType);
            LoadUniform1i(loc_noiseType, noiseType);
            LoadUniform1i(loc_seed, seed);
            LoadUniform1f(loc_offsetX, offsetX);
            LoadUniform1f(loc_offsetY, offsetY);

            // FBM 및 Cloud 모드 파라미터
            LoadUniform1i(loc_octaves, octaves);
            LoadUniform1f(loc_lacunarity, lacunarity);
            LoadUniform1f(loc_gain, gain);
            LoadUniform1i(loc_cloudMode, cloudMode);
        }

        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }

        /// <summary>
        /// Worley 노이즈 텍스처를 PNG로 저장
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
        /// Worley 노이즈를 컬러맵으로 저장
        /// </summary>
        public static Bitmap SaveToPNGWithColorMap(uint textureId, int width, int height, Func<float, Color> colorMap)
        {
            Gl.Finish();
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            float[] pixels = new float[width * height];
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, pixels);

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        float value = pixels[index];

                        Color color = colorMap(value);

                        int flippedY = height - 1 - y;
                        int pixelIndex = flippedY * stride + x * 3;

                        ptr[pixelIndex + 0] = color.B;
                        ptr[pixelIndex + 1] = color.G;
                        ptr[pixelIndex + 2] = color.R;
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
            return bitmap;
        }
    }
}