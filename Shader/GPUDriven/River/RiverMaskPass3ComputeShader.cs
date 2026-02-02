using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// River Mask Generation - Pass 3: Gaussian Blur
    /// 부드러운 경계선 생성 (Anti-aliasing)
    /// Mode 0: Horizontal Blur - 가로 방향 블러
    /// Mode 1: Vertical Blur - 세로 방향 블러
    /// </summary>
    public class RiverMaskPass3ComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\River\river_mask_pass3.comp";

        // ====================================================================
        // Uniform 위치 캐싱
        // ====================================================================
        private int loc_inputMask;
        private int loc_outputMask;
        private int loc_mode;
        private int loc_blurSigma;

        // ====================================================================
        // 생성자
        // ====================================================================
        public RiverMaskPass3ComputeShader(string projectPath) : base()
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
            loc_blurSigma = GetUniformLocation("blurSigma");
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
        /// <param name="mode">0: Horizontal Blur, 1: Vertical Blur</param>
        public void SetMode(int mode)
        {
            Gl.Uniform1(loc_mode, mode);
        }

        /// <summary>
        /// Horizontal Blur 모드로 설정
        /// </summary>
        public void SetModeHorizontal()
        {
            SetMode(0);
        }

        /// <summary>
        /// Vertical Blur 모드로 설정
        /// </summary>
        public void SetModeVertical()
        {
            SetMode(1);
        }

        // ====================================================================
        // 파라미터 설정
        // ====================================================================

        /// <summary>
        /// 블러 강도 설정 (가우시안 시그마)
        /// </summary>
        /// <param name="sigma">
        /// 0.5 = 약한 블러 (선명)
        /// 1.0 = 보통 블러 (권장)
        /// 1.5 = 강한 블러
        /// 2.0 = 매우 강한 블러
        /// </param>
        public void SetBlurSigma(float sigma)
        {
            Gl.Uniform1(loc_blurSigma, sigma);
        }

        // ====================================================================
        // 버퍼 바인딩
        // ====================================================================

        /// <summary>
        /// 입력/출력 텍스처 바인딩
        /// </summary>
        /// <param name="inputMask">입력 마스크 (R32F)</param>
        /// <param name="outputMask">출력 마스크 (R32F)</param>
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
        // 헬퍼 메서드 - Separable Gaussian Blur
        // ====================================================================

        /// <summary>
        /// Separable Gaussian Blur 수행
        /// Horizontal Blur → Vertical Blur
        /// </summary>
        /// <param name="input">입력 마스크</param>
        /// <param name="temp">임시 버퍼 (horizontal 결과 저장)</param>
        /// <param name="output">출력 마스크 (최종 결과)</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="sigma">블러 강도 (기본 1.0)</param>
        public void PerformGaussianBlur(uint input, uint temp, uint output,
                                        int width, int height, float sigma = 1.0f)
        {
            Bind();
            SetBlurSigma(sigma);

            // Step 1: Horizontal Blur
            SetModeHorizontal();
            BindBuffers(input, temp);
            Dispatch(width, height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // Step 2: Vertical Blur
            SetModeVertical();
            BindBuffers(temp, output);
            Dispatch(width, height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }

        /// <summary>
        /// 다중 패스 Gaussian Blur
        /// 여러 번 블러를 적용하여 더 부드러운 결과 생성
        /// </summary>
        /// <param name="input">입력 마스크</param>
        /// <param name="temp1">임시 버퍼 1</param>
        /// <param name="temp2">임시 버퍼 2</param>
        /// <param name="output">출력 마스크</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="passes">블러 반복 횟수 (1~3 권장)</param>
        /// <param name="sigma">블러 강도</param>
        public void PerformMultiPassBlur(uint input, uint temp1, uint temp2, uint output,
                                         int width, int height, int passes = 2, float sigma = 1.0f)
        {
            Bind();
            SetBlurSigma(sigma);

            uint currentInput = input;

            for (int i = 0; i < passes; i++)
            {
                // Horizontal
                SetModeHorizontal();
                BindBuffers(currentInput, temp1);
                Dispatch(width, height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                // Vertical
                SetModeVertical();

                // 마지막 패스는 output으로, 나머지는 temp2로
                uint verticalOutput = (i == passes - 1) ? output : temp2;
                BindBuffers(temp1, verticalOutput);
                Dispatch(width, height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                // 다음 반복을 위해 입력 갱신
                currentInput = temp2;
            }

            Unbind();
        }
    }
}