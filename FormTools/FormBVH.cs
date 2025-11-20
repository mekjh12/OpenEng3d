using Common;
using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Windows.Forms;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormBVH : Form
    {
        private readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private GlControl3 _glControl3;         // OpenGL 컨트롤
        private ColorShader _colorShader;       // 컬러 셰이더
        private AABBBoxShader _aabbBoxShader;   // AABB 박스 셰이더
        private TextNamePlate _textNamePlate;   // 텍스트 네임플레이트
        private BVH3f _bvh3f;                   // BVH 구조체

        private Polyhedron _viewFrustum;

        /// <summary>
        /// Binary Volume Hierarchy 테스트 폼
        /// </summary>
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

            // GL 컨트롤 시작
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
            ShaderManager.Instance.AddShader(new AABBBoxShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _aabbBoxShader = ShaderManager.Instance.GetShader<AABBBoxShader>();

            // 앱 시작 시 한 번만 초기화
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
            _bvh3f = new BVH3f(4000);
            _bvh3f.Clear();

            // 랜덤 AABB 20개 삽입
            for (int i = -30; i < 30; i++)
            {
                for (int j = -30; j < 30; j++)
                {
                    Vertex3f center = new Vertex3f(i * 5f, j * 5f, Rand.NextFloat * 2f);
                    Vertex3f halfSize = Rand.NextVector3f * 0.5f + Vertex3f.One * 1.0f;
                    _bvh3f.InsertLeaf(new AABB3f(center - halfSize, center + halfSize));
                }
            }

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void Init2d(int width, int height)
        {
            // 화면 구성요소 초기화

            // 전체화면 모드 설정
            //if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            // 시간 간격을 초 단위로 변환
            float duration = deltaTime * 0.001f;

            // 뷰 프러스텀 컬링 테스트
            _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);
            _bvh3f.ClearBackTreeNodeLink();
            _bvh3f.CullingTestByViewFrustum(_viewFrustum, true);
            
            // FPS 업데이트
            _textNamePlate.Text = FramePerSecond.FPS.ToString() +
                $"FPS 가시노드={_bvh3f.LinkLeafCount}개";
            _textNamePlate.WorldPosition = camera.PivotPosition + (camera.Forward - camera.Right) * 0.5f;
            _textNamePlate.Update(deltaTime);

        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // FPS 렌더링
            _textNamePlate.Render();

            // AABB 박스 렌더링
            Renderer3d.RenderAABBGeometry(_aabbBoxShader, in _bvh3f.VisibleAABBs, _bvh3f.LinkLeafCount, camera);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(ShaderManager.Instance.GetShader<ColorShader>(), camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        private void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
        }

        private void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        private void KeyDownEvent(object sender, KeyEventArgs e)
        {
        }

        private void KeyUpEvent(object sender, KeyEventArgs e)
        {
        }

        private void FormBVH_Load(object sender, EventArgs e)
        {
            MemoryProfiler.StartFrameMonitoring();
        }
    }
}
