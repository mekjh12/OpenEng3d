using Camera3d;
using Common;
using Common.Abstractions;
using FastMath;
using FormTools.Properties;
using Geometry;
using GlWindow;
using GPUDriven;
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
    /// <summary>
    /// 3D 렌더링을 수행하는 Windows Form 클래스
    /// OpenGL을 사용하여 3D 그래픽스를 처리하며 IRenderer 인터페이스를 구현하여 렌더링 기능 제공
    /// </summary>
    public partial class FormImpostor : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string ExE_PATH = Application.StartupPath;

        // 시뮬레이션 및 렌더링 설정을 위한 상수
        private const int RANDOM_SEED = 500;    // 시뮬레이션의 일관성을 위한 랜덤 시드값
        private const float FAR_PLANE = 20000f; // 원거리 시야 제한 평면 거리
        private const float NEAR_PLANE = 1f;    // 근거리 시야 제한 평면 거리

        // 렌더링 관련 핵심 컴포넌트
        private GlControl3 _glControl3;         // 3D 그래픽스 처리를 위한 OpenGL 컨트롤
        private ColorShader _colorShader;       // 단색 객체 렌더링용 쉐이더
        private ImpostorShader _impostorShader; // 임포스터 렌더링용 쉐이더
        private UnlitShader _unlitShader;       // 비발광 객체 렌더링용 쉐이더

        // 최적화 시스템
        OcclusionCullingSystem _ocs;            // 가시성 컬링 시스템
        ImpostorLODSystem _impostor;            // LOD 기반 임포스터 시스템

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
        GPUDriven.AABB _aabb;

        // 폼 생성자
        public FormImpostor()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("임포스터", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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

        public void Init(int width, int height)
        {
            // 난수 초기화 및 수학 라이브러리 초기화
            Rand.InitSeed(500);
            MathFast.Initialize();

            // 쉐이더 초기화 및 셰이더 매니저에 추가
            ShaderManager.Instance.AddShader(new ColorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new ImpostorShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new UnlitShader(PROJECT_PATH));

            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _impostorShader = ShaderManager.Instance.GetShader<ImpostorShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 20);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d("임포스터", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _titleText.Color = Color.Red;

            _descText = new Text2d("1번키: 원점이동", 10, height, width, height,
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

            // 임포스터 LOD 시스템 초기화
            _impostor = new ImpostorLODSystem(20.0f);

            // 3D 모델 매니저 초기화 및 나무 모델 로드
            _model3DManager = new Model3dManager(PROJECT_PATH, ExE_PATH + "\\nullTexture.jpg");
            _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm1.obj");
            _treeModel = _model3DManager.GetModels("Palm1");
            _impostor.CreateImpostorModel("Palm1", ImpostorSettings.CreateSettings(256, 16, 8),
                _unlitShader, _treeModel);

            
            _aabb = new GPUDriven.AABB(new Vertex3f(float.MaxValue), new Vertex3f(float.MinValue));
            for (int i = 0; i < _treeModel.Length; i++)
            {
                Vertex3f min = new Vertex3f(float.MaxValue);
                Vertex3f max = new Vertex3f(float.MinValue);
                Vertex3f[] vertices = _treeModel[i].Vertices;

                for (int j = 0; j < vertices.Length; j++)
                {
                    Vertex3f pos = vertices[j];
                    min = Vertex3f.Min(min, pos);
                    max = Vertex3f.Max(max, pos);
                }
                _aabb.Min = Vertex3f.Min(_aabb.Min, min);
                _aabb.Max = Vertex3f.Max(_aabb.Max, max);
            }

            _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\Palm5.obj");
            _treeModel = _model3DManager.GetModels("Palm5");
            _impostor.CreateImpostorModel("Palm5", ImpostorSettings.CreateSettings(256, 16, 8),
                _unlitShader, _treeModel);

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            //if (!_isLoaded) return;

            // 뷰 프러스텀 업데이트
            _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

            // 네임플레이트 업데이트            
            _textNamePlate.Text = $"가시객체";
            _textNamePlate.WorldPosition = camera.Position + camera.Forward * 1f - camera.Right * 0.2f;
            _textNamePlate.Update(deltaTime);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
            _culledText.Text = $"컬링된 노드";
            _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            //if (!_isLoaded) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            Gl.Disable(EnableCap.Blend);

            _impostorShader.Bind();
            _impostorShader.LoadEnableEdgeLine(true);
            _impostorShader.LoadVPMatrix(camera.VPMatrix);
            _impostorShader.LoadCameraPosition(camera.Position);

            ImpostorSettings settings = _impostor.GetImpostorSettings("Palm1");
            Vertex2f atlasOffset = _impostor.GetAtlasOffset(settings, camera.Position, Matrix4x4f.Identity);
            uint textureId = _impostor.AtlasTexture("Palm1");

            _impostorShader.LoadImpostorAtlas(TextureUnit.Texture0, textureId);
            _impostorShader.LoadAtlasOffset(atlasOffset);
            _impostorShader.LoadWorldPosition(Vertex3f.Zero);
            _impostorShader.LoadModelMatrix(Matrix4x4f.Identity);
            _impostorShader.LoadAtlasSize(settings.AtlasSize);
            _impostorShader.LoadIndividualSize(settings.IndividualSize);
            _impostorShader.LoadAABBSizeModel(_aabb.SphereRadius);
            _impostorShader.LoadAABBCenterEntity( _aabb.Center);

            Gl.BindVertexArray(Renderer3d.Point.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            _impostorShader.Unbind();

            Gl.Enable(EnableCap.Blend);



            // 2D 렌더링을 위한 상태 설정
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);  // ← 여기서 켜기
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
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
            Gl.Disable(EnableCap.Blend);
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void Form_Load(object sender, EventArgs e)
        {

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

        private void FormImpostor_Resize(object sender, EventArgs e)
        {
            int width = _glControl3.Width;
            int height = _glControl3.Height;
        }
    }
}