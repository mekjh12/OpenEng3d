using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace Renderer
{
    /// <summary>
    /// 컴퓨트 셰이더를 사용하여 하늘 텍스처를 생성하고 렌더링하는 클래스입니다.
    /// </summary>
    public class SkyRendererCompTextureGen
    {
        // 스카이돔 메시를 위한 변수들
        private uint _skyDomeVAO;
        private int _skyDomeVertexCount;
        private SkyDomeShader _skyDomeShader;
        private SkyDomeTextureComputeShader _skyComputeShader;

        // 텍스처 ID
        private uint _skyTextureId;
        private Vertex3f _sunPosition;

        /// <summary>
        /// 태양의 위치를 가져옵니다.
        /// </summary>
        public Vertex3f SunPosition => _sunPosition;

        /// <summary>
        /// 텍스처 ID를 가져옵니다.  
        /// </summary>
        public uint SkyTextureId
        {
            get => _skyTextureId;
            set => _skyTextureId = value;
        }

        public SkyDomeTextureComputeShader SkyComputeShader
        {
            get => _skyComputeShader; 
            set => _skyComputeShader = value;
        }

        /// <summary>
        /// SkyRendererCompTextureGen 클래스의 생성자입니다.
        /// </summary>
        /// <param name="skyDomeShader"></param>
        /// <param name="skyTextureComputeShader"></param>
        public SkyRendererCompTextureGen(SkyDomeShader skyDomeShader, SkyDomeTextureComputeShader skyTextureComputeShader)
        {
            // 셰이더 초기화
            _skyDomeShader = skyDomeShader;

            // 스카이돔 메시 생성
            CreateSkyDomeMesh();

            // 컴퓨트 셰이더 초기화
            SkyComputeShader = skyTextureComputeShader;

            // 초기 하늘 텍스처 생성
            GenerateSkyTexture(cloudCoverage: 0.9f, cloudAltitude: 0.1f, cloudThickness: 0.9f);
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
        /// GPU를 사용하여 하늘 텍스처를 생성합니다.
        /// </summary>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudAltitude">구름의 고도 (0.0 = 지평선에 가까움, 0.8 = 천정에 가까움)</param>
        /// <param name="cloudThickness">구름의 두께 (0.1 = 얇음, 1.0 = 두꺼움)</param>
        /// <param name="useFixedCloudPattern">고정된 구름 패턴 사용 여부 (태양 위치와 독립적, 기본값: true)</param>
        public void GenerateSkyTexture(float cloudCoverage = 0.5f, float cloudAltitude = 0.2f,
                                      float cloudThickness = 0.5f, bool useFixedCloudPattern = true)
        {
            // 매개변수 범위 확인
            cloudCoverage = cloudCoverage.Clamp(0.0f, 1.0f);
            cloudAltitude = cloudAltitude.Clamp(0.0f, 0.8f);
            cloudThickness = cloudThickness.Clamp(0.1f, 1.0f);

            // 랜덤 태양 위치 생성
            Vertex3f sunPosition = SkyComputeShader.GenerateRandomSunPosition();

            // 컴퓨트 셰이더로 텍스처 생성 (수정된 메서드 호출)
            SkyTextureId = SkyComputeShader.GenerateSkyTextureWithSunPosition(
                sunPosition, cloudCoverage, cloudAltitude, cloudThickness, useFixedCloudPattern);

            Console.WriteLine($"하늘 텍스처 생성 완료 - 구름 설정: 양={cloudCoverage}, 고도={cloudAltitude}, 두께={cloudThickness}");
        }

        /// <summary>
        /// 지정된 태양 위치로 하늘 텍스처를 생성합니다.
        /// </summary>
        /// <param name="sunPosition">태양 위치 벡터</param>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudAltitude">구름의 고도 (0.0 = 지평선에 가까움, 0.8 = 천정에 가까움)</param>
        /// <param name="cloudThickness">구름의 두께 (0.1 = 얇음, 1.0 = 두꺼움)</param>
        /// <param name="useFixedCloudPattern">고정된 구름 패턴 사용 여부 (태양 위치와 독립적, 기본값: true)</param>
        public void GenerateSkyTextureWithSunPosition(Vertex3f sunPosition, float cloudCoverage = 0.5f,
                                                 float cloudAltitude = 0.2f, float cloudThickness = 0.5f,
                                                 bool useFixedCloudPattern = true)
        {
            // 컴퓨트 셰이더로 텍스처 생성
            SkyTextureId = SkyComputeShader.GenerateSkyTextureWithSunPosition(
                sunPosition, cloudCoverage, cloudAltitude, cloudThickness, useFixedCloudPattern);
        }

        /// <summary>
        /// 스카이돔을 렌더링합니다.
        /// </summary>
        /// <param name="camera">현재 카메라</param>
        /// <param name="customSkyTextureId">커스텀 하늘 텍스처 ID (null이면 자동 생성된 텍스처 사용)</param>
        public void RenderSkyDome(Camera camera, uint? customSkyTextureId = null)
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

            // 텍스처 바인딩 (제공된 텍스처 또는 자동 생성된 텍스처 사용)
            uint textureToUse = customSkyTextureId.HasValue ? customSkyTextureId.Value : SkyTextureId;
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
        /// 현재 생성된 하늘 텍스처를 비트맵 파일로 저장합니다. (디버깅용)
        /// </summary>
        /// <param name="filename">저장할 파일 경로</param>
        public void __SaveSkyTextureToFile(string filename)
        {
            SkyComputeShader.SaveTextureToBitmap(filename);
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            // 컴퓨트 셰이더 해제
            if (SkyComputeShader != null)
            {
                SkyComputeShader.CleanUp();
                SkyComputeShader = null;
            }

            // 스카이돔 셰이더 해제
            if (_skyDomeShader != null)
            {
                _skyDomeShader.CleanUp();
                _skyDomeShader = null;
            }

            // VAO 해제
            if (_skyDomeVAO != 0)
            {
                Gl.DeleteVertexArrays(_skyDomeVAO);
                _skyDomeVAO = 0;
            }
        }
    }
}