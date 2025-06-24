using ActionEnums;
using Animate;
using Common.Abstractions;
using GlWindow;
using OpenGL;
using Shader;
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using ZetaExt;

namespace FormTools
{
    public partial class FormAnimation : Form
    {

        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private GlControl3 _glControl3;

        StaticShader _staticShader;
        AnimateShader _animateShader;
        HumanAniModel _humanAniModel;
        AniDae _xmlDae;

        public FormAnimation()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("characterAnimation", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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

        private void FormUi2d_Load(object sender, EventArgs e)
        {

        }

        public void Init(int width, int height)
        {
            // 랜덤변수 생성
            Rand.InitSeed(500);

            // 셰이더 초기화
            if (_staticShader == null) _staticShader = new StaticShader(PROJECT_PATH);
            if (_animateShader == null) _animateShader = new AnimateShader(PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            // 화면 구성요소 초기화
            _glControl3.AddLabel("resolution", $"resolution={w >> 2}x{h >> 2}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // 전체 화면 여부 
            //if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        private void Init3d(int w, int h)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            _xmlDae = new AniDae(PROJECT_PATH + @"\Res\abe.dae", isLoadAnimation: false);
            AnimateEntity animateEntity = new AnimateEntity("man", _xmlDae.Models.ToArray());
            _humanAniModel = new HumanAniModel("man", animateEntity, _xmlDae);

            // *** Action ***
            foreach (string fn in Directory.GetFiles(PROJECT_PATH + "\\Res\\Action\\"))
            {
                if (Path.GetExtension(fn) == ".dae")
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(fn);
                    string motionName = Path.GetFileNameWithoutExtension(fn);
                    AniXmlLoader.LoadMixamoMotion(_xmlDae, xml, motionName);
                }
            }

            _humanAniModel.SetMotion(ACTION.BREATHING_IDLE);

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

            // 애니메이션 업데이트
            _humanAniModel.Update(deltaTime);

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
                        
            _humanAniModel.Render(camera, _staticShader, _animateShader, isBoneVisible: true);

            // 폴리곤 모드 설정
            Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);

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
            if (e.KeyCode == Keys.F)
            {
                _humanAniModel.PolygonMode = 
                    _humanAniModel.PolygonMode == PolygonMode.Fill ? PolygonMode.Line : PolygonMode.Fill;
                Debug.PrintLine($"PolygonMode: {_humanAniModel.PolygonMode}");
            }
            else if (e.KeyCode == Keys.D1)
            {
                _humanAniModel.SetMotion(ACTION.BREATHING_IDLE);
            }
            else if (e.KeyCode == Keys.D2)
            {
                _humanAniModel.SetMotion(ACTION.WALKING);
            }
            else if (e.KeyCode == Keys.D3)
            {
                _humanAniModel.SetMotion(ACTION.A_T_POSE);
            }
            else if (e.KeyCode == Keys.D4)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 1.0f);
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
        }

        private void FormCloud_Load(object sender, EventArgs e)
        {

        }
    }
}
