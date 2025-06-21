using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 대기 산란 투과율 LUT(LookUp Table) 계산을 위한 컴퓨트 셰이더
    /// </summary>
    public class AtmosphericLUTComputeShader : ShaderProgram<AtmosphericLUTComputeShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 컴퓨트 셰이더 유니폼
            R_e,                // 지구 반지름
            R_a,                // 대기 반지름
            beta_R,             // Rayleigh 산란 계수
            beta_M,             // Mie 산란 계수
            H_R,                // Rayleigh 스케일 높이
            H_M,                // Mie 스케일 높이
            lightSamples,       // 광원 광선 샘플 수
            lutSize,            // LUT 텍스처 크기

            // 총 유니폼 개수
            Count
        }

        const string COMPUTE_FILE = @"\Shader\AtmosphericScatteringLUT\atmosphere.comp";
        private const int SAMPLE_COUNT = 8;

        // LUT 텍스처 속성 추가
        private uint _transmittanceLUTId;
        private readonly int _lutWidth = 64;
        private readonly int _lutHeight = 32;

        public uint TransmittanceLUTId { get => _transmittanceLUTId; }

        public AtmosphericLUTComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 컴퓨트 셰이더만 설정
            ComputeFileName = projectPath + COMPUTE_FILE;

            // 셰이더 초기화
            InitCompileShader();

            // LUT 텍스처 초기화
            InitializeLUT();
        }

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

        /// <summary>
        /// LUT 텍스처 초기화 메서드
        /// </summary>
        private void InitializeLUT()
        {
            // 텍스처 생성
            _transmittanceLUTId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _transmittanceLUTId);

            // 텍스처 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            // 텍스처 데이터 할당 (초기 값은 빈 상태)
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba32f,
                         _lutWidth, _lutHeight, 0,
                         PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 컴퓨트 셰이더로 LUT 계산 실행
            UpdateLUT();
        }

        /// <summary>
        /// LUT 업데이트 메서드 (매개변수가 변경될 때 호출)
        /// </summary>
        /// <param name="earthRadius"></param>
        /// <param name="atmosphereRadius"></param>
        public void UpdateLUT(float earthRadius = 6371.0f, float atmosphereRadius = 6471.0f)
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
            LoadUniform(UNIFORM_NAME.lightSamples, SAMPLE_COUNT);

            // lutSize 유니폼 설정
            Gl.Uniform2(GetUniformLocation(UNIFORM_NAME.lutSize.ToString()), _lutWidth, _lutHeight);

            // 이미지 바인딩
            Gl.BindImageTexture(0, _transmittanceLUTId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba32f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute((uint)(_lutWidth / 16) + 1, (uint)(_lutHeight / 16) + 1, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // 언바인딩
            Unbind();
        }

        /// <summary>
        /// 메모리 해제 메서드 (프로그램 종료 시 호출)
        /// </summary>
        public override void CleanUp()
        {
            base.CleanUp();

            // LUT 텍스처 삭제
            Gl.DeleteTextures(_transmittanceLUTId);
        }
    }
}