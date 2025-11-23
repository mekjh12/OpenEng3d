using Common;
using Common.Abstractions;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 3D 텍스처를 이용한 구름 렌더링 셰이더
    /// </summary>
    public class CloudTestRenderShader : ShaderProgram<CloudTestRenderShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 버텍스 셰이더 유니폼
            model,
            view,
            proj,
            mvp,
            camPosMeter,
            // 프래그먼트 셰이더 유니폼
            cloudTexture,  // 구름 3D 텍스처
            cloudDensity,  // 구름 밀도 스케일
            cloudColor,    // 구름 색상
            cubeSize,      // 큐브 크기 (추가됨)
                           // 광원 관련 유니폼
            lightDir,      // 광원 방향
            lightColor,     // 광원 색상
            lightIntensity, // 광원 세기

            coverage,       // 
            cloudBottom,
            cloudTop,
            /// <summary>
            /// Henyey-Greenstein 위상 함수의 비대칭 인자입니다.
            /// 값 범위는 -1에서 1 사이이며:
            /// g = 0: 등방성 산란(모든 방향으로 균등하게 산란)
            /// g > 0: 전방 산란이 우세
            /// g < 0: 후방 산란이 우세
            /// </summary>
            g,
            // 그림자 맵 유니폼 추가
            shadowTexture,  // 사전 계산된 그림자맵
                            // 총 유니폼 개수
            Count
        }

        const string VERTEx_FILE = @"\Shader\CloudShader\cloudRenderTest.vert";
        const string FRAGMENT_FILE = @"\Shader\CloudShader\volumeRaycasting.frag";

        // 구름을 렌더링할 큐브 메시
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _indexCount;
        private float _cubeSize = 10.0f; // 기본 큐브 크기

        public CloudTestRenderShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 버텍스/프래그먼트 셰이더만 설정
            VertFileName = projectPath + VERTEx_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            InitCompileShader();
            InitCloudCube();
        }

        // 큐브 크기 설정 메서드 추가
        public void SetCubeSize(float size)
        {
            _cubeSize = size;
            // 큐브 크기가 변경되면 메시를 다시 초기화
            InitCloudCube();
        }

        // 구름을 렌더링할 큐브 초기화
        private void InitCloudCube()
        {
            // 큐브의 크기 설정 (멤버 변수 사용)
            float halfSize = _cubeSize * 0.5f;

            // 큐브의 8개 정점 정의
            float[] vertices = {
                // 앞면 (z = halfSize)
                -halfSize,  halfSize,  halfSize,     0.0f, 1.0f,  // 0: 좌상단
                -halfSize, -halfSize,  halfSize,     0.0f, 0.0f,  // 1: 좌하단
                 halfSize, -halfSize,  halfSize,     1.0f, 0.0f,  // 2: 우하단
                 halfSize,  halfSize,  halfSize,     1.0f, 1.0f,  // 3: 우상단
                
                // 뒷면 (z = -halfSize)
                -halfSize,  halfSize, -halfSize,     0.0f, 1.0f,  // 4: 좌상단
                -halfSize, -halfSize, -halfSize,     0.0f, 0.0f,  // 5: 좌하단
                 halfSize, -halfSize, -halfSize,     1.0f, 0.0f,  // 6: 우하단
                 halfSize,  halfSize, -halfSize,     1.0f, 1.0f,  // 7: 우상단
            };

            // 큐브의 6개 면 (각 면은 2개의 삼각형으로 구성)
            uint[] indices = {
                // 앞면
                0, 1, 2,
                0, 2, 3,    
                
                // 오른쪽 면
                3, 2, 6,
                3, 6, 7,    
                
                // 뒷면
                7, 6, 5,
                7, 5, 4,    
                
                // 왼쪽 면
                4, 5, 1,
                4, 1, 0,    
                
                // 윗면
                4, 0, 3,
                4, 3, 7,    
                
                // 아랫면
                1, 5, 6,
                1, 6, 2
            };

            _indexCount = indices.Length;

            _vao = Gl.GenVertexArray();
            _vbo = Gl.GenBuffer();
            _ebo = Gl.GenBuffer();

            Gl.BindVertexArray(_vao);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length), vertices, BufferUsage.StaticDraw);

            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (uint)(sizeof(uint) * indices.Length), indices, BufferUsage.StaticDraw);

            // 위치 속성
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 5 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            // 텍스처 좌표 속성
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 5 * sizeof(float), (IntPtr)(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindVertexArray(0);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        /// <summary>
        /// 구름 렌더링 실행
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="g">Henyey-Greenstein 비대칭 인자</param>
        /// <param name="clouldDensity">구름 밀도 스케일</param>
        /// <param name="cloud3dTextureId">구름 3D 텍스처 핸들</param>
        /// <param name="shadowMapTextureId">그림자 맵 텍스처 핸들</param>
        public void Run(Camera camera, float g, float clouldDensity, uint cloud3dTextureId, uint shadowMapTextureId)
        {
            // 셰이더 시작
            Bind();

            // 뷰, 프로젝션 행렬 설정
            Matrix4x4f view = camera.ViewMatrix;
            Matrix4x4f proj = camera.ProjectiveMatrix;

            // 모델 행렬 - 카메라를 따라가도록 설정
            Matrix4x4f model = Matrix4x4f.Identity;

            // MVP 행렬 설정
            Matrix4x4f mvp = proj * view * model;

            // 유니폼 변수 설정
            LoadUniform(UNIFORM_NAME.model, model);
            LoadUniform(UNIFORM_NAME.view, view);
            LoadUniform(UNIFORM_NAME.proj, proj);
            LoadUniform(UNIFORM_NAME.mvp, mvp);
            LoadUniform(UNIFORM_NAME.camPosMeter, camera.Position);

            // 큐브 크기 추가
            LoadUniform(UNIFORM_NAME.cubeSize, _cubeSize);

            // 깊이 테스트 설정 - 반투명 렌더링을 위한 설정
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 컬링 비활성화 - 큐브 내부에서 렌더링할 수 있도록
            //Gl.Disable(EnableCap.CullFace);

            // VAO 바인딩 및 렌더링
            Gl.BindVertexArray(_vao);
            Gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            Gl.BindVertexArray(0);

            // 상태 복원
            //Gl.Enable(EnableCap.CullFace);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.Disable(EnableCap.Blend);

            // 셰이더 종료
            Unbind();
        }
    }
}