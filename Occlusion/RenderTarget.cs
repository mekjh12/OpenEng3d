using OpenGL;
using System;

namespace Occlusion
{
    /// <summary>
    /// 메인 씬 렌더링을 위한 프레임버퍼
    /// </summary>
    public class RenderTarget
    {
        public uint FramebufferId { get; private set; }
        public uint ColorTextureId { get; private set; }
        public uint DepthTextureId { get; private set; }

        private int _width;
        private int _height;

        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;

            // FBO 생성
            FramebufferId = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferId);

            // ✅ 컬러 텍스처 (RGBA8)
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
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d,
                ColorTextureId,
                0
            );

            // ✅ 깊이 텍스처 (R32F - HiZ와 동일한 포맷)
            DepthTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, DepthTextureId);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32f,  // HiZ와 동일한 포맷
                width, height,
                0,
                PixelFormat.Red,
                PixelType.Float,
                IntPtr.Zero
            );
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2d,
                DepthTextureId,
                0
            );

            // FBO 완성도 체크
            var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception($"Main RenderTarget Framebuffer incomplete: {status}");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            Console.WriteLine($"메인 렌더 타겟 생성: {width}x{height}");
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
        /// FBO의 컬러 버퍼를 화면으로 복사 (일반 렌더링 모드)
        /// </summary>
        public void BlitToScreen(int screenWidth, int screenHeight)
        {
            // Read FBO 설정
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferId);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // 컬러 버퍼만 복사
            Gl.BlitFramebuffer(
                0, 0, _width, _height,      // 소스 영역
                0, 0, screenWidth, screenHeight,  // 대상 영역
                ClearBufferMask.ColorBufferBit,   // 컬러만 복사
                BlitFramebufferFilter.Linear       // 선형 필터링
            );

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        public void Dispose()
        {
            if (DepthTextureId != 0) Gl.DeleteTextures(DepthTextureId);
            if (ColorTextureId != 0) Gl.DeleteTextures(ColorTextureId);
            if (FramebufferId != 0) Gl.DeleteFramebuffers(FramebufferId);
        }
    }
}