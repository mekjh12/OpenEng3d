using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenGL;


namespace Fog
{
    public class FogTextureLoader
    {
        public static uint LoadFogTextureAsR8(string path)
        {
            using (Bitmap bitmap = new Bitmap(path))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;

                // R 채널만 추출
                byte[] rChannelData = new byte[width * height];

                // BitmapData로 빠르게 읽기
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = Math.Abs(bmpData.Stride);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int pixelOffset = y * stride + x * 4;
                            // BGRA 포맷이므로 R은 2번째 인덱스
                            byte r = ptr[pixelOffset + 2];
                            rChannelData[y * width + x] = r;
                        }
                    }
                }

                bitmap.UnlockBits(bmpData);

                // OpenGL.Net으로 텍스처 생성
                uint textureId = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, textureId);

                // R8 포맷으로 업로드
                Gl.TexImage2D(
                    TextureTarget.Texture2d,
                    0,
                    InternalFormat.R8,          // R8 포맷
                    width,
                    height,
                    0,
                    OpenGL.PixelFormat.Red,     // Red 채널만
                    PixelType.UnsignedByte,
                    rChannelData
                );

                // Swizzle 설정 (R을 모든 채널로 맵핑)
                int[] swizzleMask = new int[]
                {
                Gl.RED,    // R → R
                Gl.RED,    // G → R
                Gl.RED,    // B → R
                Gl.ONE     // A → 1
                };

                Gl.TexParameter(
                    TextureTarget.Texture2d,
                    TextureParameterName.TextureSwizzleRgba,
                    swizzleMask
                );

                // 필터링 설정
                Gl.TexParameter(
                    TextureTarget.Texture2d,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.LinearMipmapLinear
                );

                Gl.TexParameter(
                    TextureTarget.Texture2d,
                    TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Linear
                );

                // Wrap 모드
                Gl.TexParameter(
                    TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapS,
                    (int)TextureWrapMode.Repeat
                );

                Gl.TexParameter(
                    TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapT,
                    (int)TextureWrapMode.Repeat
                );

                // Mipmap 생성
                Gl.GenerateMipmap(TextureTarget.Texture2d);

                Console.WriteLine($"R8 텍스처 로드: {width}x{height}, {rChannelData.Length} bytes");

                return textureId;
            }
        }

        // Unsafe 없이 느리지만 안전한 버전
        public static uint LoadFogTextureAsR8_Safe(string path)
        {
            using (Bitmap bitmap = new Bitmap(path))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                byte[] rChannelData = new byte[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        rChannelData[y * width + x] = pixel.R;
                    }
                }

                uint textureId = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, textureId);

                Gl.TexImage2D(
                    TextureTarget.Texture2d,
                    0,
                    InternalFormat.R8,
                    width,
                    height,
                    0,
                    OpenGL.PixelFormat.Red,
                    PixelType.UnsignedByte,
                    rChannelData
                );

                // Swizzle
                int[] swizzle = new int[] { Gl.RED, Gl.RED, Gl.RED, Gl.ONE };
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureSwizzleRgba, swizzle);

                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.LinearMipmapLinear);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapS,
                    (int)TextureWrapMode.Repeat);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapT,
                    (int)TextureWrapMode.Repeat);

                Gl.GenerateMipmap(TextureTarget.Texture2d);

                return textureId;
            }
        }
    }
}