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
    public partial class FormGPUDrivenModelInstance : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;

        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private ColorShader _colorShader;                   // 컬러 셰이더
        private bool _isLoaded = false;                     // 로드 여부

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
        ModelBatchManager _modelBatchManager;
        FrustumCullingRenderer _gpuDriven;
        const int MAX_INSTANCES = 100000;

        int _level = 0;                                     // 현재 Z 버퍼 레벨
        uint _visibleCount = 0;                             // 가시 객체 수
        uint _visibleCountLod0 = 0;                         // 가시 객체 수 LOD0
        uint _visibleCountLod1 = 0;                         // 가시 객체 수 LOD1
        uint _frustumPassCount = 0;                         // 프러스텀 패스 수
        uint _lastVisibleCount = 0;                         // 이전 가시 객체 수
        uint _lastFrustumPassCount = 0;                     // 이전 프러스텀 패스 수
        string _visibleReport = "";                         // 가시 객체 리포트

        public FormGPUDrivenModelInstance()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("GPU Driven(모델 인스턴스)", Application.StartupPath, 
                @"\fonts\fontList.txt", @"\Res\", useRenderTarget: true);
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
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 20);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d("GPU Driven = 모델 인스턴싱", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 24);
            _titleText.Color = Color.Yellow;

            _descText = new Text2d("1번키: 원점으로", 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.Color = Color.LightGray;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);

            _culledText = new Text2d("컬링된 노드 0개", width - 10, 10, width, height,
                Text2d.TextAlignment.Right, heightInPixels: 15);
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 3D 모델 매니저 초기화 및 모델 로드
            _model3DManager = new Model3dManager(PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            UnifiedTexturedModel model1 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm4.obj");
            UnifiedTexturedModel model2 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm1.obj");
            UnifiedTexturedModel model3 = _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");

            // 모델 배치 매니저 초기화
            _modelBatchManager = new ModelBatchManager();
            _modelBatchManager.AddModel(model1.Name, 100, model1);
            _modelBatchManager.AddModel(model2.Name, 100, model2);
            _modelBatchManager.AddModel(model3.Name, 100, model3);

            // 인스턴스 변환 행렬 생성 및 추가
            int gridSize = 300;
            float spacing = 15f;
            float halfSpacing = spacing / 2f;
            float quaterSpacing = spacing / 4f;
            Random rand = new Random(42);
            Vertex3f position = Vertex3f.Zero;

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;

                float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                position.x = posX;
                position.y = posY;
                float posZ = 0;

                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                float scale = 0.5f + (float)(rand.NextDouble() * 1.0f);

                Matrix4x4f transform = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);
                _modelBatchManager.AddInstance((uint)(x % 3), transform);
                //_modelBatchManager.AddInstance((uint)Rand.NextInt(0, 2), transform);

            }

            Console.WriteLine($"Generated {MAX_INSTANCES} tree instances");
            _modelBatchManager.Finalized();
            _isLoaded = true;

            // GPU 드리븐 렌더러 초기화
            _gpuDriven = new FrustumCullingRenderer(PROJECT_PATH);
            _gpuDriven.Initialize(_modelBatchManager, _glControl3.Camera);

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            // 뷰 프러스텀 업데이트
            _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

            // GPU 드리븐 업데이트
            _gpuDriven.Update(camera, _viewFrustum);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
            _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";

            _gpuDriven.GetVisibleCountDebug(ref _visibleCount,
                ref _visibleCountLod0,
                ref _visibleCountLod1,
                ref _frustumPassCount,
                ref _visibleReport);

            // 가시 정보 업데이트            
            if (_visibleCount != _lastVisibleCount || _frustumPassCount != _lastFrustumPassCount)
            {
                _lastVisibleCount = _visibleCount;
                _lastFrustumPassCount = _frustumPassCount;
                _culledText.Text = $"배치수{_modelBatchManager.ActualBatchCount}, " +
                    $"가시객체 {_visibleCount}개({_visibleCountLod0}/{_visibleCountLod1}), " +
                    $"뷰프러스텀 {_frustumPassCount}개, " +
                    $"HZB Level: {_level}";
            }
        }


        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 기본 프레임버퍼로 전환 및 초기화
            if (!_glControl3.UseRenderTarget)
            {
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Gl.Viewport(0, 0, w, h);
                Gl.ClearColor(backcolor.x, backcolor.y, backcolor.z, backcolor.w);
                Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            }

            _gpuDriven.Render(camera);
        }


        private void BlitToScreen(int deltaTime, Camera camera)
        {
            if (!_isLoaded) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 최종 화면 출력
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _glControl3.BlitRenderTargetToScreen();

            // 2D UI 렌더링
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


        public void KeyDownEvent(object sender, KeyEventArgs e)
        {

        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 1.0f);
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

        private void FormGPUDrivenImposter_Resize(object sender, EventArgs e)
        {

        }

        private void FormGPUDrivenImposter_Load(object sender, EventArgs e)
        {

        }

        public void InitFinished()
        {
            throw new NotImplementedException();
        }
    }
}
