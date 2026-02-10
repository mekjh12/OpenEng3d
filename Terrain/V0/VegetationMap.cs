using OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Terrain
{
    /// <summary>
    /// 지형에 나무, 풀, 잡목 등의 식생을 배치하기 위한 클래스
    /// RGBA 채널을 사용하여 각각 나무(R), 풀(G), 잡목(B)의 밀도를 저장
    /// </summary>
    public class VegetationMap
    {
        private int _width;          // 맵의 너비
        private int _height;         // 맵의 높이
        private byte[] _bitmap;      // RGBA 채널 데이터를 저장하는 1차원 배열
        private uint _texture;       // OpenGL 텍스처 핸들

        /// <summary>
        /// OpenGL 텍스처 핸들에 대한 접근자
        /// </summary>
        public uint Texture
        {
            get => _texture;
            set => _texture = value;
        }

        /// <summary>
        /// 지정된 크기의 식생 맵을 생성합니다.
        /// </summary>
        /// <param name="width">맵의 너비</param>
        /// <param name="height">맵의 높이</param>
        public VegetationMap(int width, int height)
        {
            _width = width;
            _height = height;
            _bitmap = new byte[_width * _height * 4]; // RGBA 4채널
        }

        /// <summary>
        /// 지정된 위치에 식생을 추가합니다.
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="size">식생의 크기/밀도 (0~1)</param>
        /// <param name="channel">채널 선택 (0:R-나무, 1:G-풀, 2:B-잡목, 3:A-예비)</param>
        public void AddVegetation(float x, float y, float size, int channel = 0)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height || channel < 0 || channel > 3)
                return;

            int i = (int)y;
            int j = (int)x;
            int index = (i * _width + j) * 4 + channel;
            _bitmap[index] = (byte)(size * 255);
        }

        /// <summary>
        /// 비트맵 데이터를 OpenGL 텍스처로 업로드합니다.
        /// RGBA 포맷으로 텍스처를 생성하고 필터링 및 래핑 설정을 적용합니다.
        /// </summary>
        public void UploadVegetationMapTexture()
        {
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0,
                         InternalFormat.Rgba8,
                         _width, _height, 0,
                         OpenGL.PixelFormat.Rgba,
                         PixelType.UnsignedByte,
                         _bitmap);

            // 선형 필터링 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);

            // 텍스처 래핑 모드 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
        }

        /// <summary>
        /// 현재 비트맵 데이터를 System.Drawing.Bitmap으로 변환합니다.
        /// 디버깅 용도로 식생 맵의 현재 상태를 시각화하는데 사용됩니다.
        /// </summary>
        /// <returns>식생 맵을 표현하는 32비트 ARGB Bitmap</returns>
        public Bitmap BitmapTexture()
        {
            Bitmap image = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, _width, _height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.WriteOnly, image.PixelFormat);

            int stride = bmpData.Stride;
            byte[] pixelBuffer = new byte[stride * _height];

            // RGBA 채널 데이터를 Bitmap 포맷에 맞게 변환
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int srcIndex = (y * _width + x) * 4;
                    int dstIndex = (y * stride) + (x * 4);

                    pixelBuffer[dstIndex + 0] = _bitmap[srcIndex + 2]; // B (잡목)
                    pixelBuffer[dstIndex + 1] = _bitmap[srcIndex + 1]; // G (풀)
                    pixelBuffer[dstIndex + 2] = _bitmap[srcIndex + 0]; // R (나무)
                    pixelBuffer[dstIndex + 3] = 255;                   // A (불투명)
                }
            }

            Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
            image.UnlockBits(bmpData);
            return image;
        }
    }
}