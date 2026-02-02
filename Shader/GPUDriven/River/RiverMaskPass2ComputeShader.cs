using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// River Mask Generation - Pass 2: Morphological Smoothing
    /// 노이즈 제거 및 형태 다듬기
    /// Mode 0: Erosion (침식) - 작은 돌기 제거
    /// Mode 1: Dilation (팽창) - 작은 구멍 메우기
    /// </summary>
    public class RiverMaskPass2ComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\River\river_mask_pass2.comp";

        // ====================================================================
        // Uniform 위치 캐싱
        // ====================================================================
        private int loc_inputMask;
        private int loc_outputMask;
        private int loc_mode;
        private int loc_kernelRadius;

        // ====================================================================
        // 생성자
        // ====================================================================
        public RiverMaskPass2ComputeShader(string projectPath) : base()
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
            loc_inputMask = GetUniformLocation("inputMask");
            loc_outputMask = GetUniformLocation("outputMask");
            loc_mode = GetUniformLocation("mode");
            loc_kernelRadius = GetUniformLocation("kernelRadius");
        }

        // ====================================================================
        // Attribute 바인딩 (Compute Shader는 불필요)
        // ====================================================================
        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        // ====================================================================
        // 모드 설정
        // ====================================================================

        /// <summary>
        /// 처리 모드 설정
        /// </summary>
        /// <param name="mode">0: Erosion (침식), 1: Dilation (팽창)</param>
        public void SetMode(int mode)
        {
            Gl.Uniform1(loc_mode, mode);
        }

        /// <summary>
        /// Erosion 모드로 설정
        /// 작은 돌기 제거, 노이즈 감소
        /// </summary>
        public void SetModeErosion()
        {
            SetMode(0);
        }

        /// <summary>
        /// Dilation 모드로 설정
        /// 작은 구멍 메우기, 끊어진 부분 연결
        /// </summary>
        public void SetModeDilation()
        {
            SetMode(1);
        }

        // ====================================================================
        // 파라미터 설정
        // ====================================================================

        /// <summary>
        /// 커널 반경 설정
        /// 1 = 3x3 커널
        /// 2 = 5x5 커널
        /// 추천값: 1 (대부분의 경우 충분)
        /// </summary>
        public void SetKernelRadius(int radius)
        {
            Gl.Uniform1(loc_kernelRadius, radius);
        }

        // ====================================================================
        // 버퍼 바인딩
        // ====================================================================

        /// <summary>
        /// 입력/출력 텍스처 바인딩
        /// </summary>
        /// <param name="inputMask">입력: 이전 Pass 결과 (R32F)</param>
        /// <param name="outputMask">출력: 처리된 마스크 (R32F)</param>
        public void BindBuffers(uint inputMask, uint outputMask)
        {
            // 쉐이더 내부의 binding = 0, 1과 일치해야 함
            Gl.BindImageTexture(0, inputMask, 0, false, 0,
                BufferAccess.ReadOnly, InternalFormat.Rgba32f);
            Gl.BindImageTexture(1, outputMask, 0, false, 0,
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

        // ====================================================================
        // 헬퍼 메서드 - Opening/Closing 연산
        // ====================================================================

        /// <summary>
        /// Opening 연산 수행 (Erosion → Dilation)
        /// 작은 돌기 제거하면서 원래 형태 유지
        /// 노이즈 제거에 효과적
        /// </summary>
        /// <param name="input">입력 마스크</param>
        /// <param name="temp">임시 버퍼</param>
        /// <param name="output">출력 마스크</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="iterations">반복 횟수 (기본 1)</param>
        public void PerformOpening(uint input, uint temp, uint output,
                                   int width, int height, int iterations = 1)
        {
            Bind();
            SetKernelRadius(1);

            for (int i = 0; i < iterations; i++)
            {
                // Erosion
                SetModeErosion();
                BindBuffers(i == 0 ? input : temp, temp);
                Dispatch(width, height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }

            // Dilation
            SetModeDilation();
            BindBuffers(temp, output);
            Dispatch(width, height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }

        /// <summary>
        /// Closing 연산 수행 (Dilation → Erosion)
        /// 작은 구멍 메우면서 원래 형태 유지
        /// 끊어진 부분 연결에 효과적
        /// </summary>
        /// <param name="input">입력 마스크</param>
        /// <param name="temp">임시 버퍼</param>
        /// <param name="output">출력 마스크</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="iterations">반복 횟수 (기본 1)</param>
        public void PerformClosing(uint input, uint temp, uint output,
                                   int width, int height, int iterations = 1)
        {
            Bind();
            SetKernelRadius(1);

            // Dilation
            for (int i = 0; i < iterations; i++)
            {
                SetModeDilation();
                BindBuffers(i == 0 ? input : temp, temp);
                Dispatch(width, height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }

            // Erosion
            SetModeErosion();
            BindBuffers(temp, output);
            Dispatch(width, height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}