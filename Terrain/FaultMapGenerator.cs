using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZetaExt;


namespace Terrain
{
    /// <summary>
    /// 지형의 지질학적 단층선을 표현하는 보로노이 텍스처 생성기
    /// </summary>
    public class FaultMapGenerator
    {
        /// <summary>
        /// 보로노이 단층 맵 생성
        /// </summary>
        /// <param name="width">텍스처 너비 (512 권장)</param>
        /// <param name="height">텍스처 높이 (512 권장)</param>
        /// <param name="numFaults">단층점 개수 (5-20 권장)</param>
        /// <param name="seed">랜덤 시드</param>
        /// <returns>RGB 텍스처 (R: 셀ID/변위, G: 경계거리, B: 방향)</returns>
        public static Texture Generate(int width, int height, int numFaults, int seed = 0)
        {
            Random rand = new Random(seed);

            // 1. 랜덤 단층점 생성
            FaultPoint[] faults = new FaultPoint[numFaults];
            for (int i = 0; i < numFaults; i++)
            {
                Vertex2f pos = new Vertex2f(
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble()
                );

                float displacement = (float)(rand.NextDouble() * 2.0 - 1.0);
                float direction = (float)(rand.NextDouble() * Math.PI * 2.0);
                float thick = (float)(rand.NextDouble() * 0.15 + 0.05);

                faults[i] = new FaultPoint(pos, displacement, direction, thick);
            }

            // 2. Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // 3. 각 픽셀마다 보로노이 계산
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vertex2f uv = new Vertex2f(x / (float)width, y / (float)height);

                    // 가장 가까운 단층점 찾기
                    float minDist = float.MaxValue;
                    int closestIdx = 0;

                    for (int i = 0; i < numFaults; i++)
                    {
                        float dist = (uv - faults[i].Position).Norm();
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestIdx = i;
                        }
                    }

                    // 두 번째로 가까운 단층점 찾기
                    float minDist2 = float.MaxValue;
                    for (int i = 0; i < numFaults; i++)
                    {
                        if (i == closestIdx) continue;
                        float dist = (uv - faults[i].Position).Norm();
                        if (dist < minDist2)
                        {
                            minDist2 = dist;
                        }
                    }

                    FaultPoint closest = faults[closestIdx];

                    // R 채널: 변위량 (0~1)
                    float displacementNorm = (closest.Displacement + 1.0f) * 0.5f;

                    // G 채널: 경계 거리 (0~1)
                    float edgeDistance = Math.Min((minDist2 - minDist) * 50.0f, 1.0f);

                    // B 채널: 단층 방향 (0~1)
                    float directionNorm = (float)(closest.Direction / (Math.PI * 2.0));

                    // 픽셀 설정 (0~255 범위로 변환)
                    Color color = Color.FromArgb(
                        255,
                        (int)(displacementNorm * 255),
                        (int)(edgeDistance * 255),
                        (int)(directionNorm * 255)
                    );

                    bitmap.SetPixel(x, y, color);
                }
            }

            // 4. Texture 생성 (Bitmap 생성자 사용)
            Texture texture = new Texture(bitmap);

            // 5. 추가 파라미터 설정 (Repeat + Mipmap)
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureID);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
            Gl.GenerateMipmap(TextureTarget.Texture2d);

            bitmap.Dispose();

            return texture;
        }

        /// <summary>
        /// 실제 지질학적으로 그럴듯한 단층 맵 생성
        /// (메인 단층선 + 주변 소규모 단층)
        /// </summary>
        public static Texture GenerateRealistic(int width, int height, int seed = 0)
        {
            Random rand = new Random(seed);
            List<FaultPoint> faults = new List<FaultPoint>();

            // 1. 메인 단층선 2-4개 (큰 변위)
            int numMajor = rand.Next(2, 5);
            for (int i = 0; i < numMajor; i++)
            {
                // 지도를 가로지르는 긴 단층
                float angle = (float)(rand.NextDouble() * Math.PI);
                float offset = (float)rand.NextDouble();

                Vertex2f pos = new Vertex2f(
                    (float)Math.Cos(angle) * 0.5f + 0.5f,
                    (float)Math.Sin(angle) * 0.5f + 0.5f
                );

                float displacement = (float)(rand.NextDouble() * 1.6 - 0.8); // 큰 변위
                float direction = angle;
                float wid = 0.15f; // 넓은 단층대

                faults.Add(new FaultPoint(pos, displacement, direction, wid));
            }

            // 2. 보조 단층 10-15개 (작은 변위)
            int numMinor = rand.Next(10, 16);
            for (int i = 0; i < numMinor; i++)
            {
                Vertex2f pos = new Vertex2f(
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble()
                );

                float displacement = (float)(rand.NextDouble() * 0.6 - 0.3); // 작은 변위
                float direction = (float)(rand.NextDouble() * Math.PI * 2.0);
                float wid = (float)(rand.NextDouble() * 0.08 + 0.03); // 좁은 단층대

                faults.Add(new FaultPoint(pos, displacement, direction, wid));
            }

            // 3. Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // 4. 보로노이 계산
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vertex2f uv = new Vertex2f(x / (float)width, y / (float)height);

                    float minDist = float.MaxValue;
                    int closestIdx = 0;

                    for (int i = 0; i < faults.Count; i++)
                    {
                        float dist = (uv - faults[i].Position).Norm();
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestIdx = i;
                        }
                    }

                    float minDist2 = float.MaxValue;
                    for (int i = 0; i < faults.Count; i++)
                    {
                        if (i == closestIdx) continue;
                        float dist = (uv - faults[i].Position).Norm();
                        if (dist < minDist2)
                        {
                            minDist2 = dist;
                        }
                    }

                    FaultPoint closest = faults[closestIdx];

                    float displacementNorm = (closest.Displacement + 1.0f) * 0.5f;
                    float edgeDistance = Math.Min((minDist2 - minDist) * 50.0f, 1.0f);
                    float directionNorm = (float)(closest.Direction / (Math.PI * 2.0));

                    // 픽셀 설정 (0~255 범위로 변환)
                    Color color = Color.FromArgb(
                        255,
                        (int)(displacementNorm * 255),
                        (int)(edgeDistance * 255),
                        (int)(directionNorm * 255)
                    );

                    bitmap.SetPixel(x, y, color);
                }
            }

            // 5. Texture 생성 (Bitmap 생성자 사용)
            Texture texture = new Texture(bitmap);

            // 6. 추가 파라미터 설정 (Repeat + Mipmap)
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureID);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
            Gl.GenerateMipmap(TextureTarget.Texture2d);

            bitmap.Dispose();

            return texture;
        }

        /// <summary>
        /// 텍스처를 PNG 파일로 저장
        /// </summary>
        public static void SaveTexture(Texture texture, string path, int width, int height)
        {
            // 1. 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureID);

            // 2. GPU에서 픽셀 데이터 읽기
            byte[] pixels = new byte[width * height * 4]; // RGBA
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            // 3. Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            // 4. RGBA를 BGRA로 변환하며 복사 (Bitmap은 BGRA 순서)
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int i = 0; i < width * height; i++)
                {
                    int srcIdx = i * 4;
                    int dstIdx = i * 4;

                    ptr[dstIdx + 0] = pixels[srcIdx + 2]; // B
                    ptr[dstIdx + 1] = pixels[srcIdx + 1]; // G
                    ptr[dstIdx + 2] = pixels[srcIdx + 0]; // R
                    ptr[dstIdx + 3] = pixels[srcIdx + 3]; // A
                }
            }

            bitmap.UnlockBits(data);

            // 5. PNG로 저장
            bitmap.Save(path, ImageFormat.Png);

            bitmap.Dispose();
        }
    }

}