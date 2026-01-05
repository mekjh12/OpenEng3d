using BillBoard;
using Common;
using Common.Abstractions;
using FastMath;
using Geometry;
using GlWindow;
using Light;
using Lights;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormCrossBillboardLight : Form, GlControlerable
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;
        readonly string TITLE = "CrossBillboard + Lighting";
        private GlControl3 _glControl3;                     // OpenGL 컨트롤

        // 렌더러 변수들
        private WorldAxisRenderer _worldAxisRenderer;       // 월드 축 렌더러

        // 셰이더 변수들
        private ColorShader _colorShader;                   // 컬러 셰이더
        private UnlitShader _unlitShader;                   // 언릿 셰이더
        private ModelNormalShader _modelNormalShader;       // 모델 노멀 셰이더

        private bool _isLoaded = false;                     // 로드 여부
        private bool _isStarted = false;                    // 시작 여부

        // UI 2D 관련 변수들
        private TextNamePlate _textNamePlate;               // 텍스트 네임플레이트
        private Polyhedron _viewFrustum;                    // 뷰 프러스텀
        private Text2d _fpsText;                            // FPS 텍스트
        private Text2d _titleText;                          // 타이틀 텍스트
        private Text2d _descText;                           // 설명 텍스트
        private Text2d _camPosText;                         // 카메라 위치 텍스트   
        private Text2d _culledText;                         // 컬링된 노드 텍스트   

        // 3D 관련 변수들
        UnifiedTexturedModel _unifiedTexturedModel;         // 통합 텍스쳐 모델

        // 라이팅 관련 변수들
        LightingManager _lightingManager;                   // 라이팅 매니저
        SunLight _sunLight;                                 // 태양광

        // 하늘과 구름 관련 변수들
        SkyRenderer _skyRenderer;                           // 하늘 렌더러
        SkyDomeTexture2dShader _skyDomeTexture2DShader;     // 스카이돔 텍스처 2D 셰이더

        // 크로스 빌보드 관련 변수들
        CrossBillboardRenderer _crossBillboardRenderer;     // 크로스 빌보드 렌더러
        CrossBillboardData _crossBillboardData;             // 크로스 빌보드 데이터
        CrossBillboardShader _crossBillboardShader;         // 크로스 빌보드 셰이더
        CrossBillboardAtlasGenerator _crossBillboardAtlasGenerator; // 크로스 빌보드 아틀라스 생성기
        UnifiedModelRenderer _unifiedModelRenderer;         // 통합 모델 렌더러

        // 디버깅 관련 변수들
        bool _isVisibleModel = false;                       // 모델 가시성
        bool _isVisibleNormal = false;                      // 노멀 가시성
        bool _isLighting = true;                            // 라이팅 사용 여부 
        bool _isDebugTextDirty = true;                      // 디버그 텍스트 갱신 여부
        Matrix4x4f _modelMatrix;                            // 모델 행렬

        public FormCrossBillboardLight()
        {
            InitializeComponent();

            // GL 생성
            this.Text = TITLE;
            _glControl3 = new GlControl3(TITLE, Application.StartupPath, @"\fonts\fontList.txt", @"\Res\", useRenderTarget: true);
            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.BlitToScreen = (deltaTime, camera) => BlitToScreen(deltaTime, camera);
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Load += (s, e) => Form_Load(s, e);
            _glControl3.PreviewKeyDown += PreviewKeyDownEvent;
            _glControl3.AutoBlitToScreen = false;
            _glControl3.Start();
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
            ShaderManager.Instance.AddShader(new CrossBillboardShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new UnlitShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new ModelNormalShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _crossBillboardShader = ShaderManager.Instance.GetShader<CrossBillboardShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();
            _modelNormalShader = ShaderManager.Instance.GetShader<ModelNormalShader>();
            
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

            _culledText = new Text2d("태양각", 10, (height * 0.2f), width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _culledText.Color = Color.White;
        }

        public void Init3d(int width, int height)
        {
            // 그리드셰이더 초기화
            _glControl3.InitGridShader(PROJECT_PATH);

            // 렌더러 초기화
            _worldAxisRenderer = new WorldAxisRenderer(PROJECT_PATH);

            // 모델 초기화
            //_unifiedTexturedModel = ObjLoaderEx.LoadObjUnified(PROJECT_PATH + @"FormTools\bin\Debug\Res\chinese-ancient-building-h26.obj");
            _unifiedTexturedModel = ObjLoaderEx.LoadObjUnified(PROJECT_PATH + @"FormTools\bin\Debug\Res\Big_rock1.obj");
            _isLoaded = true;

            // 초기 라이팅 설정 (선택사항)
            _sunLight = new SunLight(0, 20);
            _lightingManager = new LightingManager();
            _lightingManager.Lighting.SunDirection = _sunLight.Direction;
            _lightingManager.Lighting.SunIntensity = 1.5f;
            _lightingManager.SetDirty();

            // 하늘 렌더러 초기화
            _skyDomeTexture2DShader = new SkyDomeTexture2dShader(PROJECT_PATH);
            _skyDomeTexture2DShader.GenerateSkyTexture(_glControl3.Camera.Position, -_sunLight.Direction);
            _skyRenderer = new SkyRenderer(PROJECT_PATH, _skyDomeTexture2DShader);

            // 크로스 빌보드 아틀라스 생성
            _crossBillboardAtlasGenerator = new CrossBillboardAtlasGenerator();
            _crossBillboardData = _crossBillboardAtlasGenerator.GenerateAtlas(_unlitShader, _unifiedTexturedModel);
            _crossBillboardRenderer = new CrossBillboardRenderer(PROJECT_PATH);
            List<TreeInstance> treeInstances = new List<TreeInstance>();
            treeInstances.Add(new TreeInstance(new Vertex3f(-2, -2, 0), 1.0f));
            _crossBillboardRenderer.SetInstances(treeInstances);

            // 통합 모델 렌더러 초기화
            _unifiedModelRenderer = new UnifiedModelRenderer(_unifiedTexturedModel, _unlitShader);
            AABB3f aabb = _unifiedTexturedModel.AABB;
            _modelMatrix = Matrix4x4f.RotatedZ(60);
            _modelMatrix[3, 0] = -2;
            _modelMatrix[3, 1] = -2;

            // UI 3D 텍스트 네임플레이트 초기화
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            // 1회 시작시 초기화
            if (!_isStarted)
            {
                _isStarted = true;
            }

            // 라이팅 매니저 업데이트
            _lightingManager.Update();  // UBO 업데이트

            // 카메라 위치가 변경되었는지 확인
            if (camera.IsCameraFrameMoved)
            {
                // 뷰 프러스텀 업데이트
                _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);
                _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
            }

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;
            if (!_isStarted) return;

            // 배경 타겟의 컬러 버퍼 초기화
            Gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // 하늘 렌더링
            _skyRenderer.RenderSkyDome(camera);
            Gl.PolygonMode(MaterialFace.FrontAndBack, _glControl3.PolygonMode);

            if (_isVisibleModel)
            {
                // 통합 모델 렌더링
                _unifiedModelRenderer.Render(camera.VPMatrix * _modelMatrix, camera.ViewMatrix * _modelMatrix, _isLighting);

                if (_isVisibleNormal)
                {
                    _modelNormalShader.Bind();
                    {
                        _modelNormalShader.LoadTransforms(
                             camera.ProjectiveMatrix,
                             camera.ViewMatrix,
                             _modelMatrix
                         );
                        _modelNormalShader.LoadNormalLength(0.2f);
                        _modelNormalShader.LoadNormalColor(0.0f, 0.0f, 0.0f);
                        Gl.LineWidth(1.0f);

                        // 같은 VAO 사용
                        Gl.BindVertexArray(_unifiedTexturedModel.VaoID);
                        Gl.DrawElements(PrimitiveType.Triangles, _unifiedTexturedModel.IndexCount,
                                        DrawElementsType.UnsignedInt, IntPtr.Zero);
                    }
                    _modelNormalShader.Unbind();
                }
            }
            else
            {
                // 크로스 빌보드 렌더링
                _crossBillboardRenderer.Render(_crossBillboardShader,
                    _crossBillboardData.AtlasTexture.TextureID, 
                    _crossBillboardData.NormalTexture.TextureID,
                    _unifiedTexturedModel.AABB);

            }

            // 월드 축 렌더링
            _worldAxisRenderer.Render(camera.VPMatrix);
        }

        private void BlitToScreen(int deltaTime, Camera camera)
        {
            if (!_isLoaded) return;
            if (!_isStarted) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 최종 화면 출력
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 안개 없이 그냥 출력
            _glControl3.BlitRenderTargetToScreen();

            // ---------------------------------------
            // 2D UI 렌더링
            // ---------------------------------------
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
            Gl.Viewport(0, 0, w, h);

            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //_textNamePlate.Render();
            _fpsText.Render();
            _titleText.Render();
            _descText.Render();
            _camPosText.Render();
            _culledText.Render();

            Gl.Disable(EnableCap.Blend);
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void PreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    e.IsInputKey = true;
                    break;
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            float deltaTheta = 3;
            bool needUpdate = false;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    _sunLight.SetDeltaAzimuth(deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Right:
                    _sunLight.SetDeltaAzimuth(-deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Up:
                    _sunLight.SetDeltaElevation(deltaTheta);
                    needUpdate = true;
                    break;

                case Keys.Down:
                    _sunLight.SetDeltaElevation(-deltaTheta);
                    needUpdate = true;
                    break;
            }

            if (needUpdate)
            {
                _lightingManager.Lighting.SunDirection = _sunLight.Direction;
                _lightingManager.SetDirty();
                _skyDomeTexture2DShader.GenerateSkyTexture(_glControl3.Camera.Position, -_sunLight.Direction);
                _culledText.Text = $"태양각: 방위각{_sunLight.Azimuth}도, 고도각{_sunLight.Elevation}도 {_sunLight.Direction}";
            }
        }

        // --------------------------------
        // 도움말 텍스트
        // --------------------------------
        readonly string HELP_TEXT =
            "0번키: 원점으로\n" +
            "1번키: 안개\n" +
            "2번키: G버퍼\n" +
            "L번키: 광원\n" +
            "화살표: 태양각 조절\n";

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D0)
            {
                _glControl3.Camera.PivotPosition = Vertex3f.Zero;
            }
            else if (e.KeyCode == Keys.D1)
            {
                _isVisibleModel = !_isVisibleModel;
            }
            else if (e.KeyCode == Keys.L)
            {
                _isLighting = !_isLighting;
            }
            else if (e.KeyCode == Keys.N)
            {
                _isVisibleNormal = !_isVisibleNormal;
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

        public void Form_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(1280, 800);
            this.Location = new Point(500, 100);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Resize += new EventHandler(this.FormGPUDriven_Resize);
            MemoryProfiler.StartFrameMonitoring();
        }

        public void InitFinished()
        {
            throw new NotImplementedException();
        }
    }
}
