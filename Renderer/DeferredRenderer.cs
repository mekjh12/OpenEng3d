using GlWindow;
using OpenGL;
using Shader;

namespace Renderer
{
    public class DeferredRenderer
    {
        DeferredShadingShader _shader;
        GBuffer _gbuffer;

        // Shadow Map 정보
        private uint _shadowMapTexture;
        private Matrix4x4f _lightViewMatrix;
        private Matrix4x4f _lightProjMatrix;

        public DeferredRenderer(GBuffer gbuffer, DeferredShadingShader shader)
        {
            _gbuffer = gbuffer;
            _shader = shader;
        }

        public void SetShadowMap(uint shadowMapTexture, Matrix4x4f lightView, Matrix4x4f lightProj)
        {
            _shadowMapTexture = shadowMapTexture;
            _lightViewMatrix = lightView;
            _lightProjMatrix = lightProj;
        }

        public void Render(int width, int height)
        {
            _shader.Bind();

            // G-Buffer 텍스처 바인딩
            _shader.LoadGBufferTextures(
                _gbuffer.ColorTextureId,
                _gbuffer.PositionTextureId,
                _gbuffer.NormalTextureId,
                _gbuffer.DepthTextureId
            );

            // Shadow Map 바인딩
            _shader.LoadShadowMap(_shadowMapTexture);
            _shader.LoadLightMatrices(_lightViewMatrix, _lightProjMatrix);

            // 풀스크린 쿼드 렌더링
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);

            _shader.Unbind();
        }
    }

}
