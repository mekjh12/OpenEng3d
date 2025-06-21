using Camera3d;
using Fog;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Physics;
using Physics.Collision;
using Renderer;
using Shader;
using System;
using System.Windows.Forms;
using Ui2d;
using ZetaExt;

namespace OpenEng3d
{
    public partial class FormPhysics : Form
    {
        string ROOT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d";

        enum MOUSE_GAME_MODE { CAMERA_ROUND_ROT, CAMERA_ROUND_ROT2 };

        MOUSE_GAME_MODE _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT;

        EngineLoop _gameLoop;

        OcclusionCullingSystem _ocs;
        FogArea _fogArea;
        ShaderGroup _shaders;
        WorldCoordinate worldCoordinate;
        ParticleForceRegistry _particleForceRegistry;
        PhysicEngine _physicEngine;


        Vertex3f testVelocity;
        Particle testParticle;
        RigidBody testRigidBody;
        Vertex3f viewDirection;

        Vertex3f _prevPos = Vertex3f.Zero;

        PolygonMode _polygonMode = PolygonMode.Line;

        float _duration = 0.0f;

        float _yaw = 0.0f;
        float _pitch = 0.0f;

        Vertex3f _forcePosition = Vertex3f.Zero;

        public static bool _istestUpdate = true;
        bool _istestOnceUpdate = false;

        public FormPhysics()
        {
            InitializeComponent();
        }

        public void Init3d(int w, int h)
        {
            // 물리엔진 초기화
            _physicEngine.Init(min: -100, max: 100, cellSize: 5);

            // Entity add
            int count = 1;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    _ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j * 5.0f, i * 5.0f, -5));

                    //RigidCylinder body = new RigidCylinder(new Iron(), 0.1f, 3.0f);
                    //body.Position = new Vertex3f(j * 5.0f, i * 5.0f, 1.0f);
                    //_physicEngine.AddRigidBody(body);
                    //_physicEngine.AddForce(body, new Gravity());
                    //_physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                }
            }

            // 카메라 설정
            float cx = float.Parse(IniFile.GetPrivateProfileString("camera", "x", "0.0"));
            float cy = float.Parse(IniFile.GetPrivateProfileString("camera", "y", "0.0"));
            float cz = float.Parse(IniFile.GetPrivateProfileString("camera", "z", "0.0"));
            float yaw = float.Parse(IniFile.GetPrivateProfileString("camera", "yaw", "0.0"));
            float pitch = float.Parse(IniFile.GetPrivateProfileString("camera", "pitch", "0.0"));
            float dist = float.Parse(IniFile.GetPrivateProfileString("camera", "dist", "1.0"));
            _gameLoop.Camera = new OrbitCamera("", cx, cy, cz, dist);
            _gameLoop.Camera.CameraPitch = pitch;
            _gameLoop.Camera.CameraYaw = yaw;

            // 테스트용
            //_physicEngine.AddRigidBody(new RigidCube(new MatterInfinity(), 35.1f, 35.1f, 1.05f)
            //{
                //Position = Vertex3f.Zero + new Vertex3f(5, 5, -8),
            //});

            _istestUpdate = true;
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
                    "1-원<br>" +
                    "2-입방체<br>" +
                    "3-반평면<br>" +
                    "4-긴막대<br>" +
                    "5-랜덤입방체<br>" +
                    "6-<br>" +
                    "7-<br>" +
                    "8-<br>" +
                    "9-랜덤<br>" +
                    "0-충돌점<br>",
                });
            };

            UIEngine.InitFrame(w, h);
            UIEngine.StartFrame();
            UIEngine.EnableMouse = true;
        }

        public void Update(int deltaTime)
        {
            if (_istestOnceUpdate)
            {
                _istestOnceUpdate = false;
            }
            else
            {
                if (!_istestUpdate) return;
            }

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

            // 입자물리 업데이트
            _particleForceRegistry.UpdateForces(duration);
            _particleForceRegistry.Update(duration);
            Debug.Write($"입자수={_particleForceRegistry.ParticleCount}, 힘개수={_particleForceRegistry.ParticleForceCount} / ");

            // 강체물리 업데이트
            _physicEngine.Update(duration);
            Debug.Write($"/ 강체수={_physicEngine.RigidbodyCount}, 힘개수={_physicEngine.ForceCount}");

            //_physicEngine.DebugGridMap();

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
            Gl.LineWidth(3.0f);
            Renderer3d.Render(_shaders.ColorShader, camera, worldCoordinate);
            Gl.LineWidth(1.0f);

            // 물체 렌더링
            foreach (OcclusionEntity entity in _ocs.OccludedEntity)
            {
                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity, camera, _fogArea.Color, _fogArea.FogPlane, _fogArea.FogDensity);
            }

            // 물체의 투명부분 렌더링
            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Enable(EnableCap.CullFace);
            Gl.PolygonMode(MaterialFace.Front, _polygonMode);
            foreach (OcclusionEntity entity in _ocs.OccludedEntity)
            {
                if (entity.IsVisibleAABB)
                {
                    Renderer3d.RenderAABB(_shaders.ColorShader, entity.AABB, entity.AABB.Color.xyzw(0.3f), camera);
                }
                if (entity.IsVisibleRigidBody)
                {
                    Renderer3d.RenderAABB(_shaders.ColorShader, entity.RigidBody, entity.AABB.Color.xyzw(0.3f), camera);
                }
            }
            Gl.Disable(EnableCap.Blend);

            // 입자 물리 테스트
            _particleForceRegistry.Render(Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, _shaders.ColorShader, camera.ProjectiveMatrix, camera.ViewMatrix);

            // 강체 물리 렌더링
            _physicEngine.Render(_shaders.ColorShader, camera.ProjectiveMatrix, camera.ViewMatrix, true, isVisibleAABB: false);
            Renderer3d.RenderPoint(_shaders.ColorShader, _forcePosition, camera, new Vertex4f(1, 0, 0, 1), 0.08f);

            // 카메라 중심점 렌더링
            //Gl.Disable(EnableCap.DepthTest);
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);

            // 디버깅을 위한 점-선 렌더링
            foreach (RenderLine line in Debug.RenderLines)
                Renderer3d.RenderLine(_shaders.ColorShader, camera, line.Start, line.End, line.Color, line.Thick);
            foreach (RenderPont point in Debug.RenderPoints)
                Renderer3d.RenderPoint(_shaders.ColorShader, point.Position, camera, point.Color, point.Thick);

            // UI렌더링
            Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
            UIEngine.RenderFrame(deltaTime);
        }

        private void FormPhysics_Load(object sender, EventArgs e)
        {
            // setup
            IniFile.SetFileName("setup_physic.ini");

            // engine loop
            _gameLoop = new EngineLoop();

            // random init
            Rand.InitSeed(500);

            // shader init
            _shaders = new ShaderGroup()
            {
                IsAnimateShader = true, 
                IsBillboardShader = true,
                IsColorShader = true,
                IsSkyBoxShader = true,
                IsStaticShader = true,
                IsInertialShader = true,
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

            // 입자물리 초기화
            _particleForceRegistry = new ParticleForceRegistry();
            _physicEngine = new PhysicEngine();

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

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;

            if (e.Button == MouseButtons.Left)
            {
                if (testRigidBody != null)
                {
                    Vertex3f a = camera.Forward.Normalized;
                    Vertex3f p = testRigidBody.Position;
                    Vertex3f b = camera.OrbitPosition;
                    float t = a.Dot(p - b);
                    _forcePosition = a * t + b;

                    _forcePosition = p + testRigidBody.Up;
                    testRigidBody.AddForceAtPoint(testRigidBody.Forward * 50.0f, _forcePosition);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                _yaw = camera.CameraYaw;
                _pitch = camera.CameraPitch;
                _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT2;

                Vertex3f p = testRigidBody.Position;
                _forcePosition = p + testRigidBody.Right * 0.1f;
                testRigidBody.AddForceAtPoint(testRigidBody.Forward * 50.0f, _forcePosition);
            }
        }

        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            Camera camera = _gameLoop.Camera;
            if (camera is FPSCamera) camera?.GoForward(0.02f * e.Delta);
            if (camera is OrbitCamera) (camera as OrbitCamera)?.FarAway(-0.005f * e.Delta);
        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            OrbitCamera camera = (OrbitCamera)_gameLoop.Camera;

            if (e.Button == MouseButtons.Left)
            {

            }
            else if (e.Button == MouseButtons.Right)
            {
                camera.CameraYaw = _yaw;
                camera.CameraPitch = _pitch;
                _mouseMode = MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }
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
            }
            else if (e.KeyCode == Keys.T)
            {
                for (int i = 0; i < 250; i++)
                {
                    Particle particle = new Particle()
                    {
                        Position = camera.Position,
                        Velocity = (Rand.NextColor3f * 2.0f - new Vertex3f(-1, -1, -1)) * Rand.Next(2.0f, 10.0f),
                        Life = Rand.Next(1.0f, 6.0f),
                    };
                    ParticleGravity gravity = new ParticleGravity();
                    _particleForceRegistry.AddParticle(particle);
                    _particleForceRegistry.AddForce(particle, gravity);
                }
            }
            else if (e.KeyCode == Keys.G)
            {
                Particle b = new Particle();
                b.Position = new Vertex3f(camera.Position.x, camera.Position.y, Rand.Next(-10.0f, 10.0f));
                b.Mass = 500.0f;
                b.HalfSize = 0.5f;
                
                ParticleBuoyancy pb = new ParticleBuoyancy(maxDepth: 1.0f, volume: 1.0f, waterHeight: 0);
                _particleForceRegistry.AddParticle(b);
                _particleForceRegistry.AddForce(b, pb);
                _particleForceRegistry.AddForce(b, new ParticleGravity());
                testParticle = b;
            }
            else if (e.KeyCode == Keys.D9)
            {
                for (int i = 0; i < 1000; i++)
                {
                    int rnd = Rand.NextInt(1, 2);
                    RigidBody body = null;
                    if (rnd == 0)
                    {
                        body = new RigidCylinder(new MatterWater(), 0.1f, 3.0f);
                    }
                    else if (rnd == 1)
                    {
                        body = new RigidCube(new MatterIron(), 0.5f, 0.8f, 1.5f);
                    }
                    else if (rnd == 2)
                    {
                        body = new RigidSphere(new MatterIron(), 3.5f);
                    }
                    body.Position = new Vertex3f(camera.Position.x, camera.Position.y, camera.Position.z);
                    body.Position = Rand.NextVector3f * 100.0f;
                    body.Position = new Vertex3f(body.Position.x, body.Position.y, body.Position.z < 5.0f ? 10.0f : body.Position.z);
                    _physicEngine.AddRigidBody(body);
                    _physicEngine.AddForce(body, new Gravity());
                    _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                    body.AddForceAtPoint(Rand.NextVector3f, body.Position);
                    testRigidBody = body;
                }
                _physicEngine.Update(0);
                _physicEngine.DebugGridMap();
            }
            else if (e.KeyCode == Keys.D1) 
            {
                RigidBody body = new RigidSphere(new MatterIron(), Rand.NextFloat * 0.5f + 0.5f);
                body.Position = camera.Position + new Vertex3f(0, 0, 5);
                _physicEngine.AddRigidBody(body);
                _physicEngine.AddForce(body, new Gravity());
                _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                testRigidBody = body;
            }
            else if (e.KeyCode == Keys.D2)
            {
                Vertex3f rnd = Rand.NextVector3f * 0.2f + Vertex3f.One * 0.8f;
                RigidBody body = new RigidCube(new MatterIron(), rnd.x, rnd.y, 0.3f);
                body.Position = camera.Position + new Vertex3f(0, 0, 5);
                _physicEngine.AddRigidBody(body);
                _physicEngine.AddForce(body, new Gravity());
                _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                testRigidBody = body;
            }
            else if (e.KeyCode == Keys.D3)
            {
                Vertex3f rnd = Rand.NextVector3f * 0.2f + Vertex3f.One * 0.8f;
                RigidBody body = new RigidCube(new MatterInfinity(), 10, 10, 1);
                body.Position = new Vertex3f(0, 0, 1);
                _physicEngine.AddRigidBody(body);
            }
            else if (e.KeyCode == Keys.D4)
            {
                RigidBody body = new RigidCube(new MatterIron(), 0.05f, 0.05f, 2.0f);
                body.Position = camera.Position + new Vertex3f(0, 0, 5);
                _physicEngine.AddRigidBody(body);
                _physicEngine.AddForce(body, new Gravity());
                _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                testRigidBody = body;
            }
            else if (e.KeyCode == Keys.D5)
            {
                Vertex3f rnd = Rand.NextVector3f * 0.2f + Vertex3f.One * 0.8f;
                RigidBody body = new RigidCube(new MatterIron(), rnd.x, rnd.y, rnd.z);
                body.Position = camera.Position + new Vertex3f(0, 0, 5);
                body.Orientation = Quaternion4.ToQuaternion(Rand.NextVector3f);
                _physicEngine.AddRigidBody(body);
                _physicEngine.AddForce(body, new Gravity());
                _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                testRigidBody = body;
            }
            else if (e.KeyCode == Keys.D6)
            {
                
            }
            else if (e.KeyCode == Keys.D0)
            {
                CollisionDetector.VisibleContactPoint = !CollisionDetector.VisibleContactPoint;
                Console.WriteLine("충돌점보이기=" + CollisionDetector.VisibleContactPoint);
            }
            else if (e.KeyCode == Keys.Space)
            {
                if (_istestUpdate)
                {
                    _istestUpdate = false;
                    _istestOnceUpdate = false;
                }
                else
                {
                    _istestUpdate = true;
                    _istestOnceUpdate = false;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (!_istestUpdate)
                {
                    _istestOnceUpdate = true;
                }
            }
            else if (e.KeyCode == Keys.R)
            {
                int rnd = Rand.NextInt(0, 2);
                RigidBody body = null;
                if (rnd == 0)
                {
                    body = new RigidCube(new MatterIron(), 0.5f, 0.8f, 1.5f);
                }
                else if (rnd == 1)
                {
                    body = new RigidSphere(new MatterIron(), 1.5f);
                }
                else if (rnd == 2)
                {
                    body = new RigidCylinder(new MatterWater(), 0.1f, 3.0f);
                }
                body.Position = new Vertex3f(camera.Position.x, camera.Position.y, camera.Position.z);
                _physicEngine.AddRigidBody(body);
                _physicEngine.AddForce(body, new Gravity());
                _physicEngine.AddForce(body, new Buoyancy(maxDepth: 1.0f, volume: body.Volume, waterHeight: 0));
                body.AddForceAtPoint(Rand.NextVector3f, body.Position);
                testRigidBody = body;
            }

        }
    }
}
