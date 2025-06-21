using Camera3d;
using Geometry;
using GlWindow;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Windows.Forms;
using Ui2d;
using ZetaExt;
using Sky;
using Common.Abstractions;

namespace FormTools
{
    public partial class FormAtmosphereScattering : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private string EXE_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\";

        private GlControl3 _glControl3;

        ColorShader _colorShader;
        StaticShader _staticShader;
        Atmosphere _atmosphere;

        public FormAtmosphereScattering()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("AtmosphereScattering", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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
            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);
        }

        public void Init(int width, int height)
        {
            // 랜덤변수 생성
            Rand.InitSeed(500);

            // 쉐이더 초기화
            _colorShader = new ColorShader(PROJECT_PATH);
            _staticShader = new StaticShader(PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            _glControl3.AddLabel("sun", $"sun=0", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // in my house with GPU.
            Debug.PrintLine($"{Screen.PrimaryScreen.DeviceName} {w}x{h}");
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0)
            {
                _glControl3.FullScreen(true);
            }
        }

        private void Init3d(int w, int h)
        {
            _atmosphere = new Atmosphere(PROJECT_PATH);

            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);
            _glControl3.Camera.FAR = 20000;
            _glControl3.Camera.NEAR = 1;
            _glControl3.Camera.Position = new Vertex3f(0, 0, _atmosphere.RadiusEarth + 10);
        }

        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            OrbitCamera obitcam = camera as OrbitCamera;
            float g = 1.0f / (float)Math.Tan((obitcam.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(obitcam.Position,
                obitcam.Forward, obitcam.Up, obitcam.Right, g, obitcam.AspectRatio, obitcam.NEAR, obitcam.FAR);


            _glControl3.CLabel("sun").Text = $"sun={_atmosphere.Theta}";
            _glControl3.CLabel("cam").Text = $"CamPos={obitcam.Position},CameraPitch={obitcam.CameraPitch},CameraYaw={obitcam.CameraYaw}, Dist={obitcam.Distance}";
        }

        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            OrbitCamera orbCamera = camera as OrbitCamera;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            float r = _glControl3.BackClearColor.x;
            float g = _glControl3.BackClearColor.y;
            float b = _glControl3.BackClearColor.z;

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0); //기본프레임버퍼로 전환
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            _atmosphere.Render(camera);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_colorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
                _glControl3.Camera.Position = new Vertex3f(0, 0, _atmosphere.RadiusEarth + 10);
            }
        }

        public void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        private void FormAtmosphereScattering_Load(object sender, EventArgs e)
        {

        }
    }
}
