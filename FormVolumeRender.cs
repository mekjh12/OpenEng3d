using Camera3d;
using Cloud;
using Fog;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Windows.Forms;
using Ui2d;
using ZetaExt;

namespace OpenEng3d
{
    public partial class FormVolumeRender : Form
    {
        string ROOT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d";

        enum MOUSE_GAME_MODE { CAMERA_ROUND_ROT, CAMERA_ROUND_ROT2 };

        EngineLoop _gameLoop;
        OcclusionCullingSystem _ocs;
        ShaderGroup _shaders;
        FogArea _fogArea;
        WorldCoordinate worldCoordinate;
        VolumeRender _volumeRender;
        WorleyNoise _worleyNoise;

        float _duration = 0.0f;
        MOUSE_GAME_MODE _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
        Vertex3f _prevPos = Vertex3f.Zero;
        PolygonMode _polygonMode = PolygonMode.Line;

        private float stepLength = 0.01f;

        public FormVolumeRender()
        {
            InitializeComponent();
        }

        private void GlControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            Camera camera = _gameLoop.Camera;
            if (camera is FPSCamera) camera?.GoForward(0.02f * e.Delta);
            if (camera is OrbitCamera) (camera as OrbitCamera)?.FarAway(-0.005f * e.Delta);
        }

        private void glControl1_Render(object sender, GlControlEventArgs e)
        {
            int glLeft = this.Width - this.glControl1.Width;
            int glTop = this.Height - this.glControl1.Height;
            int glWidth = this.glControl1.Width;
            int glHeight = this.glControl1.Height;
            _gameLoop.DetectInput(this.Left + glLeft, this.Top + glTop, glWidth, glHeight);

            // 엔진 루프, 처음 로딩시 deltaTime이 커지는 것을 방지
            if (FramePerSecond.DeltaTime < 1000)
            {
                _gameLoop.Update(deltaTime: FramePerSecond.DeltaTime);
                _gameLoop.Render(deltaTime: FramePerSecond.DeltaTime);
            }
            FramePerSecond.Update();
        }

        private void FormVolumeRender_Load(object sender, EventArgs e)
        {
            // setup
            IniFile.SetFileName("setup_volume_render.ini");

            // engine loop
            _gameLoop = new EngineLoop();

            // random init
            Rand.InitSeed(500);

            // shader init
            _shaders = new ShaderGroup()
            {
                IsColorShader = true,
                IsStaticShader = true,
                IsVolumeRenderShader = true,
                IsWorleyNoiseShader = true,
            };
            _shaders.Create(EngineLoop.PROJECT_PATH);

            // OCS init
            _ocs = new OcclusionCullingSystem(ROOT_PATH);
            _ocs.AddRawModel("brick", Loader3d.LoadCube(2, 2), "\\Res\\bricks.jpg");

            // fog
            _fogArea = new FogArea();
            _fogArea.FogPlane = new Vertex4f(0, 0, 1, 1000);
            _fogArea.FogDensity = 0.0001f;
            _fogArea.Color = new Vertex3f(0.1f, 0.1f, 0.1f);

            _volumeRender = new VolumeRender();
            _worleyNoise = new WorleyNoise();

            // 엔진초기화
            _gameLoop.InitFrame += (w, h) =>
            {
                Init2d(w, h);
                Init3d(w, h);
            };

            // 업데이트
            _gameLoop.UpdateFrame = (deltaTime) => Update(deltaTime);

            // 렌더링
            _gameLoop.RenderFrame = (deltaTime) => Render(deltaTime);
        }

        public void Init3d(int w, int h)
        {
            // Entity add
            int count = 10;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    _ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j * 2.0f + 0.5f, i * 2.0f + 0.35f, -8));
                }
            }

            // 카메라 설정
            float cx = float.Parse(IniFile.GetPrivateProfileString("camera", "x", "0.0"));
            float cy = float.Parse(IniFile.GetPrivateProfileString("camera", "y", "0.0"));
            float cz = float.Parse(IniFile.GetPrivateProfileString("camera", "z", "0.0"));
            float yaw = float.Parse(IniFile.GetPrivateProfileString("camera", "yaw", "0.0"));
            float pitch = float.Parse(IniFile.GetPrivateProfileString("camera", "pitch", "0.0"));
            float dist = float.Parse(IniFile.GetPrivateProfileString("camera", "dist", "1.0"));

            //
            _volumeRender.Init(w, h);
            _volumeRender.LoadSphere();
            //_volumeRender.LoadRaw(ROOT_PATH + "\\christmastree128x124x128.dat");

            

            _gameLoop.Camera = new OrbitCamera("", cx, cy, cz, dist);
            _gameLoop.Camera.CameraPitch = pitch;
            _gameLoop.Camera.CameraYaw = yaw;
        }

        public void Init2d(int w, int h)
        {
            // UIEngine add
            FontFamilySet.AddFonts(EngineLoop.EXECUTE_PATH + "\\fonts\\fontList.txt");
            UIEngine.REOURCES_PATH = ROOT_PATH + @"\UIDesign2d\Res\";
            UITextureLoader.LoadTexture2d(UIEngine.REOURCES_PATH);

            Console.WriteLine("game loop init!");
            UIEngine.Add(new UIEngine("mainUI", w, h) { AlwaysRender = true }, w, h);
            UIEngine.DesignInit += (w1, h1) =>
            {
                UIEngine.AddControl("mainUI", new Ui2d.Label("fps", FontFamilySet.조선100년체)
                {
                    Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TL,
                    IsCenter = true,
                    Margin = 0.010f,
                    FontSize = 1.3f,
                    Alpha = 0.1f,
                    ForeColor = new Vertex3f(1, 0, 0),
                    BackColor = new Vertex3f(1, 1, 1),
                    BorderColor = new Vertex3f(1, 0, 0),
                    BorderWidth = 1.0f,
                    IsBorder = true,
                });

                UIEngine.AddControl("mainUI", new Ui2d.Label("msg", FontFamilySet.연성체)
                {
                    Align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
                    AdjontControl = UIEngine.UI2d<Ui2d.Label>("fps"),
                    ForeColor = new Vertex3f(1, 1, 0),
                    FontSize = 1.0f,
                });

                UIEngine.AddControl("mainUI", new Ui2d.Label("help", FontFamilySet.연성체)
                {
                    Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TR,
                    ForeColor = new Vertex3f(1, 1, 0),
                    FontSize = 1.0f,
                    Text = "도움말<br> " +
                    "Space-시작/정지<br>" +
                    "1-<br>"
                });
            };

            UIEngine.InitFrame(w, h);
            UIEngine.StartFrame();
            UIEngine.EnableMouse = true;
        }

        public void Update(int deltaTime)
        {
            int w = this.glControl1.Width;
            int h = this.glControl1.Height;
            if (_gameLoop.Width * _gameLoop.Height == 0)
            {
                // 초기화 부분이다.
                _gameLoop.Init(w, h);
                _gameLoop.Camera.Init(w, h);
                UIEngine.EnableMouse = false;
                worldCoordinate = new WorldCoordinate(800);
            }

            float duration = deltaTime * 0.001f;
            _duration = duration;

            Debug.ClearFrameText();
            Debug.ClearPointAndLine();

            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;
            _prevPos = camera.Position;

            float g = 1.0f / (float)Math.Tan((camera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(camera.OrbitPosition,
                camera.Forward, camera.Up, camera.Right, g, camera.AspectRatio, camera.NEAR, 100); //camera.FAR);
                                                                                                   //_ocs.UpdateRigid(camera, _humanAniModel.Collider, out Node contactNode1);
            _ocs.Update(camera, viewFrustum, _fogArea.FogPlane, _fogArea.FogDensity);

            Debug.Write(_ocs.OccludedEntity.Count + "개, ");
            Debug.Write("cam=" + camera.Position);

            _volumeRender.Update(w, h, duration);

            int glLeft = this.Width - this.glControl1.Width;
            int glTop = this.Height - this.glControl1.Height;
            int glWidth = this.glControl1.Width;
            int glHeight = this.glControl1.Height;

            UIEngine.UI2d<Ui2d.Label>("fps").Text = "프레임율(" + FramePerSecond.FPS + "fps)";
            UIEngine.UI2d<Ui2d.Label>("msg").Text = "*디버깅=" + Debug.TextFrame;
            UIEngine.MouseUpdateFrame(this.Left + glLeft, this.Top + glTop, glWidth, glHeight, 0);
            UIEngine.UpdateFrame(deltaTime);

        }

        public void Render(int deltaTime)
        {
            Camera camera = _gameLoop.Camera;

            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);

            Vertex3f fogColor = _fogArea.Color;
            Gl.ClearColor(fogColor.x, fogColor.y, fogColor.z, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.Enable(EnableCap.DepthTest);

            Gl.PolygonMode(MaterialFace.Front, _polygonMode);

            // 좌표계
            Renderer3d.Render(_shaders.ColorShader, camera, worldCoordinate);

            // 물체 렌더링
            foreach (OcclusionEntity entity in _ocs.OccludedEntity)
                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity, camera, _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);

            // 볼륨 렌더링
            //_volumeRender.Render(_shaders.VolumeRenderShader, Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, camera, stepLength);

            // 디버깅을 위한 점-선 렌더링
            foreach (RenderLine line in Debug.RenderLines)
                Renderer3d.RenderLine(_shaders.ColorShader, camera, line.Start, line.End, line.Color, line.Thick);
            foreach (RenderPont point in Debug.RenderPoints)
                Renderer3d.RenderPoint(_shaders.ColorShader, point.Position, camera, point.Color, point.Thick);

            // UI렌더링
            Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
            UIEngine.RenderFrame(deltaTime);
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            Mouse.CurrentPosition = new Vertex2i(e.X, e.Y);
            Vertex2i delta = Mouse.DeltaPosition;

            Camera camera = _gameLoop.Camera;

            if (_mouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT)
            {
                // 카메라를 회전
                camera?.Yaw(-delta.x);
                camera?.Pitch(delta.y);
            }
            else if (_mouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT2)
            {
                // 카메라를 회전
                camera?.Yaw(-delta.x);
                camera?.Pitch(delta.y);
            }

            Mouse.PrevPosition = new Vertex2i(e.X, e.Y);
        }

        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;
            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("정말로 끝내시겠습니까?", "종료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // 종료 설정 저장
                    IniFile.WritePrivateProfileString("camera", "x", _gameLoop.Camera.Position.x);
                    IniFile.WritePrivateProfileString("camera", "y", _gameLoop.Camera.Position.y);
                    IniFile.WritePrivateProfileString("camera", "z", _gameLoop.Camera.Position.z);
                    IniFile.WritePrivateProfileString("camera", "yaw", _gameLoop.Camera.CameraYaw);
                    IniFile.WritePrivateProfileString("camera", "pitch", _gameLoop.Camera.CameraPitch);
                    IniFile.WritePrivateProfileString("camera", "dist", (_gameLoop.Camera as OrbitCamera).Distance);
                    Application.Exit();
                }
            }
            else if (e.KeyCode == Keys.E) camera.GoUp(0.1f);
            else if (e.KeyCode == Keys.Q) camera.GoUp(-0.1f);
            else if (e.KeyCode == Keys.F)
            {
                _polygonMode = (_polygonMode == PolygonMode.Fill) ?
                    PolygonMode.Line : PolygonMode.Fill;
                Console.WriteLine(_polygonMode);
            }
            else if (e.KeyCode == Keys.D1)
            {
                stepLength -= 0.001f;
                stepLength = stepLength < 0.001f? 0.001f: stepLength;
                Console.WriteLine(stepLength);
            }
            else if (e.KeyCode == Keys.D2)
            {
                stepLength += 0.001f;
                Console.WriteLine(stepLength);
            }
            else if (e.KeyCode == Keys.D0)
            {
                camera.GoTo(Vertex3f.Zero);
            }
        }


    }
}
