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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormAsyncTest : Form, IRenderer
    {
        private const int RANDOM_SEED = 500;
        private const float FAR_PLANE = 10000f;
        private const float NEAR_PLANE = 1f;

        private GlControl3 _glControl3;
        private ColorShader _colorShader;
        private UnlitShader _unlitShader;

        OcclusionCullingSystem _ocs;
        //EntityBatchProcessor _batchProcessor;
        private Task _loadingTask;
        Entity _fern;

        private uint _textureId;  // 로드한 텍스처 ID

        public FormAsyncTest()
        {
            InitializeComponent();
            InitializeGlControl();
            this.KeyPreview = true;  // 폼이 모든 키 이벤트를 먼저 받도록 설정
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // 방향키를 사용하기 위하여 꼭 필요함.
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down) return false;
            return base.ProcessDialogKey(keyData);
        }

        public void Initialize(string projectPath)
        {
            try
            {
                Rand.InitSeed(RANDOM_SEED);
                _colorShader = new ColorShader(Resources.PROJECT_PATH);
                _unlitShader = new UnlitShader(Resources.PROJECT_PATH);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void InitializeGlControl()
        {
            _glControl3 = new GlControl3(Name, Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(0, 0, 0),
                IsVisibleUi2d = true,
            };

            _glControl3.Init += (w, h) => Initialize(Resources.PROJECT_PATH);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) =>
                UpdateFrame(deltaTime * 0.001f, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) =>
                RenderFrame(deltaTime * 0.001f, (int)w, (int)h, backcolor, camera);

            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            //_glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);

            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);
        }

        public void Init2d(int w, int h)
        {
            _glControl3.AddLabel("sun", $"sun=0",
                align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_BOTTOM,
                foreColor: new Vertex3f(1, 0, 0));
            _glControl3.AddLabel("cam", "camera position, yaw, pitch",
                align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL,
                foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs",
                align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP,
                foreColor: new Vertex3f(1, 1, 0));

            _glControl3.IsVisibleDebug = bool.Parse(
                IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(
                IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            Debug.PrintLine($"{Screen.PrimaryScreen.DeviceName} {w}x{h}");
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY") > 0) _glControl3.FullScreen(true);
        }

        public void Init3d(int w, int h)
        {
            _glControl3.InitGridShader(Resources.PROJECT_PATH);
            _glControl3.Camera.FAR = FAR_PLANE;
            _glControl3.Camera.NEAR = NEAR_PLANE;
            _glControl3.Camera.Position = new Vertex3f(0, 0, 1);

            _ocs = new OcclusionCullingSystem(Resources.PROJECT_PATH);
            _ocs.IsFogCulled = false;
            _ocs.IsViewFrustumCulled = true;
            _ocs.IsHierachicalZbuffer = false;
            _ocs.AddRawModel(@"FormTools\bin\Debug\Res\fern.obj");
            _ocs.AddRawModel(@"FormTools\bin\Debug\Res\fern_lod1.obj");
            _ocs.AddRawModel(@"FormTools\bin\Debug\Res\Sunflower_01.obj");
            //_ocs.AddRawModel(@"FormTools\bin\Debug\Res\tree1.obj");
            Console.WriteLine("모델 로딩 완료!");

            //_batchProcessor = new EntityBatchProcessor(_ocs);

            for (int i = 0; i < 50000; i++)
            {
                Vertex3f pos = Rand.NextVector3fWithPlane2d * 500;
                Entity lodEntity = _ocs.BakeEntity(entityName: $"fern", modelName: "fern", lowModelName: "fern",
                     pos, yaw: 0, pitch: 0, roll: 0);
                //_batchProcessor.EnqueueEntity(lodEntity);
            }
            Console.WriteLine("모델 배치 위치 완료!");

            //_loadingTask = _batchProcessor.StartProcessing();  // 비동기 로딩 시작
        }

        public void UpdateFrame(float duration, int w, int h, Camera camera)
        {
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera);

            _ocs.Update(camera, viewFrustum);
            _ocs.UpdateVisibleEntitiesFromCulledTree(camera);

            _ocs.OccludeEntityByOccluder(camera.Position, camera.ModelMatrix, ViewFrustum.BuildFrustumPlane(camera));

            _glControl3.CLabel("sun").Text = $"sun=";
            _glControl3.CLabel("ocs").Text = $"total={_ocs.EntityTotalCount}" + $",FrustumPassEntity={_ocs.FrustumPassEntity.Count}"
                + $",OccludedEntity={_ocs.OccludedEntity.Count}";
            _glControl3.CLabel("cam").Text =
                $"CamPos={camera.Position}," +
                $"CameraPitch={camera.CameraPitch}," +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance: 0.0f}";
        }

        public void RenderFrame(float duration, int w, int h, Vertex4f backcolor, Camera camera)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Viewport(0, 0, w, h);
            Gl.ClearColor(backcolor.x, backcolor.y, backcolor.z, 1.0f);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            Renderer3d.Render(_unlitShader, _ocs.OccludedEntity, camera);

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

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (_ocs.FrustumPassEntity.Count == 0) return;
            Entity eleEntity = _ocs.FrustumPassEntity[0];
            if (eleEntity == null) return;

            if (e.KeyCode == Keys.Down)
            {
                eleEntity.Pitch(-1.0f);
            }
            if (e.KeyCode == Keys.Up)
            {
                eleEntity.Pitch(1.0f);
            }
            if (e.KeyCode == Keys.Left)
            {
                eleEntity.Yaw(-1.0f);
            }
            if (e.KeyCode == Keys.Right)
            {
                eleEntity.Yaw(1.0f);
            }
            if (e.KeyCode == Keys.PageDown)
            {
                eleEntity.Roll(-1.0f);
            }
            if (e.KeyCode == Keys.PageUp)
            {
                eleEntity.Roll(1.0f);
            }
        }

        public void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.NONE;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}