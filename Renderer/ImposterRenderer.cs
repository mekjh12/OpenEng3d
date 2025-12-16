using Model3d;
using OpenGL;
using Shader;
using System;

namespace Renderer
{
    public class ImposterRenderer
    {
        ImpostorShader _shader;
        ImpostorRenderData _renderData;

        public ImposterRenderer(ImpostorShader shader, ImpostorRenderData renderData)
        {
            _shader = shader;
            _renderData = renderData;
        }

        // 명시적 수정 메서드들
        public void SetEdgeLineEnabled(bool enabled)
        {
            _renderData.enableEdgeLine = enabled;
        }

        public void UpdateAtlasTexture(uint textureId)
        {
            _renderData.atlasTextureId = textureId;
        }

        public void UpdateRenderData(ImpostorRenderData data)
        {
            _renderData = data;
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public void Render(Vertex2f atlasOffset, Matrix4x4f vp, Vertex3f cameraPosition)
        {
            _shader.Bind();

            // 매 프레임 변경 데이터
            _shader.LoadVPMatrix(vp);
            _shader.LoadCameraPosition(cameraPosition);
            _shader.LoadAtlasOffset(atlasOffset);

            // 불변 데이터 (내부 캐시)
            _shader.LoadImpostorAtlas(TextureUnit.Texture0, _renderData.atlasTextureId);
            _shader.LoadAABBSphereRadius(_renderData.modelRadius);
            _shader.LoadAABBCenterPosition(_renderData.modelCenter);
            _shader.LoadModelMatrix(_renderData.modelMatrix);
            _shader.LoadAtlasSize(_renderData.atlasSize);
            _shader.LoadIndividualSize(_renderData.individualSize);
            _shader.LoadHorizontalFrames(_renderData.horizontalFrames);
            _shader.LoadVerticalFrames(_renderData.verticalFrames);
            _shader.LoadEnableEdgeLine(_renderData.enableEdgeLine, 3.0f);
            Gl.BindVertexArray(Renderer3d.Point.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            _shader.Unbind();
        }
    }

    /// <summary>
    /// 임포스터 렌더링에 필요한 불변 데이터
    /// Billboard에서 재사용 가능
    /// </summary>
    public class ImpostorRenderData
    {
        public uint atlasTextureId;
        public float modelRadius;
        public Vertex3f modelCenter;
        public Matrix4x4f modelMatrix;
        public int atlasSize;
        public int individualSize;
        public int horizontalFrames;
        public int verticalFrames;
        public bool enableEdgeLine;

        // 계산 프로퍼티 (매번 계산하지 않도록)
        public Vertex2f FrameSize => new Vertex2f(
            (float)individualSize / atlasSize,
            (float)individualSize / atlasSize
        );

        public ImpostorRenderData()
        {

        }
    }
}
