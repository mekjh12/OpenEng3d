using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace Renderer
{
    /// <summary>
    /// 스카이돔을 렌더링하는 클래스입니다.
    /// </summary>
    public class SkyDomeRenderer
    {
        // 스카이돔 메시를 위한 변수들
        private uint _skyDomeVAO;
        private int _skyDomeVertexCount;
        private SkyDomeShader _skyDomeShader;

        // 임시 텍스처 ID
        private uint _tempSkyTextureId;

        /// <summary>
        /// SkyDomeRenderer 클래스의 생성자입니다.
        /// </summary>
        /// <param name="projectPath">프로젝트의 루트 경로</param>
        public SkyDomeRenderer(string projectPath)
        {
            // 셰이더 초기화
            _skyDomeShader = new SkyDomeShader(projectPath);

            // 스카이돔 메시 생성
            CreateSkyDomeMesh();

            // 임시 텍스처 생성
            CreateSkyTextureWithRandomSunAndClouds( cloudCoverage: 0.9f, cloudAltitude:0.1f, cloudThickness: 0.9f);
        }

        /// <summary>
        /// 반구형 스카이돔 메시를 생성합니다.
        /// </summary>
        private void CreateSkyDomeMesh()
        {
            // 메시 파라미터
            int stacks = 20;  // 수직 분할 수
            int slices = 40;  // 수평 분할 수
            float radius = 1.0f;  // 반지름 (나중에 스케일링)

            // 삼각형 메시 계산
            // 정점 수 = 스택 * 슬라이스 * 6 (각 쿼드는 2개의 삼각형, 각 삼각형은 3개의 정점)
            int vertexCount = (stacks) * slices * 6;
            float[] vertices = new float[vertexCount * 3];  // 각 정점은 x,y,z 좌표를 가짐
            float[] texCoords = new float[vertexCount * 2]; // 각 정점은 u,v 텍스처 좌표를 가짐

            int vertexIndex = 0;
            int texCoordIndex = 0;

            // 반구 생성 (z가 위쪽 방향)
            for (int stack = 0; stack < stacks; stack++)
            {
                // 스택의 시작과 끝 각도 계산 (0 = 하단, PI/2 = 상단)
                float phi1 = (float)Math.PI * 0.5f * (float)stack / stacks;
                float phi2 = (float)Math.PI * 0.5f * (float)(stack + 1) / stacks;

                // 반지름과 고도 계산
                float z1 = (float)Math.Sin(phi1);
                float z2 = (float)Math.Sin(phi2);
                float r1 = (float)Math.Cos(phi1);
                float r2 = (float)Math.Cos(phi2);

                for (int slice = 0; slice < slices; slice++)
                {
                    // 슬라이스의 시작과 끝 각도 계산
                    float theta1 = (float)slice / slices * (float)Math.PI * 2.0f;
                    float theta2 = (float)(slice + 1) / slices * (float)Math.PI * 2.0f;

                    // 사각형의 네 꼭지점 계산
                    float x1 = r1 * (float)Math.Cos(theta1);
                    float y1 = r1 * (float)Math.Sin(theta1);
                    float x2 = r1 * (float)Math.Cos(theta2);
                    float y2 = r1 * (float)Math.Sin(theta2);
                    float x3 = r2 * (float)Math.Cos(theta2);
                    float y3 = r2 * (float)Math.Sin(theta2);
                    float x4 = r2 * (float)Math.Cos(theta1);
                    float y4 = r2 * (float)Math.Sin(theta1);

                    // 첫 번째 삼각형 (v1, v2, v3)
                    vertices[vertexIndex++] = x1 * radius;
                    vertices[vertexIndex++] = y1 * radius;
                    vertices[vertexIndex++] = z1 * radius;

                    vertices[vertexIndex++] = x2 * radius;
                    vertices[vertexIndex++] = y2 * radius;
                    vertices[vertexIndex++] = z1 * radius;

                    vertices[vertexIndex++] = x3 * radius;
                    vertices[vertexIndex++] = y3 * radius;
                    vertices[vertexIndex++] = z2 * radius;

                    // 두 번째 삼각형 (v1, v3, v4)
                    vertices[vertexIndex++] = x1 * radius;
                    vertices[vertexIndex++] = y1 * radius;
                    vertices[vertexIndex++] = z1 * radius;

                    vertices[vertexIndex++] = x3 * radius;
                    vertices[vertexIndex++] = y3 * radius;
                    vertices[vertexIndex++] = z2 * radius;

                    vertices[vertexIndex++] = x4 * radius;
                    vertices[vertexIndex++] = y4 * radius;
                    vertices[vertexIndex++] = z2 * radius;

                    // 텍스처 좌표 계산
                    float u1 = (float)slice / slices;
                    float u2 = (float)(slice + 1) / slices;
                    float v1 = (float)stack / stacks;
                    float v2 = (float)(stack + 1) / stacks;

                    // 첫 번째 삼각형 텍스처 좌표
                    texCoords[texCoordIndex++] = u1;
                    texCoords[texCoordIndex++] = v1;

                    texCoords[texCoordIndex++] = u2;
                    texCoords[texCoordIndex++] = v1;

                    texCoords[texCoordIndex++] = u2;
                    texCoords[texCoordIndex++] = v2;

                    // 두 번째 삼각형 텍스처 좌표
                    texCoords[texCoordIndex++] = u1;
                    texCoords[texCoordIndex++] = v1;

                    texCoords[texCoordIndex++] = u2;
                    texCoords[texCoordIndex++] = v2;

                    texCoords[texCoordIndex++] = u1;
                    texCoords[texCoordIndex++] = v2;
                }
            }

            _skyDomeVertexCount = vertexCount;

            // VAO 및 VBO 생성
            _skyDomeVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_skyDomeVAO);

            // 위치 정보를 위한 VBO
            uint positionVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, positionVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), vertices, BufferUsage.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            // 텍스처 좌표를 위한 VBO
            uint texCoordVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, texCoordVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(texCoords.Length * sizeof(float)), texCoords, BufferUsage.StaticDraw);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(1);

            // VAO 바인딩 해제
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 상반구에 태양을 랜덤하게 배치하고 그 위치를 기준으로 하늘색 텍스처를 생성합니다.
        /// </summary>
        public void CreateSkyTextureWithRandomSun()
        {
            const int width = 512;
            const int height = 512;

            // 랜덤한 태양 위치 생성 (상반구에서만)
            Random random = new Random();

            // 태양의 고도각 - 수평선 위 10-75도 (최상단 90도 제외)
            double sunAltitude = (10.0 + random.NextDouble() * 65.0) * Math.PI / 180.0;

            // 태양의 방위각 - 0-360도
            double sunAzimuth = random.NextDouble() * 2.0 * Math.PI;

            // 태양 위치를 카테시안 좌표로 변환
            float sunX = (float)(Math.Cos(sunAltitude) * Math.Sin(sunAzimuth));
            float sunY = (float)(Math.Sin(sunAltitude));  // y축이 상향
            float sunZ = (float)(Math.Cos(sunAltitude) * Math.Cos(sunAzimuth));

            // 태양 위치 정규화
            Vertex3f sunPosition = new Vertex3f(sunX, sunY, sunZ).Normalized;

            Console.WriteLine($"랜덤 태양 위치: X={sunPosition.x}, Y={sunPosition.y}, Z={sunPosition.z}");

            // 태양 색상과 하늘색 정의
            Vertex3f sunColor = new Vertex3f(1.0f, 0.95f, 0.8f);      // 태양 (약간 황색)
            Vertex3f skyZenithColor = new Vertex3f(0.3f, 0.5f, 0.9f); // 천정 (짙은 파랑)
            Vertex3f skyHorizonColor = new Vertex3f(0.7f, 0.85f, 1.0f); // 지평선 (연한 파랑)

            // 태양 주변 그라데이션에 사용될 색상
            Vertex3f sunGlowColor = new Vertex3f(1.0f, 0.9f, 0.7f);   // 태양 주변 (황금빛)

            // 텍스처 데이터 생성
            float[] textureData = new float[width * height * 4]; // RGBA

            for (int y = 0; y < height; y++)
            {
                // 정규화된 y 좌표 (0 = 하단, 1 = 상단)
                float v = (float)y / height;

                // 상단 반구에서의 고도각 계산 (0 = 지평선, PI/2 = 천정)
                float altitude = (float)(v * Math.PI / 2.0);

                for (int x = 0; x < width; x++)
                {
                    // 정규화된 x 좌표 (0 = 왼쪽, 1 = 오른쪽)
                    float u = (float)x / width;

                    // 텍스처에서의 방위각 계산 (0 ~ 2PI)
                    float azimuth = (float)(u * 2.0 * Math.PI);

                    // 현재 픽셀의 방향 벡터 계산 (구면 좌표)
                    float pixelX = (float)(Math.Cos(altitude) * Math.Sin(azimuth));
                    float pixelY = (float)(Math.Sin(altitude));
                    float pixelZ = (float)(Math.Cos(altitude) * Math.Cos(azimuth));

                    Vertex3f pixelDir = new Vertex3f(pixelX, pixelY, pixelZ);

                    // 태양과 현재 픽셀 방향 사이의 각도 계산 (내적 이용)
                    float dotProduct = pixelDir.x * sunPosition.x +
                                      pixelDir.y * sunPosition.y +
                                      pixelDir.z * sunPosition.z;

                    // 내적값은 -1 ~ 1 범위, 1에 가까울수록 태양에 가까움
                    float sunFactor = Math.Max(0, (dotProduct - 0.9f) * 10.0f); // 태양 주변 그라데이션

                    // 천정과 지평선 사이의 혼합
                    float skyBlend = (float)Math.Sin(altitude); // 천정(1)에서 지평선(0)으로

                    // 기본 하늘색 (천정에서 지평선으로)
                    Vertex3f skyColor = new Vertex3f(
                        skyZenithColor.x * skyBlend + skyHorizonColor.x * (1 - skyBlend),
                        skyZenithColor.y * skyBlend + skyHorizonColor.y * (1 - skyBlend),
                        skyZenithColor.z * skyBlend + skyHorizonColor.z * (1 - skyBlend)
                    );

                    // 태양과 하늘색 혼합
                    Vertex3f finalColor = new Vertex3f(
                        skyColor.x * (1 - sunFactor) + sunColor.x * sunFactor,
                        skyColor.y * (1 - sunFactor) + sunColor.y * sunFactor,
                        skyColor.z * (1 - sunFactor) + sunColor.z * sunFactor
                    );

                    // 태양 주변 글로우 효과 적용
                    if (dotProduct > 0.95f && dotProduct < 0.99f)
                    {
                        float glowFactor = (dotProduct - 0.95f) * 25.0f; // 0.95~0.99 사이에서 0~1
                        finalColor = new Vertex3f(
                            finalColor.x * (1 - glowFactor) + sunGlowColor.x * glowFactor,
                            finalColor.y * (1 - glowFactor) + sunGlowColor.y * glowFactor,
                            finalColor.z * (1 - glowFactor) + sunGlowColor.z * glowFactor
                        );
                    }

                    // 태양 자체
                    if (dotProduct > 0.995f)
                    {
                        finalColor = sunColor;
                    }

                    // 텍스처 데이터에 저장
                    int index = (y * width + x) * 4;
                    textureData[index + 0] = finalColor.x; // R
                    textureData[index + 1] = finalColor.y; // G
                    textureData[index + 2] = finalColor.z; // B
                    textureData[index + 3] = 1.0f;         // A (완전 불투명)
                }
            }

            // 텍스처 생성 및 업로드
            _tempSkyTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _tempSkyTextureId);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0,
                         PixelFormat.Rgba, PixelType.Float, textureData);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

        }

        /// <summary>
        /// 상반구에 태양을 랜덤하게 배치하고 그 위치를 기준으로 하늘색 텍스처와 구름을 생성합니다.
        /// </summary>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudAltitude">구름의 고도 (0.0 = 지평선에 가까움, 1.0 = 천정에 가까움)</param>
        /// <param name="cloudThickness">구름의 두께 (0.0 = 얇음, 1.0 = 두꺼움)</param>
        public void CreateSkyTextureWithRandomSunAndClouds(float cloudCoverage = 0.5f, float cloudAltitude = 0.2f, float cloudThickness = 0.5f)
        {
            const int width = 512;
            const int height = 512;

            // 매개변수 범위 검증
            cloudCoverage = cloudCoverage.Clamp(0.0f, 1.0f);
            cloudAltitude = cloudAltitude.Clamp(0.0f, 0.8f); // 최대 0.8로 제한 (천정에 구름이 없도록)
            cloudThickness = cloudThickness.Clamp(0.1f, 1.0f);

            // 랜덤한 태양 위치 생성 (상반구에서만)
            Random random = new Random();

            // 태양의 고도각 - 수평선 위 10-75도 (최상단 90도 제외)
            double sunAltitude = (10.0 + random.NextDouble() * 65.0) * Math.PI / 180.0;

            // 태양의 방위각 - 0-360도
            double sunAzimuth = random.NextDouble() * 2.0 * Math.PI;

            // 태양 위치를 카테시안 좌표로 변환
            float sunX = (float)(Math.Cos(sunAltitude) * Math.Sin(sunAzimuth));
            float sunY = (float)(Math.Sin(sunAltitude));  // y축이 상향
            float sunZ = (float)(Math.Cos(sunAltitude) * Math.Cos(sunAzimuth));

            // 태양 위치 정규화
            Vertex3f sunPosition = new Vertex3f(sunX, sunY, sunZ).Normalized;

            Console.WriteLine($"랜덤 태양 위치: X={sunPosition.x}, Y={sunPosition.y}, Z={sunPosition.z}");
            Console.WriteLine($"구름 설정: 양={cloudCoverage}, 고도={cloudAltitude}, 두께={cloudThickness}");

            // 태양 색상과 하늘색 정의
            Vertex3f sunColor = new Vertex3f(1.0f, 0.95f, 0.8f);      // 태양 (약간 황색)
            Vertex3f skyZenithColor = new Vertex3f(0.3f, 0.5f, 0.9f); // 천정 (짙은 파랑)
            Vertex3f skyHorizonColor = new Vertex3f(0.7f, 0.85f, 1.0f); // 지평선 (연한 파랑)

            // 태양 주변 그라데이션에 사용될 색상
            Vertex3f sunGlowColor = new Vertex3f(1.0f, 0.9f, 0.7f);   // 태양 주변 (황금빛)

            // 일출/일몰 색상 (태양 고도가 낮을 때)
            Vertex3f sunriseColor = new Vertex3f(1.0f, 0.6f, 0.4f);   // 일출/일몰 (붉은 오렌지색)

            // 구름 색상 정의
            Vertex3f cloudBrightColor = new Vertex3f(1.0f, 1.0f, 1.0f);   // 밝은 구름 (흰색)
            Vertex3f cloudDarkColor = new Vertex3f(0.6f, 0.6f, 0.7f);     // 그림자진 구름 (짙은 회색)
            Vertex3f cloudSunsetColor = new Vertex3f(1.0f, 0.7f, 0.5f);   // 일몰 때 구름 (오렌지색)

            // 태양 고도에 따른 일출/일몰 효과 강도
            float sunsetFactor = 0.0f;
            if (sunPosition.y < 0.5f) // 태양이 낮은 위치에 있을 때
            {
                // 태양 고도가 낮을수록 일출/일몰 효과 강화
                sunsetFactor = Math.Max(0, 1.0f - sunPosition.y * 2.0f);
            }

            // 구름 패턴 생성을 위한 노이즈 시드값
            float[] cloudOffsets = new float[6];
            for (int i = 0; i < 6; i++)
            {
                cloudOffsets[i] = (float)random.NextDouble() * 100.0f;
            }

            // 구름 패턴의 세부 조정을 위한 매개변수 (변경된 부분)
            float cloudNoiseScale = 3.0f + cloudThickness * 2.0f;  // 구름 패턴의 크기 (두꺼울수록 더 큰 패턴)
            float cloudDetailScale = 6.0f + cloudThickness * 3.0f; // 구름 세부 패턴의 크기
            float cloudThreshold = 0.1f - cloudCoverage * 0.2f;   // 임계값 낮춤: 0.3f -> 0.1f, 변화 범위 확대: 0.25f -> 0.2f

            // 구름 고도 계산을 위한 매개변수 (변경된 부분)
            float cloudBaseAltitude = Math.Max(0.01f, cloudAltitude * 0.5f - 0.1f); // 구름 하한 낮춤
            float cloudTopAltitude = Math.Min(0.95f, cloudBaseAltitude + 0.3f + cloudThickness * 0.4f); // 구름 상한 높임

            // 텍스처 데이터 생성
            float[] textureData = new float[width * height * 4]; // RGBA

            for (int y = 0; y < height; y++)
            {
                // 정규화된 y 좌표 (0 = 하단, 1 = 상단)
                float v = (float)y / height;

                // 상단 반구에서의 고도각 계산 (0 = 지평선, PI/2 = 천정)
                float altitude = (float)(v * Math.PI / 2.0);

                for (int x = 0; x < width; x++)
                {
                    // 정규화된 x 좌표 (0 = 왼쪽, 1 = 오른쪽)
                    float u = (float)x / width;

                    // 텍스처에서의 방위각 계산 (0 ~ 2PI)
                    float azimuth = (float)(u * 2.0 * Math.PI);

                    // 현재 픽셀의 방향 벡터 계산 (구면 좌표)
                    float pixelX = (float)(Math.Cos(altitude) * Math.Sin(azimuth));
                    float pixelY = (float)(Math.Sin(altitude));
                    float pixelZ = (float)(Math.Cos(altitude) * Math.Cos(azimuth));

                    Vertex3f pixelDir = new Vertex3f(pixelX, pixelY, pixelZ);

                    // 태양과 현재 픽셀 방향 사이의 각도 계산 (내적 이용)
                    float dotProduct = pixelDir.x * sunPosition.x +
                                      pixelDir.y * sunPosition.y +
                                      pixelDir.z * sunPosition.z;

                    // 내적값은 -1 ~ 1 범위, 1에 가까울수록 태양에 가까움
                    float sunFactor = Math.Max(0, (dotProduct - 0.9f) * 10.0f); // 태양 주변 그라데이션

                    // 천정과 지평선 사이의 혼합
                    float skyBlend = (float)Math.Sin(altitude); // 천정(1)에서 지평선(0)으로

                    // 기본 하늘색 (천정에서 지평선으로)
                    Vertex3f skyColor = new Vertex3f(
                        skyZenithColor.x * skyBlend + skyHorizonColor.x * (1 - skyBlend),
                        skyZenithColor.y * skyBlend + skyHorizonColor.y * (1 - skyBlend),
                        skyZenithColor.z * skyBlend + skyHorizonColor.z * (1 - skyBlend)
                    );

                    // 일출/일몰 효과 적용 (태양 고도가 낮을 때)
                    if (sunsetFactor > 0)
                    {
                        // 지평선 근처일수록 더 강한 일출/일몰 색상
                        float horizonFactor = (1.0f - skyBlend) * sunsetFactor;

                        // 태양 방향으로 더 강한 일출/일몰 색상
                        // 태양 방향과의 각도 계산 (azimuth 차이)
                        float sunDirFactor = 0.5f + 0.5f * dotProduct;  // -1~1 범위를 0~1로 변환

                        // 일출/일몰 색상 적용 강도
                        float sunsetBlend = horizonFactor * sunDirFactor * 0.7f;

                        // 하늘색에 일출/일몰 색상 혼합
                        skyColor = new Vertex3f(
                            skyColor.x * (1 - sunsetBlend) + sunriseColor.x * sunsetBlend,
                            skyColor.y * (1 - sunsetBlend) + sunriseColor.y * sunsetBlend,
                            skyColor.z * (1 - sunsetBlend) + sunriseColor.z * sunsetBlend
                        );
                    }

                    // 태양과 하늘색 혼합
                    Vertex3f finalColor = new Vertex3f(
                        skyColor.x * (1 - sunFactor) + sunColor.x * sunFactor,
                        skyColor.y * (1 - sunFactor) + sunColor.y * sunFactor,
                        skyColor.z * (1 - sunFactor) + sunColor.z * sunFactor
                    );

                    // 태양 주변 글로우 효과 적용
                    if (dotProduct > 0.95f && dotProduct < 0.99f)
                    {
                        float glowFactor = (dotProduct - 0.95f) * 25.0f; // 0.95~0.99 사이에서 0~1
                        finalColor = new Vertex3f(
                            finalColor.x * (1 - glowFactor) + sunGlowColor.x * glowFactor,
                            finalColor.y * (1 - glowFactor) + sunGlowColor.y * glowFactor,
                            finalColor.z * (1 - glowFactor) + sunGlowColor.z * glowFactor
                        );
                    }

                    // 태양 자체
                    if (dotProduct > 0.995f)
                    {
                        finalColor = sunColor;
                    }

                    // 구름 효과 적용 - 프로시저럴 노이즈 기반 구름
                    // 고도값으로 구름 영역 결정 (입력 매개변수에 따라 조절)
                    float normalizedAltitude = skyBlend; // 0 = 지평선, 1 = 천정

                    // 현재 고도가 구름 영역 내에 있는지 확인
                    bool isInCloudRange = normalizedAltitude >= cloudBaseAltitude &&
                                         normalizedAltitude <= cloudTopAltitude;

                    if (isInCloudRange && cloudCoverage > 0.01f)
                    {
                        // 고도에 따른 구름 밀도 계산 부분 (변경된 부분)
                        float cloudMiddle = (cloudBaseAltitude + cloudTopAltitude) * 0.5f;
                        float verticalPosition = 1.0f - Math.Min(1.0f, Math.Abs(normalizedAltitude - cloudMiddle) /
                                                ((cloudTopAltitude - cloudBaseAltitude) * 0.5f));

                        // 구름 밀도는 중심에서 최대 (변경된 부분)
                        float cloudDensityByAltitude = verticalPosition * 1.2f; // 1.2배 증가

                        // 구름 패턴 노이즈 계산
                        // 여러 주파수의 노이즈를 결합하여 구름 패턴 생성
                        float cloudNoise = 0;

                        // 첫 번째 노이즈 레이어 (큰 구름 형태)
                        float nx1 = (pixelX * 1.5f + cloudOffsets[0]) * cloudNoiseScale;
                        float ny1 = (pixelY * 1.5f + cloudOffsets[1]) * cloudNoiseScale;
                        float nz1 = (pixelZ * 1.5f + cloudOffsets[2]) * cloudNoiseScale;
                        float noise1 = SimplexNoise(nx1, ny1, nz1) * 0.65f;

                        // 두 번째 노이즈 레이어 (작은 구름 디테일)
                        float nx2 = (pixelX * 4.0f + cloudOffsets[3]) * cloudDetailScale;
                        float ny2 = (pixelY * 4.0f + cloudOffsets[4]) * cloudDetailScale;
                        float nz2 = (pixelZ * 4.0f + cloudOffsets[5]) * cloudDetailScale;
                        float noise2 = SimplexNoise(nx2, ny2, nz2) * 0.35f;

                        // 노이즈 결합
                        cloudNoise = noise1 + noise2;

                        // 구름 threshold를 적용한 밀도 계산 (변경된 부분)
                        float cloudAmount = Math.Max(0, (cloudNoise - cloudThreshold) / (1.0f - cloudThreshold));

                        // 고도에 따른 구름 밀도 적용
                        cloudAmount *= cloudDensityByAltitude;

                        // 구름 양(coverage) 매개변수 적용 (변경된 부분)
                        cloudAmount *= cloudCoverage * 1.5f; // 1.5배 증가

                        if (cloudAmount > 0.01f)
                        {
                            // 태양 방향의 구름은 밝게, 반대 방향은 어둡게
                            // 구름 내부에서의 그림자도 고려

                            // 빛의 방향과 픽셀 사이의 각도
                            float cloudSunDot = dotProduct;

                            // 일반적인 구름 밝기 계산 (태양과의 각도에 따라)
                            float cloudBrightness = 0.5f + 0.5f * cloudSunDot; // 0.0 ~ 1.0

                            // 구름 내부 그림자 계산 (구름이 두꺼울수록 더 어두움)
                            float cloudShadow = cloudAmount * cloudThickness;
                            cloudBrightness = Math.Max(0.2f, cloudBrightness - cloudShadow * 0.5f);

                            // 일출/일몰 효과를 구름 색상에 적용
                            Vertex3f baseCloudColor;
                            if (sunsetFactor > 0)
                            {
                                // 태양 방향과 가까울수록, 지평선에 가까울수록 일몰 색상 강화
                                float cloudSunsetFactor = sunsetFactor * Math.Max(0, cloudSunDot) * (1.0f - skyBlend);

                                // 구름 색상 혼합 (일반 구름 + 일몰 효과)
                                baseCloudColor = new Vertex3f(
                                    cloudBrightColor.x * (1 - cloudSunsetFactor) + cloudSunsetColor.x * cloudSunsetFactor,
                                    cloudBrightColor.y * (1 - cloudSunsetFactor) + cloudSunsetColor.y * cloudSunsetFactor,
                                    cloudBrightColor.z * (1 - cloudSunsetFactor) + cloudSunsetColor.z * cloudSunsetFactor
                                );
                            }
                            else
                            {
                                baseCloudColor = cloudBrightColor;
                            }

                            // 최종 구름 색상 계산 (밝은 부분과 어두운 부분 혼합)
                            Vertex3f actualCloudColor = new Vertex3f(
                                baseCloudColor.x * cloudBrightness + cloudDarkColor.x * (1 - cloudBrightness),
                                baseCloudColor.y * cloudBrightness + cloudDarkColor.y * (1 - cloudBrightness),
                                baseCloudColor.z * cloudBrightness + cloudDarkColor.z * (1 - cloudBrightness)
                            );

                            // 구름 색상에 약간의 파란색 추가 (대기 색상 반영)
                            actualCloudColor = new Vertex3f(
                                actualCloudColor.x * 0.95f + skyColor.x * 0.05f,
                                actualCloudColor.y * 0.95f + skyColor.y * 0.05f,
                                actualCloudColor.z * 0.95f + skyColor.z * 0.05f
                            );

                            // 구름과 하늘색 혼합
                            finalColor = new Vertex3f(
                                finalColor.x * (1 - cloudAmount) + actualCloudColor.x * cloudAmount,
                                finalColor.y * (1 - cloudAmount) + actualCloudColor.y * cloudAmount,
                                finalColor.z * (1 - cloudAmount) + actualCloudColor.z * cloudAmount
                            );
                        }
                    }

                    // 텍스처 데이터에 저장
                    int index = (y * width + x) * 4;
                    textureData[index + 0] = finalColor.x; // R
                    textureData[index + 1] = finalColor.y; // G
                    textureData[index + 2] = finalColor.z; // B
                    textureData[index + 3] = 1.0f;         // A (완전 불투명)
                }
            }

            // 텍스처 생성 및 업로드
            _tempSkyTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _tempSkyTextureId);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0,
                         PixelFormat.Rgba, PixelType.Float, textureData);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }


        /// <summary>
        /// Simplex 노이즈 함수 - 구름 패턴 생성에 사용
        /// </summary>
        /// <param name="x">x 좌표</param>
        /// <param name="y">y 좌표</param>
        /// <param name="z">z 좌표</param>
        /// <returns>-1에서 1 사이의 노이즈 값</returns>
        private float SimplexNoise(float x, float y, float z)
        {
            // 간단한 구현을 위해 Perlin 노이즈의 근사치 사용
            // 실제 프로젝트에서는 좀 더 복잡하고 효율적인 Simplex 노이즈 구현을 사용하는 것이 좋음

            // 정수 부분과 소수 부분 분리
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            int zi = (int)Math.Floor(z);
            float xf = x - xi;
            float yf = y - yi;
            float zf = z - zi;

            // Fade 함수 적용
            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            // 해시 함수 대신 간단한 방법으로 랜덤값 생성
            float n000 = Gradient(Hash(xi, yi, zi), xf, yf, zf);
            float n001 = Gradient(Hash(xi, yi, zi + 1), xf, yf, zf - 1);
            float n010 = Gradient(Hash(xi, yi + 1, zi), xf, yf - 1, zf);
            float n011 = Gradient(Hash(xi, yi + 1, zi + 1), xf, yf - 1, zf - 1);
            float n100 = Gradient(Hash(xi + 1, yi, zi), xf - 1, yf, zf);
            float n101 = Gradient(Hash(xi + 1, yi, zi + 1), xf - 1, yf, zf - 1);
            float n110 = Gradient(Hash(xi + 1, yi + 1, zi), xf - 1, yf - 1, zf);
            float n111 = Gradient(Hash(xi + 1, yi + 1, zi + 1), xf - 1, yf - 1, zf - 1);

            // 선형 보간
            float n00 = Lerp(n000, n100, u);
            float n01 = Lerp(n001, n101, u);
            float n10 = Lerp(n010, n110, u);
            float n11 = Lerp(n011, n111, u);

            float n0 = Lerp(n00, n10, v);
            float n1 = Lerp(n01, n11, v);

            // 결과 반환 (범위: -1 ~ 1)
            return Lerp(n0, n1, w) * 2.0f;
        }

        /// <summary>
        /// Fade 함수 - 노이즈의 부드러운 보간에 사용
        /// </summary>
        private float Fade(float t)
        {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        /// <summary>
        /// 해시 함수 - 랜덤값 생성에 사용
        /// </summary>
        private int Hash(int x, int y, int z)
        {
            // 간단한 해시 함수
            return (x * 73856093 ^ y * 19349663 ^ z * 83492791) & 0xFF;
        }

        /// <summary>
        /// 그래디언트 함수 - 노이즈 방향성 부여에 사용
        /// </summary>
        private float Gradient(int hash, float x, float y, float z)
        {
            // h에 따라 서로 다른 그래디언트 방향 선택
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// 선형 보간 함수
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// 스카이돔을 렌더링합니다.
        /// </summary>
        /// <param name="camera">현재 카메라</param>
        /// <param name="skyTextureId">하늘 텍스처 ID (null이면 임시 텍스처 사용)</param>
        public void RenderSkyDome(Camera camera, uint? skyTextureId = null)
        {
            // 셰이더 바인딩
            _skyDomeShader.Bind();

            // 깊이 테스트 설정 (스카이돔은 항상 가장 뒤에)
            Gl.DepthFunc(DepthFunction.Lequal);
            Gl.DepthMask(false); // 깊이 버퍼에 쓰기 비활성화

            // 컬링 설정 (안쪽 면은 렌더링하지 않음)
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);
            Gl.FrontFace(FrontFaceDirection.Cw); // 시계 방향으로 설정

            // 모델 행렬 설정 (카메라 위치에 스카이돔 배치, 큰 반지름으로 스케일링)
            Matrix4x4f modelMatrix = Matrix4x4f.Translated(camera.Position.x, camera.Position.y, camera.Position.z)
                                  * Matrix4x4f.Scaled(1000.0f, 1000.0f, 1000.0f); // 충분히 큰 스케일로 설정

            // 셰이더 유니폼 설정
            _skyDomeShader.LoadUniform(SkyDomeShader.UNIFORM_NAME.model, modelMatrix);
            _skyDomeShader.LoadUniform(SkyDomeShader.UNIFORM_NAME.view, camera.ViewMatrix);
            _skyDomeShader.LoadUniform(SkyDomeShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);

            // 텍스처 바인딩 (제공된 텍스처 또는 임시 텍스처 사용)
            uint textureToUse = skyTextureId.HasValue ? skyTextureId.Value : _tempSkyTextureId;
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureToUse);
            _skyDomeShader.LoadUniform(SkyDomeShader.UNIFORM_NAME.skyTexture, 0);

            // 스카이돔 메시 렌더링
            Gl.BindVertexArray(_skyDomeVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, _skyDomeVertexCount);
            Gl.BindVertexArray(0);

            // 렌더링 상태 복원
            Gl.FrontFace(FrontFaceDirection.Ccw); // 기본 프론트 페이스 방향으로 복원
            Gl.DepthMask(true); // 깊이 버퍼에 쓰기 활성화
            Gl.DepthFunc(DepthFunction.Less); // 기본 깊이 함수로 복원

            // 셰이더 언바인딩
            _skyDomeShader.Unbind();
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            // 텍스처 해제
            Gl.DeleteTextures(_tempSkyTextureId);

            // VAO 및 VBO 해제 (실제 구현에서는 VBO도 저장하고 삭제해야 함)
            Gl.DeleteVertexArrays(_skyDomeVAO);
        }
    }
}
