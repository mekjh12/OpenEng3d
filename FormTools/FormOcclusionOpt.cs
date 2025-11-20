using Common;
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
using System.Drawing;
using System.Windows.Forms;
using Terrain;
using Ui3d;
using ZetaExt;

namespace FormTools
{
    public partial class FormOcclusionOpt : Form, GlControlerable
    {
        int _level = 0;                             // 현재 Z 버퍼 레벨
        const int DOWN_LEVEL = 2;                   // 다운샘플링 레벨
        bool _isDepthZBuffer = false;               // 깊이 Z-버퍼 표시 여부
        bool _isViewFrustum = false;                // 뷰 프러스텀 표시 여부
        bool _isAABBDepth = true;                   // AABB 깊이 표시 여부

        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        readonly string EXE_PATH = Application.StartupPath;

        private GlControl3 _glControl3;                     // OpenGL 컨트롤
        private bool _isLoaded = false;                     // 로드 여부

        private ColorShader _colorShader;                   // 컬러 셰이더
        private HzmDepthShader _hzmDepthShader;             // HZM 깊이 셰이더
        private TerrainTessellationShader _terrainShader;   // 지형 테셀레이션 셰이더
        private AABBBoxShader _aabbBoxShader;               // AABB 박스 셰이더
        private AABBDepthShader _aabbDepthShader;           // AABB 깊이 셰이더
        private UnlitShader _unlitShader;                   // 비발광 셰이더      

        private TextNamePlate _textNamePlate;               // 텍스트 네임플레이트
        private Polyhedron _viewFrustum;                    // 뷰 프러스텀
        private Text2d _fpsText;                            // FPS 텍스트
        private Text2d _titleText;                          // 타이틀 텍스트
        private Text2d _descText;                           // 설명 텍스트
        private Text2d _camPosText;                         // 카메라 위치 텍스트   
        private Text2d _culledText;                         // 컬링된 노드 텍스트   

        private QuadTree3f _quadTree;

        HierarchyZBuffer _hzbuffer;                 // 계층적 GPU Z 버퍼
        TerrainRegion _terrainRegion;               // 지형 영역
        Texture[] _levelTextureMap = null;          // 지형 레벨 텍스쳐
        Texture _detailTextureMap = null;           // 지형 디테일 텍스쳐

        RawModel3d _treeRawModel;                   // 나무 로우 모델
        TexturedModel[] _treeModel;                 // 나무 모델
        Entity _entity;                             // 나무 엔티티   
        Model3dManager _model3DManager;             // 3D 모델 매니저

        public FormOcclusionOpt()
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

        public void Form_Load(object sender, EventArgs e)
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
            ShaderManager.Instance.AddShader(new TerrainTessellationShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new UnlitShader(PROJECT_PATH));
            ShaderManager.Instance.AddShader(new AABBDepthShader(PROJECT_PATH));
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();
            _hzmDepthShader = ShaderManager.Instance.GetShader<HzmDepthShader>();
            _aabbBoxShader = ShaderManager.Instance.GetShader<AABBBoxShader>();
            _terrainShader = ShaderManager.Instance.GetShader<TerrainTessellationShader>();
            _unlitShader = ShaderManager.Instance.GetShader<UnlitShader>();
            _aabbDepthShader = ShaderManager.Instance.GetShader<AABBDepthShader>();

            // 앱 시작 시 한 번만 초기화
            Ui3d.BillboardShader.Initialize();
        }

        public void Init2d(int width, int height)
        {
            _fpsText = new Text2d("FPS: 60.0", width / 2, 10, width, height,
                Text2d.TextAlignment.Center, heightInPixels: 20);
            _fpsText.Color = Color.Yellow;

            _titleText = new Text2d("쿼드트리탐색 최적화", 10, 10, width, height,
                Text2d.TextAlignment.Left, heightInPixels: 15);
            _titleText.Color = Color.Red;

            _descText = new Text2d("1,2번키: Z버퍼 레벨 변경, 3번키: Z버퍼 On/Off", 10, height, width, height,
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

            // UI 3D 텍스트 네임플레이트 초기화
            _textNamePlate = new TextNamePlate(_glControl3.Camera, "FPS");
            _textNamePlate.Height = 0.35f;
            _textNamePlate.Width = 0.35f;
            CharacterTextureAtlas.Initialize();
            TextBillboardShader.Initialize();

            _model3DManager = new Model3dManager(PROJECT_PATH, EXE_PATH + "\\nullTexture.jpg");
            _model3DManager.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");
            _treeModel = _model3DManager.GetModels("tree1");
            _entity = new Entity("tree1_entity", "tree1", _treeModel);

            AABB3f worldBound = new AABB3f(new Vertex3f(-3000, -3000, 0), new Vertex3f(3000, 3000, 10));
            _quadTree = new QuadTree3f(worldBound);

            // 지형 영역 초기화
            RegionCoord regionCoord = new RegionCoord(0, 0);
            _terrainRegion = new TerrainRegion(regionCoord, chunkSize: 100, n: 10, null);
            _terrainRegion.LoadTerrainLowResMap(regionCoord, EXE_PATH + "\\Res\\Terrain\\low\\region0x0.png",
                completed: () =>
                {
                    int idx = 0;
                    int numBoxes = 150;

                    /*
                    for (int i = 0; i < numBoxes; i++)
                    {
                        Vertex3f center = Rand.NextVector3f * 1000;
                        _terrainRegion.TerrainData.GetTerrainHeightVertex3f(ref center);
                        Vertex3f halfSize = Rand.NextVector3f * 5f + Vertex3f.One * 10.0f;
                        AABB3f aabb = new AABB3f(center - halfSize, center + halfSize);
                        _quadTree.Insert(aabb, idx, center);
                        idx++;
                    }

                    _isLoaded = true;
                    _quadTree.PrintStatistics();
                    return;
                    
                    */

                    for (int i = -numBoxes; i < numBoxes; i++)
                    {
                        for (int j = -numBoxes; j < numBoxes; j++)
                        {
                            Vertex3f center = new Vertex3f(i * 30, j * 30, 0);
                            _terrainRegion.TerrainData.GetTerrainHeightVertex3f(ref center);
                            center.z += 5.0f;
                            Vertex3f halfSize = Rand.NextVector3f * 1f + Vertex3f.One * 5.0f;
                            AABB3f aabb = new AABB3f(center - halfSize, center + halfSize);
                            _quadTree.Insert(aabb, idx, center);
                            idx++;
                        }
                    }
                    _isLoaded = true;
                    _quadTree.PrintStatistics();
                });


            // 지형 레벨 텍스쳐 로딩
            string heightMap = PROJECT_PATH + @"FormTools\bin\Debug\Res\Terrain\";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EXE_PATH + @"\Res\Terrain\blend\water1.png";
            levelTextureMap[1] = EXE_PATH + @"\Res\Terrain\blend\grass_1.png";
            levelTextureMap[2] = EXE_PATH + @"\Res\Terrain\blend\lowestTile.png";
            levelTextureMap[3] = EXE_PATH + @"\Res\Terrain\blend\HighTile.png";
            levelTextureMap[4] = EXE_PATH + @"\Res\Terrain\blend\highestTile.png";
            string detailMap = EXE_PATH + @"\Res\Terrain\blend\detailMap.png";
            _levelTextureMap = new Texture[levelTextureMap.Length];
            _detailTextureMap = new Texture(detailMap);
            for (int i = 0; i < _levelTextureMap.Length; i++)
            {
                _levelTextureMap[i] = new Texture(levelTextureMap[i]);
            }

            // 계층적깊이버퍼 생성
            _hzbuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);

            // 셰리더 해시정보는 파일로 저장
            FileHashManager.SaveHashes();
        }

        public void UpdateFrame(int deltaTime, int width, int height, Camera camera)
        {
            float duration = deltaTime * 0.001f;
            if (!_isLoaded) return;

            _viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera, scaled: 0.5f);

            _quadTree.Clear();
            _quadTree.CullingTestByViewFrustum(_viewFrustum, false);
            _quadTree.CullTestFinish(camera.Position);

            // ✅ HZB 업데이트
            _hzbuffer.BindFramebuffer();
            _hzbuffer.PrepareRenderSurface();

            _hzbuffer.RenderSimpleTerrain(camera.ProjectiveMatrix, camera.ViewMatrix, TerrainConstants.DEFAULT_VERTICAL_SCALE, _terrainRegion.TerrainEntity);

            if (_isAABBDepth)
                _aabbDepthShader.RenderAABBDepth(in _quadTree.VisibleObjectsLod0, _quadTree.IndexLod0, camera);

            //Renderer3d.RenderAABBGeometry(_aabbBoxShader, in _quadTree.VisibleObjectsLod0, _quadTree.IndexLod0, camera);

            _hzbuffer.UnbindFramebuffer();

            // ✅ 밉맵 생성
            _hzbuffer.GenerateMipmapsUsingFragment();

            _quadTree.CullingTestByHiZBuffer(camera, camera.VPMatrix, camera.ViewMatrix, _hzbuffer, true);

            // 네임플레이트 업데이트            
            _textNamePlate.Text = $"뷰컬링노드 {_quadTree.VisibleObjectCount}개";
            _textNamePlate.WorldPosition = camera.Position + camera.Forward * 1f - camera.Right * 0.2f;
            _textNamePlate.Update(deltaTime);

            // 렌더링 루프에서
            _fpsText.Text = $"FPS: {FramePerSecond.FPS:F1}";
            _culledText.Text = $"컬링된 노드 {_quadTree.VisibleObjectCount}개/{_quadTree.TotalCountObjects}개";
            _camPosText.Text = $"카메라 위치 ({camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1})";
        }

        public void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera)
        {
            if (!_isLoaded) return;

            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            if (_isDepthZBuffer)
            {
                // 계층적 Z-버퍼 렌더링
                Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                _hzbuffer.RenderDepthBuffer(_hzmDepthShader, camera, level: _level);
            }
            else
            {
                // 일반 렌더링 화면
                Renderer3d.RenderByTerrainTessellationShader(_terrainShader, _terrainRegion.TerrainEntity, camera, _levelTextureMap, _detailTextureMap,
                    isDetailMap: true,
                    lightDirection: Vertex3f.UnitZ,
                    vegetationMap: 0,
                    heightScale: TerrainConstants.DEFAULT_VERTICAL_SCALE
                    );

                // AABB 박스 렌더링
                Renderer3d.RenderAABBGeometry(_aabbBoxShader, in _quadTree.VisibleObjects, _quadTree.VisibleObjectCount, camera);

            }

            // 2D 렌더링을 위한 완전한 상태 리셋
            Gl.Disable(EnableCap.DepthTest);           // 깊이 테스트 끄기
            Gl.Enable(EnableCap.Blend);                // 블렌딩 켜기
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);            // 컬링 끄기
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
                _level = Math.Max(0, _level - 1);
                _titleText.Text = $"해상도={_hzbuffer.Width}x{_hzbuffer.Height} 레벨{_level}/{_hzbuffer.Levels - 1}";
            }
            else if (e.KeyCode == Keys.D2)
            {
                _level = Math.Min(_hzbuffer.Levels - 1, _level + 1);
                _titleText.Text = $"해상도={_hzbuffer.Width}x{_hzbuffer.Height} 레벨{_level}/{_hzbuffer.Levels - 1}";
            }
            else if (e.KeyCode == Keys.D3)
            {
                _isDepthZBuffer = !_isDepthZBuffer;
            }
            else if (e.KeyCode == Keys.D4)
            {
                _isViewFrustum = !_isViewFrustum;
            }
            else if (e.KeyCode == Keys.D5)
            {
                _isAABBDepth = !_isAABBDepth;
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

        private void FormOcclusionOpt_Resize(object sender, EventArgs e)
        {
            int width = _glControl3.Width;
            int height = _glControl3.Height;
            _hzbuffer = new HierarchyZBuffer(width >> DOWN_LEVEL, height >> DOWN_LEVEL, PROJECT_PATH);
        }
    }
}
