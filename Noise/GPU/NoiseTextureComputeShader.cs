using Common;
using OpenGL;
using System.Drawing.Imaging;
using System.Drawing;
using System;
using ZetaExt;

namespace Noise
{
    public class NoiseTextureComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"Noise\GPU\comp\noise_texture.comp";

        private int loc_width;
        private int loc_height;
        private int loc_scale;
        private int loc_octaves;
        private int loc_persistence;
        private int loc_lacunarity;
        private int loc_seed;
        private int loc_offsetX;
        private int loc_offsetY;
        private int loc_mode;

        public NoiseTextureComputeShader(string projectPath) : base()
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
            loc_octaves = GetUniformLocation("u_Octaves");
            loc_persistence = GetUniformLocation("u_Persistence");
            loc_lacunarity = GetUniformLocation("u_Lacunarity");
            loc_seed = GetUniformLocation("u_Seed");
            loc_offsetX = GetUniformLocation("u_OffsetX");
            loc_offsetY = GetUniformLocation("u_OffsetY");
            loc_mode = GetUniformLocation("u_Mode");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }


        public void LoadMode(int mode)
        {
            Gl.Uniform1(loc_mode, mode);
        }

        /// <summary>
        /// 노이즈 파라미터 로드
        /// </summary>
        /// <param name="scale">노이즈 스케일 (클수록 더 넓은 패턴)</param>
        /// <param name="octaves">옥타브 수 (디테일 레벨)</param>
        /// <param name="persistence">각 옥타브의 진폭 감소율 (0.0~1.0)</param>
        /// <param name="lacunarity">각 옥타브의 주파수 증가율 (보통 2.0)</param>
        /// <param name="seed">랜덤 시드</param>
        /// <param name="offsetX">X 오프셋</param>
        /// <param name="offsetY">Y 오프셋</param>
        public void LoadParams(
            int width,
            int height,
            float scale = 50.0f,
            int octaves = 4,
            float persistence = 0.5f,
            float lacunarity = 2.0f,
            int seed = 0,
            float offsetX = 0.0f,
            float offsetY = 0.0f)
        {
            Gl.Uniform1(loc_width, width);
            Gl.Uniform1(loc_height, height);
            Gl.Uniform1(loc_scale, scale);
            Gl.Uniform1(loc_octaves, octaves);
            Gl.Uniform1(loc_persistence, persistence);
            Gl.Uniform1(loc_lacunarity, lacunarity);
            Gl.Uniform1(loc_seed, seed);
            Gl.Uniform1(loc_offsetX, offsetX);
            Gl.Uniform1(loc_offsetY, offsetY);
        }

        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }

        /// <summary>
        /// 노이즈 텍스처를 PNG 파일로 저장
        /// </summary>
        /// <param name="textureId">OpenGL 텍스처 ID</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        public static Bitmap SaveToPNG(uint textureId, int width, int height)
        {
            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            // R32F 데이터 읽기
            float[] pixels = new float[width * height];
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, pixels);

            // Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // 픽셀 데이터 변환 (float → byte)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float value = pixels[index];

                    // 0.0~1.0 범위를 0~255로 변환
                    byte gray = (byte)(value.Clamp(0.0f, 1.0f) * 255.0f);

                    Color color = Color.FromArgb(gray, gray, gray);
                    bitmap.SetPixel(x, height - 1 - y, color);  // OpenGL은 상하 반전
                }
            }

            return bitmap;
        }

        /// <summary>
        /// 노이즈 텍스처를 PNG로 저장 (컬러 맵 적용 버전)
        /// </summary>
        /// <param name="textureId">OpenGL 텍스처 ID</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="colorMap">컬러 매핑 함수 (0.0~1.0 → Color)</param>
        public static Bitmap SaveToPNGWithColorMap(uint textureId, int width, int height, Func<float, Color> colorMap = null)
        {
            // 기본 그레이스케일 맵
            if (colorMap == null)
            {
                colorMap = (value) =>
                {
                    byte gray = (byte)(value.Clamp(0.0f, 1.0f) * 255.0f);
                    return Color.FromArgb(gray, gray, gray);
                };
            }

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            // R32F 데이터 읽기
            float[] pixels = new float[width * height];
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, pixels);

            // Bitmap 생성 (더 빠른 방법: LockBits 사용)
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

                        // OpenGL은 상하 반전
                        int flippedY = height - 1 - y;
                        int pixelIndex = flippedY * stride + x * 3;

                        ptr[pixelIndex + 0] = color.B;  // BGR 순서
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