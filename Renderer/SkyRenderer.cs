using Common.Abstractions;
using OpenGL;
using Shader;
using System;

namespace Renderer
{
    /// <summary>
    /// 하늘과 구름 렌더링을 관리하는 중앙 렌더러 클래스
    /// </summary>
    public class SkyRenderer
    {
        // 스카이돔 메시를 위한 변수들
        private uint _skyDomeVAO;
        private int _skyDomeVertexCount;

        private SkyDomeTexture2dShader _skyDomeTexture2DShader;
        private SkyDomeRenderShader _skyDomeRenderShader;

        // 랜덤 생성기
        private Random _random = new Random();

        // 고정된 구름 오프셋
        private Vertex3f _fixedCloudOffset;

        /// <summary>
        /// 하늘 텍스처 생성기 초기화
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="width">텍스처 너비(픽셀)</param>
        /// <param name="height">텍스처 높이(픽셀)</param>
        public SkyRenderer(string projectPath, SkyDomeTexture2dShader skyDomeTexture2DShader)
        {
            // 셰이더 초기화
            _skyDomeTexture2DShader = skyDomeTexture2DShader;

            // 스카이돔 메시 생성
            CreateSkyDomeMesh();

            // 스카이돔 렌더링 셰이더 초기화
            _skyDomeRenderShader = new SkyDomeRenderShader(projectPath);

            // 고정된 구름 오프셋 초기화 (한 번만 생성하여 모든 렌더링에 사용)
            _fixedCloudOffset = new Vertex3f(
                (float)_random.NextDouble() * 100.0f - 50.0f,
                (float)_random.NextDouble() * 10.0f,
                (float)_random.NextDouble() * 100.0f - 50.0f
            );

            // 테스트용 고정 값
            _fixedCloudOffset = new Vertex3f(32.0f, 5.0f, 61.0f);
        }


        /// <summary>
        /// 스카이돔을 렌더링합니다.
        /// </summary>
        /// <param name="camera">현재 카메라</param>
        public void RenderSkyDome(Camera camera)
        {
            // 셰이더 바인딩
            _skyDomeRenderShader.Bind();

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
            _skyDomeRenderShader.LoadUniform(SkyDomeRenderShader.UNIFORM_NAME.model, modelMatrix);
            _skyDomeRenderShader.LoadUniform(SkyDomeRenderShader.UNIFORM_NAME.view, camera.ViewMatrix);
            _skyDomeRenderShader.LoadUniform(SkyDomeRenderShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);

            // 텍스처 바인딩
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, _skyDomeTexture2DShader.FinalTexture);
            _skyDomeRenderShader.LoadUniform(SkyDomeRenderShader.UNIFORM_NAME.skyTexture, 0);

            // 스카이돔 메시 렌더링
            Gl.BindVertexArray(_skyDomeVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, _skyDomeVertexCount);
            Gl.BindVertexArray(0);

            // 렌더링 상태 복원
            Gl.FrontFace(FrontFaceDirection.Ccw);   // 기본 프론트 페이스 방향으로 복원
            Gl.DepthMask(true);                     // 깊이 버퍼에 쓰기 활성화
            Gl.DepthFunc(DepthFunction.Less);       // 기본 깊이 함수로 복원

            // 셰이더 언바인딩
            _skyDomeRenderShader.Unbind();
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

    }
}