using OpenGL;
using System;
using System.IO;

namespace Common
{
    /// <summary>
    /// RAW 16bit 하이트맵을 GPU 텍스처로 로드
    /// </summary>
    public static class HeightmapTextureLoader
    {
        /// <summary>
        /// RAW 16bit 파일을 읽어서 OpenGL 텍스처로 업로드
        /// </summary>
        /// <param name="filePath">RAW 파일 경로</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <returns>OpenGL 텍스처 ID (실패시 0)</returns>
        public static uint LoadRaw16ToTexture(string filePath, int width, int height)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[TextureLoader] 파일 없음: {filePath}");
                return 0;
            }

            try
            {
                // 파일 읽기
                byte[] rawBytes = File.ReadAllBytes(filePath);
                int expectedSize = width * height * 2; // 16bit = 2 bytes

                if (rawBytes.Length != expectedSize)
                {
                    Console.WriteLine($"[TextureLoader] 파일 크기 오류: {rawBytes.Length} bytes (expected {expectedSize})");
                    return 0;
                }

                // ushort 배열로 변환
                ushort[] heightData = new ushort[width * height];
                System.Buffer.BlockCopy(rawBytes, 0, heightData, 0, rawBytes.Length);

                // float 배열로 정규화 (0~1)
                float[] normalizedData = new float[width * height];
                float max = 0.0f;
                float min = 65535.0f;
                for (int i = 0; i < heightData.Length; i++)
                {
                    max = Math.Max(max, heightData[i]);
                    min = Math.Min(min, heightData[i]);
                    normalizedData[i] = (heightData[i] / 65535.0f);
                }
                Console.WriteLine($"max{max}, min{min}");

                // 통계 출력
                float minH = float.MaxValue, maxH = float.MinValue;
                foreach (float h in normalizedData)
                {
                    if (h < minH) minH = h;
                    if (h > maxH) maxH = h;
                }

                // OpenGL 텍스처 생성
                uint texture = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, texture);

                // R32F 포맷으로 업로드 (단일 채널, 32bit float)
                Gl.TexImage2D(
                    TextureTarget.Texture2d,
                    0,
                    InternalFormat.R16f,
                    width, height, 0,
                    OpenGL.PixelFormat.Red,
                    PixelType.Float,
                    normalizedData
                );

                // 텍스처 파라미터 설정
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureMagFilter, Gl.LINEAR);

                Gl.BindTexture(TextureTarget.Texture2d, 0);

                Console.WriteLine($"[TextureLoader] 로드 완료: {Path.GetFileName(filePath)}");
                Console.WriteLine($"[TextureLoader] 크기: {width}x{height}, ID: {texture}");
                Console.WriteLine($"[TextureLoader] 높이 범위: {minH:F6} ~ {maxH:F6}");

                return texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TextureLoader] 로드 실패: {ex.Message}");
                return 0;
            }
        }
    }
}