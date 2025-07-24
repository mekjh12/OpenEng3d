using Animate;
using AutoGenEnums;
using Common.Abstractions;
using GlWindow;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormAnimation : Form
    {

        private readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private GlControl3 _glControl3;

        StaticShader _staticShader;
        AnimateShader _animateShader;
        AxisShader _axisShader;

        MixamoRotMotionStorage _mixamoRotMotionStorage;
        List<Human> _humans = new List<Human>();

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
            if (_axisShader == null) _axisShader = new AxisShader(PROJECT_PATH);
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

            PrimateRig aniRig = new PrimateRig(PROJECT_PATH + @"\Res\abe.dae", isLoadAnimation: false);
            PrimateRig aniRig2 = new PrimateRig(PROJECT_PATH + @"\Res\Guybrush_final.dae", isLoadAnimation: false);

            //_humans.Add(new Human($"abe", aniRig));
            //_humans[0].Transform.IncreasePosition(0, 0, 0);

            //_humans.Add(new Human($"Guybrush_final", aniRig2));
           // _humans[1].Transform.IncreasePosition(2, 0, 0);

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    _humans.Add(new Human($"abe{i}x{j}", aniRig));
                    _humans[5 * i + j].Transform.IncreasePosition(i * 2, j * 2, 0);
                }
            }

            // 믹사모 애니메이션 로드
            _mixamoRotMotionStorage = new MixamoRotMotionStorage();
            foreach (string fileName in Directory.GetFiles(PROJECT_PATH + "\\Res\\Action\\"))
            {
                if (Path.GetExtension(fileName).Equals(".dae"))
                {
                    Motion motion = AniXmlLoader.LoadMixamoMotion(aniRig, fileName);
                    _mixamoRotMotionStorage.AddMotion(motion);
                }
            }

            // 애니메이션 리타겟팅
            _mixamoRotMotionStorage.RetargetMotionsTransfer(targetAniRig: aniRig);
            _mixamoRotMotionStorage.RetargetMotionsTransfer(targetAniRig: aniRig2);

            // 애니메이션 모델에 애니메이션 초기 지정
            foreach (Human human in _humans)
            {
                human.SetMotion(HUMAN_ACTION.RANDOM);
            }

            // 아이템 장착
            Model3d.TextureStorage.NullTextureFileName = PROJECT_PATH + "\\Res\\debug.jpg";
            TexturedModel hat = LoadModel(PROJECT_PATH + @"\Res\Items\Merchant_Hat.dae")[0];
            TexturedModel sword = LoadModel(PROJECT_PATH + @"\Res\Items\sword1.dae")[0];

            _humans[0].EquipItem(ATTACHMENT_SLOT.Head,"hat0", "hat", hat, 200.0f, positionY: -6.0f, pitch:-20);
            _humans[0].EquipItem(ATTACHMENT_SLOT.LeftHand, "sword0", "sword", sword, 5.0f, yaw: 90);

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
            foreach (Human human in _humans)
            {
                human.Update(deltaTime);
            }

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
            Vertex3f backColor = _glControl3.BackClearColor;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 카메라 중심점 렌더링
            Gl.Enable(EnableCap.DepthTest);

            Matrix4x4f vp = camera.VPMatrix;

            foreach (Human human in _humans)
            {
                human.Render(camera, vp, _animateShader, _staticShader, isBoneVisible: true);
            }

            foreach (Human human in _humans)
            {
               //_axisShader.RenderAxes(human.ModelMatrix, human.Animator.RootTransforms, vp);
            }

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
                foreach (Human human in _humans)
                {
                    human.PolygonMode = human.PolygonMode == PolygonMode.Fill ? PolygonMode.Line : PolygonMode.Fill;
                }
            }
            else if (e.KeyCode == Keys.D1)
            {
                _humans[Rand.NextInt(0,_humans.Count-1)].SetMotion(HUMAN_ACTION.RANDOM); 
            }
            else if (e.KeyCode == Keys.D2)
            {
                _humans[Rand.NextInt(0, _humans.Count - 1)].SetMotionOnce(HUMAN_ACTION.RANDOM);
            }
            else if (e.KeyCode == Keys.D3)
            {
                _humans[Rand.NextInt(0, _humans.Count - 1)].SetMotionImmediately(HUMAN_ACTION.RANDOM);
            }
            else if (e.KeyCode == Keys.H)
            {
                _humans[0].FoldHand(true);
            }
            else if (e.KeyCode == Keys.J)
            {
                _humans[0].UnfoldHand(true);
            }
            else if (e.KeyCode == Keys.D0)
            {
                _glControl3.Camera.PivotPosition = new Vertex3f(0, 0, 1.0f);
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
        }

        public TexturedModel[] LoadModel(string modelFileName)
        {
            string materialFileName = modelFileName.Replace(".obj", ".mtl");

            // 텍스쳐모델을 읽어온다.
            List<TexturedModel> texturedModels = ObjLoader.LoadObj(modelFileName);

            // 모델에 맞는 원래 모양의 바운딩 박스를 만든다.
            foreach (TexturedModel texturedModel in texturedModels)
            {
                texturedModel.GenerateBoundingBox();
            }

            return texturedModels.ToArray();
        }
    }
}
