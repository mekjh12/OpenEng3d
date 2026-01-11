using OpenGL;
using System;

namespace Renderer
{
    /// <summary>
    /// Shadow Map을 위한 깊이 텍스처와 프레임버퍼를 관리합니다.
    /// </summary>
    public class ShadowMap : IDisposable
    {
        public uint FramebufferID { get; private set; }
        public uint DepthTextureID { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ShadowMap(int width = 4096, int height = 4096)
        {
            Width = width;
            Height = height;
            CreateShadowMap();
        }

        private void CreateShadowMap()
        {
            // 현재 상태 저장
            int[] currentFBO = new int[1];
            Gl.GetInteger(GetPName.DrawFramebufferBinding,out currentFBO[0]);

            // 프레임버퍼 생성
            FramebufferID = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);

            // 깊이 텍스처 생성
            DepthTextureID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, DepthTextureID);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.DepthComponent32f,
                Width,
                Height,
                0,
                PixelFormat.DepthComponent,
                PixelType.Float,
                IntPtr.Zero
            );

            // 텍스처 파라미터 설정 (PCF를 위한 LINEAR 필터링)
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_BORDER);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_BORDER);

            // 경계 밖은 그림자 없음 (깊이 1.0)
            float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, borderColor);

            // Shadow Map 비교 모드 활성화 (하드웨어 PCF)
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureCompareMode, 0x884E);  // GL_COMPARE_REF_TO_TEXTURE
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureCompareFunc, Gl.LEQUAL);

            // 프레임버퍼에 깊이 텍스처 연결
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2d,
                DepthTextureID,
                0
            );

            // ⭐ Shadow FBO에만 적용되도록 DrawBuffer 설정
            Gl.DrawBuffer(DrawBufferMode.None);

            // 프레임버퍼 완성도 체크
            FramebufferStatus status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception($"Shadow Map FBO 생성 실패: {status}");
            }

            // ⭐ 원래 FBO로 복원
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)currentFBO[0]);

            // ⭐ 텍스처 언바인드
            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        public void Bind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
            Gl.Viewport(0, 0, Width, Height);
            Gl.Clear(ClearBufferMask.DepthBufferBit);
            Gl.DrawBuffer(DrawBufferMode.None);
        }


        public void Unbind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            if (DepthTextureID != 0)
            {
                Gl.DeleteTextures(DepthTextureID);
                DepthTextureID = 0;
            }

            if (FramebufferID != 0)
            {
                Gl.DeleteFramebuffers(FramebufferID);
                FramebufferID = 0;
            }
        }
    }
}