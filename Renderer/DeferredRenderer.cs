using GlWindow;
using OpenGL;
using Shader;

namespace Renderer
{
    public class DeferredRenderer
    {
        DeferredShadingShader _shader;
        GBuffer _gbuffer;

        public DeferredRenderer(GBuffer gbuffer, DeferredShadingShader shader)
        {
            _gbuffer = gbuffer;
            _shader = shader;
        }

        public void Render(int w, int h)
        {
            // 기본 프레임버퍼로 전환
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);

            // 깊이 테스트 비활성화 (풀스크린 쿼드)
            Gl.Disable(EnableCap.DepthTest);

            _shader.Bind();

            // G-Buffer 텍스처 바인딩
            _shader.LoadGBufferTextures(
                _gbuffer.ColorTextureId,
                _gbuffer.PositionTextureId,
                _gbuffer.NormalTextureId,
                _gbuffer.DepthTextureId
            );

            // 풀스크린 쿼드 렌더링
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);

            _shader.Unbind();

            // 깊이 테스트 복원
            Gl.Enable(EnableCap.DepthTest);
        }
    }

}
