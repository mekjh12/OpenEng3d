using OpenGL;
using Shader;
using System;

namespace GlWindow
{
    /// <summary>
    /// Deferred Rendering을 위한 G-Buffer 렌더 타겟
    /// - ColorAttachment0: RGBA8 (알베도/컬러)
    /// - ColorAttachment1: RGBA16F (월드 위치 xyz + 여분)
    /// - ColorAttachment2: RGBA16F (법선 xyz + 여분)
    /// - ColorAttachment3: R32F (선형 깊이 - 안개용)
    /// - DepthAttachment: Renderbuffer (깊이 테스트 전용)
    /// </summary>
    public class GBuffer
    {
        public uint FramebufferId { get; private set; }

        public uint AlbedoTextureId{ get; private set; }      // Albedo/Color
        public uint PositionTextureId { get; private set; }   // World Position
        public uint NormalTextureId { get; private set; }     // World Normal
        public uint DepthTextureId { get; private set; }      // Linear Depth (안개용)
        public uint StructureTextureId { get; private set; }   // Attachment4 ← 새로 추가!

        private uint _depthRenderbuffer;  // 깊이 테스트 전용

        private int _width;
        private int _height;

        /// <summary>
        /// G-Buffer 초기화
        /// </summary>
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;

            // FBO 생성
            FramebufferId = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferId);

            // 1. Albedo Texture (RGB: albedo, A: metallic)
            AlbedoTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, AlbedoTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba8,
                width, height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );
            SetTextureParameters(TextureTarget.Texture2d);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d,
                AlbedoTextureId,
                0
            );

            // ============================================
            // ColorAttachment1: RGBA16F (월드 위치)
            // xyz: 위치, w: 여분 (거리 또는 기타)
            // ============================================
            PositionTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, PositionTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba16f,
                width, height,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                IntPtr.Zero
            );
            SetTextureParameters(TextureTarget.Texture2d);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2d,
                PositionTextureId,
                0
            );

            // ============================================
            // ColorAttachment2: RGBA16F (법선 벡터)
            // xyz: 법선, w: 여분 (러프니스, AO 등)
            // ============================================
            NormalTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, NormalTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba16f,
                width, height,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                IntPtr.Zero
            );
            SetTextureParameters(TextureTarget.Texture2d);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment2,
                TextureTarget.Texture2d,
                NormalTextureId,
                0
            );

            // ============================================
            // ColorAttachment3: R32F (선형 깊이 - 안개용)
            // ============================================
            DepthTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, DepthTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32f,
                width, height,
                0,
                PixelFormat.Red,
                PixelType.Float,
                IntPtr.Zero
            );
            SetTextureParameters(TextureTarget.Texture2d);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment3,
                TextureTarget.Texture2d,
                DepthTextureId,
                0
            );

            // ============================================
            // DepthAttachment: Renderbuffer (깊이 테스트 전용)
            // ============================================
            _depthRenderbuffer = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRenderbuffer);
            Gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.DepthComponent32f,
                width,
                height
            );
            Gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                _depthRenderbuffer
            );

            // ============================================
            // ColorAttachment4: RGBA16F (Structure Buffer)
            // R: dz/dx, G: dz/dy, B: h, A: z-h
            // ============================================
            StructureTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, StructureTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba16f,
                width, height,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                IntPtr.Zero
            );
            SetTextureParameters(TextureTarget.Texture2d);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment4,
                TextureTarget.Texture2d,
                StructureTextureId,
                0
            );

            // ============================================
            // MRT 드로우 버퍼 설정 (5개로 증가!)
            // ============================================
            int[] drawBuffers = new int[]
            {
            Gl.COLOR_ATTACHMENT0,  // 알베도
            Gl.COLOR_ATTACHMENT1,  // 위치
            Gl.COLOR_ATTACHMENT2,  // 법선
            Gl.COLOR_ATTACHMENT3,  // 선형 깊이
            Gl.COLOR_ATTACHMENT4   // Structure Buffer ← 추가!
            };
            Gl.DrawBuffers(drawBuffers);

            // FBO 완성도 체크
            var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception($"RenderTarget Framebuffer incomplete: {status}");
            }

            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            Console.WriteLine($"✅ G-Buffer RenderTarget 생성 완료: {width}x{height}");
            Console.WriteLine($"   - Attachment0: RGBA8 (알베도)");
            Console.WriteLine($"   - Attachment1: RGBA16F (월드 위치)");
            Console.WriteLine($"   - Attachment2: RGBA16F (법선)");
            Console.WriteLine($"   - Attachment3: R32F (선형 깊이)");
            Console.WriteLine($"   - Attachment4: RGBA16F (Structure Buffer)");
            Console.WriteLine($"   - Depth: Renderbuffer");

            // 메모리 사용량
            long memoryBytes = (long)width * height * (
                4 +      // RGBA8
                8 +      // RGBA16F (위치)
                8 +      // RGBA16F (법선)
                4 +      // R32F (깊이)
                8 +      // RGBA16F (Structure) ← 추가!
                4        // Depth renderbuffer
            );
            Console.WriteLine($"   - 예상 메모리: {memoryBytes / 1024.0 / 1024.0:F2} MB");
        }

        /// <summary>
        /// 텍스처 공통 파라미터 설정
        /// </summary>
        private void SetTextureParameters(TextureTarget target)
        {
            Gl.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Gl.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public void Bind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferId);

            // MRT 드로우 버퍼 재설정 (5개!)
            int[] drawBuffers = new int[]
            {
                Gl.COLOR_ATTACHMENT0,
                Gl.COLOR_ATTACHMENT1,
                Gl.COLOR_ATTACHMENT2,
                Gl.COLOR_ATTACHMENT3,
                Gl.COLOR_ATTACHMENT4   // ← 추가!
            };
            Gl.DrawBuffers(drawBuffers);
        }

        public void Unbind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        /// <summary>
        /// FBO의 특정 컬러 버퍼를 화면으로 복사
        /// </summary>
        public void BlitToScreen(int screenWidth, int screenHeight, int attachmentIndex = 0)
        {
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferId);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // 읽을 attachment 선택
            ReadBufferMode readBuffer = (ReadBufferMode)(Gl.COLOR_ATTACHMENT0 + attachmentIndex);
            Gl.ReadBuffer(readBuffer);

            Gl.BlitFramebuffer(
                0, 0, _width, _height,
                0, 0, screenWidth, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
            );

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        /// <summary>
        /// G-Buffer 디버그 시각화 (4분할 화면)
        /// 우하단 깊이는 히트맵 색상으로 표현
        /// </summary>
        public void BlitDebugView(int screenWidth, int screenHeight, RenderDepthBufferShader depthShader)
        {
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferId);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            int halfW = screenWidth / 2;
            int halfH = screenHeight / 2;

            // ============================================
            // 처음 3개: 일반 blit
            // ============================================

            // 좌상: 알베도
            Gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            Gl.BlitFramebuffer(
                0, 0, _width, _height,
                0, halfH, halfW, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
            );

            // 우상: 위치
            Gl.ReadBuffer(ReadBufferMode.ColorAttachment1);
            Gl.BlitFramebuffer(
                0, 0, _width, _height,
                halfW, halfH, screenWidth, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
            );

            // 좌하: 법선
            Gl.ReadBuffer(ReadBufferMode.ColorAttachment2);
            Gl.BlitFramebuffer(
                0, 0, _width, _height,
                0, 0, halfW, halfH,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
            );

            // FBO 언바인드
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            // ============================================
            // 우하: 깊이 (히트맵 색상 셰이더 사용)
            // ============================================
            Gl.Viewport(halfW, 0, halfW, halfH);

            depthShader.Bind();
            depthShader.LoadIsPerspective(true);
            depthShader.LoadCameraNear(0.1f);    // 실제 카메라 값으로 교체
            depthShader.LoadCameraFar(10000.0f); // 실제 카메라 값으로 교체
            depthShader.LoadDepthTexture(TextureUnit.Texture0, DepthTextureId);

            Gl.DrawArrays(PrimitiveType.Points, 0, 1);

            depthShader.Unbind();

            // 뷰포트 복원
            Gl.Viewport(0, 0, screenWidth, screenHeight);
        }

        public void Dispose()
        {
            if (_depthRenderbuffer != 0) Gl.DeleteRenderbuffers(_depthRenderbuffer);
            if (DepthTextureId != 0) Gl.DeleteTextures(DepthTextureId);
            if (NormalTextureId != 0) Gl.DeleteTextures(NormalTextureId);
            if (PositionTextureId != 0) Gl.DeleteTextures(PositionTextureId);
            if (AlbedoTextureId != 0) Gl.DeleteTextures(AlbedoTextureId);
            if (FramebufferId != 0) Gl.DeleteFramebuffers(FramebufferId);
            if (StructureTextureId != 0) Gl.DeleteTextures(StructureTextureId);
        }
    }
}