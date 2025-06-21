using Camera3d;
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
    public partial class FormFrameBuffer : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";
        private GlControl3 _glControl3;
        private OcclusionCullingSystem _ocs;
        private ShaderGroup _shaders;

        uint _fbo;
        uint _rbo;
        uint _texture;
        uint _depth;


        public FormFrameBuffer()
        {
            InitializeComponent();

            // 컨트롤 생성
            _glControl3 = new GlControl3("FormFrameBuffer", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                HelpText = "Help Add ToDo",
            };
            this.Controls.Add(_glControl3);
            _glControl3.SetVisibleMouse(true);

            // 초기화
            //_glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.Init2d += (w, h) => InitUi2d(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            _glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Start();

            Init();
        }

        private void Init()
        {
            
        }


        private void InitUi2d(int w, int h)
        {
            _glControl3.AddLabel("title", "프레임버퍼와 렌더링버퍼", foreColor: new Vertex3f(0, 1, 0), fontSize: 1.5f, align: Ui2d.Control.CONTROL_ALIGN.ROOT_TC);

            _glControl3.AddLabel("desc", 
                "[설명] 프레임버퍼와 렌더링버퍼을 생성하는 방법에 대하여 설명한다.<br>" +
                "- 렌더링버퍼는 텍스쳐버퍼의 일종이다.<br>" +
                "- 렌더링버퍼는 디스플레이의 백버퍼를 위하여 텍스처타입의 픽셀로 구성되어 있지 않고,<br>" +
                "- 화면에는 두 개의 render-to-texture하여 텍스처 2개를 생성하여 이를 큐브의 텍스처로 활용하였다.<br>",
                fontSize: 1.2f, align: Ui2d.Control.CONTROL_ALIGN.ROOT_ML);
        } 

        private void Init2d(int w, int h)
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

            // OCS init
            _ocs = new OcclusionCullingSystem(PROJECT_PATH);
            _ocs.AddRawModel("brick", Loader3d.LoadCube(1, 1), "\\Res\\bricks.jpg");

            // Entity add
            int count = 5;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    _ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j * 0.5f + 0.5f, i * 0.5f + 0.35f, -0.05f)
                        , Vertex3f.One * 0.1f);
                }
            }

            //----------------------------------------------------------------------------------------------------------------------
            // Framebuffer 생성
            //----------------------------------------------------------------------------------------------------------------------
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // Texturebuffer attach
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, w, h, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);

            // Depthbuffer attach
            _depth = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depth);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent, w, h, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depth, 0);

            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            //_rbo = Gl.GenRenderbuffer();
            //Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            //Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, w, h);
            //Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
            //Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                Console.WriteLine($"Framebuffer(ID={_fbo},texture={_texture}) is completed.");
            }
            else
            {
                Console.WriteLine("Framebuffer is errored!");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void UpdateFrame(int deltaTime, int w, int h, Camera camera)
        {
            float duration = deltaTime * 0.001f;

            OrbitCamera orbitCamera = camera as OrbitCamera;
            float g = 1.0f / (float)Math.Tan((orbitCamera.FOV * 0.5f).ToRadian());
            Polyhedron viewFrustum = ViewFrustum.BuildFrustumPolyhedron(orbitCamera.Position,
                orbitCamera.Forward, orbitCamera.Up, orbitCamera.Right, g, orbitCamera.AspectRatio, orbitCamera.NEAR, 100);
            _ocs.Update(orbitCamera, viewFrustum, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);

            Debug.Write("OccEntity=" + _ocs.OccludedEntity.Count + "개, ");
            Debug.Write(",CamPos=" + orbitCamera.Position);
            Debug.Write(",CameraPitch=" + orbitCamera.CameraPitch);
            Debug.Write(",CameraYaw=" + orbitCamera.CameraYaw);
            //(_glControl3.Ctrl("desc") as Ui2d.Label).Text = _ocs.OccludedEntity.Count + "개";
        }

        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            //----------------------------------------------------------------------------------------------------------------------
            // 1-pass : render-to-texture
            //----------------------------------------------------------------------------------------------------------------------
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.ClearColor(0, 1, 0, 1.0f);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            // 물체 렌더링
            foreach (OccluderEntity entity1 in _ocs.OccludedEntity)
            {
                Renderer3d.Render(_shaders.StaticShader, _shaders.ColorShader, entity1, camera, _glControl3.BackClearColor, new Vertex4f(0, 0, 1, 0), fogDensity: 0.003f);
            }

            //----------------------------------------------------------------------------------------------------------------------
            // 2-pass : render-to-texture
            //----------------------------------------------------------------------------------------------------------------------
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.ClearColor(0, 0, 0, 1.0f);
            Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            StaticShader shader = _shaders.StaticShader;

            shader.Bind();
            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogColor, _glControl3.BackClearColor);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogDensity, 0.003f);
            shader.LoadUniform(StaticShader.UNIFORM_NAME.fogPlane, new Vertex4f(0, 0, 1, 0));
            shader.LoadUniform(StaticShader.UNIFORM_NAME.camPos, camera.Position);

            int[] arrayEntity = new int[2] { 0, 1 };
            foreach (int index in arrayEntity)
            {
                Entity entity = _ocs.OccludedEntity[index];
                shader.LoadUniform(StaticShader.UNIFORM_NAME.mvp, camera.ProjectiveMatrix * camera.ViewMatrix * entity.ModelMatrix);

                // 모델을 그린다.
                foreach (RawModel3d rawModel in entity.Model)
                {
                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.EnableVertexAttribArray(1);
                    Gl.EnableVertexAttribArray(2);

                    if (entity.IsTextured)
                    {
                        shader.LoadUniform( StaticShader.UNIFORM_NAME.isTextured, true);
                        TexturedModel modelTextured = rawModel as TexturedModel;
                        if (modelTextured.Texture != null)
                        {
                            if (modelTextured.Texture.TexureType.HasFlag(Texture.TextureMapType.Diffuse))
                            {
                                shader.LoadTexture( StaticShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, index==0? _texture: _depth);
                            }
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                        }
                    }

                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);

                    Gl.DisableVertexAttribArray(2);
                    Gl.DisableVertexAttribArray(1);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }           

            shader.Unbind();


            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void KeyUpEvent(object sender, KeyEventArgs e)
        {

        }

        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            
        }

        private void FormFrameBuffer_Load(object sender, EventArgs e)
        {

        }
    }
}
