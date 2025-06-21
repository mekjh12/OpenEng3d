using Camera3d;
using Common.Abstractions;
using OpenGL;
using Renderer;
using Shader;
using System;
using ZetaExt;

namespace Sky
{
    /// <summary>
    /// 물리 기반 대기 산란 시뮬레이션을 관리하고 렌더링하는 클래스입니다.
    /// 지구와 대기권을 모델링하여 현실적인 하늘을 시각화합니다.
    /// </summary>
    [Obsolete("Atmosphere 클래스는 더 이상 사용되지 않습니다. 대신 SkySystem 클래스를 사용하세요.")]
    public class Atmosphere
    {
        private AtmosphereShader _atmosphereShader;     // 대기 산란 계산을 위한 셰이더
        readonly float _radius_Atmosphere;              // 대기권의 반지름 (km)
        readonly float _radius_Earth;                   // 지구의 반지름 (km)
        float _theta = 0.0f;                            // 태양의 방위각(각도)

        /// <summary>
        /// 태양의 방위각을 가져옵니다.
        /// </summary>
        public float Theta => _theta;

        /// <summary>
        /// 지구 반지름 값을 가져옵니다.
        /// </summary>
        public float RadiusEarth => _radius_Earth;

        /// <summary>
        /// 대기권 반지름 값을 가져옵니다.
        /// </summary>
        public float RadiusAtmosphere => _radius_Atmosphere;

        /// <summary>
        /// Atmosphere 클래스의 생성자입니다.
        /// </summary>
        /// <param name="PROJECT_PATH">프로젝트 파일 경로</param>
        public Atmosphere(string PROJECT_PATH, float radius_Earth = 16361.0f, float radius_Atmosphere = 19421.0f)
        {
            _atmosphereShader = new AtmosphereShader(PROJECT_PATH);
            _radius_Atmosphere = radius_Atmosphere;
            _radius_Earth = radius_Earth;
        }

        /// <summary>
        /// 대기 시스템을 업데이트합니다.
        /// </summary>
        /// <param name="deltaTime">이전 프레임과 현재 프레임 사이의 시간 간격(밀리초)</param>
        public void Update(int deltaTime)
        {

        }

        /// <summary>
        /// 대기 효과를 렌더링합니다.
        /// </summary>
        /// <param name="camera">씬을 관찰하는 카메라</param>
        public void Render(Camera camera)
        {
            OrbitCamera orbCamera = camera as OrbitCamera;  // 카메라를 궤도 카메라로 캐스팅

            // TODO :: 땅위에서 (0,0,0)에서 대기를 구현할 필요가 있음.
            // 현재까지는 (0,0,R_e)에서 구현됨.

            // 태양 위치 계산 (구면 좌표계 사용)
            Vertex3f sunPos = new Vertex3f(
                MathF.Cos(_theta.ToRadian()),   // x 좌표 (방위각의 코사인)
                0.0f,                           // y 좌표 (항상 0, 수평면 기준)
                MathF.Sin(_theta.ToRadian())    // z 좌표 (방위각의 사인)
            );

            // 셰이더 바인딩 및 유니폼 변수 설정
            _atmosphereShader.Bind();

            // 광선 샘플링 매개변수 설정
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.viewSamples, 16);    // 시점 광선 샘플링 수
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.lightSamples, 8);    // 광원 광선 샘플링 수

            // 관찰자 및 태양 위치 설정
            _atmosphereShader.LoadUniform(
                AtmosphereShader.UNIFORM_NAME.viewPos, 
                orbCamera.Position                         // 지구 표면 상의 카메라 위치
            );
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.sunPos, sunPos);     // 태양 위치

            // 대기 산란 물리 매개변수 설정
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.I_sun, 120.0f);              // 태양 광원 강도
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.R_e, _radius_Earth);        // 지구 반지름 (km)
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.R_a, _radius_Atmosphere);   // 대기권 반지름 (km)

            // Rayleigh 산란 계수 (파장에 따라 다름 - RGB)
            _atmosphereShader.LoadUniform(
                AtmosphereShader.UNIFORM_NAME.beta_R,
                new Vertex3f(0.0058f, 0.0135f, 0.0331f)  // 각 RGB 채널에 대한 산란 계수
            );

            // Mie 산란 계수 및 매개변수
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.beta_M, 0.0210f);    // Mie 산란 계수
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.H_R, 7.994f);        // Rayleigh 스케일 높이
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.H_M, 1.20f);         // Mie 스케일 높이
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.g, 0.888f);          // Mie 비등방성 계수

            // 톤 매핑 설정
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.toneMappingFactor, true);

            // 모델 변환 행렬 설정 (대기권 크기로 스케일)
            Matrix4x4f model = Matrix4x4f.Scaled(
                _radius_Atmosphere,   // x축 스케일
                _radius_Atmosphere,   // y축 스케일
                _radius_Atmosphere    // z축 스케일
            );

            // 변환 행렬 로드
            _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.model, model);   // 모델 행렬
                _atmosphereShader.LoadUniform(AtmosphereShader.UNIFORM_NAME.mvp,         // 모델-뷰-프로젝션 행렬
                orbCamera.VPMatrix * model);                                            

            // 렌더링 상태 설정 및 그리기
            Gl.Disable(EnableCap.CullFace);               // 후면 컬링 비활성화 (대기는 양면 렌더링)
            Gl.BindVertexArray(Renderer3d.Earth.VAO);     // 지구 메시의 VAO 바인딩
            Gl.EnableVertexAttribArray(0);                // 정점 위치 속성 활성화
            Gl.DrawArrays(                                // 지구 메시 그리기
                PrimitiveType.Triangles,                  // 삼각형으로 그리기
                0,                                        // 시작 인덱스
                Renderer3d.Earth.VertexCount              // 정점 수
            );
            Gl.DisableVertexAttribArray(0);               // 정점 속성 비활성화
            Gl.BindVertexArray(0);                        // VAO 바인딩 해제
            _atmosphereShader.Unbind();                   // 셰이더 언바인딩
            Gl.Enable(EnableCap.CullFace);                // 후면 컬링 다시 활성화

        }
    }
}