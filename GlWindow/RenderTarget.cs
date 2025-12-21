using OpenGL;
using System;

namespace GlWindow
{
    /// <summary>
    /// 오프스크린 렌더링을 위한 프레임버퍼 타겟
    /// - ColorAttachment0: RGBA8 (일반 컬러)
    /// - ColorAttachment1: R32F (커스텀 선형 깊이 - 안개용)
    /// - DepthAttachment: Renderbuffer (깊이 테스트 전용)
    /// </summary>
    public class RenderTarget
    {
        public uint FramebufferId { get; private set; }
        public uint ColorTextureId { get; private set; }
        public uint DepthTextureId { get; private set; }  // ✅ 안개용 선형 깊이

        private uint _depthRenderbuffer;  // 깊이 테스트 전용 (읽을 필요 없음)
        private int _width;
        private int _height;

        /// <summary>
        /// 렌더 타겟 초기화
        /// </summary>
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;

            // FBO 생성
            FramebufferId = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferId);

            // ============================================
            // ColorAttachment0: RGBA8 (일반 컬러)
            // ============================================
            ColorTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, ColorTextureId);
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
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d,
                ColorTextureId,
                0
            );

            // ============================================
            // ColorAttachment1: R32F (커스텀 선형 깊이 - 안개용)
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
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);  // ✅ 안개는 Linear 샘플링
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment1,  // ✅ ColorAttachment1
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
            // MRT 드로우 버퍼 설정
            // ============================================
            int[] drawBuffers = new int[]
            {
                Gl.COLOR_ATTACHMENT0,  // 컬러
                Gl.COLOR_ATTACHMENT1   // 커스텀 깊이 (안개용)
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

            Console.WriteLine($"✅ RenderTarget 생성 완료 (안개용): {width}x{height}");
            Console.WriteLine($"   - Color0: RGBA8 (일반 컬러)");
            Console.WriteLine($"   - Color1: R32F (선형 깊이 - 안개/포스트 프로세싱용)");
            Console.WriteLine($"   - Depth: Renderbuffer (깊이 테스트 전용)");
        }

        public void Bind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferId);
        }

        public void Unbind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        /// <summary>
        /// FBO의 컬러 버퍼를 화면으로 복사
        /// </summary>
        public void BlitToScreen(int screenWidth, int screenHeight)
        {
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferId);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // ColorAttachment0 (컬러)만 복사
            Gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            Gl.BlitFramebuffer(
                0, 0, _width, _height,
                0, 0, screenWidth, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear
            );

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        public void Dispose()
        {
            if (_depthRenderbuffer != 0) Gl.DeleteRenderbuffers(_depthRenderbuffer);
            if (DepthTextureId != 0) Gl.DeleteTextures(DepthTextureId);
            if (ColorTextureId != 0) Gl.DeleteTextures(ColorTextureId);
            if (FramebufferId != 0) Gl.DeleteFramebuffers(FramebufferId);
        }
    }
}