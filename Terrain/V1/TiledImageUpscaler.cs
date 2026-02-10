using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using OpenGL;

namespace Terrain
{

    public class TiledImageUpscaler
    {
        /// <summary>
        /// 타일을 129x129 RAW로 업스케일
        /// RAW 포맷: 그레이스케일 16비트 (2바이트) 리틀 엔디안
        /// </summary>
        private static void UpscaleTileToRaw129(Bitmap tile, string outputPath, int targetWidth, int targetHeight)
        {
            using (Bitmap upscaled = new Bitmap(targetWidth, targetHeight))
            {
                using (Graphics g = Graphics.FromImage(upscaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.CompositingMode = CompositingMode.SourceCopy;

                    using (ImageAttributes imageAttributes = new ImageAttributes())
                    {
                        imageAttributes.SetWrapMode(WrapMode.TileFlipXY);

                        g.DrawImage(
                            tile,
                            new Rectangle(0, 0, targetWidth, targetHeight),
                            0, 0, tile.Width, tile.Height,
                            GraphicsUnit.Pixel,
                            imageAttributes);
                    }
                }

                // RAW 파일로 저장 (16비트 그레이스케일)
                SaveAsRaw16bit(upscaled, outputPath);
            }
        }

        /// <summary>
        /// 1081x1081 원본 이미지를 타일로 나누고 두 가지 RAW 버전으로 저장
        /// </summary>
        public static void ProcessTiledUpscale(string sourceImagePath, string outputFolder)
        {
            using (Bitmap sourceImage = new Bitmap(sourceImagePath))
            {
                Console.WriteLine($"원본 이미지 크기: {sourceImage.Width}x{sourceImage.Height}");

                if (sourceImage.Width != 1081 || sourceImage.Height != 1081)
                {
                    throw new ArgumentException($"원본 이미지는 1081x1081이어야 합니다. 현재: {sourceImage.Width}x{sourceImage.Height}");
                }

                // 출력 폴더 생성
                string folder129 = Path.Combine(outputFolder, "129x129_raw");
                string folder1025 = Path.Combine(outputFolder, "1025x1025_raw");

                Directory.CreateDirectory(folder129);
                Directory.CreateDirectory(folder1025);

                // 타일 개수 계산
                int tileSize = 20;
                int tilesX = (int)Math.Ceiling((sourceImage.Width - 1) / (double)tileSize);
                int tilesY = (int)Math.Ceiling((sourceImage.Height - 1) / (double)tileSize);

                Console.WriteLine($"생성할 타일 개수: {tilesX}x{tilesY} = {tilesX * tilesY}개");

                int processedCount = 0;
                int totalTiles = tilesX * tilesY;

                // 각 타일 처리
                for (int indexY = 0; indexY < tilesY; indexY++)
                {
                    for (int indexX = 0; indexX < tilesX; indexX++)
                    {
                        // 타일 시작 위치
                        int px = indexX * tileSize;
                        int py = indexY * tileSize;

                        // 타일 크기 (21x21, 단 경계에서는 작을 수 있음)
                        int tileWidth = Math.Min(21, sourceImage.Width - px);
                        int tileHeight = Math.Min(21, sourceImage.Height - py);

                        // 타일 추출
                        using (Bitmap tile = ExtractTile(sourceImage, px, py, tileWidth, tileHeight))
                        {
                            string baseName = $"tile_{indexX:D3}_{indexY:D3}";

                            // (1) 129x129 RAW 버전
                            string raw129Path = Path.Combine(folder129, baseName + ".raw");
                            UpscaleTileToRaw129(tile, raw129Path, 129, 129);

                            // (2) 1025x1025 RAW 버전
                            string raw1025Path = Path.Combine(folder1025, baseName + ".raw");
                            UpscaleTileToRaw(tile, raw1025Path, 1025, 1025);

                            processedCount++;
                            if (processedCount % 100 == 0 || processedCount == totalTiles)
                            {
                                Console.WriteLine($"진행률: {processedCount}/{totalTiles} ({(processedCount * 100.0 / totalTiles):F1}%)");
                            }
                        }
                    }
                }

                Console.WriteLine($"\n완료! 총 {processedCount}개 타일 생성");
                Console.WriteLine($"  - 129x129 RAW: {folder129}");
                Console.WriteLine($"  - 1025x1025 RAW: {folder1025}");
            }
        }

        /// <summary>
        /// 원본 이미지에서 타일 추출
        /// </summary>
        private static Bitmap ExtractTile(Bitmap source, int px, int py, int width, int height)
        {
            Bitmap tile = new Bitmap(width, height, source.PixelFormat);

            using (Graphics g = Graphics.FromImage(tile))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                g.DrawImage(
                    source,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(px, py, width, height),
                    GraphicsUnit.Pixel);
            }

            return tile;
        }

        /// <summary>
        /// 타일을 1025x1025 RAW로 업스케일
        /// RAW 포맷: 그레이스케일 16비트 (2바이트) 리틀 엔디안
        /// </summary>
        private static void UpscaleTileToRaw(Bitmap tile, string outputPath, int targetWidth, int targetHeight)
        {
            using (Bitmap upscaled = new Bitmap(targetWidth, targetHeight))
            {
                using (Graphics g = Graphics.FromImage(upscaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.CompositingMode = CompositingMode.SourceCopy;

                    using (ImageAttributes imageAttributes = new ImageAttributes())
                    {
                        imageAttributes.SetWrapMode(WrapMode.TileFlipXY);

                        g.DrawImage(
                            tile,
                            new Rectangle(0, 0, targetWidth, targetHeight),
                            0, 0, tile.Width, tile.Height,
                            GraphicsUnit.Pixel,
                            imageAttributes);
                    }
                }

                // RAW 파일로 저장 (16비트 그레이스케일)
                SaveAsRaw16bit(upscaled, outputPath);
            }
        }

        /// <summary>
        /// Bitmap을 16비트 그레이스케일 RAW로 저장
        /// </summary>
        private static void SaveAsRaw16bit(Bitmap bitmap, string outputPath)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            // 16비트 버퍼 생성 (2바이트 per 픽셀)
            byte[] rawData = new byte[width * height * 2];

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // ARGB 읽기
                            int offset = y * stride + x * 4;
                            byte b = ptr[offset];
                            byte g = ptr[offset + 1];
                            byte r = ptr[offset + 2];
                            byte a = ptr[offset + 3];

                            // 그레이스케일 변환 (표준 luminance 공식)
                            float gray = r * 0.299f + g * 0.587f + b * 0.114f;

                            // 8비트 → 16비트 변환 (0~255 → 0~65535)
                            ushort gray16 = (ushort)(gray * 257); // 257 = 65535/255

                            // 리틀 엔디안으로 저장
                            int rawOffset = (y * width + x) * 2;
                            rawData[rawOffset] = (byte)(gray16 & 0xFF);           // Low byte
                            rawData[rawOffset + 1] = (byte)((gray16 >> 8) & 0xFF); // High byte
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            // 파일에 쓰기
            File.WriteAllBytes(outputPath, rawData);
        }

        /// <summary>
        /// RAW 파일을 직접 GPU 텍스처로 로드
        /// </summary>
        public static uint LoadRaw16bitToGPU(string rawPath, int width, int height)
        {
            byte[] rawData = File.ReadAllBytes(rawPath);

            if (rawData.Length != width * height * 2)
            {
                throw new ArgumentException($"RAW 파일 크기가 맞지 않습니다. 예상: {width * height * 2}, 실제: {rawData.Length}");
            }

            // 텍스처 생성
            uint textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, textureId);

            // 16비트 데이터를 ushort 배열로 변환
            ushort[] heightData = new ushort[width * height];
            for (int i = 0; i < heightData.Length; i++)
            {
                // 리틀 엔디안 읽기
                heightData[i] = (ushort)(rawData[i * 2] | (rawData[i * 2 + 1] << 8));
            }

            // GPU에 업로드 (16비트 그레이스케일)
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R16,          // 16비트 red channel
                width,
                height,
                0,
                OpenGL.PixelFormat.Red,
                PixelType.UnsignedShort,
                heightData
            );

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return textureId;
        }
    }
}