using Common;
using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using GPUDriven;
using Light;
using Lights;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Drawing;
using System.Windows.Forms;
using Terrain;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormTerrainRegion : Form, GlControlerable
    {
        readonly string EXE_PATH = Application.StartupPath;
        readonly string TITLE = "GPU Driven 지형 리전";

        string[] _objFileNames = new string[]
        {
            @"MedievalHouse01.obj",
            @"florida_foliage\bananaPlant1.obj",
            @"florida_foliage\palm1.obj",
            @"florida_foliage\fern1.obj",
        };

        /*            
            @"oak_tree.obj",
            @"pine_tree.obj",
            @"florida_foliage\palm1.obj",
            @"florida_foliage\palm2.obj",
            @"florida_foliage\bananaPlant1.obj",
            @"florida_foliage\bananaPlant2.obj",
            @"florida_foliage\bananaPlant3.obj",
            @"florida_foliage\fern1.obj",
            @"florida_foliage\fern2.obj",
            @"florida_foliage\fern3.obj",
            @"florida_foliage\fern4.obj",
            @"florida_foliage\fern5.obj",
        */

        // GL 컨트롤 변수들
        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private bool _isLoaded = false;                     // 로드 여부
        private bool _isStarted = false;                    // 시작 여부

        // 렌더러 변수들
        private WorldAxisRenderer _worldAxisRenderer;       // 월드 축 렌더러
        private DeferredRenderer _deferredRenderer;         // 디퍼드 렌더러
        private GBuffer _gbuffer;                           // 렌더타겟 지버퍼
        private ShadowMap _sunShadowMap;                    // 태양 그림자맵

        // 셰이더 변수들
        private ColorShader _colorShader;                   // 컬러 셰이더
        private HzmDepthShader _hzmDepthShader;             // HZM 깊이 셰이더
        private TerrainTessellationShader _terrainShader;   // 지형 테셀레이션 셰이더
        private RenderDepthBufferShader _renderDepthShader; // 렌더 깊이 셰이더
        private DeferredShadingShader _deferredShadingShader;       // 디퍼드 셰이딩 셰이더

        // UI 2D 관련 변수들
        private Polyhedron _viewFrustum;                    // 뷰 프러스텀
        private Text2d _fpsText;                            // FPS 텍스트
        private Text2d _titleText;                          // 타이틀 텍스트
        private BackgroundText2d _descText;                 // 설명 텍스트
        private Text2d _camPosText;                         // 카메라 위치 텍스트   
        private BackgroundText2d _culledText;               // 컬링된 노드 텍스트   

        // 3D 관련 변수들
        Model3dManager _model3DManager;                     // 3D 모델 매니저
        ModelBatchManager _modelBatchManager;               // 모델 배치 매니저
        HierarchyZBuffer _hiZBuffer;                        // 계층적 Z 버퍼
        GeometryRenderPass _gpuDriven;                      // GPU 드리븐 렌더러
        const int DOWN_LEVEL = 2;                           // 다운샘플링 레벨

        // 지형 관련 변수들
        TerrainRenderer _terrainRenderer;                   // 지형 렌더러
        TerrainRegion _terrainRegion;                       // 지형 영역

        // 라이팅 관련 변수들
        LightingManager _lightingManager;                   // 라이팅 매니저
        SunLight _sunLight;                                 // 태양광

        // 하늘과 구름 관련 변수들
        SkyRenderer _skyRenderer;                           // 하늘 렌더러
        SkyDomeTexture2dShader _skyDomeTexture2DShader;     // 스카이돔 텍스처 2D 셰이더

        // HiZ 렌더 패스 디버깅 변수들
        int _level = 0;                                     // 현재 Z 버퍼 레벨
        uint _visibleCount = 0;                             // 가시 객체 수
        uint _visibleCountLod0 = 0;                         // 가시 객체 수 LOD0
        uint _visibleCountLod1 = 0;                         // 가시 객체 수 LOD1
        uint _visibleCountLod2 = 0;                         // 가시 객체 수 LOD2
        uint _visibleCountLod3 = 0;                         // 가시 객체 수 LOD3
        uint _frustumPassCount = 0;                         // 프러스텀 패스 수
        uint _lastVisibleCount = 0;                         // 이전 가시 객체 수
        uint _lastFrustumPassCount = 0;                     // 이전 프러스텀 패스 수
        string _visibleReport = "";                         // 가시 객체 리포트

        // 디버깅 관련 변수들
        bool _isVisibleHiZDepthBuffer = false;              // 깊이 Z버퍼 가시화 여부
        bool _isVisibleRenderDepthBuffer = false;           // 렌더링 깊이 버퍼 가시화 여부
        bool _isDebugTextDirty = true;                      // 디버그 텍스트 갱신 여부
        bool _isVisibleGbuffer = false;                     // G-버퍼 가시화 여부
        bool _isVisibleWorldAxis = false;                   // 월드 축 가시화 여부
        bool _isFlyMode = false;                            // 플라이 모드 여부
        Vertex3f _cameraPivotPosition;                      // 디버깅용 카메라 피벗 위치 

        // 스트럭처버퍼 디버그 상태
        private StructureDebugShader _structureBufferShader;
        private bool _showStructureDebug = false;
        private int _debugMode = 0;  // 0~5
        private float _depthRange = 500.0f;

        private GroundFogRenderer _groundFogRenderer;
        private Shader.BillboardShader _billboardShader;  // ✅ BillboardShader로 변경


        public FormTerrainRegion()
        {
            InitializeComponent();

            // GL 생성
            this.Text = TITLE;
            _glControl3 = new GlControl3(TITLE, Application.StartupPath, StrRes.FONT_RESOURCES_FILENAME, @"\Res\", true);
            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.BlitToScreen = (deltaTime, camera) => BlitToScreen(deltaTime, camera);
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Load += (s, e) => Form_Load(s, e);
            _glControl3.PreviewKeyDown += PreviewKeyDownEvent;
            _glControl3.AutoBlitToScreen = false;
            _glControl3.Start();
            Controls.Add(_glControl3);
            this.FormClosed += FormCompleteClosed;

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = StrRes.PROJECT_PATH;

            // 로그 프로파일 초기화
            LogProfile.Create(StrRes.PROJECT_PATH + "\\log.txt");
        }

        private void FormCompleteClosed(object sender, EventArgs e)
        {
            IniFile.WritePrivateProfileString("sunlight", "Azimuth", _sunLight.Azimuth);
            IniFile.WritePrivateProfileString("sunlight", "Elevation", _sunLight.Elevation);
        }

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new ColorShader(StrRes.PROJECT_PATH));
            ShaderManager.Instance.AddShader(new HzmDepthShader(StrRes.PROJECT_PATH));
            ShaderManager.Instance.AddShader(new TerrainTessellationShader(StrRes.PROJECT_PATH));
            ShaderManager.Instance.AddShader(new RenderDepthBufferShader(StrRes.PROJECT_PATH));
            ShaderManager.Instance.AddShader(new DeferredShadingShader(StrRes.PROJECT_PATH));
            ShaderManager.Instance.AddShader(new StructureDebugShader(StrRes.PROJECT_PATH));

            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _terrainShader = ShaderManager.Instance.GetShader<TerrainTessellationShader>();
            _renderDepthShader = ShaderManager.Instance.GetShader<RenderDepthBufferShader>();
            _deferredShadingShader = ShaderManager.Instance.GetShader<DeferredShadingShader>();
            _structureBufferShader = new StructureDebugShader(StrRes.PROJECT_PATH);

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 30);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d(TITLE, 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 18);
            _titleText.Color = Color.Yellow;

            _descText = new BackgroundText2d(HELP_TEXT, 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.ShowBackground = true;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);

            _culledText = new BackgroundText2d("태양각", 10, (height * 0.1f), width, height,
                Text2d.TextAlignment.Left, heightInPixels: 24);
            _culledText.EnableFrameCounter(120);
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(StrRes.PROJECT_PATH);

            // 렌더러 초기화
            _worldAxisRenderer = new WorldAxisRenderer(StrRes.PROJECT_PATH);

            // 계층적 Z 버퍼 초기화
            _hiZBuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, StrRes.PROJECT_PATH);

            // 3D 모델 매니저 초기화 및 모델 로드
            _model3DManager = new Model3dManager(StrRes.PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            _modelBatchManager = new ModelBatchManager(Constants.MAX_INSTANCES, Constants.MAX_BATCHES);

            for (int i = 0; i < _objFileNames.Length; i++)
            {
                UnifiedTexturedModel model3 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\" + _objFileNames[i]);
                UnifiedTexturedModelLOD model3_lod1 = model3 as UnifiedTexturedModelLOD;
                _modelBatchManager.AddModel(model3.Name, 100, model3, model3_lod1.ModelLod1);
            }

            _modelBatchManager.ImposterFinalize();

            // 지형 영역 초기화
            RegionCoord regionCoord = new RegionCoord(0, 0);
            string heightMapFile = EXE_PATH + $"\\Res\\Terrain\\{regionCoord.FileName}.png";
            _terrainRegion = new TerrainRegion(regionCoord, chunkSize: 100, n: 10, null);
            _terrainRegion.LoadTerrainLowResMap(regionCoord, heightMapFile, completed: LoadTerrainRegionCompleted);

            // 지형 노말맵 생성
            uint normalMapTexture = NormalMapGenerator.GenerateNormalMap(
                heightMapFile,
                heightScale: Constants.TERRAIN_VERTICAL_SCALE,
                wrapMode: true
            );

            // 지형 렌더러 초기화
            _terrainRenderer = new TerrainRenderer(_terrainShader, StrRes.PROJECT_PATH);
            _terrainRenderer.LoadTerrainLevelTextures(EXE_PATH + @"\Res\Terrain\blend\", StrRes.TERRAIN_BIOM_TOLEDO_TEXTURES);
            _terrainRenderer.LoadDetailTexture(EXE_PATH + StrRes.TERRAIN_DETAILMAP_FILENAMES);
            _terrainRenderer.LoadTerrainNormalMap(normalMapTexture);
            _terrainRenderer.LoadRockTexture(StrRes.PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\blend\toledo\rock.png");
            _terrainRenderer.LoadRiverMapTexture(StrRes.PROJECT_PATH + $"FormTools\\bin\\Debug\\Res\\Terrain\\{regionCoord.FileName}_river.png");
            _terrainRenderer.LoadMossRockTexture(StrRes.PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\blend\mossy_rock.png");

            // 낮은 안개 렌더러 초기화
            _billboardShader = new Shader.BillboardShader(StrRes.PROJECT_PATH);
            _groundFogRenderer = new GroundFogRenderer(StrRes.PROJECT_PATH);
            _groundFogRenderer.HeightThreshold = 50.0f;
            _groundFogRenderer.SlopeThreshold = 15.0f;

            //_groundFogRenderer.CreateTestTexture();
            _groundFogRenderer.LoadTexture(EXE_PATH + @"\Res\Terrain\fog_noise.png");

            // 하늘 렌더러 초기화
            float azimuth = float.Parse(IniFile.GetPrivateProfileString("sunlight", "Azimuth", "0"));
            float elevation = float.Parse(IniFile.GetPrivateProfileString("sunlight", "elevation", "30"));
            _sunLight = new SunLight(azimuth, elevation);
            _skyDomeTexture2DShader = new SkyDomeTexture2dShader(StrRes.PROJECT_PATH);
            _skyDomeTexture2DShader.GenerateSkyTexture(_glControl3.Camera.Position, -_sunLight.Direction);
            _skyRenderer = new SkyRenderer(StrRes.PROJECT_PATH, _skyDomeTexture2DShader);

            // 초기 라이팅 설정 (선택사항)
            _lightingManager = new LightingManager();
            _lightingManager.Lighting.SunDirection = _sunLight.Direction;
            _lightingManager.Lighting.SunIntensity = 1.5f;
            _lightingManager.SetDirty();

            // 태양 그림자 맵 초기화
            _sunShadowMap = new ShadowMap(2048, 2048);

            // UI 3D 텍스트 네임플레이트 초기화
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();
            SimpleColorShader.Initialize();
        }

        public void Start()
        {
            // 셰리더 해시정보는 파일로 저장
            FileHashManager.Finalize();

            // GPU 드리븐 렌더러 초기화
            _gpuDriven = new GeometryRenderPass("초목용 렌더패스", StrRes.PROJECT_PATH);
            _gpuDriven.Initialize(_glControl3.Camera, _modelBatchManager, distance0: 50, distance1: 150, distance2: 150);

            // 디퍼드 렌더러 초기화
            _gbuffer = _glControl3.GBuffer;
            _deferredRenderer = new DeferredRenderer(_gbuffer, _deferredShadingShader);

            // 지형 단층맵 만들기
            _terrainRenderer.CreateFaultTexture();

            // 연무 패치 배치
            _groundFogRenderer.BatchInstances(_terrainRegion.TerrainData);

            // 연무 만들기
            _groundFogRenderer.Init(_glControl3.Camera);
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            // 1회 시작시 초기화
            if (!_isStarted)
            {
                Start();
                _isStarted = true;
            }

            // 라이팅 매니저 UBO 업데이트
            _lightingManager.Update();

            // 카메라 위치가 변경되었는지 확인
            if (camera.IsCameraFrameMoved) //해결해야 할 부분 
            {
                // 뷰 프러스텀 업데이트
                _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

                // 카메라 피벗 위치에 대한 지형 높이 얻기
                if (!_isFlyMode)
                {
                    _cameraPivotPosition = camera.PivotPosition;
                    _terrainRegion.TerrainData.GetTerrainHeightVertex3f(ref _cameraPivotPosition);
                    _cameraPivotPosition.z += 2.0f; // 약간 띄우기   
                    camera.PivotPosition = _cameraPivotPosition;
                }

                // === HiZ 버퍼 업데이트 ===
                _hiZBuffer.BindFramebuffer();
                _hiZBuffer.PrepareRenderSurface();

                // 지형 깊이 렌더링
                _hiZBuffer.RenderTerrainDepth(
                    Constants.TERRAIN_VERTICAL_SCALE,
                    _terrainRegion.TerrainEntity
                );

                // 2. 이전 프레임 LOD0, LOD1 깊이 렌더링 (Temporal Z-PrePass)
                _gpuDriven?.RenderDepthPrePassFromPrevFrame(camera);
                //_groundFogRenderer.RenderDepthPrePassFromPrevFrame(camera);

                _hiZBuffer.UnbindFramebuffer();

                // HiZ 밉맵 생성
                _hiZBuffer.GenerateMipmapsUsingFragment(maxLevel: -1);

                _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";

                // 디버그 텍스트 갱신
                //_culledText.Text = $"풀타일수 {_grassSystem.PoolCount} 활성 타일\n" + _grassSystem.ActiveTileNames;
            }

            // 지형 렌더링 업데이트
            _terrainRenderer.Update(duration);

            // GPU 드리븐 업데이트
            _gpuDriven?.Update(camera, _viewFrustum, _hiZBuffer);

            // 연무 렌더러 업데이트
            _groundFogRenderer.Update(camera, _viewFrustum, _hiZBuffer);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";

            // 최적화가 안되고 있음(TODO)
            if (_visibleCount != _lastVisibleCount || _frustumPassCount != _lastFrustumPassCount)
            {

            }

            // 테스트 후 지울 것
            _gpuDriven.GetVisibleCountDebug(ref _visibleCount,
                   ref _visibleCountLod0,
                   ref _visibleCountLod1,
                   ref _visibleCountLod2,
                   ref _visibleCountLod3,
                   ref _frustumPassCount, ref _visibleReport);

            _lastVisibleCount = _visibleCount;
            _lastFrustumPassCount = _frustumPassCount;
            _culledText.Text = _groundFogRenderer.GetDebugInfo();
            _isDebugTextDirty = true;

        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;
            if (!_isStarted) return;

            // ========================================
            // 1단계: Shadow Map 패스 (별도 FBO)
            // ========================================

            // 지형 세도우맵 갱신
            _terrainRenderer.RenderShadowMap(
                _sunLight.Direction,
                heightScale: Constants.TERRAIN_VERTICAL_SCALE, isClearBuffer: true
            );

            // 물체 세도우맵 갱신
            _gpuDriven.RenderShadowMap(_sunShadowMap, camera, _sunLight.Direction, lightViewWidth: camera.Distance * 0.8f, isClearBuffer: true);
            //_groundFogRenderer.RenderShadowMap(_sunShadowMap, camera, _sunLight.Direction, isClearBuffer: false);

            // ========================================
            // 2단계: G-Buffer 패스 (메인 지오메트리)
            // ========================================
            _gbuffer.Bind();  // ⭐ G-Buffer 바인딩

            // G-Buffer 클리어 (빨간색)
            Gl.ClearColor(1.0f, 0.0f, 0.0f, 0.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 뷰포트 설정
            Gl.Viewport(0, 0, _glControl3.Width, _glControl3.Height);

            // 하늘 렌더링
            _skyRenderer.RenderSkyDome(camera);

            // 지형 렌더링 (G-Buffer에 기록)
            Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);
            _terrainRenderer.Render(camera, heightScale: Constants.TERRAIN_VERTICAL_SCALE);

            // GPU DRIVEN 렌더링
            _gpuDriven?.Render(camera);

            // 연무 렌더링 (한 줄!)
            _groundFogRenderer.Render(camera);

            // 월드 축 렌더링
            if (_isVisibleWorldAxis) _worldAxisRenderer.Render(camera.VPMatrix);

            _gbuffer.Unbind();  // ⭐ G-Buffer 언바인드
        }

        private void BlitToScreen(int deltaTime, Camera camera)
        {
            if (!_isLoaded) return;
            if (!_isStarted) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 최종 화면 출력
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_isVisibleHiZDepthBuffer)
            {
                // [디버깅] HiZ 깊이 버퍼 시각화 (지형만)
                Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                _hiZBuffer.RenderDepthBuffer(_hzmDepthShader, camera, level: _level);
                Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);
            }
            else if (_isVisibleRenderDepthBuffer)
            {
                // [디버깅] 메인 렌더 깊이 버퍼 시각화 (지형 + 나무)
                _renderDepthShader.Bind();
                {
                    _renderDepthShader.LoadCameraNear(camera.NEAR);
                    _renderDepthShader.LoadCameraFar(camera.FAR);
                    _renderDepthShader.LoadIsPerspective(true);

                    // 선형 깊이 텍스처 사용 (MRT location 1)
                    // GlControl3가 LinearDepthTextureId를 제공한다고 가정
                    _renderDepthShader.LoadDepthTexture(
                        TextureUnit.Texture0,
                        _sunShadowMap.DepthTextureID //_terrainRenderer.ShadowMapTextureID //_glControl3.DepthTextureId  // 또는 적절한 프로퍼티명
                    );

                    Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                }
                _renderDepthShader.Unbind();
            }
            else if (_showStructureDebug)
            {
                RenderStructureDebugQuad();
            }
            else
            {
                if (_isVisibleGbuffer)
                {
                    _glControl3.BlitDebugView(_renderDepthShader);
                }
                else
                {
                    // Deferred Shading (불투명 객체들)
                    _deferredRenderer.SetTerrainShadowMap(_terrainRenderer.ShadowMap);
                    _deferredRenderer.SetInstanceShadowMap(_sunShadowMap);

                    _deferredRenderer.Render(w, h);

                }
            }

            // ========================================
            // 2D UI 렌더링
            // ========================================
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
            Gl.Viewport(0, 0, w, h);

            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            _fpsText.Render();
            _titleText.Render();
            _descText.Render();
            _camPosText.Render();
            _culledText.Render();

            Gl.Disable(EnableCap.Blend);
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void PreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    e.IsInputKey = true;
                    break;
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            float deltaTheta = 3;
            bool needUpdate = false;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    _sunLight.SetDeltaAzimuth(deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Right:
                    _sunLight.SetDeltaAzimuth(-deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Up:
                    _sunLight.SetDeltaElevation(deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Down:
                    _sunLight.SetDeltaElevation(-deltaTheta);
                    needUpdate = true;
                    break;
            }

            if (needUpdate)
            {
                _lightingManager.Lighting.SunDirection = _sunLight.Direction;
                _lightingManager.SetDirty();
                _skyDomeTexture2DShader.GenerateSkyTexture(_glControl3.Camera.Position, -_sunLight.Direction);
                _culledText.Text = $"태양각: 방위각{_sunLight.Azimuth}도, 고도각{_sunLight.Elevation}도";
            }
        }

        public void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
        }

        public void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        private void FormGPUDriven_Resize(object sender, EventArgs e)
        {
            int width = _glControl3.Width;
            int height = _glControl3.Height;
            _hiZBuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, StrRes.PROJECT_PATH);
        }

        public void Form_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(1280, 800);
            this.Location = new Point(600, 100);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Resize += new EventHandler(this.FormGPUDriven_Resize);
            MemoryProfiler.StartFrameMonitoring();
        }

        private void LoadTerrainRegionCompleted()
        {
            // 지형 로드 완료 후 처리할 작업들
            _terrainRenderer.SetTerrain(_terrainRegion.TerrainEntity);

            // 인스턴스 변환 행렬 생성 및 추가
            int gridSize = 300;
            float spacing = 15f;
            float halfSpacing = spacing / 2f;
            float quaterSpacing = spacing / 4f;
            Random rand = new Random(42);
            Vertex3f position = Vertex3f.Zero;
            int numInstance = 0;

            while (true)
            {
                //int x = i % gridSize;
                //int y = i / gridSize;

                //float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                //float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                float posX = 1000f * (float)(rand.NextDouble() * 2.0f - 1.0f);
                float posY = 1000f * (float)(rand.NextDouble() * 2.0f - 1.0f);

                position.x = posX;
                position.y = posY;

                float slope = _terrainRegion.TerrainData.GetTerrainSlope(posX, posY);

                float offsetX =_terrainRegion.TerrainData.Width * _terrainRegion.RegionCoord.X;
                float offsetY =_terrainRegion.TerrainData.Height * _terrainRegion.RegionCoord.Y;

                if (slope < 15)
                {
                    float posZ = _terrainRegion.TerrainData.GetTerrainHeight(ref position, Constants.TERRAIN_VERTICAL_SCALE);

                    float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                    float scale = 0.5f + (float)(rand.NextDouble() * 1.0f);

                    Matrix4x4f transform = Matrix4x4f.Translated(offsetX + posX, offsetY + posY, posZ) *
                                    Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                    Matrix4x4f.Scaled(scale, scale, scale);
                    _modelBatchManager.AddInstance((uint)(numInstance % _objFileNames.Length), transform);
                    //_modelBatchManager.AddInstance((uint)Rand.NextInt(0, objFileNames.Length), transform);
                    numInstance++;
                }

                if (numInstance >= Constants.MAX_INSTANCES) break;
            }

            Console.WriteLine($"Generated {Constants.MAX_INSTANCES} tree instances");
            _modelBatchManager.Finalized();

            _isLoaded = true;
        }

        // ----------------------------------------------------------------------------------------
        // 도움말 텍스트
        // ----------------------------------------------------------------------------------------
        readonly string HELP_TEXT =
            "D1: HiZ버퍼  " +
            "D2: 스트러처버퍼 " +
            "D3: G버퍼 " +
            "D4: 렌더깊이버퍼 " +
            "D5: 기능ON/OFF " +
            "D0: 랜덤위치  " +
            "O: 원점  " +
            "\n" +

            "F: FillMode " +
            "G: Grid " +
            "-키: HiZDown  " +
            "+키: HiZUp  " +
            "H: Fly모드  " +
            "/: 월드축  " +
            "화살표: 태양각" +
            "\n";

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.O)
            {
                Vertex3f pos = Vertex3f.Zero;
                pos.z = _terrainRegion.TerrainData.GetTerrainHeight(ref pos, Constants.TERRAIN_VERTICAL_SCALE);
                _glControl3.Camera.PivotPosition = pos;
            }
            else if (e.KeyCode == Keys.D1)
            {
                _isVisibleHiZDepthBuffer = !_isVisibleHiZDepthBuffer;
                _culledText.Text = $"HiZBuffer {_isVisibleHiZDepthBuffer}";
            }
            else if (e.KeyCode == Keys.D2)
            {
                _showStructureDebug = !_showStructureDebug;
                _culledText.Text = $"Structure Buffer {_showStructureDebug}";
            }
            else if (e.KeyCode == Keys.D3)
            {
                _isVisibleGbuffer = !_isVisibleGbuffer;
                _culledText.Text = $"G-Buffer {_isVisibleGbuffer}";
            }
            else if (e.KeyCode == Keys.D4)
            {
                _isVisibleRenderDepthBuffer = !_isVisibleRenderDepthBuffer;
                _culledText.Text = $"RenderDepthBuffer {_isVisibleRenderDepthBuffer}";
            }
            else if (e.KeyCode == Keys.D5)
            {
                _culledText.Text = "기능 " + _terrainRenderer.ToggleFunction();
            }
            else if (e.KeyCode == Keys.D9)
            {
                Vertex3f pos = new Vertex3f(0, 0, 0);
                float z = _terrainRegion.TerrainData.GetTerrainHeight(ref pos, Constants.TERRAIN_VERTICAL_SCALE);

                float px = ((float)_terrainRegion.RegionCoord.X + 0.5f) * _terrainRegion.TerrainData.Width;
                float py = ((float)_terrainRegion.RegionCoord.Y + 0.5f) * _terrainRegion.TerrainData.Height;
                pos.x = px;
                pos.y = py;
                pos.z = z;
                _glControl3.Camera.PivotPosition = pos;
            }
            else if (e.KeyCode == Keys.D0)
            {
                Vertex3f pos = Rand.NextColor3f * 2000.0f - new Vertex3f(1000.0f, 1000.0f, 0.0f);
                float z = _terrainRegion.TerrainData.GetTerrainHeight(ref pos, Constants.TERRAIN_VERTICAL_SCALE);
                pos.z = z;
                _glControl3.Camera.PivotPosition = pos;
            }
            else if (e.KeyCode == Keys.OemMinus)
            {
                _level = Math.Min(_level + 1, _hiZBuffer.Levels - 1);
                _isDebugTextDirty = true;
                _culledText.Text = $"HiZBuffer 레벨 {_level}";
                Console.WriteLine(_level);
            }
            else if (e.KeyCode == Keys.Oemplus)
            {
                _level = Math.Max(_level - 1, 0);
                _isDebugTextDirty = true;
                _culledText.Text = $"HiZBuffer 레벨 {_level}";
                Console.WriteLine(_level);
            }
            else if (e.KeyCode == Keys.OemQuestion)
            {
                _isVisibleWorldAxis = !_isVisibleWorldAxis;
            }
            else if (e.KeyCode == Keys.H)
            {
                _isFlyMode = !_isFlyMode;
                _culledText.Text = $"플라이모드 {_level}";
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Console.WriteLine("카메라 위치 = " + _glControl3.Camera.PivotPosition);
            }
        }

        private void RenderStructureDebugQuad()
        {
            int screenWidth = _glControl3.Width;
            int screenHeight = _glControl3.Height;

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Disable(EnableCap.DepthTest);

            _structureBufferShader.Bind();
            _structureBufferShader.LoadStructureBuffer(_gbuffer.StructureTextureId);
            _structureBufferShader.LoadDepthRange(_depthRange);

            int halfW = screenWidth / 2;
            int halfH = screenHeight / 2;

            // 좌상: Depth
            Gl.Viewport(0, halfH, halfW, halfH);
            _structureBufferShader.LoadDebugMode(GENG.STRUCTUREBUFFER_DEBUG_MODE.DEPTH);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // 우상: dz/dx
            Gl.Viewport(halfW, halfH, halfW, halfH);
            _structureBufferShader.LoadDebugMode(GENG.STRUCTUREBUFFER_DEBUG_MODE.DZDX);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // 좌하: dz/dy
            Gl.Viewport(0, 0, halfW, halfH);
            _structureBufferShader.LoadDebugMode(GENG.STRUCTUREBUFFER_DEBUG_MODE.DZDY);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // 우하: Gradient
            Gl.Viewport(halfW, 0, halfW, halfH);
            _structureBufferShader.LoadDebugMode(GENG.STRUCTUREBUFFER_DEBUG_MODE.GRADIENT);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            _structureBufferShader.Unbind();

            Gl.Viewport(0, 0, screenWidth, screenHeight);
            Gl.Enable(EnableCap.DepthTest);
        }

        private void FormStructureBuffer_Load(object sender, EventArgs e)
        {

        }

        private void FormTerrainRegion_Load(object sender, EventArgs e)
        {

        }
    }
}
