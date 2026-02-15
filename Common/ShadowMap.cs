using OpenGL;
using System;

namespace Common
{
    /// <summary>
    /// Shadow Map을 위한 깊이 텍스처와 프레임버퍼를 관리합니다.
    /// </summary>
    public class ShadowMap : IDisposable
    {
        private Matrix4x4f _lightProjMatrix;
        private Matrix4x4f _lightViewMatrix;

        public Matrix4x4f LightViewMatrix => _lightViewMatrix;
        public Matrix4x4f LightProjMatrix => _lightProjMatrix;

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

        /// <summary>
        /// 광원뷰 행렬을 계산한다.
        /// </summary>
        /// <param name="sunDirection"></param>
        /// <param name="terrainCenter"></param>
        /// <param name="terrainSize"></param>
        public void Update(Vertex3f sunDirection, Vertex3f terrainCenter, float terrainSize)
        {
            // lightSpaceMatrix 계산 및 저장
            CalculateLightSpaceMatrix(
                sunDirection,
                terrainCenter,
                terrainSize,
                ref _lightProjMatrix,
                ref _lightViewMatrix

            );
        }

        /// <summary>
        /// 태양 관점의 Light Space Matrix를 계산합니다.
        /// </summary>
        private void CalculateLightSpaceMatrix(Vertex3f sunDirection, Vertex3f terrainCenter, float terrainSize,
            ref Matrix4x4f lightProj, ref Matrix4x4f lightView)
        {
            // 태양 위치 (지형에서 충분히 멀리)
            Vertex3f lightPos = terrainCenter - sunDirection.Normalized * terrainSize * 2.0f;

            // Light View Matrix
            lightView = Matrix4x4f.LookAt(
                lightPos,
                terrainCenter,
                new Vertex3f(0, 0, 1)  // Up 벡터
            );

            // Orthographic Projection (태양은 평행광이지만 테스트)
            // ⭐ 평행 투영 (Orthographic Projection)
            // 태양은 평행광이므로 원근이 없는 평행투영 사용
            float orthoSize = terrainSize * 1.3f;  // 지형을 충분히 포함하는 크기

            lightProj = Matrix4x4f.Ortho(
                -orthoSize, orthoSize,  // left, right
                -orthoSize, orthoSize,  // bottom, top
                0.1f, terrainSize * 4.0f  // near, far
            );
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

            // ⭐ Hardware PCF 설정 (중복 제거)
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);

            // Wrap 모드
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
            Gl.DrawBuffer(DrawBufferMode.None);
        }

        public void Clear()
        {
            Gl.Clear(ClearBufferMask.DepthBufferBit);
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