using OpenGL;
using Shader;
using ZetaExt;

namespace Renderer
{
    /// <summary>
    /// 임포스터 렌더링에 필요한 데이터
    /// Billboard에서 재사용 가능
    /// </summary>
    public class ImpostorRenderData
    {
        public int atlasSize;
        public int individualSize;
        public int horizontalFrames;
        public int verticalFrames;
        public bool enableEdgeLine;

        public Matrix4x4f ModelMatrix;
        public Vertex3f CenterPosition;
        public float ModelSphereRadius;
        public uint AtlasTextureId;

        // 계산 프로퍼티 (매번 계산하지 않도록)
        public Vertex2f FrameSize => new Vertex2f(
            (float)individualSize / atlasSize,
            (float)individualSize / atlasSize
        );

        public static ImpostorRenderData CreateDefault()
        {
            return new ImpostorRenderData
            {
                atlasSize = 1024,
                individualSize = 128,
                horizontalFrames = 16,
                verticalFrames = 8,
                enableEdgeLine = false,
                ModelMatrix = Matrix4x4f.Identity,
                CenterPosition = Vertex3f.Zero,
                ModelSphereRadius = 1.0f,
                AtlasTextureId = 0
            };
        }
    }

    public class ImposterRenderer
    {
        ImpostorShader _shader;
        ImpostorRenderData _renderData;

        bool _isCenterAndRadiusInitialized = false;
        bool _isMatrixInitialized = false;

        public ImposterRenderer(ImpostorShader shader, ImpostorRenderData renderData)
        {
            Assert.Notify(renderData == null, "렌더러 생성 전에 ImpostorRenderData를 설정해야 합니다.");

            _shader = shader;
            _renderData = renderData;
        }

        public void UpdateCenterAndRadius(Vertex3f center, float radius)
        {
            _isCenterAndRadiusInitialized = true;
            _renderData.CenterPosition = center;
            _renderData.ModelSphereRadius = radius;
        }

        public void SetEdgeLineEnabled(bool enabled)
        {
            _renderData.enableEdgeLine = enabled;
        }

        public void UpdateModelMatrix(Matrix4x4f model)
        {
            _isMatrixInitialized = true;
            _renderData.ModelMatrix = model;
        }

        public void UpdateRenderData(ImpostorRenderData renderData)
        {
            _renderData = renderData;
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public void Render(Matrix4x4f vp, Vertex3f cameraPosition)
        {
            Assert.Notify(!_isCenterAndRadiusInitialized, "임포스터 렌더러의 중심과 반경이 초기화되지 않았습니다.");
            Assert.Notify(!_isMatrixInitialized, "임포스터 렌더러의 모델 매트릭스가 초기화되지 않았습니다.");

            _shader.Bind();

            // 매 프레임 변경 데이터
            _shader.LoadVPMatrix(vp);
            _shader.LoadCameraPosition(cameraPosition);

            // 불변 데이터 (내부 캐시)
            _shader.LoadImpostorAtlas(TextureUnit.Texture0, _renderData.AtlasTextureId);
            _shader.LoadAABBSphereRadius(_renderData.ModelSphereRadius);
            _shader.LoadAABBCenterPosition(_renderData.CenterPosition);
            _shader.LoadModelMatrix(_renderData.ModelMatrix);
            _shader.LoadAtlasSize(_renderData.atlasSize);
            _shader.LoadIndividualSize(_renderData.individualSize);
            _shader.LoadHorizontalFrames(_renderData.horizontalFrames);
            _shader.LoadVerticalFrames(_renderData.verticalFrames);
            _shader.LoadEnableEdgeLine(_renderData.enableEdgeLine, 3.0f);

            // 렌더링
            Gl.BindVertexArray(Renderer3d.Point.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            _shader.Unbind();
        }
    }    
}
