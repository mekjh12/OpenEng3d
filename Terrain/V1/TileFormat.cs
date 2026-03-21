using OpenGL;

namespace Terrain
{
    /// <summary>
    /// 타일 형식 설정 구조체
    /// </summary>
    public struct TileFormat
    {
        public uint TileSize;
        public uint ChannelCount;
        public PixelFormat PixelFormat;
        public InternalFormat InternalFormat;
        public int BytesPerChannel;
        public float NormalizeValue;

        /// <summary>
        /// 하이트맵 (16bit -> float)
        /// </summary>
        public static TileFormat HeightmapLowFloat() => new TileFormat
        {
            TileSize = 129,
            ChannelCount = 1,
            PixelFormat = OpenGL.PixelFormat.Red,
            InternalFormat = InternalFormat.R32f,
            BytesPerChannel = 2, // ushort
            NormalizeValue = 65535.0f
        };

        /// <summary>
        /// 하이트맵 (16bit -> float)
        /// </summary>
        public static TileFormat HeightmapHighFloat() => new TileFormat
        {
            TileSize = 1025,
            ChannelCount = 1,
            PixelFormat = OpenGL.PixelFormat.Red,
            InternalFormat = InternalFormat.R32f,
            BytesPerChannel = 2, // ushort
            NormalizeValue = 65535.0f
        };

        /// <summary>
        /// 노말맵 (RGB 8bit)
        /// </summary>
        public static TileFormat MapRGB() => new TileFormat
        {
            TileSize = 1025,
            ChannelCount = 3,
            PixelFormat = OpenGL.PixelFormat.Rgb,
            InternalFormat = InternalFormat.Rgb8,
            BytesPerChannel = 1, // byte
            NormalizeValue = 255.0f
        };

        public int GetExpectedFileSize()
            => (int)(TileSize * TileSize * ChannelCount * BytesPerChannel);
    }
}
