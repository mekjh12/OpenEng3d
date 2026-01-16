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
                _gbuffer.AlbedoTextureId,
                _gbuffer.PositionTextureId,
                _gbuffer.NormalTextureId,
                _gbuffer.DepthTextureId
            );

            // Shadow Map 바인딩
            if (_terrainShadowMap != null)
            {
                _shader.LoadTerrainShadowMap(_terrainShadowMap.DepthTextureID);
                _shader.LoadLightMatrices(_terrainShadowMap.LightViewMatrix, _terrainShadowMap.LightProjMatrix);
            }

            if (_instanceShadowMap != null)
            {
                _shader.LoadInstancesShadowMap(_instanceShadowMap.DepthTextureID);
                _shader.LoadLightMatrices2(_instanceShadowMap.LightViewMatrix, _instanceShadowMap.LightProjMatrix);
            }

            // 풀스크린 쿼드 렌더링
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);

            _shader.Unbind();
        }
    }

}
