using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using GPUDriven;
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
    public partial class FormGPUDriveHiZ : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;

        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private ColorShader _colorShader;                   // 컬러 셰이더
        private HzmDepthShader _hzmDepthShader;             // HZM 깊이 셰이더
        private TerrainTessellationShader _terrainShader;   // 지형 테셀레이션 셰이더

        private bool _isLoaded = false;                     // 로드 여부
        private bool _isStarted = false;                    // 시작 여부
        private Vertex3f _prevCameraPosition;               // 이전 카메라 위치

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
        TexturedModel[] _treeModel;                         // 나무 모델 배열
        GPUCullingRenderer _gpuDriven;
        HierarchyZBuffer _hzbuffer;                         // 계층적 GPU Z 버퍼

        // 지형 관련 변수들
        TerrainRegion _terrainRegion;                       // 지형 영역
        Texture[] _levelTextureMap = null;                  // 지형 레벨 텍스쳐
        Texture _detailTextureMap = null;                   // 지형 디테일 텍스쳐

        // Z 버퍼 관련 변수들
        int _level = 0;                                     // 현재 Z 버퍼 레벨
        const int DOWN_LEVEL = 1;                           // 다운샘플링 레벨
        bool _isVisibleDepthBuffer = false;                 // 깊이 버퍼 가시화 여부
        uint _visibleCount = 0;                             // 가시 객체 수

        public FormGPUDriveHiZ()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("gpuDriven", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\");
            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Load += (s, e) => Form_Load(s, e);
            _glControl3.Start();
            Controls.Add(_glControl3);

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 로그 프로파일 초기화
            LogProfile.Create(PROJECT_PATH + "\\log.txt");
        }

        public void Form_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(1008, 729);
            this.Location = new Point(100, 100);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Resize += new EventHandler(this.FormGPUDriven_Resize);

            MemoryProfiler.StartFrameMonitoring();
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
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _terrainShader = ShaderManager.Instance.GetShader<TerrainTessellationShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 20);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d("GPU Driven (임포스터, HiZ버퍼)", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _titleText.Color = Color.Red;

            _descText = new Text2d("1번키: 원점으로", 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.Color = Color.LightGray;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);

            _culledText = new Text2d("컬링된 노드 0개", width - 10, 10, width, height,
                Text2d.TextAlignment.Right, heightInPixels: 15);
            _culledText.Color = Color.YellowGreen;
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 3D 모델 매니저 및 모델 로드
            _model3DManager = new Model3dManager(PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm4.obj");
            _treeModel = _model3DManager.GetModels("Palm4");

            // 지형 영역 초기화
            RegionCoord regionCoord = new RegionCoord(0, 0);
            _terrainRegion = new TerrainRegion(regionCoord, chunkSize: 100, n: 10, null);
            _terrainRegion.LoadTerrainLowResMap(regionCoord, EXE_PATH + "\\Res\\Terrain\\low\\region0x0.png", 
                completed: () =>
                {
                    _terrainRegion.LoadTerrainHighResMap(regionCoord, EXE_PATH + "\\Res\\Terrain\\",
                        completed: () => 
                        {
                            //_culledText.Text = "상세지형 로딩 완료됨";
                            _isLoaded = true;
                        });
                });

            // 
            // 지형 레벨 텍스쳐 로딩
            string heightMap = PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EXE_PATH + @"\Res\Terrain\blend\water1.png";
            levelTextureMap[1] = EXE_PATH + @"\Res\Terrain\blend\grass_1.png";
            levelTextureMap[2] = EXE_PATH + @"\Res\Terrain\blend\lowestTile.png";
            levelTextureMap[3] = EXE_PATH + @"\Res\Terrain\blend\HighTile.png";
            levelTextureMap[4] = EXE_PATH + @"\Res\Terrain\blend\highestTile.png";
            string detailMap = EXE_PATH + @"\Res\Terrain\blend\detailMap.png";
            _levelTextureMap = new Texture[levelTextureMap.Length];
            _detailTextureMap = new Texture(detailMap);
            for (int i = 0; i < _levelTextureMap.Length; i++)
            {
                _levelTextureMap[i] = new Texture(levelTextureMap[i]);
            }

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 계층적깊이버퍼 생성
            _hzbuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;
            if (!_isStarted)
            {
                // GPU 드리븐 렌더러 초기화
                _gpuDriven = new GPUCullingRenderer(PROJECT_PATH);
                _gpuDriven.Initialize("Palm6", _treeModel, _hzbuffer.Levels, _terrainRegion);
                _culledText.Text = "상세지형이 로딩이 완료됨";
                _isStarted = true;
            }

            if (_prevCameraPosition != camera.Position)
            {
                // 뷰 프러스텀 업데이트
                _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

                // ✅ HZB 업데이트
                _hzbuffer.BindFramebuffer();
                _hzbuffer.PrepareRenderSurface();
                _hzbuffer.RenderSimpleTerrain(camera.ProjectiveMatrix, camera.ViewMatrix, TerrainConstants.DEFAULT_VERTICAL_SCALE,
                    _terrainRegion.TerrainEntity);
                _hzbuffer.UnbindFramebuffer();

                // ✅ 밉맵 생성
                _hzbuffer.GenerateMipmapsUsingFragment(maxLevel: -1);

                _gpuDriven?.Update(camera, _viewFrustum, _hzbuffer);

                _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";

                _prevCameraPosition = camera.Position;
            }

            /*
            uint visibleCount = _gpuDriven.GetVisibleCountDebug();

            if (_visibleCount != visibleCount)
            {
                // 네임플레이트 업데이트            
                _textNamePlate.Text = $"가시객체{visibleCount}";
                _textNamePlate.WorldPosition = camera.Position + camera.Forward * 1f - camera.Right * 0.2f;
                _textNamePlate.Update(deltaTime);
                _visibleCount = visibleCount;
                _culledText.Text = $"가시 객체 {_visibleCount}개, HZB Level: {_level}";
            }
            */

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 계층적 Z-버퍼 렌더링
            if (_isVisibleDepthBuffer)
            {
                Gl.PolygonMode(MaterialFace.FrontAndBack,  PolygonMode.Fill);
                _hzbuffer.RenderDepthBuffer(_hzmDepthShader, camera, level: _level);
                Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);
            }
            else
            {
                // 일반 렌더링 화면
                Renderer3d.RenderByTerrainTessellationShader(_terrainShader, _terrainRegion.TerrainEntity, camera, _levelTextureMap, _detailTextureMap,
                    isDetailMap: true,
                    lightDirection: Vertex3f.UnitZ,
                    vegetationMap: 0,
                    heightScale: TerrainConstants.DEFAULT_VERTICAL_SCALE
                    );

                _gpuDriven?.Render(camera);
            }

            // 2D 렌더링을 위한 상태 설정
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);  // ← 여기서 켜기
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
            Gl.Viewport(0, 0, w, h);

            // FPS 렌더링
            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            _textNamePlate.Render();
            _fpsText.Render();
            _titleText.Render();
            _descText.Render();
            _camPosText.Render();
            _culledText.Render();

            // 카메라 중심점 렌더링
            Gl.Disable(EnableCap.Blend);
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {

        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D0)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 1.0f);
            }
            else if (e.KeyCode == Keys.F5)
            {
                _gpuDriven.DebugDepthMode = !_gpuDriven.DebugDepthMode;
            }
            else if (e.KeyCode == Keys.D2)
            {
                _isVisibleDepthBuffer = !_isVisibleDepthBuffer;
            }
            else if (e.KeyCode == Keys.D3)
            {
               _level = (_level + 1) % _hzbuffer.Levels;
                _culledText.Text = $"HZB Level: {_level}";
            }
            else if (e.KeyCode == Keys.D4)
            {
                _level = (_level - 1 + _hzbuffer.Levels) % _hzbuffer.Levels;
                _culledText.Text = $"HZB Level: {_level}";
            }
            else if (e.KeyCode == Keys.Enter)
            {
               
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

        }

        private void FormGPUDriveHiZ_Load(object sender, EventArgs e)
        {

        }
    }
}
