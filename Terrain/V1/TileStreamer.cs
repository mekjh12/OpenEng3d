using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenGL;

namespace Terrain
{
    /// <summary>
    /// 3x3 리전의 강/도로 텍스처를 관리합니다.
    /// R채널 = 도로, B채널 = 강
    /// </summary>
    public class TileStreamer
    {
        private const float TileSize = 9216f;
        private const int SlotCount = 9;

        private readonly int[] _tileX = new int[SlotCount];
        private readonly int[] _tileY = new int[SlotCount];
        private readonly uint[] _texIds = new uint[SlotCount];

        /// <summary>
        /// 파일명 9개를 받아 텍스처를 로드합니다.
        /// 파일명 형식: {name}_{tileX}_{tileY}  예) river_road_0_0
        /// </summary>
        public TileStreamer(string[] fileNames, string directory = "tiles")
        {
            if (fileNames.Length != SlotCount)
                throw new ArgumentException($"파일명은 정확히 {SlotCount}개여야 합니다.");

            for (int i = 0; i < SlotCount; i++)
            {
                ParseTileIndex(fileNames[i], out _tileX[i], out _tileY[i]);
                _texIds[i] = LoadTexture(Path.Combine(directory, fileNames[i] + ".png"));
            }
        }

        /// <summary>
        /// 리전 좌표에 해당하는 텍스처 ID를 반환합니다.
        /// 없으면 fallback(빈) 텍스처를 반환합니다.
        /// </summary>
        public uint GetTexture(int regionX, int regionY)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_tileX[i] == regionX && _tileY[i] == regionY)
                    return _texIds[i];
            }

            return 0;
        }

        /// <summary>
        /// 월드 좌표에 해당하는 텍스처 ID를 반환합니다.
        /// </summary>
        public uint GetTexture(float worldX, float worldY)
        {
            int regionX = (int)Math.Floor(worldX / TileSize);
            int regionY = (int)Math.Floor(worldY / TileSize);
            return GetTexture(regionX, regionY);
        }


        public void Dispose()
        {
            for (int i = 0; i < SlotCount; i++)
                if (_texIds[i] != 0) Gl.DeleteTextures(_texIds[i]);
        }

        // ============================================================
        // 내부
        // ============================================================

        private static uint LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[RiverRoadStreamer] 파일 없음 → {path}");
                return 0;
            }

            Bitmap bitmap = (Bitmap)Bitmap.FromFile(path);
            //bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bitmap.RotateFlip(RotateFlipType.Rotate90FlipX);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            uint texId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, texId);
            Gl.TexImage2D(
                TextureTarget.Texture2d, 0,
                InternalFormat.Rgba,
                data.Width, data.Height, 0,
                OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                data.Scan0
            );

            // 업스케링으로 인하여 Linear보간
            Gl.TexParameteri(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
            Gl.TexParameteri(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            bitmap.UnlockBits(data);
            bitmap.Dispose();
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return texId;
        }

        private static void ParseTileIndex(string fileName, out int tileX, out int tileY)
        {
            int last = fileName.LastIndexOf('_');
            int second = fileName.LastIndexOf('_', last - 1);

            if (last < 0 || second < 0)
                throw new FormatException($"파일명 형식 오류: '{fileName}' → 예: river_road_0_0");

            if (!int.TryParse(fileName.Substring(second + 1, last - second - 1), out tileX) ||
                !int.TryParse(fileName.Substring(last + 1), out tileY))
                throw new FormatException($"타일 인덱스 파싱 실패: '{fileName}'");
        }
    }
}