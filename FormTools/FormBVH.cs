using GlWindow;
using OpenGL;
using System;
using System.Windows.Forms;
using ZetaExt;
using Common.Abstractions;
using FastMath;
using Shader;
using Terrain;
using Ui3d;
using Renderer;
using Occlusion;
using Geometry;

namespace FormTools
{
    public partial class FormBVH : Form
    {
        private readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private GlControl3 _glControl3;         // OpenGL 컨트롤
        private ColorShader _colorShader;       // 컬러 셰이더
        private TextNamePlate _textNamePlate;   // 텍스트 네임플레이트
        private BVH3f _bvh3f;                   // BVH 구조체        

        public FormBVH()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("bvh", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(0, 0, 0),
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

            // ✅ 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FTP");
            _textNamePlate.Height = 0.6f;
            _textNamePlate.Width = 0.6f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // BVH 구조체 초기화
            _bvh3f = new BVH3f();
            _bvh3f.Clear();

            for (int i = 0; i < 20; i++)
            {
                Vertex3f center = Rand.NextVector3f * 20.0f;
                Vertex3f halfSize = Rand.NextVector3f * 1.0f + Vertex3f.One * 0.1f;
                _bvh3f.InsertLeaf(new AABB3f(center - halfSize, center + halfSize));
            }

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void Init2d(int width, int height)
        {
            // 화면 구성요소 초기화
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // 전체화면 모드 설정
            //if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;

            // FPS 업데이트
            _textNamePlate.Text = FramePerSecond.FPS.ToString() + "FPS";
            _textNamePlate.WorldPosition = camera.PivotPosition + (camera.Forward - camera.Right) * 0.5f;
            _textNamePlate.Update(deltaTime);

            _bvh3f.ExtractOccluder(camera.Position);

            // UI 정보 업데이트
            _glControl3.CLabel("cam").Text = "" +
                $"CamPos={camera.Position}, " +
                $"CameraPitch={camera.CameraPitch}, " +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance}";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 백그라운드 컬러 설정
            float r = _glControl3.BackClearColor.x;
            float g = _glControl3.BackClearColor.y;
            float b = _glControl3.BackClearColor.z;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // FPS 렌더링
            _textNamePlate.Render();


            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(ShaderManager.Instance.GetShader<ColorShader>(), camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        private void MouseDnEvent(object sender, MouseEventArgs e)
        {
        }

        private void MouseUpEvent(object sender, MouseEventArgs e)
        {
        }

        private void KeyDownEvent(object sender, KeyEventArgs e)
        {
        }

        private void KeyUpEvent(object sender, KeyEventArgs e)
        {
        }

        private void FormBVH_Load(object sender, EventArgs e)
        {

        }
    }
}
