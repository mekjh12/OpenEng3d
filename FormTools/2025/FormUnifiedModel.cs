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
using System.Drawing;
using System.Windows.Forms;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormUnifiedModel : Form
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;
        readonly string TITLE = "UnifiedModel 렌더링";
        readonly string HELP_TEXT =
            "1번키: 원점으로\n";

        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private ColorShader _colorShader;                   // 컬러 셰이더
        private UnlitShader _unlitShader;                   // 언릿 셰이더

        // UI 2D 관련 변수들
        private Text2d _fpsText;                            // FPS 텍스트
        private Text2d _titleText;                          // 타이틀 텍스트
        private Text2d _descText;                           // 설명 텍스트
        private Text2d _camPosText;                         // 카메라 위치 텍스트   

        // 3D 관련 변수들
        Vertex3f _prevCameraPosition;                       // 이전 카메라 위치
        UnifiedTexturedModel _unifiedModel;                 // 통합 모델
        UnifiedModelRenderer _unifiedModelRenderer;         // 통합 모델 렌더러

        public FormUnifiedModel()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("UnifiedModel", Application.StartupPath, 
                @"\fonts\fontList.txt", @"\Res\", 
                useRenderTarget: false);

            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            //_glControl3.BlitToScreen = (deltaTime, camera) => BlitToScreen(deltaTime, camera);
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
            this.ClientSize = new Size(1280, 800);
            this.Location = new Point(500, 100);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Resize += new EventHandler(this.Form_Resize);

            MemoryProfiler.StartFrameMonitoring();
        }

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new ColorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new UnlitShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 20);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d("단일모델만들기", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _titleText.Color = Color.Red;

            _descText = new Text2d("1번키: 원점으로", 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.Color = Color.LightGray;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 3D 모델 매니저 및 모델 로드
            _unifiedModel = ObjLoaderEx.LoadObjUnified(PROJECT_PATH + @"FormTools\bin\Debug\Res\Palm5.obj");
            _unifiedModelRenderer = new UnifiedModelRenderer(_unifiedModel, _unlitShader);

            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.Finalize();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            // 시야 절두체 생성
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

            if (_prevCameraPosition != camera.Position)
            {
                _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
                _prevCameraPosition = camera.Position;
            }

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.ClearColor(backcolor.x, backcolor.y, backcolor.z, backcolor.w);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _unifiedModelRenderer.Render(camera.VPMatrix, camera.ViewMatrix);
            //Renderer3d.RenderAABB(_colorShader, _unifiedModel.AABB, camera);
        }


        private void BlitToScreen(int deltaTime, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 최종 화면 출력
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 컬러 버퍼 출력
            _glControl3.BlitRenderTargetToScreen();

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

        private void Form_Resize(object sender, EventArgs e)
        {
            int width = _glControl3.Width;
            int height = _glControl3.Height;

        }

        private void FormUnifiedModel_Load(object sender, EventArgs e)
        {

        }
    }
}
