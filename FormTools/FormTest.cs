using Camera3d;
using Cloud;
using Common.Abstractions;
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
using ZetaExt;

namespace FormTools
{
    public partial class FormTest : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private GlControl3 _glControl3;
        private OcclusionCullingSystem _ocs;
        private ShaderGroup _shaders;
        private VolumeRender _volumeRender;
        private Clouds _clouds;

        public FormTest()
        {
            InitializeComponent();

            // 컨트롤 생성
            _glControl3 = new GlControl3("FormTest", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                HelpText = "",
            };
            _glControl3.SetVisibleMouse(false);
            this.Controls.Add(_glControl3);

            // 초기화
            //_glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, w, h, backcolor, camera);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Start();
        }

        private void Init2d(int w, int h)
        {
            Rand.InitSeed(500);

            // shader init
            _shaders = new ShaderGroup()
            {
                IsColorShader = true,
                IsStaticShader = true,
                IsWorleyNoiseShader = true,
                IsWorleyNoiseGenShader = true,
            };
            _shaders.Create(PROJECT_PATH);
        }

        private void Init3d(int w, int h)
        {
            // fog init
            _glControl3.BackClearColor = new Vertex3f(0.1f, 0.1f, 0.1f);

            // cloud init
            _volumeRender = new VolumeRender();
            _clouds = new Clouds();
            _clouds.Init(w, h);

            // OCS init
            _ocs = new OcclusionCullingSystem(PROJECT_PATH);
            _ocs.AddRawModel("brick", Loader3d.LoadCube(2, 2), "\\Res\\bricks.jpg");

            // Entity add
            int count = 8;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    //_ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j * 2.0f + 0.5f, i * 2.0f + 0.35f, -8));
                }
            }
        }

        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            _volumeRender.Update(w, h, duration);
            _clouds.Update(w, h, duration, camera);

            OrbitCamera orbitCamera = camera as OrbitCamera;
            float g = 1.0f / (float)Math.Tan((orbitCamera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(orbitCamera.Position,
                orbitCamera.Forward, orbitCamera.Up, orbitCamera.Right, g, orbitCamera.AspectRatio, orbitCamera.NEAR, 100);
            _ocs.Update(orbitCamera, viewFrustum, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);

            Debug.Write("OccEntity=" + _ocs.OccludedEntity.Count + "개, ");
            Debug.Write(",CamPos=" + orbitCamera.Position);
            Debug.Write(",CameraPitch=" + orbitCamera.CameraPitch);
            Debug.Write(",CameraYaw=" + orbitCamera.CameraYaw);
        }

        private void RenderFrame(int deltaTime, float width, float height, Vertex4f backcolor, Camera camera)
        {
            // 물체 렌더링
            foreach (OccluderEntity entity in _ocs.OccludedEntity)
            {
                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity, camera, _glControl3.BackClearColor, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);
            }

            // 구름 렌더링
            //_worleyNoise.Render(_shaders.WorleyNoiseGenShader, Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, camera);
            _clouds.Render(_shaders.WorleyNoiseShader, Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, camera);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        private void FormTest_Load(object sender, EventArgs e)
        {

        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {

        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D0)
            {
                _glControl3.Camera.Position = Vertex3f.Zero;
            }
            else if (e.KeyCode == Keys.D1)
            {
                _clouds.Absorption *= 0.9f;
            }
            else if (e.KeyCode == Keys.D2)
            {
                _clouds.Absorption *= 1.09f;
            }
            else if (e.KeyCode == Keys.D3)
            {
                _clouds.BoundSize = new Vertex3f(_clouds.BoundSize.x * 0.9f, _clouds.BoundSize.y * 0.9f, _clouds.BoundSize.z * 0.95f);
            }
            else if (e.KeyCode == Keys.D4)
            {
                _clouds.BoundSize = new Vertex3f(_clouds.BoundSize.x * 1.09f, _clouds.BoundSize.y * 1.09f, _clouds.BoundSize.z * 1.05f);
            }
        }
    }
}