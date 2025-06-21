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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormEntityTest : Form, IRenderer
    {
        private const int RANDOM_SEED = 500;
        private const float FAR_PLANE = 10000f;
        private const float NEAR_PLANE = 1f;

        private GlControl3 _glControl3;
        private ColorShader _colorShader;
        private UnlitShader _unlitShader;

        OcclusionCullingSystem _ocs;
        Entity _fern;

        private bool _isBitmapLoading = false;  // 중복 로딩 방지 플래그
        private uint _textureId;  // 로드한 텍스처 ID

        public FormEntityTest()
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

        public async Task<Bitmap> LoadBitmapAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    return new Bitmap(fs);
                }
            });
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

            for (int i = 0; i < 1; i++)
            {
                Vertex3f pos = Rand.NextVector3fWithPlane2d * 3;
                _fern = _ocs.AddEntity(entityName: $"fern", modelName: "fern", pos, yaw: 0, pitch: 0, roll: 0, isOneside: true);
                pos = Rand.NextVector3fWithPlane2d * 3;
                _ocs.AddEntity(entityName: $"fern_lod1", modelName: "fern_lod1", pos, yaw: 0, pitch: 0, isOneside: true);
                pos = Rand.NextVector3fWithPlane2d * 3;
                _ocs.AddEntity(entityName: $"Sunflower_01", modelName: "Sunflower_01", pos, yaw: 0, pitch: 0, roll: 0, isOneside: false);
            }
        }

        public void UpdateFrame(float duration, int w, int h, Camera camera)
        {
            float g = 1.0f / (float)Math.Tan((camera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(
                camera.Position,
                camera.Forward,
                camera.Up,
                camera.Right,
                g,
                camera.AspectRatio,
                camera.NEAR,
                camera.FAR);

            _ocs.Update(camera, viewFrustum);
            _ocs.UpdateVisibleEntitiesFromCulledTree(camera);

            _ocs.OccludeEntityByOccluder(camera.Position, camera.ModelMatrix , ViewFrustum.BuildFrustumPlane(camera));

            _glControl3.CLabel("sun").Text = $"sun=";
            _glControl3.CLabel("ocs").Text = $"total={_ocs.EntityTotalCount}"  + $",FrustumPassEntity={_ocs.FrustumPassEntity.Count}"
                + $",OccludedEntity={_ocs.OccludedEntity.Count}";
            _glControl3.CLabel("cam").Text =
                $"CamPos={camera.Position}," +
                $"CameraPitch={camera.CameraPitch}," +
                $"CameraYaw={camera.CameraYaw}, " +
                $"Dist={camera.Distance : 0.0f}";
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

        public async void MouseDnEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _glControl3.MouseMode = GlControl3.MOUSE_GAME_MODE.CAMERA_ROUND_ROT;
            }

            if (e.Button == MouseButtons.Middle)
            {
                _isBitmapLoading = true;
                Bitmap bitmap = await LoadBitmapAsync(@"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\Res\Terrain\test.png");

                _textureId = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _textureId);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                  ImageLockMode.ReadOnly,
                                                  System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                              data.Width, data.Height, 0,
                              OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);

                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                Gl.BindTexture(TextureTarget.Texture2d, 0);

                (_fern.Models[0] as TexturedModel).Texture.TextureID = _textureId;
                Debug.PrintLine("텍스처업로드완료!");
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

        private void FormModelTest_Load(object sender, EventArgs e)
        {
        }
    }
}