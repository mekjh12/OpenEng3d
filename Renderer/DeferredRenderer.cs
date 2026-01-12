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
        private ShadowMap _terrainShadowMap;
        private ShadowMap _instanceShadowMap;

        public DeferredRenderer(GBuffer gbuffer, DeferredShadingShader shader)
        {
            _gbuffer = gbuffer;
            _shader = shader;
        }

        public void SetTerrainShadowMap(ShadowMap terrainShadowMap)
        {
            _terrainShadowMap = terrainShadowMap;
        }

        public void SetInstanceShadowMap(ShadowMap instanceShadowMap)
        {
            _instanceShadowMap = instanceShadowMap;
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
            _shader.LoadTerrainShadowMap(_terrainShadowMap.DepthTextureID);
            _shader.LoadInstancesShadowMap(_instanceShadowMap.DepthTextureID);
            _shader.LoadLightMatrices(_terrainShadowMap.LightViewMatrix, _terrainShadowMap.LightProjMatrix);
            _shader.LoadLightMatrices2(_instanceShadowMap.LightViewMatrix, _instanceShadowMap.LightProjMatrix);

            // 풀스크린 쿼드 렌더링
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);

            _shader.Unbind();
        }
    }

}
