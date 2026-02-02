using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// River Mask Generation - Pass 1: Initial Filter
    /// 물 시뮬레이션 결과로부터 강 영역을 추출
    /// </summary>
    public class RiverMaskPass1ComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\River\river_mask_pass1.comp";

        // ====================================================================
        // Uniform 위치 캐싱
        // ====================================================================
        private int loc_waterBuffer;
        private int loc_riverMask;
        private int loc_minWaterDepth;
        private int loc_minFluxMagnitude;
        private int loc_deepWaterDepth;

        // ====================================================================
        // 생성자
        // ====================================================================
        public RiverMaskPass1ComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        // ====================================================================
        // Uniform 위치 가져오기
        // ====================================================================
        protected override void GetAllUniformLocations()
        {
            loc_waterBuffer = GetUniformLocation("waterBuffer");
            loc_riverMask = GetUniformLocation("riverMask");
            loc_minWaterDepth = GetUniformLocation("minWaterDepth");
            loc_minFluxMagnitude = GetUniformLocation("minFluxMagnitude");
            loc_deepWaterDepth = GetUniformLocation("deepWaterDepth");
        }

        // ====================================================================
        // Attribute 바인딩 (Compute Shader는 불필요)
        // ====================================================================
        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        // ====================================================================
        // 파라미터 설정 메서드들
        // ====================================================================

        /// <summary>
        /// 최소 물 깊이 threshold 설정
        /// 이보다 얕은 물은 무시됩니다
        /// 추천값: 0.05 (작을수록 더 많은 개울 포함)
        /// </summary>
        public void SetMinWaterDepth(float depth)
        {
            Gl.Uniform1(loc_minWaterDepth, depth);
        }

        /// <summary>
        /// 최소 흐름 강도 threshold 설정
        /// 이보다 약한 흐름은 "고인 물"로 간주
        /// 추천값: 0.01 (작을수록 약한 흐름도 포함)
        /// </summary>
        public void SetMinFluxMagnitude(float magnitude)
        {
            Gl.Uniform1(loc_minFluxMagnitude, magnitude);
        }

        /// <summary>
        /// 깊은 물 판별 기준 설정
        /// 이보다 깊으면 흐르지 않아도 "강"으로 포함 (호수 등)
        /// 추천값: 0.2
        /// </summary>
        public void SetDeepWaterDepth(float depth)
        {
            Gl.Uniform1(loc_deepWaterDepth, depth);
        }

        /// <summary>
        /// 모든 파라미터 한 번에 설정
        /// </summary>
        public void SetParameters(float minWaterDepth = 0.05f,
                                 float minFluxMagnitude = 0.01f,
                                 float deepWaterDepth = 0.2f)
        {
            SetMinWaterDepth(minWaterDepth);
            SetMinFluxMagnitude(minFluxMagnitude);
            SetDeepWaterDepth(deepWaterDepth);
        }

        // ====================================================================
        // 버퍼 바인딩
        // ====================================================================

        /// <summary>
        /// 입력/출력 텍스처 바인딩
        /// </summary>
        /// <param name="waterBuffer">입력: 물 시뮬레이션 결과 (RGBA32F)</param>
        /// <param name="riverMask">출력: 강 마스크 (R32F)</param>
        public void BindBuffers(uint waterBuffer, uint riverMask)
        {
            // 쉐이더 내부의 binding = 0, 1과 일치해야 함
            Gl.BindImageTexture(0, waterBuffer, 0, false, 0,
                BufferAccess.ReadOnly, InternalFormat.Rgba32f);
            Gl.BindImageTexture(1, riverMask, 0, false, 0,
                BufferAccess.WriteOnly, InternalFormat.Rgba32f);
        }

        // ====================================================================
        // Compute Shader Dispatch
        // ====================================================================

        /// <summary>
        /// Compute Shader 실행
        /// </summary>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        public void Dispatch(int width, int height)
        {
            // 16x16 워크그룹 기준
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}