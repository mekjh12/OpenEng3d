using Common.Abstractions;
using FastMath;
using GlWindow;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using Sky;
using System;
using System.Windows.Forms;
using Terrain;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormHzm : Form
    {
        private readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private readonly string EXE_PATH = @"";

        private GlControl3 _glControl3;     // OpenGL 컨트롤
        HierarchicalZBuffer _hzbuffer;      // 계층깊이버퍼

        float _currentTime = 0.0f;              // 현재 시간
        bool _isDepthZBuffer = true;            // 깊이 Z-버퍼 표시 여부
        bool _isFogEnable = true;               // 안개 활성화 여부

        Texture[] _levelTextureMap = null;      // 지형 레벨 텍스쳐
        Texture _detailTextureMap = null;       // 지형 디테일 텍스쳐

        TerrainRegion _terrainRegion;
        SkyDomeTexture2dShader _skyDomeTexture2DShader;
        SkyRenderer _skyRenderer;
        SimpleSunPositionCalculator _simpleSunPositionCalculator;

        TextNamePlate _textNamePlate;

        // 쿼리 오브젝트를 위한 필드 선언
        //SamplesPassedPixelQuery _samplesPassedPixelQuery;
        //private uint _atmospherePixelQuery;
        //private bool _queryActive = false;
        //private uint _lastPixelCount = 0;
        //SkyRenderer _skyRenderer;

        public FormHzm()
        {
            InitializeComponent();

            // 프로젝트 경로 및 실행 파일 경로 설정
            EXE_PATH = Application.StartupPath;

            // GL 생성
            _glControl3 = new GlControl3("hzb", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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

        private void FormHzm_Load(object sender, EventArgs e)
        {
            // 메모리 프로파일러 시작
            MemoryProfiler.StartFrameMonitoring();
        }

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new TerrainTessellationShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new ColorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AtmosphericLUTComputeShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AtmosphericRenderShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new HzmDepthShader(PROJECT_PATH));

            // ✅ 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();

            // 스카이 렌더러 초기화
            if (_skyDomeTexture2DShader == null) _skyDomeTexture2DShader = new SkyDomeTexture2dShader(PROJECT_PATH, 1024);
            if (_skyRenderer == null) _skyRenderer = new SkyRenderer(PROJECT_PATH, _skyDomeTexture2DShader);

            // 태양 위치 계산기 초기화
            _simpleSunPositionCalculator = new SimpleSunPositionCalculator();
            _simpleSunPositionCalculator.SetParameters(0.75f, 0.0f, 0.25f, 0.5f);

            // 계층적깊이버퍼 생성
            _hzbuffer = new HierarchicalZBuffer(width >> 2, height >> 2, PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            // 화면 구성요소 초기화
            _glControl3.AddValueBar("LOD", value: 0, minValue: 0, maxValue: _hzbuffer.Levels - 1, backColor: new Vertex3f(0.6f, 0.3f, 0)).StepValue = 1;
            _glControl3.AddLabel("resolution", $"resolution={w >> 2}x{h >> 2}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("perf", $"pref", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("samplePassed", $"대기픽셀통과수=", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // 전체화면 모드 설정
            //if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        private void Init3d(int w, int h)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FTP");
            _textNamePlate.Height = 0.6f;
            _textNamePlate.Width = 0.6f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 지형 초기화
            _terrainRegion = new TerrainRegion(new RegionCoord(0, 0), chunkSize: 100, n : 10, null);
            _terrainRegion.LoadTerrainLowResMap(
                new RegionCoord(0, 0), EXE_PATH + "\\Res\\Terrain\\low\\region0x0.png");

            // UBO 초기화 및 로딩
            GlobalUniformBuffers.Initialize();

            // 안개 관련 유니폼 설정
            GlobalUniformBuffers.UpdateHalfPlaneFogData(
                fogColor: new Vertex3f(0.9f, 0.9f, 0.9f),
                fogDensity: 0.02f,
                fogPlane: new Vertex4f(0, 0, 1, -100),
                isFogEnabled: true);

            GlobalUniformBuffers.UpdateDistanceFogData(
                distFogCenter: new Vertex3f(0, 0, 0),
                distFogMinRadius: 1700,
                distFogMaxRadius: 1800,
                distFogEnabled: true);

            // 지형 텍스쳐 로딩
            string heightMap = PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EXE_PATH + @"\Res\Terrain\blend\water1.png";
            levelTextureMap[1] = EXE_PATH + @"\Res\Terrain\blend\grass_1.png";
            levelTextureMap[2] = EXE_PATH + @"\Res\Terrain\blend\lowestTile.png";
            levelTextureMap[3] = EXE_PATH + @"\Res\Terrain\blend\HighTile.png";
            levelTextureMap[4] = EXE_PATH + @"\Res\Terrain\blend\highestTile.png";
            string detailMap = EXE_PATH + @"\Res\Terrain\blend\detailMap.png";

            // 지형 레벨 텍스쳐 로딩
            _levelTextureMap = new Texture[levelTextureMap.Length];
            _detailTextureMap = new Texture(detailMap);
            for (int i = 0; i < _levelTextureMap.Length; i++)
            {
                _levelTextureMap[i] = new Texture(levelTextureMap[i]);
            }
                        
            //_samplesPassedPixelQuery = new SamplesPassedPixelQuery();           

            // 셰리더 해시정보는 파일로 저장
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
            // --------------------------------------------------------------
            // 프레임 업데이트 영역 설정
            // --------------------------------------------------------------

            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;
            _currentTime += duration;

            if (_currentTime > 1.1f)
            {                
                _currentTime = 0.0f;
                _simpleSunPositionCalculator.Update(duration);
                _skyDomeTexture2DShader.GenerateSkyTextureWithSunPosition(new Vertex3f(1,0,0).Normalized, camera.Position);
                //_skyDomeTexture2DShader.SaveTextureToBitmap(@"C:\Users\mekjh\OneDrive\바탕 화면\sky.png");
            }

            // FPS 업데이트
            _textNamePlate.Text = FramePerSecond.FPS.ToString() + "FPS";
            _textNamePlate.WorldPosition = camera.PivotPosition + (camera.Forward - camera.Right) * 0.5f;
            _textNamePlate.Update(deltaTime);

            // 계층적 Z-버퍼 업데이트
            _hzbuffer.FrameBind();
            _hzbuffer.PrepareRenderSurface();

            // 지형 오클루더들의 깊이맵 생성
            if (_terrainRegion != null)
            {
                _hzbuffer.RenderSimpleTerrain(
                    _terrainRegion.TerrainEntity,
                    camera.ProjectiveMatrix,
                    camera.ViewMatrix,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE);
            }

            // 계층적 Z-버퍼의 밉맵 생성
            //_hzbuffer.GenerateZBuffer();
            _hzbuffer.GenerateMipmapsUsingCompute();

            // UI 정보 업데이트
            /*
            _glControl3.CLabel("cam").Text =
                $"CamPos={camera.Position}, " +
                $"CameraPitch={camera.CameraPitch}, " +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance}";
            */
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

            // FPS 렌더링
            _textNamePlate.Render();

            if (_isDepthZBuffer)
            {
                // 깊이맵 디버그 표시 
                _hzbuffer.DrawDepthBuffer(ShaderManager.Instance.GetShader<HzmDepthShader>(), camera, level);
            }
            else
            {
                // 일반 렌더링 화면
                Renderer3d.RenderByTerrainTessellationShader(
                    ShaderManager.Instance.GetShader<TerrainTessellationShader>(), 
                    _terrainRegion.TerrainEntity, 
                    camera,
                    _levelTextureMap,
                    _detailTextureMap,
                    true,
                    _simpleSunPositionCalculator.SunDirection,
                    0,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE
                    );

                // 스카이돔 렌더링
                _skyRenderer.RenderSkyDome(camera);

                /*
                _samplesPassedPixelQuery.BeginQuery();
                _glControl3.CLabel("samplePassed").Text = $"대기픽셀통과수={_samplesPassedPixelQuery.LastPixelCount}";
                Renderer3d.RenderAtmosphereByRealTime(
                    _atmosphericRenderShader,
                    _cloudComputeShader.Texture3DHandle,
                    _atmosphericLUTComputeShader.TransmittanceLUTId, 
                    camera,
                    _dirLight, 
                    earthRadiusMeter: 6371.0f, 
                    atmosphereRadiusMeter: 6471.0f);
                _samplesPassedPixelQuery.EndQuery();
                */
            }

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(ShaderManager.Instance.GetShader<ColorShader>(), camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
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

        private void FormTerrain_Load(object sender, EventArgs e)
        {
            Console.WriteLine("FormLoad!");
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
                _isDepthZBuffer = !_isDepthZBuffer;
            }
            else if (e.KeyCode == Keys.D2)
            {
                //_skyRendererCompTextureGen.__SaveSkyTextureToFile("C:\\Users\\mekjh\\OneDrive\\바탕 화면\\cloud.png");
            }
            else if (e.KeyCode == Keys.D3)
            {
                //_skyRendererCompTextureGen.GenerateSkyTextureWithSunPosition(
                    //_simpleSunPositionCalculator.SunDirection,
                    //cloudCoverage:1.0f);
            }
            else if (e.KeyCode == Keys.G)
            {
                _isFogEnable = !_isFogEnable;

                // 안개 관련 유니폼 설정
                GlobalUniformBuffers.UpdateHalfPlaneFogData(
                    fogColor: new Vertex3f(0.9f, 0.9f, 0.9f),
                    fogDensity: 0.002f,
                    fogPlane: new Vertex4f(0, 0, 1, -100),
                    isFogEnabled: _isFogEnable);
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
                //GPUDownload.SaveAsBMP(_atmosphericLUTComputeShader.TransmittanceLUTId, 256, 128,
                    //@"C:\Users\mekjh\OneDrive\바탕 화면\transmittanceLUT.bmp");
            }
        }

    }
}
