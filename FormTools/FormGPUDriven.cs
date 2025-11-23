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
using Terrain;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormGPUDriven : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string ExE_PATH = Application.StartupPath;
        
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
        TexturedModel[] _treeModel;                         // 나무 모델 배열     

        public FormGPUDriven()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("occlusion", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(0, 0, 0),
                IsVisibleUi2d = true,
            };

            // GL 이벤트 연결
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

            // GL 컨트롤 시작
            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;

            // 로그 프로파일 초기화
            LogProfile.Create(PROJECT_PATH + "\\log.txt");
        }

        public void Form_Load(object sender, EventArgs e)
        {
            MemoryProfiler.StartFrameMonitoring();
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

            _titleText = new Text2d("GPU Driven", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _titleText.Color = Color.Red;

            _descText = new Text2d("1번키: ", 10, height, width, height,
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

            _model3DManager = new Model3dManager(PROJECT_PATH, ExE_PATH + "\\nullTexture.jpg");
            _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");
            _treeModel = _model3DManager.GetModels("tree1");

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
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





            // 2D 렌더링을 위한 완전한 상태 리셋
            Gl.Disable(EnableCap.DepthTest);           // 깊이 테스트 끄기
            Gl.Enable(EnableCap.Blend);                // 블렌딩 켜기
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);            // 컬링 끄기
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
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            // 네임플레이트 업데이트            
            _textNamePlate.Text = $"정보";
            _textNamePlate.WorldPosition = camera.Position + camera.Forward * 1f - camera.Right * 0.2f;
            _textNamePlate.Update(deltaTime);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
            _culledText.Text = $"컬링된 노드";
            _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {

        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
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
    }
}
