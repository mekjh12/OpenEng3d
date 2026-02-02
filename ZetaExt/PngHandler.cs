using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZetaExt
{
    /// <summary>
    /// PNG 16bit RGB/RGBA 처리 클래스
    /// PNG 포맷을 직접 작성하여 진짜 16bit 지원
    /// </summary>
    public class PngHandler
    {
        #region 저장 (Save)

        /// <summary>
        /// 16bit RGB PNG로 저장 (48bit, 각 채널 16bit)
        /// </summary>
        public static void SaveRgb16(string filePath, float[] data, int width, int height)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // PNG 시그니처
                WritePngSignature(writer);

                // IHDR 청크
                WriteIHDR(writer, width, height, bitDepth: 16, colorType: 2); // 2 = RGB

                // IDAT 청크
                WriteIDATRgb16(writer, data, width, height);

                // IEND 청크
                WriteIEND(writer);
            }

            Console.WriteLine($"[PngHandler] RGB 16bit PNG 저장 완료: {filePath}");
            Console.WriteLine($"  크기: {width}×{height}, 채널: 3 (RGB), 파일크기: {new FileInfo(filePath).Length / 1024}KB");
        }

        /// <summary>
        /// 16bit RGBA PNG로 저장 (64bit, 각 채널 16bit)
        /// </summary>
        public static void SaveRgba16(string filePath, float[] data, int width, int height)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                WritePngSignature(writer);
                WriteIHDR(writer, width, height, bitDepth: 16, colorType: 6); // 6 = RGBA
                WriteIDATRgba16(writer, data, width, height);
                WriteIEND(writer);
            }

            Console.WriteLine($"[PngHandler] RGBA 16bit PNG 저장 완료: {filePath}");
            Console.WriteLine($"  크기: {width}×{height}, 채널: 4 (RGBA), 파일크기: {new FileInfo(filePath).Length / 1024}KB");
        }

        #endregion

        #region 로드 (Load)

        /// <summary>
        /// PNG 로드 (자동으로 포맷 감지)
        /// </summary>
        public static float[] Load(string filePath, out int width, out int height)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filePath}");
            }

            using (Bitmap bitmap = new Bitmap(filePath))
            {
                width = bitmap.Width;
                height = bitmap.Height;

                Console.WriteLine($"[PngHandler] PNG 로드: {width}×{height}, 포맷: {bitmap.PixelFormat}");

                float[] data = new float[width * height * 4]; // 항상 RGBA로 반환

                switch (bitmap.PixelFormat)
                {
                    case PixelFormat.Format48bppRgb:
                        LoadFrom48bppRgb(bitmap, data, width, height);
                        break;

                    case PixelFormat.Format64bppArgb:
                        LoadFrom64bppArgb(bitmap, data, width, height);
                        break;

                    case PixelFormat.Format16bppGrayScale:
                        LoadFrom16bppGrayScale(bitmap, data, width, height);
                        break;

                    case PixelFormat.Format32bppArgb:
                        LoadFrom32bppArgb(bitmap, data, width, height);
                        break;

                    case PixelFormat.Format24bppRgb:
                        LoadFrom24bppRgb(bitmap, data, width, height);
                        break;

                    default:
                        Console.WriteLine($"[PngHandler] {bitmap.PixelFormat} → Argb32 변환 중...");
                        LoadFromOtherFormat(bitmap, data, width, height);
                        break;
                }

                return data;
            }
        }

        #endregion

        #region PNG 작성 헬퍼

        private static void WritePngSignature(BinaryWriter writer)
        {
            writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        }

        private static void WriteIHDR(BinaryWriter writer, int width, int height, int bitDepth, int colorType)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter chunkWriter = new BinaryWriter(ms))
            {
                chunkWriter.Write(SwapEndian(width));
                chunkWriter.Write(SwapEndian(height));
                chunkWriter.Write((byte)bitDepth);   // Bit depth
                chunkWriter.Write((byte)colorType);  // Color type: 0=Gray, 2=RGB, 6=RGBA
                chunkWriter.Write((byte)0);          // Compression: deflate
                chunkWriter.Write((byte)0);          // Filter: adaptive
                chunkWriter.Write((byte)0);          // Interlace: none

                byte[] chunkData = ms.ToArray();
                WriteChunk(writer, "IHDR", chunkData);
            }
        }

        private static void WriteIDATGrayscale16(BinaryWriter writer, float[] data, int width, int height)
        {
            using (MemoryStream uncompressed = new MemoryStream())
            using (BinaryWriter imageWriter = new BinaryWriter(uncompressed))
            {
                for (int y = 0; y < height; y++)
                {
                    imageWriter.Write((byte)0); // Filter type: None

                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;
                        float value = data[index]; // R 채널 사용

                        ushort value16 = (ushort)(value.Clamp(0.0f, 1.0f) * 65535);

                        // Big Endian
                        imageWriter.Write((byte)(value16 >> 8));
                        imageWriter.Write((byte)(value16 & 0xFF));
                    }
                }

                byte[] compressedData = CompressZlib(uncompressed.ToArray());
                WriteChunk(writer, "IDAT", compressedData);
            }
        }

        private static void WriteIDATRgb16(BinaryWriter writer, float[] data, int width, int height)
        {
            using (MemoryStream uncompressed = new MemoryStream())
            using (BinaryWriter imageWriter = new BinaryWriter(uncompressed))
            {
                for (int y = 0; y < height; y++)
                {
                    imageWriter.Write((byte)0); // Filter type: None

                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;

                        ushort r = (ushort)(data[index + 0].Clamp(0.0f, 1.0f) * 65535);
                        ushort g = (ushort)(data[index + 1].Clamp(0.0f, 1.0f) * 65535);
                        ushort b = (ushort)(data[index + 2].Clamp(0.0f, 1.0f) * 65535);

                        // RGB, Big Endian
                        imageWriter.Write((byte)(r >> 8));
                        imageWriter.Write((byte)(r & 0xFF));
                        imageWriter.Write((byte)(g >> 8));
                        imageWriter.Write((byte)(g & 0xFF));
                        imageWriter.Write((byte)(b >> 8));
                        imageWriter.Write((byte)(b & 0xFF));
                    }
                }

                byte[] compressedData = CompressZlib(uncompressed.ToArray());
                WriteChunk(writer, "IDAT", compressedData);
            }
        }

        private static void WriteIDATRgba16(BinaryWriter writer, float[] data, int width, int height)
        {
            using (MemoryStream uncompressed = new MemoryStream())
            using (BinaryWriter imageWriter = new BinaryWriter(uncompressed))
            {
                for (int y = 0; y < height; y++)
                {
                    imageWriter.Write((byte)0); // Filter type: None

                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;

                        ushort r = (ushort)(data[index + 0].Clamp(0.0f, 1.0f) * 65535);
                        ushort g = (ushort)(data[index + 1].Clamp(0.0f, 1.0f) * 65535);
                        ushort b = (ushort)(data[index + 2].Clamp(0.0f, 1.0f) * 65535);
                        ushort a = (ushort)(data[index + 3].Clamp(0.0f, 1.0f) * 65535);

                        // RGBA, Big Endian
                        imageWriter.Write((byte)(r >> 8));
                        imageWriter.Write((byte)(r & 0xFF));
                        imageWriter.Write((byte)(g >> 8));
                        imageWriter.Write((byte)(g & 0xFF));
                        imageWriter.Write((byte)(b >> 8));
                        imageWriter.Write((byte)(b & 0xFF));
                        imageWriter.Write((byte)(a >> 8));
                        imageWriter.Write((byte)(a & 0xFF));
                    }
                }

                byte[] compressedData = CompressZlib(uncompressed.ToArray());
                WriteChunk(writer, "IDAT", compressedData);
            }
        }

        private static void WriteIEND(BinaryWriter writer)
        {
            WriteChunk(writer, "IEND", new byte[0]);
        }

        private static void WriteChunk(BinaryWriter writer, string chunkType, byte[] data)
        {
            writer.Write(SwapEndian(data.Length));

            byte[] typeBytes = Encoding.ASCII.GetBytes(chunkType);
            writer.Write(typeBytes);
            writer.Write(data);

            byte[] crcData = new byte[typeBytes.Length + data.Length];
            Array.Copy(typeBytes, 0, crcData, 0, typeBytes.Length);
            Array.Copy(data, 0, crcData, typeBytes.Length, data.Length);
            uint crc = CalculateCRC32(crcData);
            writer.Write(SwapEndian((int)crc));
        }

        #endregion

        #region PNG 로드 헬퍼

        private static void LoadFrom48bppRgb(Bitmap bitmap, float[] data, int width, int height)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format48bppRgb
            );

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ushort* row = (ushort*)(ptr + y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            ushort r = row[x * 3 + 0];
                            ushort g = row[x * 3 + 1];
                            ushort b = row[x * 3 + 2];

                            int index = (y * width + x) * 4;
                            data[index + 0] = r / 65535.0f;
                            data[index + 1] = g / 65535.0f;
                            data[index + 2] = b / 65535.0f;
                            data[index + 3] = 1.0f;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static void LoadFrom64bppArgb(Bitmap bitmap, float[] data, int width, int height)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format64bppArgb
            );

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ushort* row = (ushort*)(ptr + y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            ushort a = row[x * 4 + 0];
                            ushort r = row[x * 4 + 1];
                            ushort g = row[x * 4 + 2];
                            ushort b = row[x * 4 + 3];

                            int index = (y * width + x) * 4;
                            data[index + 0] = r / 65535.0f;
                            data[index + 1] = g / 65535.0f;
                            data[index + 2] = b / 65535.0f;
                            data[index + 3] = a / 65535.0f;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static void LoadFrom16bppGrayScale(Bitmap bitmap, float[] data, int width, int height)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format16bppGrayScale
            );

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ushort* row = (ushort*)(ptr + y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            ushort value = row[x];
                            float normalized = value / 65535.0f;

                            int index = (y * width + x) * 4;
                            data[index + 0] = normalized;
                            data[index + 1] = normalized;
                            data[index + 2] = normalized;
                            data[index + 3] = 1.0f;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static void LoadFrom32bppArgb(Bitmap bitmap, float[] data, int width, int height)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + y * stride;

                        for (int x = 0; x < width; x++)
                        {
                            byte b = row[x * 4 + 0];
                            byte g = row[x * 4 + 1];
                            byte r = row[x * 4 + 2];
                            byte a = row[x * 4 + 3];

                            int index = (y * width + x) * 4;
                            data[index + 0] = r / 255.0f;
                            data[index + 1] = g / 255.0f;
                            data[index + 2] = b / 255.0f;
                            data[index + 3] = a / 255.0f;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static void LoadFrom24bppRgb(Bitmap bitmap, float[] data, int width, int height)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + y * stride;

                        for (int x = 0; x < width; x++)
                        {
                            byte b = row[x * 3 + 0];
                            byte g = row[x * 3 + 1];
                            byte r = row[x * 3 + 2];

                            int index = (y * width + x) * 4;
                            data[index + 0] = r / 255.0f;
                            data[index + 1] = g / 255.0f;
                            data[index + 2] = b / 255.0f;
                            data[index + 3] = 1.0f;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static void LoadFromOtherFormat(Bitmap bitmap, float[] data, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);

                    int index = (y * width + x) * 4;
                    data[index + 0] = pixel.R / 255.0f;
                    data[index + 1] = pixel.G / 255.0f;
                    data[index + 2] = pixel.B / 255.0f;
                    data[index + 3] = pixel.A / 255.0f;
                }
            }
        }

        #endregion

        #region 유틸리티

        private static byte[] CompressZlib(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                // zlib 헤더
                output.WriteByte(0x78);
                output.WriteByte(0x9C);

                // Deflate 압축
                using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, true))
                {
                    deflate.Write(data, 0, data.Length);
                }

                // Adler32 체크섬
                uint adler = CalculateAdler32(data);
                output.WriteByte((byte)(adler >> 24));
                output.WriteByte((byte)(adler >> 16));
                output.WriteByte((byte)(adler >> 8));
                output.WriteByte((byte)(adler));

                return output.ToArray();
            }
        }

        private static uint CalculateCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }
            return crc ^ 0xFFFFFFFF;
        }

        private static uint CalculateAdler32(byte[] data)
        {
            uint a = 1, b = 0;
            foreach (byte c in data)
            {
                a = (a + c) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        private static int SwapEndian(int value)
        {
            return ((value & 0xFF) << 24) |
                   ((value & 0xFF00) << 8) |
                   ((value & 0xFF0000) >> 8) |
                   ((value >> 24) & 0xFF);
        }

        /// <summary>
        /// PNG 정보 확인
        /// </summary>
        /// <summary>
        /// PNG 정보 확인
        /// </summary>
        public static void GetInfo(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[PngHandler] 파일 없음: {filePath}");
                return;
            }

            using (Bitmap bitmap = new Bitmap(filePath))
            {
                int bpp = Image.GetPixelFormatSize(bitmap.PixelFormat);

                Console.WriteLine($"[PngHandler] PNG 정보:");
                Console.WriteLine($"  파일: {Path.GetFileName(filePath)}");
                Console.WriteLine($"  크기: {bitmap.Width}×{bitmap.Height}");
                Console.WriteLine($"  포맷: {bitmap.PixelFormat}");
                Console.WriteLine($"  픽셀당 비트: {bpp}bpp");
                Console.WriteLine($"  파일 크기: {new FileInfo(filePath).Length / 1024}KB");

                // 채널 정보 (C# 7.3 호환)
                string channels;
                switch (bitmap.PixelFormat)
                {
                    case PixelFormat.Format16bppGrayScale:
                        channels = "1채널 (Grayscale, 16bit)";
                        break;
                    case PixelFormat.Format48bppRgb:
                        channels = "3채널 (RGB, 각 16bit)";
                        break;
                    case PixelFormat.Format64bppArgb:
                        channels = "4채널 (ARGB, 각 16bit)";
                        break;
                    case PixelFormat.Format24bppRgb:
                        channels = "3채널 (RGB, 각 8bit)";
                        break;
                    case PixelFormat.Format32bppArgb:
                        channels = "4채널 (ARGB, 각 8bit)";
                        break;
                    default:
                        channels = "기타";
                        break;
                }

                Console.WriteLine($"  채널: {channels}");
            }
        }

        #endregion
    }
}