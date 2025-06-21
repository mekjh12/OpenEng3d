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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZetaExt;
using static Shader.NoiseShader;

namespace FormTools
{
    public partial class FormOcclusionQuery : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private GlControl3 _glControl3;
        private ShaderGroup _shaders;
        private Entity _occluder;
        private Entity[] _entity;
        private bool _isVisibleOccluder = true;
        private bool _isOcclusionQuery = true;

        public FormOcclusionQuery()
        {
            InitializeComponent();

            // 컨트롤 생성
            _glControl3 = new GlControl3("FormOcclusionQuery", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
            };
            this.Controls.Add(_glControl3);

            // 초기화
            _glControl3.Init += (w, h) => Init2d(w, h);
            _glControl3.Init2d += (w, h) => InitUi2d(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Start();
        }

        private void InitUi2d(int w, int h)
        {
            _glControl3.AddCheckBar("chkCloudRender", "클라우드렌더링 체크박스");
            _glControl3.AddLabel("query", "ocs", foreColor: new Vertex3f(1,1,0));
            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL);
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP);
        }

        public void Init2d(int w, int h)
        {
            Rand.InitSeed(500);

            // shader init
            _shaders = new ShaderGroup()
            {
                IsColorShader = true,
                IsStaticShader = true,
                IsInfiniteGridShader = true,
            };
            _shaders.Create(PROJECT_PATH);
        }

        private void Init3d(int w, int h)
        {
            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);

            // fog init
            _glControl3.BackClearColor = new Vertex3f(0.3f, 0.3f, 0.3f);

            // _occluder
            Texture texture = new Texture(PROJECT_PATH + "\\Res\\bricks.jpg", Texture.TextureMapType.Diffuse);
            TexturedModel texturedModel = new TexturedModel(Loader3d.LoadCube(3, 3), texture);
            _occluder = new Entity("occluder", "occluder", texturedModel)
            {
                Material = Material.Blue,
                Size = new Vertex3f(10, 1, 2),
                Position = Vertex3f.Zero,
            };

            _entity = new Entity[900];

            for (int i = 0; i < 900; i++)
            {
                float x = i / 30;
                float y = i - x * 30;

                _entity[i] = new Entity($"entity{i}", "entity", texturedModel)
                {
                    Position = new Vertex3f(x, y, 0),
                    Material = Material.White,
                };
                //_entity[i].GenOcclusionQueryId();
            }
        }

        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            OrbitCamera orbitCamera = camera as OrbitCamera;
            float g = 1.0f / (float)Math.Tan((orbitCamera.FOV * 0.5f).ToRadian());

            _glControl3.CLabel("cam").Text = $"CamPos={orbitCamera.Position},CameraPitch={orbitCamera.CameraPitch},CameraYaw={orbitCamera.CameraYaw}";
        }

        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            // 물체 렌더링
            Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, _occluder, camera, Vertex3f.One, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);

            // 오클루전쿼리
            int samples = 0;
            for (int i = 0; i < 900; i++)
            {
                uint query = 0;// _entity[i].OcclusionQueryId;
                Gl.DepthMask(false);
                Gl.ColorMask(false, false, false, false);
                Gl.BeginQuery(QueryTarget.AnySamplesPassed, query);

                ColorShader shader = _shaders.ColorShader;
                shader.Bind();
                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, camera.VPMatrix * _entity[i].ModelMatrix);
                foreach (RawModel3d rawModel in _entity[i].Model)
                {
                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }

                shader.Unbind();

                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, _entity[i], camera, Vertex3f.One, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);
                Gl.EndQuery(QueryTarget.AnySamplesPassed);

                //Gl.GetQueryObject(query, QueryObjectParameterName.QueryResult, out int iSamplesPassed);
                Gl.GetQueryObject(query, QueryObjectParameterName.QueryResult, out int iSamplesPassed);
                Gl.DepthMask(true);
                Gl.ColorMask(true, true, true, true);

                if (iSamplesPassed > 0)
                {
                    //Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, _entity[i], camera, Vertex3f.One, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);
                }
                samples += iSamplesPassed;
            }

            _glControl3.CLabel("query").Text = "query=" + samples;

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {
                _isVisibleOccluder = !_isVisibleOccluder;
            }
            else if (e.KeyCode == Keys.D2)
            {
                _isOcclusionQuery = !_isOcclusionQuery;
            }
        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {

            }
        }

        private void FormPerlinNoise2d_Load(object sender, EventArgs e)
        {

        }

        private void FormOcclusionQuery_Load(object sender, EventArgs e)
        {

        }
    }
}
