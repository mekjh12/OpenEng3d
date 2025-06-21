using Camera3d;
using Common.Abstractions;
using Common.Geometry;
using Geometry;
using GlWindow;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using Sky;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Terrain;
using ZetaExt;

namespace FormTools
{
    public partial class FormTerrainImposter : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private string EXE_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\";

        private OcclusionCullingSystem _ocs;
        ImpostorLODSystem _impostorLODSystem;  // LOD 기반 임포스터 시스템

        private GlControl3 _glControl3;

        ColorShader _colorShader;
        StaticShader _staticShader;
        SimpleDepthShader _simpleDepthShader;
        UnlitShader _unlitShader;       // 비발광 객체 렌더링용 쉐이더
        ImpostorShader _impostorShader; // 임포스터 렌더링용 쉐이더

        TerrainCluster _terrainCluster;
        TerrainMap _terrainMap;

        public FormTerrainImposter()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("hzb occlusion", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
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
            //_glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
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
            _simpleDepthShader = new SimpleDepthShader(PROJECT_PATH);
            _unlitShader = new UnlitShader(PROJECT_PATH);
            _impostorShader = new ImpostorShader(PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            _glControl3.AddValueBar("LOD", value: 0, minValue: 0, maxValue: 10, backColor: new Vertex3f(0.6f, 0.3f, 0)).StepValue = 1;
            _glControl3.AddLabel("resolution", $"resolution={w}x{h}", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM, foreColor: new Vertex3f(1, 0, 0));
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
            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);

            // terrain init
            string heightMap = EXE_PATH + @"\Res\Terrain\209147.png";
            string[] levelTextureMap = new string[5];
            levelTextureMap[0] = EXE_PATH + @"\Res\Terrain\water1.png";
            levelTextureMap[1] = EXE_PATH + @"\Res\Terrain\grass_1.png";
            levelTextureMap[2] = EXE_PATH + @"\Res\Terrain\lowestTile.png";
            levelTextureMap[3] = EXE_PATH + @"\Res\Terrain\HighTile.png";
            levelTextureMap[4] = EXE_PATH + @"\Res\Terrain\highestTile.png";
            string detailMap = EXE_PATH + @"\Res\Terrain\detailMap.png";
            _terrainMap = new TerrainMap();
            _terrainMap.LoadMap(heightMap, levelTextureMap, detailMap, 20, 50);

            _terrainCluster = new TerrainCluster(PROJECT_PATH);
            _terrainCluster.BakeEntity(_terrainMap, 10, 10);

            // OCS init
            _ocs = new OcclusionCullingSystem(PROJECT_PATH);
            //_ocs.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");
            //_ocs.AddRawModel(@"FormTools\bin\Debug\Res\Palm1.obj");

            // 임포스터 LOD 시스템 초기화
            _impostorLODSystem = new ImpostorLODSystem(100.0f);
            //_impostorLODSystem.CreateImpostorModel("tree1", ImpostorSettings.CreateSettings(128, 16), _unlitShader, _ocs.GetModels("tree1"));
            //_impostorLODSystem.CreateImpostorModel("Palm1", ImpostorSettings.CreateSettings(128, 16), _unlitShader, _ocs.GetModels("Palm1"));


            // 먼저 다 담은 후에 랜덤하게 인서트하면 효율성 증대 기대
            TreeBalanceInfo info = BVHBalanceAnalyzer.AnalyzeBalance(_ocs.BoundVolumeHierachy);
            Console.WriteLine(BVHBalanceAnalyzer.GetBalanceReport(info));
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

            // 시야 절두체 생성
            float g = 1.0f / (float)Math.Tan((camera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera.Position,
                camera.Forward, camera.Up, camera.Right, g, camera.AspectRatio, camera.NEAR, camera.FAR);

            // 대기 업데이트
            //_atmosphere.Update(deltaTime);

            // 오클루전 컬링 시스템 업데이트
            _ocs.Update(camera, viewFrustum, fogPlane: new Vertex4f(0, 0, 1, 0));
            //List<OccluderEntity> occluders = _ocs.GetLargeOccludersInViewFrustum(orbCamera.VPMatrix, areaThreshold: 0.05f);

            // 지형패치로부터 뷰프러스텀 컬링 업데이트
            _terrainCluster.Update(camera);

            // UI 정보 업데이트
            _glControl3.CLabel("ocs").Text = $"Total={_ocs.EntityTotalCount}, TerrainPassEntity=, PassEntity={_ocs.FrustumPassEntity.Count}";
            _glControl3.CLabel("cam").Text = $"CamPos={camera.Position},CameraPitch={camera.CameraPitch},CameraYaw={camera.CameraYaw}, Dist={camera.Distance}";
        }

        /// <summary>
        /// 씬을 렌더링합니다.
        /// </summary>
        /// <param name="deltaTime">이전 프레임과의 시간 간격 (밀리초)</param>
        /// <param name="camera">카메라</param>
        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            // 백그라운드 컬러 설정
            float r = _glControl3.BackClearColor.x;
            float g = _glControl3.BackClearColor.y;
            float b = _glControl3.BackClearColor.z;

            // 현재 LOD 레벨 계산
            float level = (float)_glControl3.SimpHValueBar("LOD").Value;

            // 기본 프레임버퍼로 전환 및 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 지형 렌더링
            _terrainCluster.Render(_terrainMap, camera);

            // UI 정보 업데이트
            _glControl3.CLabel("resolution").Text = $"resolution=" + " v=" + _ocs.OccludedEntity.Count;

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
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

        private void FormTerrain_Load(object sender, EventArgs e)
        {
            Console.WriteLine("FormLoad!");
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            
        }

        private void FormTerrainImposter_Load(object sender, EventArgs e)
        {

        }
    }
}
