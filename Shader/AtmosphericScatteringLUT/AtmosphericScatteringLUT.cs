using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// AtmosphericScatteringLUT 클래스 정의
    /// </summary>
    public class AtmosphericScatteringLUT : ShaderProgram<AtmosphericScatteringLUT.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 기존 투과율 LUT 파라미터
            R_e, R_a, beta_R, beta_M, H_R, H_M, lightSamples,

            // 산란 LUT 추가 파라미터
            sunZenithCosAngle,    // 태양 천정각 코사인
            sunAzimuthAngle,      // 태양 방위각
            viewerHeight,         // 관찰자 고도
            lutDimensions,        // LUT 크기 (각 차원별)

            Count
        }

        // 텍스처 ID들
        private uint _scatteringLUTId;     // 새로운 산란 LUT

        // LUT 크기 정의
        private readonly int _heightSamples = 32;      // 고도 샘플 수
        private readonly int _sunAngleSamples = 16;    // 태양 각도 샘플 수
        private readonly int _viewAngleSamples = 32;   // 시야 각도 샘플 수

        // 컴퓨트 셰이더 경로
        const string COMPUTE_FILE_SCATTERING = @"\Shader\AtmosphericScatteringLUT\scattering.comp";

        public uint ScatteringLUTId { get => _scatteringLUTId;}

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 BindAttributes가 필요 없음
        }

        protected override void GetAllUniformLocations()
        {
            // 유니폼 변수 이름을 이용하여 위치 찾기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        public AtmosphericScatteringLUT(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 컴퓨트 셰이더만 설정
            ComputeFileName = projectPath + COMPUTE_FILE_SCATTERING;

            // 셰이더 초기화
            InitCompileShader();

            // LUT 텍스처 초기화
            InitializeScatteringLUT();
        }

        // LUT 초기화 및 업데이트 메서드
        public void InitializeScatteringLUT()
        {
            // 3D 텍스처 생성
            _scatteringLUTId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, _scatteringLUTId);

            // 텍스처 설정
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, Gl.CLAMP_TO_EDGE);

            // 텍스처 데이터 할당 (RGBA32F 포맷)
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.Rgba32f,
                         _viewAngleSamples, _sunAngleSamples, _heightSamples, 0,
                         PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // 컴퓨트 셰이더로 산란 LUT 계산 실행
            UpdateScatteringLUT();
        }

        // 산란 LUT 업데이트 메서드
        public void UpdateScatteringLUT(float earthRadius = 6371.0f, float atmosphereRadius = 6471.0f)
        {
            // 컴퓨트 셰이더 바인딩
            Bind();

            // 유니폼 변수 설정
            LoadUniform(UNIFORM_NAME.R_e, earthRadius);
            LoadUniform(UNIFORM_NAME.R_a, atmosphereRadius);
            LoadUniform(UNIFORM_NAME.beta_R, new Vertex3f(0.0058f, 0.0135f, 0.0331f));
            LoadUniform(UNIFORM_NAME.beta_M, 0.0210f);
            LoadUniform(UNIFORM_NAME.H_R, 7.994f);
            LoadUniform(UNIFORM_NAME.H_M, 1.20f);
            LoadUniform(UNIFORM_NAME.sunZenithCosAngle, 0.5f);  // 태양 천정각 코사인
            LoadUniform(UNIFORM_NAME.sunAzimuthAngle, 0.0f);    // 태양 방위각
            LoadUniform(UNIFORM_NAME.viewerHeight, 0.0f);       // 관찰자 고도
            LoadUniform(UNIFORM_NAME.lutDimensions, new Vertex3f(_viewAngleSamples, _sunAngleSamples, _heightSamples)); // LUT 크기 (각 차원별)

            // 이미지 바인딩
            Gl.BindImageTexture(0, _scatteringLUTId, 0, true, 0,
                               BufferAccess.WriteOnly, InternalFormat.Rgba32f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute(
                (uint)(_viewAngleSamples / 8) + 1,
                (uint)(_sunAngleSamples / 8) + 1,
                (uint)(_heightSamples / 8) + 1
            );

            // 메모리 배리어
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}
