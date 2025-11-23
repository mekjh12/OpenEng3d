using Common.Abstractions;
using GlWindow;
using OpenGL;
using Shader;
using System;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormCloud : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        //private string ExE_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\";

        private GlControl3 _glControl3;
         
        // 셰이더 변수
        CloudComputeShader _cloudComputeShader;
        CloudTestRenderShader _cloudTestRenderShader;
        CloudShadowMapShader _cloudShadowMapShader;

        float _g = 0.43f;
        float _cloudDensity = 1.0f;
        Vertex3f _lightDir = new Vertex3f(0, 0, 0);

        public FormCloud()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("cloudRendering", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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

            // 파일 해시 초기화
            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;
        }

        public void Init(int width, int height)
        {
            // 랜덤변수 생성
            Rand.InitSeed(500);

            // 셰이더 초기화
            if (_cloudComputeShader == null) _cloudComputeShader = new CloudComputeShader(PROJECT_PATH);
            if (_cloudTestRenderShader == null) _cloudTestRenderShader = new CloudTestRenderShader(PROJECT_PATH);
            if (_cloudShadowMapShader == null) _cloudShadowMapShader = new CloudShadowMapShader(PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            // 화면 구성요소 초기화
            _glControl3.AddLabel("resolution", $"resolution={w >> 2}x{h >> 2}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddValueBar("g", minValue: 0.0f, maxValue: 1.0f, value: _g, stepValue: 0.01f, align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));

            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // 전체 화면 여부 
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);

            // GPU 정보 출력
            string vendor = Gl.GetString(StringName.Vendor);
            string renderer = Gl.GetString(StringName.Renderer);
            string version = Gl.GetString(StringName.Version);
            Console.WriteLine($"GPU 제조사: {vendor}");
            Console.WriteLine($"렌더러: {renderer}");
            Console.WriteLine($"OpenGL 버전: {version}");
        }

        private void Init3d(int w, int h)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 클라우드 셰이더 초기 실행
            _cloudComputeShader.Run();

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
            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;

            // 클라우드 셰이더 업데이트
            _cloudComputeShader.UpdateTime(duration);

            // 구름 그림자 맵 업데이트
            _cloudShadowMapShader.ComputeShadowMap(_cloudComputeShader.Texture3DHandle, _lightDir, _cloudDensity);

            _g = 2.0f * (_glControl3.SimpHValueBar("g").Value) - 1.0f;

            // UI 정보 업데이트
            _glControl3.CLabel("cam").Text =
                $"CamPos={camera.Position}, " +
                $"CameraPitch={camera.CameraPitch}, " +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance}";
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

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 카메라 중심점 렌더링
            Gl.Enable(EnableCap.DepthTest);

            // 폴리곤 모드 설정
            Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);

            // 구름 렌더링
            _cloudTestRenderShader.Run(
                camera,                                 // 카메라
                _g,                                      // Henyey-Greenstein 비대칭 인자
                _cloudDensity,                          // 구름 밀도 스케일
                _cloudComputeShader.Texture3DHandle,    // 구름 3D 텍스처 핸들
                _cloudShadowMapShader.ShadowMapHandle); // 그림자 맵 텍스처 핸들

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
                _cloudComputeShader.SaveTextureToPngSlices(@"C:\Users\mekjh\OneDrive\바탕 화면\3d", separateChannels:true);
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D4)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 0);
            }
        }

        private void FormCloud_Load(object sender, EventArgs e)
        {

        }
    }
}
