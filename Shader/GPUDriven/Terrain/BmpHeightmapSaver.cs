using StbImageWriteSharp;
using System;
using System.IO;
using ZetaExt;

namespace Common
{
    public static class BmpHeightmapSaver
    {
        public static void SaveNormalMapAsRaw8(float[] normalRGB, int width, int height, string filePath, bool saveMeta = true)
        {
            EnsureDirectoryExists(filePath);

            byte[] buffer = new byte[width * height * 3];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(normalRGB[i].Clamp(0f, 1f) * 255f);
            }

            File.WriteAllBytes(filePath, buffer);

            long fileSize = new FileInfo(filePath).Length;
            Console.WriteLine($"[RAW 8bit NormalMap] {width}x{height}, {fileSize / 1024.0:F2} KB (RGB 3채널)");

            if (saveMeta)
            {
                string metaPath = Path.ChangeExtension(filePath, ".txt");
                File.WriteAllText(metaPath,
                    $"File: {Path.GetFileName(filePath)}\n" +
                    $"Width: {width}\n" +
                    $"Height: {height}\n" +
                    $"Format: 8-bit RGB (3 channels)\n" +
                    $"Byte order: R,G,B interleaved\n" +
                    $"Encoding: [-1,1] → [0,255] (decode: value/255*2-1)\n" +
                    $"File size: {fileSize} bytes\n");
            }
        }

        /// <summary>
        /// 48bit RGB BMP 저장 (Alpha 무시, R=G=B grayscale)
        /// </summary>
        public static void SaveAs48BitBmp(float[] rgbaData, int width, int height, string filePath)
        {
            ushort[] data16bit = ConvertRgbaFloatToUshort(rgbaData, width, height);
            EnsureDirectoryExists(filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                int bytesPerPixel = 6; // 48bit = 16*3
                int rowSize = width * bytesPerPixel;
                int paddedRowSize = (rowSize + 3) & ~3;
                int padding = paddedRowSize - rowSize;
                int pixelDataSize = paddedRowSize * height;

                // BMP File Header
                writer.Write((ushort)0x4D42);
                writer.Write(14 + 40 + pixelDataSize);
                writer.Write((uint)0);
                writer.Write(14 + 40);

                // DIB Header
                writer.Write(40);
                writer.Write(width);
                writer.Write(height);
                writer.Write((ushort)1);
                writer.Write((ushort)48);                  // 48bit RGB
                writer.Write((uint)0);
                writer.Write(pixelDataSize);
                writer.Write(2835);
                writer.Write(2835);
                writer.Write((uint)0);
                writer.Write((uint)0);

                // Pixel Data (BGR, R 채널만 사용)
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort value = data16bit[y * width + x];
                        writer.Write(value); // B
                        writer.Write(value); // G
                        writer.Write(value); // R
                    }
                    for (int i = 0; i < padding; i++)
                        writer.Write((byte)0);
                }
            }

            long fileSize = new FileInfo(filePath).Length;
            Console.WriteLine($"[BMP 48bit RGB] {width}x{height}, {fileSize / 1024.0:F2} KB (Alpha 무시)");
        }

        /// <summary>
        /// 64bit RGBA BMP 저장 (Alpha 포함, 각 채널 16bit)
        /// </summary>
        public static void SaveAs64BitRgbaBmp(float[] rgbaData, int width, int height, string filePath)
        {
            if (rgbaData.Length != width * height * 4)
                throw new ArgumentException($"데이터 크기 불일치: {rgbaData.Length} != {width * height * 4}");

            EnsureDirectoryExists(filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                int bytesPerPixel = 8; // 64bit = 16*4 (RGBA)
                int rowSize = width * bytesPerPixel;
                int paddedRowSize = (rowSize + 3) & ~3;
                int padding = paddedRowSize - rowSize;
                int pixelDataSize = paddedRowSize * height;

                // BMP File Header
                writer.Write((ushort)0x4D42);
                writer.Write(14 + 40 + pixelDataSize);
                writer.Write((uint)0);
                writer.Write(14 + 40);

                // DIB Header
                writer.Write(40);
                writer.Write(width);
                writer.Write(height);
                writer.Write((ushort)1);
                writer.Write((ushort)64);                  // 64bit RGBA
                writer.Write((uint)0);
                writer.Write(pixelDataSize);
                writer.Write(2835);
                writer.Write(2835);
                writer.Write((uint)0);
                writer.Write((uint)0);

                // Pixel Data (BGRA 순서)
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * width + x) * 4;

                        // RGBA -> 16bit 변환
                        ushort r = FloatToUshort(rgbaData[idx + 0]);
                        ushort g = FloatToUshort(rgbaData[idx + 1]);
                        ushort b = FloatToUshort(rgbaData[idx + 2]);
                        ushort a = FloatToUshort(rgbaData[idx + 3]);

                        // BGRA 순서로 저장
                        writer.Write(b);
                        writer.Write(g);
                        writer.Write(r);
                        writer.Write(a);
                    }
                    for (int i = 0; i < padding; i++)
                        writer.Write((byte)0);
                }
            }

            long fileSize = new FileInfo(filePath).Length;
            Console.WriteLine($"[BMP 64bit RGBA] {width}x{height}, {fileSize / 1024.0:F2} KB (Alpha 포함)");
        }

        /// <summary>
        /// 4채널 RGBA RAW 16bit 저장 (각 채널 16bit, 헤더 없음)
        /// </summary>
        public static void SaveAsRaw64Rgba(float[] rgbaData, int width, int height, string filePath, bool saveMeta = true)
        {
            if (rgbaData.Length != width * height * 4)
                throw new ArgumentException($"데이터 크기 불일치");

            EnsureDirectoryExists(filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                for (int i = 0; i < rgbaData.Length; i++)
                {
                    ushort value = FloatToUshort(rgbaData[i]);
                    writer.Write(value);
                }
            }

            long fileSize = new FileInfo(filePath).Length;
            Console.WriteLine($"[RAW 64bit RGBA] {width}x{height}, {fileSize / 1024.0:F2} KB");

            if (saveMeta)
            {
                string metaPath = Path.ChangeExtension(filePath, ".txt");
                File.WriteAllText(metaPath,
                    $"File: {Path.GetFileName(filePath)}\n" +
                    $"Width: {width}\n" +
                    $"Height: {height}\n" +
                    $"Format: 64-bit RGBA (16bit per channel)\n" +
                    $"Channels: R, G, B, A\n" +
                    $"Byte order: Little Endian\n" +
                    $"Channel order: RGBA\n" +
                    $"Value range: 0-65535\n" +
                    $"File size: {fileSize} bytes\n");
                Console.WriteLine($"[RAW] 메타데이터: {metaPath}");
            }
        }

        /// <summary>
        /// 단일 채널 RAW 16bit 저장 (R 채널만, Alpha 무시)
        /// </summary>
        public static void SaveAsRaw16(float[] rgbaData, int width, int height, string filePath, 
            bool saveMeta = true, bool saveWithLowRes = true)
        {
            // 원본 저장
            ushort[] data16bit = ConvertRgbaFloatToUshort(rgbaData, width, height);
            EnsureDirectoryExists(filePath);
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                foreach (ushort value in data16bit)
                    writer.Write(value);
            }
            long fileSize = new FileInfo(filePath).Length;
            Console.WriteLine($"[RAW 16bit] {width}x{height}, {fileSize / 1024.0:F2} KB (R 채널만)");

            if (saveMeta)
            {
                string metaPath = Path.ChangeExtension(filePath, ".txt");
                File.WriteAllText(metaPath,
                    $"File: {Path.GetFileName(filePath)}\n" +
                    $"Width: {width}\n" +
                    $"Height: {height}\n" +
                    $"Format: 16-bit grayscale\n" +
                    $"Byte order: Little Endian\n" +
                    $"Value range: 0-65535\n" +
                    $"File size: {fileSize} bytes\n");
            }

            // 129x129 low 버전 저장
            if (saveWithLowRes)
            {
                const int lowResSize = 129;
                float[] lowResRgba = DownsampleRgba(rgbaData, width, height, lowResSize, lowResSize);
                ushort[] lowRes16bit = ConvertRgbaFloatToUshort(lowResRgba, lowResSize, lowResSize);

                string lowResPath = Path.Combine(
                    Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath) + "_low" + Path.GetExtension(filePath)
                );

                using (FileStream fs = new FileStream(lowResPath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    foreach (ushort value in lowRes16bit)
                        writer.Write(value);
                }

                long lowFileSize = new FileInfo(lowResPath).Length;
                Console.WriteLine($"[RAW 16bit LOW] {lowResSize}x{lowResSize}, {lowFileSize / 1024.0:F2} KB (R 채널만)");

                if (saveMeta)
                {
                    string lowMetaPath = Path.ChangeExtension(lowResPath, ".txt");
                    File.WriteAllText(lowMetaPath,
                        $"File: {Path.GetFileName(lowResPath)}\n" +
                        $"Width: {lowResSize}\n" +
                        $"Height: {lowResSize}\n" +
                        $"Format: 16-bit grayscale\n" +
                        $"Byte order: Little Endian\n" +
                        $"Value range: 0-65535\n" +
                        $"File size: {lowFileSize} bytes\n" +
                        $"Original size: {width}x{height}\n");
                }
            }
        }

        /// <summary>
        /// RGBA float 배열을 바이리니어 보간으로 다운샘플링
        /// </summary>
        private static float[] DownsampleRgba(float[] sourceRgba, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            float[] result = new float[dstWidth * dstHeight * 4];

            float xRatio = (float)srcWidth / dstWidth;
            float yRatio = (float)srcHeight / dstHeight;

            for (int y = 0; y < dstHeight; y++)
            {
                for (int x = 0; x < dstWidth; x++)
                {
                    // 소스 이미지의 실수 좌표
                    float srcX = x * xRatio;
                    float srcY = y * yRatio;

                    // 바이리니어 보간을 위한 정수 좌표와 분수 부분
                    int x0 = (int)srcX;
                    int y0 = (int)srcY;
                    int x1 = Math.Min(x0 + 1, srcWidth - 1);
                    int y1 = Math.Min(y0 + 1, srcHeight - 1);

                    float fx = srcX - x0;
                    float fy = srcY - y0;

                    int dstIdx = (y * dstWidth + x) * 4;

                    // 4개 채널 모두 보간
                    for (int c = 0; c < 4; c++)
                    {
                        float v00 = sourceRgba[(y0 * srcWidth + x0) * 4 + c];
                        float v10 = sourceRgba[(y0 * srcWidth + x1) * 4 + c];
                        float v01 = sourceRgba[(y1 * srcWidth + x0) * 4 + c];
                        float v11 = sourceRgba[(y1 * srcWidth + x1) * 4 + c];

                        float v0 = v00 * (1 - fx) + v10 * fx;
                        float v1 = v01 * (1 - fx) + v11 * fx;

                        result[dstIdx + c] = v0 * (1 - fy) + v1 * fy;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// float RGBA 배열에서 R 채널만 추출하여 ushort로 변환
        /// </summary>
        private static ushort[] ConvertRgbaFloatToUshort(float[] rgbaData, int width, int height)
        {
            int pixelCount = width * height;
            if (rgbaData.Length != pixelCount * 4)
                throw new ArgumentException($"데이터 크기 불일치: {rgbaData.Length} != {pixelCount * 4}");

            ushort[] result = new ushort[pixelCount];
            ushort min = 65535;
            ushort max = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                result[i] = FloatToUshort(rgbaData[i * 4 + 2]); // R 채널만
                min = Math.Min(min, result[i]);
                max = Math.Max(max, result[i]);
            }
            Console.WriteLine($"max{max}, min{min}");
            return result;
        }

        /// <summary>
        /// float(0~1) -> ushort(0~65535) 변환
        /// </summary>
        private static ushort FloatToUshort(float value)
        {
            value = value.Clamp(0.0f, 1.0f);
            return (ushort)(value * 65535.0f);
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}