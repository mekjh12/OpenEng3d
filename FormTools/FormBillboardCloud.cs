using BillBoard;
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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormBillboardCloud : Form
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;
        readonly string TITLE = "Cross Billboard";
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
        private Text2d _culledText;                         // 컬링된 노드 텍스트   

        // 3D 관련 변수들
        Vertex3f _prevCameraPosition;                       // 이전 카메라 위치
        UnifiedTexturedModel _unifiedModel;                 // 통합 모델
        UnifiedModelRenderer _unifiedModelRenderer;         // 통합 모델 렌더러

        // 테스트 관련 변수들
        CrossBillboardAtlasGenerator _generator;
        CrossBillboardRenderer _renderer;
        CrossBillboardData _billboardData;
        CrossBillboardShader _cbShader;
        AABBBoxShader _aabbBoxShader;


        public FormBillboardCloud()
        {
            InitializeComponent();

            // GL 생성
            this.Text = TITLE;
            _glControl3 = new GlControl3(TITLE, Application.StartupPath, @"\fonts\fontList.txt", @"\Res\");
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
            ShaderManager.Instance.AddShader(new CrossBillboardShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AABBBoxShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();
            _cbShader = ShaderManager.Instance.GetShader<CrossBillboardShader>();
            _aabbBoxShader = ShaderManager.Instance.GetShader<AABBBoxShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 30);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d(TITLE, 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 18);
            _titleText.Color = Color.Yellow;

            _descText = new Text2d(HELP_TEXT, 10, height, width, height,
                Text2d.TextAlignment.TopLeft, heightInPixels: 15);
            _descText.Color = Color.LightGray;

            _camPosText = new Text2d("카메라 위치 (0,0,0)", width - 10, height, width, height,
                Text2d.TextAlignment.TopRight, heightInPixels: 15);

            _culledText = new Text2d("컬링된 노드 0개", 10, (height * 0.5f), width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _culledText.Color = Color.White;

        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 3D 모델 매니저 및 모델 로드
            _unifiedModel = ObjLoaderEx.LoadObjUnified(PROJECT_PATH + @"FormTools\bin\Debug\Res\tree1.obj");

            _generator = new CrossBillboardAtlasGenerator();
            _billboardData = _generator.GenerateAtlas(_unlitShader, _unifiedModel);
            _renderer = new CrossBillboardRenderer();

            // 3. 나무 인스턴스 설정
            var instances = new List<Renderer.TreeInstance>();
            instances.Add(new Renderer.TreeInstance(new Vertex3f(3, 3, 0), 1.0f));
            instances.Add(new Renderer.TreeInstance(new Vertex3f(3, -3, 0), 1.0f));

            _renderer.SetInstances(instances);

            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
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

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 4. 렌더링 루프에서
            _renderer.Render(_cbShader, camera.VPMatrix, 10, 10, _billboardData.AtlasTexture.TextureID);

            // 2D 렌더링을 위한 상태 설정
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);  // ← 여기서 켜기
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
            Gl.Viewport(0, 0, w, h);

            // FPS 렌더링
            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            _fpsText.Render();
            _titleText.Render();
            _descText.Render();
            _camPosText.Render();

            // 카메라 중심점 렌더링
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

        private void FormBillboardCloud_Load(object sender, EventArgs e)
        {

        }
    }
}
