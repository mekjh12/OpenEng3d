using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Shader.CloudShader
{
    /// <summary>
    /// GPU의 3D 텍스처를 PNG 파일로 내보내는 유틸리티 클래스
    /// </summary>
    public static class Texture3DExporter
    {
        /// <summary>
        /// 3D 텍스처의 모든 슬라이스를 개별 PNG 파일로 저장
        /// </summary>
        /// <param name="textureHandle">텍스처 핸들</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="depth">텍스처 깊이</param>
        /// <param name="outputFolder">출력 폴더 경로</param>
        /// <param name="filePrefix">파일 이름 접두사</param>
        public static void ExportSlicesToPNG(uint textureHandle, int width, int height, int depth, string outputFolder, string filePrefix = "slice_")
        {
            // 출력 폴더가 없으면 생성
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // 버퍼 생성 (전체 3D 텍스처 데이터)
            float[] buffer = new float[width * height * depth * 4]; // RGBA 형식

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture3d, textureHandle);

            // 텍스처 데이터 읽기
            Gl.GetTexImage(TextureTarget.Texture3d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, buffer);

            // 텍스처 언바인딩
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // 각 슬라이스를 개별 PNG 파일로 저장
            for (int z = 0; z < depth; z++)
            {
                // 이미지 생성
                using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    // 비트맵 데이터를 직접 조작하기 위해 Lock
                    BitmapData bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    // RGBA 바이트 배열 생성
                    byte[] pixelData = new byte[width * height * 4];

                    // 해당 슬라이스의 데이터를 바이트 배열로 변환
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = ((z * height + y) * width + x) * 4;
                            int dstIdx = (y * width + x) * 4;

                            // float(0-1) -> byte(0-255) 변환
                            pixelData[dstIdx + 3] = (byte)(buffer[srcIdx + 3] * 255); // A
                            pixelData[dstIdx + 2] = (byte)(buffer[srcIdx + 0] * 255); // R -> B (RGBA -> BGRA 변환)
                            pixelData[dstIdx + 1] = (byte)(buffer[srcIdx + 1] * 255); // G -> G
                            pixelData[dstIdx + 0] = (byte)(buffer[srcIdx + 2] * 255); // B -> R (RGBA -> BGRA 변환)
                        }
                    }

                    // 비트맵에 데이터 복사
                    Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);

                    // 비트맵 Unlock
                    bitmap.UnlockBits(bitmapData);

                    // PNG로 저장
                    string fileName = Path.Combine(outputFolder, $"{filePrefix}{z:D3}.png");
                    bitmap.Save(fileName, ImageFormat.Png);

                    Console.WriteLine($"슬라이스 {z}/{depth - 1} 저장됨: {fileName}");
                }
            }

            Console.WriteLine($"총 {depth}개의 슬라이스가 {outputFolder} 폴더에 저장되었습니다.");
        }

        /// <summary>
        /// 3D 텍스처의 슬라이스들을 타일 형태로 하나의 PNG 파일로 저장
        /// </summary>
        /// <param name="textureHandle">텍스처 핸들</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="depth">텍스처 깊이</param>
        /// <param name="outputPath">출력 파일 경로</param>
        public static void ExportTiledTextureToPNG(uint textureHandle, int width, int height, int depth, string outputPath)
        {
            // 타일 배치를 위한 계산
            int tilesPerRow = (int)Math.Ceiling(Math.Sqrt(depth));
            int tiledWidth = width * tilesPerRow;
            int tiledHeight = height * (int)Math.Ceiling((float)depth / tilesPerRow);

            // 버퍼 생성 (전체 3D 텍스처 데이터)
            float[] buffer = new float[width * height * depth * 4]; // RGBA 형식

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture3d, textureHandle);

            // 텍스처 데이터 읽기
            Gl.GetTexImage(TextureTarget.Texture3d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, buffer);

            // 텍스처 언바인딩
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // 타일 이미지 생성
            using (Bitmap bitmap = new Bitmap(tiledWidth, tiledHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 배경을 투명하게 설정
                    g.Clear(Color.Transparent);

                    // 각 슬라이스를 타일 형태로 그리기
                    for (int z = 0; z < depth; z++)
                    {
                        // 타일 위치 계산
                        int tileX = (z % tilesPerRow) * width;
                        int tileY = (z / tilesPerRow) * height;

                        // 슬라이스 이미지 생성
                        using (Bitmap sliceBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            BitmapData bitmapData = sliceBitmap.LockBits(
                                new Rectangle(0, 0, width, height),
                                ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                            // RGBA 바이트 배열 생성
                            byte[] pixelData = new byte[width * height * 4];

                            // 해당 슬라이스의 데이터를 바이트 배열로 변환
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    int srcIdx = ((z * height + y) * width + x) * 4;
                                    int dstIdx = (y * width + x) * 4;

                                    // float(0-1) -> byte(0-255) 변환
                                    pixelData[dstIdx + 3] = (byte)(buffer[srcIdx + 3] * 255); // A
                                    pixelData[dstIdx + 2] = (byte)(buffer[srcIdx + 0] * 255); // R -> B (RGBA -> BGRA 변환)
                                    pixelData[dstIdx + 1] = (byte)(buffer[srcIdx + 1] * 255); // G -> G
                                    pixelData[dstIdx + 0] = (byte)(buffer[srcIdx + 2] * 255); // B -> R (RGBA -> BGRA 변환)
                                }
                            }

                            // 비트맵에 데이터 복사
                            Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                            sliceBitmap.UnlockBits(bitmapData);

                            // 타일 이미지에 슬라이스 그리기
                            g.DrawImage(sliceBitmap, tileX, tileY);
                        }
                    }
                }

                // 출력 폴더가 없으면 생성
                string outputFolder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputFolder) && !Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // PNG로 저장
                bitmap.Save(outputPath, ImageFormat.Png);
                Console.WriteLine($"타일 이미지가 저장되었습니다: {outputPath}");
            }
        }

        /// <summary>
        /// 3D 텍스처의 단일 슬라이스를 PNG 파일로 저장
        /// </summary>
        /// <param name="textureHandle">텍스처 핸들</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="depth">텍스처 깊이</param>
        /// <param name="sliceIndex">저장할 슬라이스 인덱스</param>
        /// <param name="outputPath">출력 파일 경로</param>
        public static void ExportSingleSliceToPNG(uint textureHandle, int width, int height, int depth, int sliceIndex, string outputPath)
        {
            // 슬라이스 인덱스 유효성 검사
            if (sliceIndex < 0 || sliceIndex >= depth)
            {
                throw new ArgumentOutOfRangeException(nameof(sliceIndex), $"슬라이스 인덱스는 0에서 {depth - 1} 사이여야 합니다.");
            }

            // 버퍼 생성 (전체 3D 텍스처 데이터)
            float[] buffer = new float[width * height * depth * 4]; // RGBA 형식

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture3d, textureHandle);

            // 텍스처 데이터 읽기
            Gl.GetTexImage(TextureTarget.Texture3d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, buffer);

            // 텍스처 언바인딩
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // 이미지 생성
            using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // 비트맵 데이터를 직접 조작하기 위해 Lock
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // RGBA 바이트 배열 생성
                byte[] pixelData = new byte[width * height * 4];

                // 해당 슬라이스의 데이터를 바이트 배열로 변환
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = ((sliceIndex * height + y) * width + x) * 4;
                        int dstIdx = (y * width + x) * 4;

                        // float(0-1) -> byte(0-255) 변환
                        pixelData[dstIdx + 3] = (byte)(buffer[srcIdx + 3] * 255); // A
                        pixelData[dstIdx + 2] = (byte)(buffer[srcIdx + 0] * 255); // R -> B (RGBA -> BGRA 변환)
                        pixelData[dstIdx + 1] = (byte)(buffer[srcIdx + 1] * 255); // G -> G
                        pixelData[dstIdx + 0] = (byte)(buffer[srcIdx + 2] * 255); // B -> R (RGBA -> BGRA 변환)
                    }
                }

                // 비트맵에 데이터 복사
                Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);

                // 비트맵 Unlock
                bitmap.UnlockBits(bitmapData);

                // 출력 폴더가 없으면 생성
                string outputFolder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputFolder) && !Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // PNG로 저장
                bitmap.Save(outputPath, ImageFormat.Png);
                Console.WriteLine($"슬라이스 {sliceIndex}가 저장되었습니다: {outputPath}");
            }
        }
    }
}