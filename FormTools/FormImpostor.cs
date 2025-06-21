using Camera3d;
using Common.Abstractions;
using FormTools.Properties;
using Geometry;
using GlWindow;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    /// <summary>
    /// 3D 렌더링을 수행하는 Windows Form 클래스
    /// OpenGL을 사용하여 3D 그래픽스를 처리하며 IRenderer 인터페이스를 구현하여 렌더링 기능 제공
    /// </summary>
    public partial class FormImpostor : Form, IRenderer
    {
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
        OcclusionCullingSystem _ocs;           // 가시성 컬링 시스템
        ImpostorLODSystem _impostorLODSystem;  // LOD 기반 임포스터 시스템

        uint _guid = 0;

        // 폼 생성자
        public FormImpostor()
        {
            InitializeComponent();
            InitializeGlControl();
        }

        // 프로젝트 초기화
        public void Initialize(string projectPath)
        {
            try
            {
                // 랜덤 시드 및 쉐이더 초기화
                Rand.InitSeed(RANDOM_SEED);
                _colorShader = new ColorShader(Resources.PROJECT_PATH);
                _impostorShader = new ImpostorShader(Resources.PROJECT_PATH);
                _unlitShader = new UnlitShader(Resources.PROJECT_PATH);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // OpenGL 컨트롤 초기화 및 설정
        public void InitializeGlControl()
        {
            // OpenGL 컨트롤 기본 설정
            _glControl3 = new GlControl3(Name, Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,                    // 3D 그리드 표시 활성화
                PolygonMode = PolygonMode.Fill,         // 객체를 실체형으로 렌더링
                BackClearColor = new Vertex3f(0, 0, 0), // 배경색 검정으로 설정
                IsVisibleUi2d = true,                   // 2D UI 디버그 정보 표시
            };

            // 이벤트 핸들러 연결
            _glControl3.Init += (w, h) => Initialize(Resources.PROJECT_PATH);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) =>
                UpdateFrame(deltaTime * 0.001f, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) =>
                RenderFrame(deltaTime * 0.001f, (int)w, (int)h, backcolor, camera);

            // 입력 이벤트 설정
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);

            // 컨트롤 활성화
            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);  // 마우스 커서 표시
            Controls.Add(_glControl3);          // 폼에 컨트롤 추가
        }

        // 2D UI 초기화
        public void Init2d(int w, int h)
        {
            // 디버그 정보 레이블 생성
            _glControl3.AddLabel("sun", $"sun=0",
                align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM,
                foreColor: new Vertex3f(1, 0, 0));  // 태양 정보 (적색)
            _glControl3.AddLabel("cam", "camera position, yaw, pitch",
                align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL,
                foreColor: new Vertex3f(1, 1, 0));  // 카메라 정보 (황색)
            _glControl3.AddLabel("ocs", "ocs",
                align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP,
                foreColor: new Vertex3f(1, 1, 0));  // 좌표계 정보 (황색)

            // 설정 파일에서 UI 옵션 로드
            _glControl3.IsVisibleDebug = bool.Parse(
                IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(
                IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // 디스플레이 설정 확인
            Debug.PrintLine($"{Screen.PrimaryScreen.DeviceName} {w}x{h}");
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        // 3D 환경 초기화
        public void Init3d(int w, int h)
        {
            // 그리드 및 카메라 설정
            _glControl3.InitGridShader(Resources.PROJECT_PATH);
            _glControl3.Camera.FAR = FAR_PLANE;
            _glControl3.Camera.NEAR = NEAR_PLANE;
            _glControl3.Camera.Position = new Vertex3f(0, 0, 1);

            // 가시성 컬링 시스템 초기화
            _ocs = new OcclusionCullingSystem(Resources.PROJECT_PATH);
            //_ocs.AddRawModel(@"FormTools\bin\Debug\Res\Watermill.obj");

            // 임포스터 LOD 시스템 초기화
            _impostorLODSystem = new ImpostorLODSystem(20.0f);

            string modelName = "Medieval_House";
            _ocs.AddRawModel(@"FormTools\bin\Debug\Res\" + modelName + ".obj");
            _impostorLODSystem.CreateImpostorModel(modelName, ImpostorSettings.CreateSettings(256, 16, 8), _unlitShader, _ocs.GetModels(modelName));
            _ocs.AddEntity(entityName: modelName, modelName: modelName, lowModelName: modelName, new Vertex3f(33, -33, 30),
                yaw: 0, size: new Vertex3f(1, 1, 1));
            
            // 나무 객체 생성 및 배치
            //_ocs.AddEntity(entityName: $"Watermill0", modelName: "tree1", new Vertex3f(10, 10, 0), yaw: -40, pitch: 0);
            //_ocs.AddEntity(entityName: $"Watermill0", modelName: "tree1", new Vertex3f(0, 10, 0), yaw: 40, pitch: 0);
            //_ocs.AddEntity(entityName: $"Watermill0", modelName: "Watermill", new Vertex3f(10, 0, 0), yaw: 0, pitch: 0);

            int num = 0;
            for (int i = -num; i <= num; i++)
            {
                for (int j = -num; j <= num; j++)
                {
                    //_ocs.AddEntity(entityName: $"Watermill{i}x{j}", modelName: "Watermill", new Vertex3f(50 * i, 50 * j, 0), yaw: Rand.NextAngle360, pitch: 0).IsAxisVisible = true;
                    if (Rand.NextInt(0, 2) == 0)
                    {
                    }
                    else
                    {
                        //_ocs.AddEntity(entityName: $"tree1{i}x{j}", modelName: "tree1", new Vertex3f(50 * i, 50 * j, 0), roll: Rand.NextAngle360);
                    }
                }
            }
        }

        // 프레임 업데이트
        public void UpdateFrame(float duration, int w, int h, Camera camera)
        {
            OrbitCamera obitcam = camera as OrbitCamera;

            // 시야 프러스텀 계산
            float g = 1.0f / (float)Math.Tan((obitcam.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(
                obitcam.Position,
                obitcam.Forward,
                obitcam.Up,
                obitcam.Right,
                g,
                obitcam.AspectRatio,
                obitcam.NEAR,
                obitcam.FAR);

            // 가시성 업데이트
            _ocs.Update(obitcam, viewFrustum);
            _ocs.UpdateVisibleEntitiesFromCulledTree(obitcam);

            // UI 정보 갱신
            _glControl3.CLabel("sun").Text = $"sun=";
            _glControl3.CLabel("ocs").Text = $"ocs={_ocs.FrustumPassEntity.Count}";
            _glControl3.CLabel("cam").Text =
                $"CamPos={obitcam.Position}," +
                $"CameraPitch={obitcam.CameraPitch}," +
                $"CameraYaw={obitcam.CameraYaw}, " +
                $"Dist={obitcam.Distance}";
        }

        // 프레임 렌더링
        public void RenderFrame(float duration, int w, int h, Vertex4f backcolor, Camera camera)
        {
            OrbitCamera orbCamera = camera as OrbitCamera;

            // OpenGL 렌더링 상태 설정
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.ClearColor(backcolor.x, backcolor.y, backcolor.z, 1.0f);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 임포스터 및 일반 객체 분류
            List<Entity> impostorEntities = new List<Entity>();
            List<Entity> unlitEntities = new List<Entity>();

            // LOD 기반 렌더링 대상 분류
            foreach (LodEntity entity in _ocs.FrustumPassEntity)
            {
                bool shouldUseImpostor = entity.ShouldUseImpostor;// _impostorLODSystem.ShouldUseImpostor(entity, orbCamera.Position, orbCamera.VPMatrix);
                entity.IsDrawOneSide = true;
                (shouldUseImpostor ? impostorEntities : unlitEntities).Add(entity);
            }

            // 객체 렌더링
            Renderer3d.Render(_unlitShader, unlitEntities, orbCamera, isCullface: true);
            Renderer3d.Render(_impostorLODSystem, _impostorShader, impostorEntities, orbCamera);
            //Renderer3d.RenderLocalAxis(_colorShader, unlitEntities, orbCamera);

            foreach (Entity occlusionEntity in impostorEntities)
            {
                //Renderer3d.RenderAABB(_colorShader, occlusionEntity.AABB, occlusionEntity.Color.Vertex4f(0.3f), orbCamera);
                //Renderer3d.RenderOBB(_colorShader, occlusionEntity.OBB, occlusionEntity.Color.Vertex4f(0.3f), orbCamera);
            }

            foreach (Entity occlusionEntity in unlitEntities)
            {
                //Renderer3d.RenderAABB(_colorShader, occlusionEntity.AABB, occlusionEntity.Color.Vertex4f(0.3f), orbCamera);
                //Renderer3d.RenderOBB(_colorShader, occlusionEntity.OBB, occlusionEntity.Color.Vertex4f(0.3f), orbCamera);
            }

            // 디버그 요소 렌더링
            Renderer3d.RenderPoint(_colorShader, camera.PivotPosition, camera, new Vertex4f(1, 1, 0, 1), 0.02f); // 카메라 위치 표시 (황색)
            Gl.Enable(EnableCap.DepthTest); // 깊이 테스트 활성화
        }

        // 마우스 버튼 누름 이벤트 처리
        public void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }

            if (e.Button == MouseButtons.Left)
            {
                _ocs.AddEntity(entityName: $"Big_rock{_guid}", modelName: $"Big_rock1", lowModelName: "Big_rock1_lod1",
                    _glControl3.Camera.PivotPosition, yaw: Rand.NextAngle360, size: Vertex3f.One * (Rand.NextFloat2 * 0.2f + 1.0f));
                _guid++;
            }
        }

        // 키보드 입력 이벤트 처리
        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            // 키보드 입력 처리 로직 추가 예정
            if (e.KeyCode == Keys.D1)
            {
                _glControl3.Camera.PivotPosition = Vertex3f.Zero;
            }
            if (e.KeyCode == Keys.D2)
            {                
                
            }
        }

        // 마우스 버튼 해제 이벤트 처리
        public void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        // 폼 로드 이벤트 처리
        private void FormImpostor_Load(object sender, EventArgs e)
        {
            // 폼 로드 시 초기화 작업 추가 예정
        }
    }
}