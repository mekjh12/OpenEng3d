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
    public partial class FormPerlinNoise2d : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private GlControl3 _glControl3;
        private OcclusionCullingSystem _ocs;
        private ShaderGroup _shaders;
        private Perlin _perlin;

        public FormPerlinNoise2d()
        {
            InitializeComponent();

            // 컨트롤 생성
            _glControl3 = new GlControl3("FormPerlinNoise2d", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
            };
            this.Controls.Add(_glControl3);

            // 초기화
            _glControl3.Init += (w, h) => Init2d(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => InitUi2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.MouseDown += (s, e) => MouseDownEvent(s, e);
            _glControl3.Start();

            // 초기화
            _perlin = new Perlin();

        }

        private void InitUi2d(int w, int h)
        {
            _glControl3.AddValueBar("octaves", value: 5, minValue: 0, maxValue: 10, backColor: new Vertex3f(1, 0, 1)).StepValue = 1;
            _glControl3.AddValueBar("amplitude", value: 0.5f, minValue: 0.1f, maxValue: 1.0f);
            _glControl3.AddValueBar("frequency", value: 0.5f, minValue: 0.1f, maxValue: 30.0f).StepValue = 0.1f;
            _glControl3.AddValueBar("persistence", value: 0.5f, minValue: 0.1f, maxValue: 1.0f);
            _glControl3.AddValueBar("numCells", value: 8, minValue: 1, maxValue: 60).StepValue = 1;
            _glControl3.AddValueBar("stepLength", value: 0.01f, minValue: 0.01f, maxValue: 0.1f).StepValue = 0.01f;

            _glControl3.AddValueBar("minHeight", value: 0.0f, minValue: 0.0f, maxValue: 1.0f).StepValue = 0.01f;
            _glControl3.AddValueBar("maxHeight", value: 1.0f, minValue: 0.0f, maxValue: 1.0f).StepValue = 0.01f;

            _glControl3.AddCheckList("noiseType", "", new string[] {"perlin", "worley", "perlin-worley", "worley_g", "worley_b", "worley_a" }, fontSize: 1.9f).ChangeValue += (obj, d, b) =>
            {
                ZetaExt.Debug.PrintLine(obj.Value + "=" + obj.SelectedIndex);
            };

            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL);
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP);

            // ini file
            _glControl3.SimpHValueBar("octaves").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "octaves", "6"));
            _glControl3.SimpHValueBar("amplitude").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "amplitude", "1.0"));
            _glControl3.SimpHValueBar("frequency").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "frequency", "1.0"));
            _glControl3.SimpHValueBar("persistence").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "persistence", "0.5"));
            _glControl3.SimpHValueBar("numCells").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "numCells", "12"));
            _glControl3.SimpHValueBar("stepLength").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "stepLength", "0.005"));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            // in my house with GPU.
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY4") > 0)
            {
                _glControl3.FullScreen(true);
                _glControl3.SimpHValueBar("stepLength").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "stepLength", "0.05"));
            }
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
                IsNoiseShader = true,
            };
            _shaders.Create(PROJECT_PATH);

            _shaders.NoiseShader.LoadTextureCloud(PROJECT_PATH);
            

            List<float> points = new List<float>();
            for (int z = 0; z < 128; z++)
            {
                break;
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {

                        points.Add(y);
                    }
                    //float y = PerlinNoise.Noise(i, j);
                }
            }
        }

        private void Init3d(int w, int h)
        {
            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);

            // fog init
            _glControl3.BackClearColor = new Vertex3f(0.3f, 0.3f, 0.3f);

            // OCS init
            _ocs = new OcclusionCullingSystem(PROJECT_PATH);
            _ocs.AddRawModel("brick", Loader3d.LoadCube(2, 2), "\\Res\\bricks.jpg");

            // Entity add
            int count = 0;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    //_ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j * 0.5f + 0.5f, i * 0.5f + 0.35f, -0.05f), Vertex3f.One * 0.1f);
                }
            }
        }

        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            OrbitCamera orbitCamera = camera as OrbitCamera;
            float g = 1.0f / (float)Math.Tan((orbitCamera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(orbitCamera.Position,
                orbitCamera.Forward, orbitCamera.Up, orbitCamera.Right, g, orbitCamera.AspectRatio, orbitCamera.NEAR, 100);
            _ocs.Update(orbitCamera, viewFrustum, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);




            _glControl3.CLabel("ocs").Text = $"OccEntity={_ocs.OccludedEntity.Count}";
            _glControl3.CLabel("cam").Text = $"CamPos={orbitCamera.Position},CameraPitch={orbitCamera.CameraPitch},CameraYaw={orbitCamera.CameraYaw}";
        }

        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            // 물체 렌더링
            foreach (OccluderEntity entity in _ocs.OccludedEntity)
            {
                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity, camera, _glControl3.BackClearColor, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);
            }

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            // 노이즈쉐이더를 시작한다.
            NoiseShader shader = _shaders.NoiseShader;
            shader.Bind();

            shader.BindCloudTexture();

            shader.LoadUniform(NoiseShader.UNIFORM_NAME.focal_length, camera.FocalLength);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.viewport_size, new Vertex2f(_glControl3.Width, _glControl3.Height));
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.ray_origin, camera.Position);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.aspect_ratio, camera.AspectRatio);

            shader.LoadUniform(NoiseShader.UNIFORM_NAME.divisor, 64.0f);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.noiseType, _glControl3.CheckList("noiseType").SelectedIndex);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.numCells, (int)_glControl3.SimpHValueBar("numCells").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.persistence, _glControl3.SimpHValueBar("persistence").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.octaves, (int)_glControl3.SimpHValueBar("octaves").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.frequency, _glControl3.SimpHValueBar("frequency").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.amplitude, _glControl3.SimpHValueBar("amplitude").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.stepLength, _glControl3.SimpHValueBar("stepLength").Value);

            //Matrix4x4f M = Matrix4x4f.Translated(_centerPosition.x, _centerPosition.y, _centerPosition.z) * Matrix4x4f.Scaled(_boundSize.x, _boundSize.y, _boundSize.z);
            Matrix4x4f M = Matrix4x4f.Identity;
            Matrix4x4f V = camera.ViewMatrix;
            Matrix4x4f MVP = camera.ProjectiveMatrix * V * M;
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.mvp, MVP);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.view, V);

            Gl.BindVertexArray(Renderer3d.Cube.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();

            Gl.Disable(EnableCap.Blend);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D1)
            {

            }
        }
        public void MouseDownEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.ShowUi2dDialog(!_glControl3.IsUi2dMode);
            }
            else if (e.Button == MouseButtons.Left)
            {

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
    }
}
