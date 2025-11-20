using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Terrain;
using Ui3d;
using ZetaExt;


namespace FormTools
{
    public partial class FormTerrain : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private string EXE_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\";

        private GlControl3 _glControl3;

        ImpostorLODSystem _impostorLODSystem;   // LOD 기반 임포스터 시스템        
        RegionManager _regionManager;           // 리전관리자
        HierarchicalZBuffer _hzbuffer;          // 계층깊이버퍼        

        ColorShader _colorShader;                               // 단순색상 쉐이더
        SimpleDepthShader _simpleDepthShader;                   // 단순깊이 쉐이더
        UnlitShader _unlitShader;                               // 비발광 객체 렌더링용 쉐이더
        ImpostorShader _impostorShader;                         // 임포스터 렌더링용 쉐이더
        VegetationBillboardShader _vegetationBillboardShader;   // 식생빌보드렌더링
        SimpleTerrainShader _simpleTerrainShader;               // 
        HzmDepthShader _hzmDepthShader;                         // 디버깅 쉐이더

        Model3dManager _model3DManager;
        
        bool _isVisibleDepth = true;
        bool _visibleImposter = true;
        bool _visibleRawModel = true;

        List<Entity> _impostorEntity = new List<Entity>();
        List<Entity> _unlitEntity = new List<Entity>();
        Texture _texture;

        public FormTerrain()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("terrainOpenWorld", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(1, 1, 1),
                IsVisibleUi2d = true,
            };

            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 로그 프로파일 초기화
            LogProfile.Create(EXE_PATH + "\\log.txt");
        }

        private void FormTerrain_Load(object sender, EventArgs e)
        {
            MemoryProfiler.StartFrameMonitoring();
        }

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new HzmDepthShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new ColorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new SimpleDepthShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new UnlitShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new ImpostorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new SimpleTerrainShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new VegetationBillboardShader(PROJECT_PATH));

            // 셰이더 참조 가져오기
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _simpleDepthShader = ShaderManager.Instance.GetShader<SimpleDepthShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();
            _impostorShader = ShaderManager.Instance.GetShader<ImpostorShader>();
            _simpleTerrainShader = ShaderManager.Instance.GetShader<SimpleTerrainShader>();
            _vegetationBillboardShader = ShaderManager.Instance.GetShader<VegetationBillboardShader>();


            // 
            _hzbuffer = new HierarchicalZBuffer(width >> 3, height >> 3, PROJECT_PATH);

            // ✅ 세이더를 위한 UBO 설정
            GlobalUniformBuffers.Initialize();

            GlobalUniformBuffers.UpdateHalfPlaneFogData(
                fogColor: new Vertex3f(1, 1, 0),
                fogDensity: 0.01f,
                fogPlane: new Vertex4f(0, 0, 1, -10),
                isFogEnabled: true);

            GlobalUniformBuffers.BindUBOsToShader(_simpleTerrainShader.ProgramID);

            // ✅ 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        private void Init2d(int w, int h)
        {
            _glControl3.AddValueBar("LOD", value: 0, minValue: 0, maxValue: _hzbuffer.Levels - 1, backColor: new Vertex3f(0.6f, 0.3f, 0)).StepValue = 1;
            _glControl3.AddLabel("resolution", $"resolution={w}x{h}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("regchunk", $"", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));

            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            _glControl3.AddLabel("terrainPassSucess", $"지형패치통과수=", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, 
                adjoint:_glControl3.Ctrl("fps"),foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("region", $"region=0개", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM,
                adjoint: _glControl3.Ctrl("terrainPassSucess"), foreColor: new Vertex3f(1, 0, 0), fontSize: 1.0f);
            _glControl3.AddLabel("chunk", $"chunk=0개", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM,
                adjoint: _glControl3.Ctrl("region"), foreColor: new Vertex3f(1, 0, 0), fontSize: 1.0f);

            // in my house with GPU.
            Debug.PrintLine($"{Screen.PrimaryScreen.DeviceName} {w}x{h}");
            //if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        private void Init3d(int w, int h)
        {
            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);

            // _atmosphere init
            //_atmosphere = new Atmosphere(PROJECT_PATH);

            // 아틀라스 초기화
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // terrain init
            string heightMap = PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EXE_PATH + @"\Res\Terrain\blend\water1.png";
            levelTextureMap[1] = EXE_PATH + @"\Res\Terrain\blend\grass_1.png";
            levelTextureMap[2] = EXE_PATH + @"\Res\Terrain\blend\lowestTile.png";
            levelTextureMap[3] = EXE_PATH + @"\Res\Terrain\blend\HighTile.png";
            levelTextureMap[4] = EXE_PATH + @"\Res\Terrain\blend\highestTile.png";
            string detailMap = EXE_PATH + @"\Res\Terrain\blend\detailMap.png";

            _model3DManager = new Model3dManager(PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            //_model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");
            //_model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm1.obj");

            _regionManager = new RegionManager(_model3DManager, PROJECT_PATH, heightMap);
            _regionManager.LoadTerrainTextures(levelTextureMap, detailMap);
            _regionManager.SetSunLight(new Lights.SunLight(90, 60));

            //_atmosphere = new Atmosphere(PROJECT_PATH);

            // 임포스터 LOD 시스템 초기화
            /*
            _impostorLODSystem = new ImpostorLODSystem(100.0f);
            _impostorLODSystem.CreateImpostorModel("tree1", 
                ImpostorSettings.CreateSettings(128, 16), 
                _unlitShader,
                _model3DManager.GetModels("tree1"));
            */

            // 셰이더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        /// <summary>
        /// 프레임 업데이트를 처리합니다.
        /// </summary>
        /// <param name="deltaTime">이전 프레임과의 시간 간격 (밀리초)</param>
        /// <param name="w">화면 너비</param>
        /// <param name="h">화면 높이</param>
        /// <param name="camera">카메라</param>
        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;

            // 카메라 위치를 지형 높이에 맞춤
            camera.PivotPosition = _regionManager.GetTerrainHeightVertex3f(camera.PivotPosition);
            camera.Update(0);

            // 카메라 뷰-프로젝트 행렬
            Matrix4x4f vp = camera.VPMatrix;

            // 시야 절두체 생성
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

            // 대기를 업데이트한다.
            //_atmosphere.Update(deltaTime);

            // 보이는 리전들을 가져온다.
            _regionManager.PreUpdate(camera, viewFrustum);

            _glControl3.CLabel("regchunk").Text = $"외곽단순지형={_regionManager.SimpleTerrainRegionCache.Count}개," +
                $"가시단순지형={_regionManager.OuterVisibleSimpleRegion.Count} VisibleRegions={_regionManager.VisibleRegions.Count}";

            // 계층적 Z-버퍼 업데이트
            _hzbuffer.BindFramebuffer();
            _hzbuffer.PrepareRenderSurface();

            // 지형과 큰 오클루더들의 깊이맵 생성
            foreach (TerrainRegion terrainRegion in _regionManager.VisibleRegions)
            {
                if (terrainRegion.TerrainEntity == null) continue;
                _hzbuffer.RenderSimpleTerrain(
                    camera.ProjectiveMatrix,
                    camera.ViewMatrix,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE,
                terrainRegion.TerrainEntity);
            }

            // 계층적 Z-버퍼의 밉맵 생성
            _hzbuffer.GenerateZBuffer();
            //_hzbuffer.GenerateMipmapsUsingCompute();

            // 지형 청크로부터 뷰프러스텀 컬링 업데이트
            RegionManager.DEBUG_STRING = "";
            _regionManager.Update(camera, viewFrustum, _hzbuffer, duration);

            // 임포스터 및 일반 객체 분류
            _unlitEntity.Clear();
            _impostorEntity.Clear();

            // LOD 기반 렌더링 대상 분류
            List<Entity> renderEntities = _regionManager.GetVisibleEntities();
            foreach (Entity entity in renderEntities)
            {
                //if (_hzbuffer.TestVisibility(vp, camera.ViewMatrix, entity.AABB))
                {
                    if (entity is LodEntity)
                    {
                        bool shouldUseImpostor = _impostorLODSystem.ShouldUseImpostor(entity as LodEntity, camera.Position, vp);
                        (shouldUseImpostor ? _impostorEntity : _unlitEntity).Add(entity);
                    }
                    else
                    {
                        _unlitEntity.Add(entity);
                    }
                }
            }

            // UI 정보 업데이트
            //_glControl3.CLabel("ocs").Text = $"Total={_ocs?.EntityTotalCount}, PassEntity={_ocs?.FrustumPassEntity.Count} trav=";
            _glControl3.CLabel("cam").Text = 
                $"CamPos={camera.Position}, " +
                $"PivotPosition={camera.PivotPosition}, " +
                $"CameraPitch={camera.CameraPitch}, " +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance}";

            if (_regionManager.CurrentTerrainRegion != null)
            {
                _glControl3.CLabel("region").Text =
                    $"리전좌표({_regionManager.CurrentTerrainRegion.RegionCoord}) " +
                    $"보이는리전수={_regionManager.VisibleRegions.Count}개 {_regionManager._DEBUG_GetVisibleRegionText()}";
            }

            _glControl3.CLabel("chunk").Text =
                $"청크수{_regionManager._DEBUG_GetVisibleChunkCount()}개 " +
                $"{_regionManager._DEBUG_GetVisibleChunkCountText()} ";

        }

        /// <summary>
        /// 씬을 렌더링합니다.
        /// </summary>
        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 백그라운드 컬러 설정
            float r = _glControl3.BackClearColor.x;
            float g = _glControl3.BackClearColor.y;
            float b = _glControl3.BackClearColor.z;

            // 현재 LOD 레벨 계산
            int level = (int)_glControl3.SimpHValueBar("LOD").Value;
            Vertex2i resolution = _hzbuffer.GetLevelResolution(level);

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 지형 렌더링 또는 깊이맵 디버그 표시
            if (!_isVisibleDepth)
            {
                // 지형 렌더링
                //_atmosphere.Render(camera);
                _regionManager.Render(camera, _simpleTerrainShader);
            }
            else
            {
                // 깊이맵 디버그 표시 
                _hzbuffer.RenderDepthBuffer(_hzmDepthShader, camera, level);
            }

            // 객체 렌더링
            if (_visibleRawModel)
            {
                Renderer3d.Render(_unlitShader, _unlitEntity, camera);
            }

            if (_visibleImposter)
            {
                Renderer3d.Render(_impostorLODSystem, _impostorShader, _impostorEntity, camera);
            }

            // UI 정보 업데이트
            _glControl3.CLabel("terrainPassSucess").Text = $"" +
                //$"컬링통과 지형수={_terrain.FrustumedChunk.Count}," +
                //$"보유수={_terrain.EntityCount}," +
                $"임포스터수={_impostorEntity.Count}," +
                $"원형물체수={_unlitEntity.Count},";
            _glControl3.CLabel("resolution").Text = $"resolution={resolution.x}x{resolution.y}";

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
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


        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
                _isVisibleDepth = !_isVisibleDepth;
            }
            else if (e.KeyCode == Keys.D2)
            {
                _visibleImposter = !_visibleImposter;
            }
            else if (e.KeyCode == Keys.D3)
            {
                _visibleRawModel = !_visibleRawModel;
            }
            else if (e.KeyCode == Keys.T)
            {
                _regionManager._DEBUG_STRING = "0";
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D4)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(-58, 70, 0);
            }
            else if (e.KeyCode == Keys.D5)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(1006, 1023, 0);
            }
            else if (e.KeyCode == Keys.D6)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(340, 1006, 0);
            }
            else if (e.KeyCode == Keys.D7)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, -1006, 0);
            }
            else if (e.KeyCode == Keys.D8)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(-1006, 0, 0);
            }
            else if (e.KeyCode == Keys.J)
            {
                _regionManager.SunLight.SetDeltaAzimuth(-3.0f);
            }
            else if (e.KeyCode == Keys.L)
            {
                _regionManager.SunLight.SetDeltaAzimuth(3.0f);
            }
            else if (e.KeyCode == Keys.I)
            {
                _regionManager.SunLight.SetDeltaElevation(3.0f);
            }
            else if (e.KeyCode == Keys.K)
            {
                _regionManager.SunLight.SetDeltaElevation(-3.0f);
            }

        }
    }
}
