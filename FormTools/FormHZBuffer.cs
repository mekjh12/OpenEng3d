using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.IO;
using System.Windows.Forms;
using Terrain;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormHZBuffer : Form
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;

        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private ColorShader _colorShader;                   // 컬러 셰이더
        private HzmDepthShader _hzmDepthShader;             // HZM 깊이 셰이더
        private TextNamePlate _textNamePlate;               // 텍스트 네임플레이트
        private Polyhedron _viewFrustum;                    // 뷰 프러스텀

        HierarchicalGpuZBuffer _hzbuffer;           // 계층적 GPU Z 버퍼
        TerrainRegion _terrainRegion;               // 지형 영역
        int _level = 0;                             // 현재 Z 버퍼 레벨
        const int DOWN_LEVEL = 0;                   // 다운샘플링 레벨

        private BVH3f _bvh3f;                       // BVH 구조체
        private AABBBoxShader _aabbBoxShader;       // AABB 박스 셰이더


        public FormHZBuffer()
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

        private void FormHZBuffer_Load(object sender, EventArgs e)
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
            ShaderManager.Instance.AddShader(new HzmDepthShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AABBBoxShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _aabbBoxShader = ShaderManager.Instance.GetShader<AABBBoxShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
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
                    Vertex3f center = new Vertex3f(i * 25f, j * 25, Rand.NextFloat * 200f);
                    Vertex3f halfSize = Rand.NextVector3f * 5f + Vertex3f.One * 10.0f;
                    _bvh3f.InsertLeaf(new AABB3f(center - halfSize, center + halfSize));
                }
            }

            // 계층적깊이버퍼 생성
            _hzbuffer = new HierarchicalGpuZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);

            // 지형 영역 초기화
            _terrainRegion = new TerrainRegion(new RegionCoord(0, 0), chunkSize: 100, n: 10, null);
            _terrainRegion.LoadTerrainLowResMap(new RegionCoord(0, 0), EXE_PATH + "\\Res\\Terrain\\low\\region0x0.png");

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
            float duration = deltaTime * 0.001f;

            _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);
            _bvh3f.ClearBackTreeNodeLink();
            _bvh3f.CullingTestByViewFrustum(_viewFrustum, canMineVisibleAABB: true);

            _textNamePlate.Text = $"{FramePerSecond.FPS}FPS " +
                $"해상도={_hzbuffer.Width}x{_hzbuffer.Height} " +
                $"레벨{_level}/{_hzbuffer.Levels - 1} " +
                $"가시성통과{_bvh3f.LinkLeafCount}";
            _textNamePlate.WorldPosition = camera.Position + camera.Forward * 1f - camera.Right * 0.2f;
            _textNamePlate.Update(deltaTime);

            // ✅ HZB 업데이트
            _hzbuffer.BindFramebuffer();
            _hzbuffer.PrepareRenderSurface();
            _hzbuffer.RenderSimpleTerrain(
                _terrainRegion.TerrainEntity,
                camera.ProjectiveMatrix,
                camera.ViewMatrix,
                TerrainConstants.DEFAULT_VERTICAL_SCALE);
            _hzbuffer.UnbindFramebuffer();

            // ✅ 밉맵 생성 (디버깅 코드 제거)
            _hzbuffer.GenerateMipmapsUsingCompute();

            //_bvh3f.CullingTestByHiZBuffer(camera.VPMatrix, camera.ViewMatrix, _hzbuffer, canMineVisibleAABB: true);

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

            // 계층적 Z-버퍼 렌더링
            _hzbuffer.RenderDepthBuffer(_hzmDepthShader, camera, level: _level);

            // AABB 박스 렌더링
            //Renderer3d.RenderAABBGeometry(_aabbBoxShader, in _bvh3f.VisibleAABBs, _bvh3f.LinkLeafCount, camera);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
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
            string dirPath = @"C:\Users\mekjh\OneDrive\바탕 화면\HiZ";
            if (e.KeyCode == Keys.D1)
            {
                _level = Math.Max(0, _level - 1);
            }
            else if (e.KeyCode == Keys.D2)
            {
                _level = Math.Min(_hzbuffer.Levels - 1, _level + 1);
            }
            else if (e.KeyCode == Keys.D3)
            {
                if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);
                DebugZBufferVisualizer.SaveAllLevelsAsImages(_hzbuffer.Zbuffer, _hzbuffer.Width, _hzbuffer.Height, dirPath, true);
            }
            else if (e.KeyData == Keys.D4)
            {
                if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);
                for (int i = 0; i < 5; i++)
                {
                    _hzbuffer.SaveTextureToImageWithColorMap(i, dirPath + $"\\hzb_level{i}.png");
                }
            }
        }

        private void FormHZBuffer_Resize(object sender, EventArgs e)
        {
            int width = _glControl3.Width;
            int height = _glControl3.Height;
            //_hzbuffer = new HierarchicalGpuZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);
        }
    }
}
