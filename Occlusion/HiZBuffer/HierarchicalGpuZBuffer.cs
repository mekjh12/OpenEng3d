using OpenGL;
using System;

namespace Occlusion
{
    /// <summary>
    /// AMD RX 580 GPU 완전 호환 버전
    /// 핵심 해결: 이미지 바인딩과 텍스처 접근 사이의 동기화
    /// </summary>
    public class HierarchicalGpuZBuffer : HierarchicalAbstracZBuffer
    {
        protected HzbComputeShader _computeShader;

        public HierarchicalGpuZBuffer(int width, int height, string projectPath)
            : base(width, height, projectPath)
        {
            if (_computeShader == null)
                _computeShader = new HzbComputeShader(projectPath);

            InitializeAllTextures();
        }

        /// <summary>
        /// 모든 HZB 텍스처를 0으로 초기화
        /// </summary>
        private void InitializeAllTextures()
        {
            for (int level = 0; level < _levels; level++)
            {
                int w = _width >> level;
                int h = _height >> level;

                float[] zeroData = new float[w * h];
                Array.Clear(zeroData, 0, zeroData.Length);

                Gl.BindTexture(TextureTarget.Texture2d, _hzbTextures[level]);
                Gl.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, w, h,
                    PixelFormat.Red, PixelType.Float, zeroData);
            }

            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        protected void TransferDepthBufferToLevel0()
        {
            _mipmapShader.Bind();
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d, _hzbTextures[0], 0);
            Gl.Viewport(0, 0, _width, _height);
            _mipmapShader.LoadDepthBuffer(TextureUnit.Texture0, _depthTexture);
            _mipmapShader.LoadLastMipSize(new Vertex2i(_width, _height));
            Gl.Disable(EnableCap.DepthTest);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.Enable(EnableCap.DepthTest);
            _mipmapShader.Unbind();

            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d, _colorTexture, 0);
        }

        public void GenerateMipmapsUsingFragment(int maxLevel = -1)
        {
            if (maxLevel < 0)
                maxLevel = _levels - 1;

            BindFramebuffer();
            GenerateHierachyZBufferOnGPU(maxLevel);
            UnbindFramebuffer();

            // CPU 전송
            if (_zbuffer != null)
            {
                TransferDepthDataToCPU(maxLevel);
            }
        }

        [Obsolete("이 메서드는 AMD GPU에서 호환성 문제가 발생할 수 있습니다. GenerateMipmapsUsingFragment 메서드를 사용하세요.")]
        public void GenerateMipmapsUsingCompute(int maxLevel = -1)
        {
            if (_computeShader == null)
                throw new InvalidOperationException("컴퓨트 셰이더가 초기화되지 않았습니다.");

            if (maxLevel < 0)
                maxLevel = _levels - 1;

            // ========================================
            // 1단계: Fragment Shader로 레벨 0 생성
            // ========================================
            BindFramebuffer();
            TransferDepthBufferToLevel0();
            UnbindFramebuffer();

            // ✅ AMD GPU: Fragment → Compute 전환 전 완전 동기화
            Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
            Gl.Finish();

            // ========================================
            // 2단계: Compute Shader로 레벨 1+ 생성
            // ========================================
            if (maxLevel > 0)
            {
                _computeShader.Bind();

                for (int level = 1; level <= maxLevel; level++)
                {
                    int inputWidth = _width >> (level - 1);
                    int inputHeight = _height >> (level - 1);
                    int outputWidth = _width >> level;
                    int outputHeight = _height >> level;

                    // ✅ AMD GPU: 텍스처를 모두 언바인드 (중요!)
                    for (int unit = 0; unit < 8; unit++)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture0 + unit);
                        Gl.BindTexture(TextureTarget.Texture2d, 0);
                    }

                    // ✅ Uniform 설정
                    _computeShader.LoadInputSize(new Vertex2i(inputWidth, inputHeight));
                    _computeShader.LoadOutputSize(new Vertex2i(outputWidth, outputHeight));

                    // ✅ AMD GPU: 이미지 바인딩 전 메모리 배리어
                    Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit |
                                   MemoryBarrierMask.TextureFetchBarrierBit);

                    // ✅ 이미지 바인딩 (명시적 InternalFormat.R32f)
                    Gl.BindImageTexture(0, _hzbTextures[level - 1], 0, false, 0,
                        BufferAccess.ReadOnly, InternalFormat.R32f);
                    Gl.BindImageTexture(1, _hzbTextures[level], 0, false, 0,
                        BufferAccess.WriteOnly, InternalFormat.R32f);

                    // ✅ AMD GPU: 이미지 바인딩 후 추가 동기화
                    Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                    // Work Group 계산
                    const int WORK_GROUP_SIZE = 16;
                    int groupsX = (outputWidth + WORK_GROUP_SIZE - 1) / WORK_GROUP_SIZE;
                    int groupsY = (outputHeight + WORK_GROUP_SIZE - 1) / WORK_GROUP_SIZE;

                    // Compute Shader 실행
                    Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);

                    // ✅ AMD GPU: 각 디스패치 후 완전 동기화
                    Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
                    Gl.Finish();
                }

                _computeShader.Unbind();

                // ✅ 모든 이미지 바인딩 해제
                Gl.BindImageTexture(0, 0, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.R32f);
                Gl.BindImageTexture(1, 0, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.R32f);
            }

            // CPU 전송
            if (_zbuffer != null)
            {
                TransferDepthDataToCPU(maxLevel);
            }
        }

        /// <summary>
        /// HZB 레벨 검증 (간소화 버전)
        /// </summary>
        public void ValidateHZBLevels()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════╗");
            Console.WriteLine("║        HZB 전체 레벨 검증 시작           ║");
            Console.WriteLine("╚═══════════════════════════════════════════╝");

            bool hasErrors = false;

            for (int level = 0; level < _levels; level++)
            {
                int w = _width >> level;
                int h = _height >> level;
                float[] data = new float[w * h];

                Gl.BindTexture(TextureTarget.Texture2d, _hzbTextures[level]);
                Gl.GetTexImage(TextureTarget.Texture2d, 0, PixelFormat.Red, PixelType.Float, data);
                Gl.BindTexture(TextureTarget.Texture2d, 0);

                float minVal = float.MaxValue;
                float maxVal = float.MinValue;
                int zeroCount = 0;

                for (int i = 0; i < data.Length; i++)
                {
                    float val = data[i];
                    if (val == 0.0f) zeroCount++;
                    minVal = Math.Min(minVal, val);
                    maxVal = Math.Max(maxVal, val);
                }

                // ✅ 정상 범위: 0.0 ~ 1.0 (정규화된 깊이값)
                bool isValid = maxVal <= 1.0f || (level == 0 && maxVal <= 1.0f);

                string statusIcon = isValid ? "✅" : "❌";
                ConsoleColor color = isValid ? ConsoleColor.Green : ConsoleColor.Red;

                if (!isValid) hasErrors = true;

                Console.ForegroundColor = color;
                Console.WriteLine($"{statusIcon} 레벨 {level} ({w}x{h})");
                Console.ResetColor();
                Console.WriteLine($"   깊이 범위: {minVal:F6} ~ {maxVal:F6}");
                Console.WriteLine($"   통계: Total={data.Length}, Zero={zeroCount}");

                if (!isValid)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"   ⚠️ 비정상 깊이값 감지! (최댓값 > 1.0)");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\n╔═══════════════════════════════════════════╗");
            if (hasErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("║          ❌ 검증 실패 - 오류 발견        ║");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("║          ✅ 모든 레벨 검증 통과          ║");
            }
            Console.ResetColor();
            Console.WriteLine("╚═══════════════════════════════════════════╝\n");
        }
    }
}