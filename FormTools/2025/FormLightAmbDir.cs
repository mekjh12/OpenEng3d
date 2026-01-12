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
    public partial class FormLightAmbDir : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;
        readonly string TITLE = "GPU드라이븐 LOD4 광원";

        // GL 컨트롤 변수들
        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private bool _isLoaded = false;                     // 로드 여부
        private bool _isStarted = false;                    // 시작 여부

        string[] _objFileNames = new string[]
            {
                @"oak_tree.obj",
                @"pine_tree.obj",
            };

        /*
            @"tree1.obj",               
            @"florida_foliage\bananaPlant1.obj",
            @"florida_foliage\bananaPlant2.obj",
            @"florida_foliage\bananaPlant3.obj",
            @"florida_foliage\palm1.obj",
            @"florida_foliage\palm2.obj",
            @"florida_foliage\palm3.obj",
            @"florida_foliage\bananaPlant1.obj",
            @"florida_foliage\bananaPlant2.obj",
            @"florida_foliage\bananaPlant3.obj",
            @"florida_foliage\fern1.obj",
            @"florida_foliage\fern2.obj",
            @"florida_foliage\fern3.obj",
            @"florida_foliage\fern4.obj",
            @"florida_foliage\fern5.obj",
         */


        // 렌더러 변수들
        private WorldAxisRenderer _worldAxisRenderer;       // 월드 축 렌더러
        private DeferredRenderer _deferredRenderer;         // 디퍼드 렌더러
        private GBuffer _gbuffer;                           // 렌더타겟 지버퍼

        // 셰이더 변수들
        private ColorShader _colorShader;                   // 컬러 셰이더
        private HzmDepthShader _hzmDepthShader;             // HZM 깊이 셰이더
        private TerrainTessellationShader _terrainShader;   // 지형 테셀레이션 셰이더
        private RenderDepthBufferShader _renderDepthShader; // 렌더 깊이 셰이더
        private FogShader _fogShader;                       // 안개 셰이더
        private DeferredShadingShader _deferredShadingShader;       // 디퍼드 셰이딩 셰이더

        // UI 2D 관련 변수들
        private TextNamePlate _textNamePlate;               // 텍스트 네임플레이트
        private Polyhedron _viewFrustum;                    // 뷰 프러스텀
        private Text2d _fpsText;                            // FPS 텍스트
        private Text2d _titleText;                          // 타이틀 텍스트
        private Text2d _descText;                           // 설명 텍스트
        private Text2d _camPosText;                         // 카메라 위치 텍스트   
        private Text2d _culledText;                         // 컬링된 노드 텍스트   

        // 3D 관련 변수들
        Model3dManager _model3DManager;                     // 3D 모델 매니저
        ModelBatchManager _modelBatchManager;               // 모델 배치 매니저
        HiZRenderPass _gpuDriven;                           // GPU 드리븐 렌더러
        HierarchyZBuffer _hiZBuffer;                        // 계층적 Z 버퍼
        const int DOWN_LEVEL = 1;                           // 다운샘플링 레벨
        const int MAX_INSTANCES = 100000;                   // 최대 인스턴스 수

        // 지형 관련 변수들
        TerrainRegion _terrainRegion;                       // 지형 영역
        Texture[] _levelTextureMap = null;                  // 지형 레벨 텍스쳐
        Texture _detailTextureMap = null;                   // 지형 디테일 텍스쳐
        TerrainRenderer _terrainRenderer;                   // 지형 렌더러
        Texture _normalMapTexture;                          // 노말 맵 텍스쳐
        Texture _rockTexture;                               // 바위 텍스쳐

        // 라이팅 관련 변수들
        LightingManager _lightingManager;                   // 라이팅 매니저
        SunLight _sunLight;                                 // 태양광

        // 하늘과 구름 관련 변수들
        SkyRenderer _skyRenderer;                           // 하늘 렌더러
        SkyDomeTexture2dShader _skyDomeTexture2DShader;     // 스카이돔 텍스처 2D 셰이더

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

        // 안개 효과 관련 변수들
        private bool _isFogEnabled = false;
        private int _fogType = 1;  // 0: Linear, 1: Exp, 2: Exp2
        private float _fogDensity = 0.001f;
        private float _fogStart = 1000f;
        private float _fogEnd = 5000f;
        private Vertex3f _fogColor = new Vertex3f(0.7f, 0.8f, 0.9f);  // 연한 하늘색
        private const float MAX_DEPTH_DISTANCE = 10000.0f;  // 셰이더에서 사용한 정규화 값

        private AdvancedFogShader _advancedFogShader;
        private AdvancedFogParams _fogParams;

        // 디버깅 관련 변수들
        bool _isVisibleHiZDepthBuffer = false;              // 깊이 Z버퍼 가시화 여부
        bool _isVisibleRenderDepthBuffer = false;           // 렌더링 깊이 버퍼 가시화 여부
        bool _isDebugTextDirty = true;                      // 디버그 텍스트 갱신 여부
        bool _isVisibleGbuffer = false;                     // G-버퍼 가시화 여부
        bool _isVisibleCullingInfo = false;                 // 컬링 정보 가시화 여부
        bool _isVisibleWorldAxis = false;                   // 월드 축 가시화 여부
        bool _isFlyMode = false;                            // 플라이 모드 여부
        Vertex3f _cameraPivotPosition;                      // 디버깅용 카메라 피벗 위치 

        public FormLightAmbDir()
        {
            InitializeComponent();

            // GL 생성
            this.Text = TITLE;
            _glControl3 = new GlControl3(TITLE, Application.StartupPath, @"\fonts\fontList.txt", @"\Res\", useRenderTarget: true);
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

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 로그 프로파일 초기화
            LogProfile.Create(PROJECT_PATH + "\\log.txt");
        }

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new ColorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new HzmDepthShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new TerrainTessellationShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new RenderDepthBufferShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new FogShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AdvancedFogShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new DeferredShadingShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _terrainShader = ShaderManager.Instance.GetShader<TerrainTessellationShader>();
            _renderDepthShader = ShaderManager.Instance.GetShader<RenderDepthBufferShader>();
            _fogShader = ShaderManager.Instance.GetShader<FogShader>();
            _advancedFogShader = ShaderManager.Instance.GetShader<AdvancedFogShader>();
            _deferredShadingShader = ShaderManager.Instance.GetShader<DeferredShadingShader>();

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

            _descText = new Text2d(HELP_TEXT, 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.Color = Color.LightGray;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);

            _culledText = new Text2d("태양각", 10, (height * 0.2f), width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _culledText.Color = Color.White;
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 렌더러 초기화
            _worldAxisRenderer = new WorldAxisRenderer(PROJECT_PATH);

            // 계층적 Z 버퍼 초기화
            _hiZBuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);

            // 3D 모델 매니저 초기화 및 모델 로드
            _model3DManager = new Model3dManager(PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            _modelBatchManager = new ModelBatchManager();

            for (int i = 0; i < _objFileNames.Length; i++)
            {
                UnifiedTexturedModel model3 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\" + _objFileNames[i]);
                UnifiedTexturedModelLOD model3_lod1 = model3 as UnifiedTexturedModelLOD;
                _modelBatchManager.AddModel(model3.Name, 100, model3, model3_lod1.ModelLod1);
            }

            // 지형 영역 초기화
            RegionCoord regionCoord = new RegionCoord(0, 0);
            string heightMapFile = EXE_PATH + "\\Res\\Terrain\\region0x0.png";
            _terrainRegion = new TerrainRegion(regionCoord, chunkSize: 100, n: 10, null);
            _terrainRegion.LoadTerrainLowResMap(regionCoord, heightMapFile, completed: LoadTerrainRegionCompleted);

            // Normal Map 생성
            uint normalMapTexture = NormalMapGenerator.GenerateNormalMap(
                heightMapFile,
                heightScale: TerrainConstants.DEFAULT_VERTICAL_SCALE,
                wrapMode: true
            );
            _normalMapTexture = new Texture(normalMapTexture, _terrainRegion.Width, _terrainRegion.Height);

            // 지형 레벨 텍스쳐 로딩
            string heightMap = PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\";
            string[] levelTextureMap = new string[5]
            {
                "water1.png",
                "grass_1.png",
                "lowestTile.png",
                "HighTile.png",
                "highestTile.png"
            };
            _levelTextureMap = new Texture[levelTextureMap.Length];
            for (int i = 0; i < _levelTextureMap.Length; i++)
            {
                _levelTextureMap[i] = new Texture(EXE_PATH + @"\Res\Terrain\blend\" + levelTextureMap[i]);
            }

            string detailMap = EXE_PATH + @"\Res\Terrain\blend\detailMap.png";
            _detailTextureMap = new Texture(detailMap);
            _rockTexture = new Texture(PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\blend\castleTowerBaseTexture.bmp");

            // 지형 렌더러 초기화
            _terrainRenderer = new TerrainRenderer(_terrainShader, PROJECT_PATH);
            _terrainRenderer.SetGroundTextures(_levelTextureMap, _normalMapTexture, _detailTextureMap);
            //_terrainRenderer.SetRockTexture(_rockTexture);

            // 하늘 렌더러 초기화
            _sunLight = new SunLight(0, 15);
            _skyDomeTexture2DShader = new SkyDomeTexture2dShader(PROJECT_PATH);
            _skyDomeTexture2DShader.GenerateSkyTexture(_glControl3.Camera.Position, -_sunLight.Direction);
            _skyRenderer = new SkyRenderer(PROJECT_PATH, _skyDomeTexture2DShader);

            // 초기 라이팅 설정 (선택사항)
            _lightingManager = new LightingManager();
            _lightingManager.Lighting.SunDirection = _sunLight.Direction;
            _lightingManager.Lighting.SunIntensity = 1.5f;
            _lightingManager.SetDirty();

            // 안개 프리셋 사용
            _fogParams = AdvancedFogParams.CreateMountain();

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void InitFinished()
        {
            // GPU 드리븐 렌더러 초기화
            _gpuDriven = new HiZRenderPass(PROJECT_PATH);
            _gpuDriven.Initialize(_modelBatchManager, _glControl3.Camera, _hiZBuffer.Levels);

            // 디퍼드 렌더러 초기화
            _gbuffer = _glControl3.GBuffer;
            _deferredRenderer = new DeferredRenderer(_gbuffer, _deferredShadingShader);
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            // 1회 시작시 초기화
            if (!_isStarted)
            {
                InitFinished();
                _isStarted = true;
            }

            // 라이팅 매니저 UBO 업데이트
            _lightingManager.Update();

            // 카메라 위치가 변경되었는지 확인
            if (camera.IsCameraFrameMoved)
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

                // 1. 지형 깊이 렌더링
                _hiZBuffer.RenderTerrainDepth(
                    TerrainConstants.DEFAULT_VERTICAL_SCALE,
                    _terrainRegion.TerrainEntity
                );

                // 2. 이전 프레임 LOD0, LOD1 깊이 렌더링 (Temporal Z-PrePass)
                _gpuDriven?.RenderDepthPrePassFromPrevFrame(camera);

                _hiZBuffer.UnbindFramebuffer();

                // 3. HiZ 밉맵 생성
                _hiZBuffer.GenerateMipmapsUsingFragment(maxLevel: -1);

                _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
            }

            // GPU 드리븐 업데이트
            _gpuDriven?.Update(camera, _viewFrustum, _hiZBuffer);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";

            // 가시 객체 통계 정보 얻기
            _gpuDriven.GetVisibleCountDebug(ref _visibleCount,
                ref _visibleCountLod0,
                ref _visibleCountLod1,
                ref _visibleCountLod2,
                ref _visibleCountLod3,
                ref _frustumPassCount,
                ref _visibleReport, _isVisibleCullingInfo);

            // 최적화가 안되고 있음(TODO)
            if (_visibleCount != _lastVisibleCount || _frustumPassCount != _lastFrustumPassCount)
            {
                _isDebugTextDirty = true;
            }

            if (_isDebugTextDirty)
            {
                // 네임플레이트 업데이트            
                _lastVisibleCount = _visibleCount;
                _lastFrustumPassCount = _frustumPassCount;
                _culledText.Text = $"배치수{_modelBatchManager.ActualBatchCount}\n" +
                    $"가시객체 모델별 통계\n" +
                    $"----------------------\n" +
                    $"{_visibleReport}\n" +
                    $"----------------------\n" +
                    $"뷰프러스텀 {_frustumPassCount}개\n" +
                    $"{_hiZBuffer.Width}x{_hiZBuffer.Height} HZB Level: {_level}\n" +
                    $"안개유무: {_isFogEnabled}";

                _isDebugTextDirty = false;
            }
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;
            if (!_isStarted) return;

            // 배경 타겟의 컬러 버퍼 초기화
            Gl.ClearColor(1.0f, 0.0f, 0.0f, 0.0f);      // R32F는 색상 버퍼이므로 ClearColor 사용
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // 지형 렌더링
            _terrainRenderer.Render(camera, heightScale: TerrainConstants.DEFAULT_VERTICAL_SCALE);

            // GPU DRIVEN 렌더링
            _gpuDriven?.Render(camera);

            // 하늘 렌더링
            _skyRenderer.RenderSkyDome(camera);

            // 월드 축 렌더링
            if (_isVisibleWorldAxis) _worldAxisRenderer.Render(camera.VPMatrix);
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

                    // ✅ 선형 깊이 텍스처 사용 (MRT location 1)
                    // GlControl3가 LinearDepthTextureId를 제공한다고 가정
                    _renderDepthShader.LoadDepthTexture(
                        TextureUnit.Texture0,
                        _glControl3.DepthTextureId  // 또는 적절한 프로퍼티명
                    );

                    Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                }
                _renderDepthShader.Unbind();
            }
            else
            {
                // 지버퍼 출력
                if (_isVisibleGbuffer)
                {
                    _glControl3.BlitDebugView(_renderDepthShader);
                }
                else
                {
                    // [일반] 컬러 버퍼 출력
                    if (_isFogEnabled)
                    {
                        // === 안개 Post-Processing 패스 ===
                        Gl.Disable(EnableCap.DepthTest);
                        Gl.Disable(EnableCap.Blend);

                        _advancedFogShader.Bind();
                        {
                            // 텍스처만 별도 바인딩
                            _advancedFogShader.LoadColorTexture(TextureUnit.Texture0, _glControl3.ColorTextureId);
                            _advancedFogShader.LoadPositionTexture(TextureUnit.Texture1, _glControl3.PositionTextureId);
                            _advancedFogShader.LoadDepthTexture(TextureUnit.Texture2, _glControl3.DepthTextureId);

                            // ✅ 파라미터 로드(한번에)
                            _advancedFogShader.LoadAllFogParameters(_fogParams, camera.Position, fogMode: 2);

                            Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
                        }
                        _advancedFogShader.Unbind();

                        /*
                        _fogShader.Bind();
                        {
                            // 원본 색상 텍스처 (MRT location 0)
                            _fogShader.LoadColorTexture(TextureUnit.Texture0, _glControl3.ColorTextureId);

                            // 선형 깊이 텍스처 (MRT location 1)
                            _fogShader.LoadLinearDepthTexture(TextureUnit.Texture1, _glControl3.DepthTextureId);

                            // 안개 색상
                            _fogShader.LoadFogColor(_fogColor);

                            // 안개 타입
                            _fogShader.LoadFogType(_fogType);

                            // 최대 거리 (정규화에 사용한 값)
                            _fogShader.LoadMaxDistance(MAX_DEPTH_DISTANCE);

                            // 안개 파라미터
                            if (_fogType == 0)  // Linear
                            {
                                _fogShader.LoadFogRange(_fogStart, _fogEnd);
                            }
                            else  // Exponential or Exponential Squared
                            {
                                _fogShader.LoadFogDensity(_fogDensity);
                            }

                            // 풀스크린 삼각형 렌더링
                            Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
                        }
                        _fogShader.Unbind();
                        */

                        Gl.Enable(EnableCap.DepthTest);
                    }
                    else
                    {
                        _deferredRenderer.Render(w, h);
                    }
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
            _textNamePlate.Render();
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
            _hiZBuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);
        }

        public void Form_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(1280, 800);
            this.Location = new Point(500, 100);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Resize += new EventHandler(this.FormGPUDriven_Resize);
            MemoryProfiler.StartFrameMonitoring();
        }

        private void FormLightAmbDir_Load(object sender, EventArgs e)
        {

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

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                break;
                int x = i % gridSize;
                int y = i / gridSize;

                //float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                //float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                float posX = 1000f * (float)(rand.NextDouble() * 10.0f - 5.0f);
                float posY = 1000f * (float)(rand.NextDouble() * 10.0f - 5.0f);

                position.x = posX;
                position.y = posY;
                float posZ = _terrainRegion.TerrainData.GetTerrainHeight(ref position, Terrain.TerrainConstants.DEFAULT_VERTICAL_SCALE);

                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                float scale = 0.5f + (float)(rand.NextDouble() * 1.0f);

                Matrix4x4f transform = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);
                _modelBatchManager.AddInstance((uint)(x % _objFileNames.Length), transform);
                //_modelBatchManager.AddInstance((uint)Rand.NextInt(0, objFileNames.Length), transform);
            }

            Console.WriteLine($"Generated {MAX_INSTANCES} tree instances");
            _modelBatchManager.Finalized();

            _isLoaded = true;
        }

        // ----------------------------------------------------------------------------------------
        // 도움말 텍스트
        // ----------------------------------------------------------------------------------------
        readonly string HELP_TEXT =
            "D1: HiZ버퍼  " +
            "D2: 안개  " +
            "D3: G버퍼" +
            "D5: 랜덤위치  " +
            "D8: 깊이버퍼  " +
            "D9: LOD1  " +
            "D0: 원점  " +
            "\n" +
            "-키: HiZDown  " +
            "+키: HiZUp  " +
            "H: Fly모드  " +
            "C: 컬링정보  " +
            "/: 월드축  " +
            "화살표: 태양각" +
            "\n";

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D0)
            {
                Vertex3f pos = _glControl3.Camera.PivotPosition;
                float z = _terrainRegion.TerrainData.GetTerrainHeight(ref pos, Terrain.TerrainConstants.DEFAULT_VERTICAL_SCALE);
                pos.z = z;
                _glControl3.Camera.PivotPosition = pos;
            }
            else if (e.KeyCode == Keys.D1)
            {
                _isVisibleHiZDepthBuffer = !_isVisibleHiZDepthBuffer;
            }
            else if (e.KeyCode == Keys.D2)
            {
                _isFogEnabled = !_isFogEnabled;
            }
            else if (e.KeyCode == Keys.D3)
            {
                _isVisibleGbuffer = !_isVisibleGbuffer;
            }
            else if (e.KeyCode == Keys.D5)
            {
                Vertex3f pos = Rand.NextColor3f * 2000.0f - new Vertex3f(1000.0f, 1000.0f, 0.0f);
                float z = _terrainRegion.TerrainData.GetTerrainHeight(ref pos, Terrain.TerrainConstants.DEFAULT_VERTICAL_SCALE);
                pos.z = z;
                _glControl3.Camera.PivotPosition = pos;
            }
            else if (e.KeyCode == Keys.D8)
            {
                _isVisibleRenderDepthBuffer = !_isVisibleRenderDepthBuffer;
            }
            else if (e.KeyCode == Keys.D9)
            {
                _gpuDriven.IsDebugLOD1 = !_gpuDriven.IsDebugLOD1;
            }
            else if (e.KeyCode == Keys.OemMinus)
            {
                _level = Math.Min(_level + 1, _hiZBuffer.Levels - 1);
                _isDebugTextDirty = true;
            }
            else if (e.KeyCode == Keys.Oemplus)
            {
                _level = Math.Max(_level - 1, 0);
                _isDebugTextDirty = true;
            }
            else if (e.KeyCode == Keys.C)
            {
                _isVisibleCullingInfo = !_isVisibleCullingInfo;
            }
            else if (e.KeyCode == Keys.OemQuestion)
            {
                _isVisibleWorldAxis = !_isVisibleWorldAxis;
            }
            else if (e.KeyCode == Keys.H)
            {
                _isFlyMode = !_isFlyMode;
            }
        }
    }
}
