using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using Terrain;
using ZetaExt;

namespace GPUDriven
{
    /// <summary>
    /// 낮은 지형에 깔리는 연무 렌더러
    /// Billboard 기반 인스턴스 렌더링
    /// </summary>
    public class GroundFogRenderer
    {
        private GroundFogRenderPass _renderPass;
        private BillboardShader _shader;
        private ModelBatchManager _batchManager;
        private Model3dManager _model3DManager;

        // 설정 파라미터
        private bool _isInitialized = false;
        private uint _textureID = 0;

        // 생성 파라미터
        public float HeightThreshold { get; set; } = 50.0f;      // 이 높이 이하에만 생성
        public float SlopeThreshold { get; set; } = 15.0f;       // 이 경사 이하에만 생성
        public float Spacing { get; set; } = 25.0f;              // 연무 간격
        public int MaxPatches { get; set; } = 5000;              // 최대 패치 수
        public float HeightOffset { get; set; } = 0.3f;          // 지면에서 띄우는 높이

        // 렌더링 파라미터
        public Vertex3f FogColor { get; set; } = new Vertex3f(0.8f, 0.85f, 0.9f);
        public float FogDensity { get; set; } = 0.02f;
        public int AtlasIndex { get; set; } = 0;

        public bool IsEnabled { get; set; } = true;

        public GroundFogRenderer(BillboardShader shader, string projPath)
        {
            _shader = shader;
            _renderPass = new GroundFogRenderPass("낮은연무 렌더패스", projPath);
            _batchManager = new ModelBatchManager();
            _model3DManager = new Model3dManager(StrRes.PROJECT_PATH, "");

            string[] _objFileNames = new string[]
            {
                @"tree1.obj"
            };

            // 모델 배치 매니저에 모델 추가
            for (int i = 0; i < _objFileNames.Length; i++)
            {
                UnifiedTexturedModel model3 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\" + _objFileNames[i]);
                UnifiedTexturedModelLOD model3_lod1 = model3 as UnifiedTexturedModelLOD;
                _batchManager.AddModel(model3.Name, 100, model3, model3_lod1.ModelLod1);
            }

        }

        /// <summary>
        /// 텍스처 로드
        /// </summary>
        public void LoadTexture(string texturePath)
        {
            Console.WriteLine($"🔍 텍스처 로드 시도: {texturePath}");

            _textureID = new Texture(texturePath).TextureID;

            if (_textureID == 0)
            {
                Console.WriteLine($"❌ 연무 텍스처 로드 실패: {texturePath}");
            }
            else
            {
                Console.WriteLine($"✅ 연무 텍스처 로드 성공: ID={_textureID}");
                _renderPass.SetFogTexture(_textureID);
            }
        }

        public void BatchInstances(TerrainData terrainData)
        {
            // 인스턴스 변환 행렬 생성 및 추가
            int gridSize = 300;
            float spacing = 15f;
            float halfSpacing = spacing / 2f;
            float quaterSpacing = spacing / 4f;
            Random rand = new Random(533);

            for (int i = 0; i < 5_000; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;
                float posX = 1000f * (float)(rand.NextDouble() * 2.0f - 1.0f);
                float posY = 1000f * (float)(rand.NextDouble() * 2.0f - 1.0f);
                float posZ = terrainData.GetTerrainHeight(posX, posY);
                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                float scale = 5f + (float)(rand.NextDouble() * 10.0f);

                Matrix4x4f transform = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);
                _batchManager.AddInstance(0, transform);
            }

            _batchManager.Finalized();
        }

        public void Init(Camera camera)
        {
            _renderPass.Initialize(camera, _batchManager);
        }

        public void Update(Camera camera, Polyhedron viewFrustum, HierarchyZBuffer hizBuffer)
        {
            _renderPass.Update(camera, viewFrustum, hizBuffer);
        }

        public void Render(Camera camera)
        {
           _renderPass.Render(camera);
        }

        public void RenderDepthPrePassFromPrevFrame(Camera camera)
        {
            _renderPass.RenderDepthPrePassFromPrevFrame(camera);
        }

        public void RenderShadowMap(ShadowMap shadowMap, Camera camera, Vertex3f sunLightDirection, bool isClearBuffer = false)
        {
            _renderPass.RenderShadowMap(shadowMap, camera, sunLightDirection, isClearBuffer);
        }

    }
}