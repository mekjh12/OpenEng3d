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
using static Shader.NoiseShader;

namespace FormTools
{
    public partial class FormRealTimeCloudRendering : Form
    {
        private string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private OcclusionCullingSystem _ocs;
        private ShaderGroup _shaders;
        private GlControl3 _glControl3;

        bool _isIncludedInBox;
        Vertex3f _cloudPosition = Vertex3f.Zero;
        Vertex3f _cloudSize = Vertex3f.One;
        CloudFrameBuffer _cloudFrameBuffer;
        CloudFrameBuffer _drawFrameBuffer;
        NullDepthShader _nullDepthShader;
        NullDepthShader3 _nullDepthShader3;

        public FormRealTimeCloudRendering()
        {
            InitializeComponent();

            // GL 생성
            _glControl3 = new GlControl3("real-time-cloud-rendering", Application.StartupPath, @"\fonts\fontList.txt", @"\Res\")
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                IsVisibleGrid = true,
                PolygonMode = PolygonMode.Fill,
                BackClearColor = new Vertex3f(0, 0, 0),
            };

            _glControl3.Init += (w, h) => Init(w, h);
            _glControl3.Init3d += (w, h) => Init3d(w, h);
            _glControl3.Init2d += (w, h) => Init2d(w, h);
            _glControl3.UpdateFrame = (deltaTime, w, h, camera) => UpdateFrame(deltaTime, w, h, camera);
            _glControl3.RenderFrame = (deltaTime, w, h, backcolor, camera) => RenderFrame(deltaTime, backcolor, camera);
            _glControl3.MouseDown += (s, e) => MouseDnEvent(s, e);
            _glControl3.MouseUp += (s, e) => MouseUpEvent(s, e);
            //_glControl3.KeyDown += (s, e) => KeyDownEvent(s, e);
            //_glControl3.KeyUp += (s, e) => KeyUpEvent(s, e);
            _glControl3.Start();
            _glControl3.SetVisibleMouse(true);
            Controls.Add(_glControl3);
        }

        private void FormRealTimeCloudRendering_Load(object sender, EventArgs e)
        {

        }

        public void Init(int width, int height)
        {
            // 랜덤변수 생성
            Rand.InitSeed(500);

            // 쉐이더 초기화
            _shaders = new ShaderGroup()
            {
                IsColorShader = true,
                IsStaticShader = true,
                IsInfiniteGridShader = true,
                IsNoiseShader = true,
                IsScreenShader = true,
            };
            _shaders.Create(PROJECT_PATH);
            _shaders.NoiseShader.LoadTextureCloud(PROJECT_PATH);
            _glControl3.ColorShader = _shaders.ColorShader;

            // 프레임버퍼 생성
            _cloudFrameBuffer = new CloudFrameBuffer();
            _cloudFrameBuffer.CreateBuffer(width, height);

            _drawFrameBuffer = new CloudFrameBuffer();
            _drawFrameBuffer.CreateBuffer2(width, height);

            _nullDepthShader = new NullDepthShader(PROJECT_PATH);
            _nullDepthShader3 = new NullDepthShader3(PROJECT_PATH);
        }

        private void Init2d(int w, int h)
        {
            _glControl3.AddValueBar("stepLength", value: 0.01f, minValue: 0.001f, maxValue: 0.02f, backColor: new Vertex3f(0.6f, 0.3f, 0)).StepValue = 0.001f;
            _glControl3.AddValueBar("cloudtype", value: 1.0f, minValue: 1.0f, maxValue: 10.0f, backColor: new Vertex3f(0.6f, 0.3f, 0)).StepValue = 0.1f;

            _glControl3.AddValueBar("boundHeight", value: 1, minValue: 1, maxValue: 20, stepValue: 1).ValueChange += (obj, value) => _cloudPosition.z = value;
            _glControl3.AddValueBar("boundSizeX", value: 1, minValue: 1, maxValue: 100, stepValue: 1).ValueChange += (obj, value) => _cloudSize.x = value;
            _glControl3.AddValueBar("boundSizeY", value: 1, minValue: 1, maxValue: 100, stepValue: 1).ValueChange += (obj, value) => _cloudSize.y = value;
            _glControl3.AddValueBar("boundSizeZ", value: 1, minValue: 1, maxValue: 100, stepValue: 1).ValueChange += (obj, value) => _cloudSize.z = value;

            _glControl3.AddCheckBar("highQuality", "highQuality");
            _glControl3.AddCheckList("noiseType", "", new string[] { "base", "base-lowFreq" }, fontSize: 1.9f).ChangeValue += (obj, d, b) => Debug.PrintLine(obj.Value + "=" + obj.SelectedIndex);

            _glControl3.AddLabel("cam", "camera position, yaw, pitch", align: Ui2d.Control.CONTROL_ALIGN.ROOT_BL, foreColor: new Vertex3f(1, 1, 0));
            _glControl3.AddLabel("ocs", "ocs", align: Ui2d.Control.CONTROL_ALIGN.ADJOINT_TOP, foreColor: new Vertex3f(1, 1, 0));

            // ini file을 읽고 컨트롤에 매칭한다.
            _glControl3.SimpHValueBar("stepLength").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "stepLength", "0.005"));
            _glControl3.IsVisibleDebug = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleDebugWindow", "False"));
            _glControl3.IsVisibleGrid = bool.Parse(IniFile.GetPrivateProfileString("sysInfo", "visibleGrid", "False"));

            _cloudSize.x = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "boundSizeX", "1.0"));
            _cloudSize.y = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "boundSizeY", "1.0"));
            _cloudSize.z = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "boundSizeZ", "1.0"));
            _cloudPosition.z = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "boundHeight", "1.0"));

            _glControl3.SimpHValueBar("boundSizeX").Value = _cloudSize.x;
            _glControl3.SimpHValueBar("boundSizeY").Value = _cloudSize.y;
            _glControl3.SimpHValueBar("boundSizeZ").Value = _cloudSize.z;
            _glControl3.SimpHValueBar("boundHeight").Value = _cloudPosition.z;

            // in my house with GPU.
            if (Screen.PrimaryScreen.DeviceName.IndexOf("DISPLAY4") > 0)
            {
                _glControl3.FullScreen(true);
                _glControl3.SimpHValueBar("stepLength").Value = float.Parse(IniFile.GetPrivateProfileString("sysInfo", "stepLength", "0.05"));
            }
        }

        private void Init3d(int w, int h)
        {
            // grid init
            _glControl3.InitGridShader(PROJECT_PATH);

            // OCS init
            _ocs = new OcclusionCullingSystem(PROJECT_PATH);
            _ocs.AddRawModel("brick", Loader3d.LoadCube(2, 2), "\\Res\\bricks.jpg");

            // Entity add
            int count = 10;
            for (int i = -count; i <= count; i++)
            {
                for (int j = -count; j <= count; j++)
                {
                    //_ocs.AddEntity($"brick{i}", "brick", new Vertex3f(j + 2, i + 2, 0), Vertex3f.One * 0.25f);
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




            // 타이트한 박스의 경우에 경계를 왔다갔다하는 경우에 화면이 급격하게 변하기 때문에,
            // 느스한 경계박스를 이용하여 박스의 안과 밖 유무를 판단함.
            float errFixDist = 1.02f; 
            _isIncludedInBox = Clouds.IsIncludedByBox(orbitCamera.Position, _cloudPosition - _cloudSize * errFixDist, _cloudPosition + _cloudSize * errFixDist);

            _glControl3.CLabel("ocs").Text = $"OccEntity={_ocs.OccludedEntity.Count}, Total={_ocs.EntityTotalCount}";
            _glControl3.CLabel("cam").Text = $"CamPos={orbitCamera.Position},CameraPitch={orbitCamera.CameraPitch},CameraYaw={orbitCamera.CameraYaw}";
        }

        private void RenderFrame(int deltaTime, Vertex4f backcolor, Camera camera)
        {
            int w = _glControl3.Width;
            int h = _glControl3.Height;

            float r = _glControl3.BackClearColor.x;
            float g = _glControl3.BackClearColor.y;
            float b = _glControl3.BackClearColor.z;

            OrbitCamera orbitCamera = camera as OrbitCamera;

            //-------------------------------------------------------------------------------------------
            // one-pass rendering
            //-------------------------------------------------------------------------------------------
            #region cloud rendering

            _cloudFrameBuffer.BindFrameBuffer();

            Gl.Enable(EnableCap.DepthTest);
            Gl.ClearColor(r, g, b, 1);
            Gl.ClearDepth(1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _nullDepthShader.Bind();
            Gl.BindVertexArray(Renderer3d.Quad.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _nullDepthShader.Unbind();

            // 노이즈쉐이더를 시작한다.
            NoiseShader shader = _shaders.NoiseShader;
            shader.Bind();

            shader.BindCloudTexture();

            shader.LoadUniform(NoiseShader.UNIFORM_NAME.focal_length, camera.FocalLength);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.viewport_size, new Vertex2f(w, h));
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.ray_origin, camera.Position);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.aspect_ratio, camera.AspectRatio);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.highQuality, _glControl3.SimpCheckBox("highQuality").Checked);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.stepLength, _glControl3.SimpHValueBar("stepLength").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.debug, _glControl3.CheckList("noiseType").SelectedIndex);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.cloudtype, _glControl3.SimpHValueBar("cloudtype").Value);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.cloudPosition, _cloudPosition);
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.cloudSize, _cloudSize);

            Matrix4x4f V = orbitCamera.ViewMatrix;
            shader.LoadUniform(NoiseShader.UNIFORM_NAME.view, V);

            // 카메라가 구름 안에 들어가면 앞면에 항상 사각형 상자를 띄운다.
            if (_isIncludedInBox)
            {
                Gl.Disable(EnableCap.DepthTest);
                Matrix4x4f M = Matrix4x4f.Translated(-1,-1,0);
                    //* Matrix4x4f.Scaled(2,2,2);
                Matrix4x4f View = Matrix4x4f.LookAtDirection(Vertex3f.One * camera.NEAR, -Vertex3f.UnitZ, Vertex3f.UnitY).Inverse;
                Matrix4x4f MVP = camera.ProjectiveMatrix * View * M;
                shader.LoadUniform(NoiseShader.UNIFORM_NAME.model, M);
                shader.LoadUniform(NoiseShader.UNIFORM_NAME.mvp, MVP);
                Gl.BindVertexArray(Renderer3d.Rect.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Rect.VertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
                Gl.Enable(EnableCap.DepthTest);
            }
            else
            {
                Matrix4x4f M = Matrix4x4f.Translated(_cloudPosition.x, _cloudPosition.y, _cloudPosition.z)
                    * Matrix4x4f.Scaled(_cloudSize.x, _cloudSize.y, _cloudSize.z);
                Matrix4x4f MVP = camera.ProjectiveMatrix * V * M;
                shader.LoadUniform(NoiseShader.UNIFORM_NAME.model, M);
                shader.LoadUniform(NoiseShader.UNIFORM_NAME.mvp, MVP);
                Gl.BindVertexArray(Renderer3d.Cube.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
            _cloudFrameBuffer.UnbindFrameBuffer();


            //Gl.Disable(EnableCap.Blend);
            #endregion


            //-------------------------------------------------------------------------------------------
            // 1-pass rendering
            //-------------------------------------------------------------------------------------------
            _drawFrameBuffer.BindFrameBuffer();
            Gl.Enable(EnableCap.DepthTest);
            Gl.ClearColor(r, g, b, 1);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.PolygonMode(MaterialFace.Front, _glControl3.PolygonMode);

            _nullDepthShader3.Bind();
            _nullDepthShader3.LoadUniform(NullDepthShader3.UNIFORM_NAME.backColor, new Vertex3f(r, g, b));
            Gl.BindVertexArray(Renderer3d.Quad.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _nullDepthShader3.Unbind();

            StaticShader sShader = _shaders.StaticShader;
            sShader.Bind();
            sShader.LoadUniform(StaticShader.UNIFORM_NAME.proj, orbitCamera.ProjectiveMatrix);
            sShader.LoadUniform(StaticShader.UNIFORM_NAME.view, orbitCamera.ViewMatrix);

            foreach (OccluderEntity entity in _ocs.OccludedEntity)
            {
                OccluderEntity occlusionEntity = (entity is OccluderEntity) ? entity as OccluderEntity : null;

                sShader.LoadUniform(StaticShader.UNIFORM_NAME.fogColor, _glControl3.BackClearColor);
                sShader.LoadUniform(StaticShader.UNIFORM_NAME.fogDensity, 0.003f);
                sShader.LoadUniform(StaticShader.UNIFORM_NAME.fogPlane, new Vertex4f(0, 0, 1, 0));
                sShader.LoadUniform(StaticShader.UNIFORM_NAME.camPos, camera.Position);
                sShader.LoadUniform(StaticShader.UNIFORM_NAME.viewport_size, new Vertex2f(camera.Width, camera.Height));

                sShader.LoadUniform(StaticShader.UNIFORM_NAME.model, entity.ModelMatrix);
                sShader.LoadUniform(StaticShader.UNIFORM_NAME.mvp, orbitCamera.ProjectiveMatrix * orbitCamera.ViewMatrix * entity.ModelMatrix);

                // 모델을 그린다.
                foreach (RawModel3d rawModel in entity.Model)
                {
                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.EnableVertexAttribArray(1);
                    Gl.EnableVertexAttribArray(2);

                    if (entity.IsTextured)
                    {
                        sShader.LoadUniform(StaticShader.UNIFORM_NAME.isTextured, true);
                        TexturedModel modelTextured = rawModel as TexturedModel;
                        if (modelTextured.Texture != null)
                        {
                            if (modelTextured.Texture.TexureType.HasFlag(Texture.TextureMapType.Diffuse))
                            {
                                sShader.LoadTexture(StaticShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.DiffuseMapID);
                            }
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                        }
                    }

                    sShader.LoadUniform(StaticShader.UNIFORM_NAME.color, entity.Material.Ambient);

                    if (occlusionEntity is OccluderEntity)
                    {
                        //if (occlusionEntity.IsVisibleLOD)
                        {
                            if (occlusionEntity != null)
                            {
                                //if (occlusionEntity.LOD == 0) sShader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0, 1));
                                //if (occlusionEntity.LOD == 1) sShader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0, 1));
                                //if (occlusionEntity.LOD == 2) sShader.LoadUniform(StaticShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1, 1));
                            }
                        }
                    }

                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);

                    Gl.DisableVertexAttribArray(2);
                    Gl.DisableVertexAttribArray(1);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }

            }
            sShader.Unbind();
            Gl.Disable(EnableCap.Blend);
            _drawFrameBuffer.UnbindFrameBuffer();

            //Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _cloudFrameBuffer.FrameBufferID);
            //Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            //Gl.BlitFramebuffer(0, 0, w, h, 0, 0, w, h, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            //Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.Enable(EnableCap.DepthTest);
            Gl.ClearColor(r, g, b, 1);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            ScreenShader screenShader = _shaders.ScreenShader;
            screenShader.Bind();
            screenShader.LoadUniform(ScreenShader.UNIFORM_NAME.viewport_size, new Vertex2f(w, h));
            screenShader.LoadUniform(ScreenShader.UNIFORM_NAME.isPositionInBox, _isIncludedInBox);
            screenShader.LoadTexture(ScreenShader.UNIFORM_NAME.screenTexture1, TextureUnit.Texture0, _cloudFrameBuffer.ColorTextureID);
            screenShader.LoadTexture(ScreenShader.UNIFORM_NAME.depthTexture1, TextureUnit.Texture1, _cloudFrameBuffer.DepthTextureID);
            screenShader.LoadTexture(ScreenShader.UNIFORM_NAME.screenTexture2, TextureUnit.Texture2, _drawFrameBuffer.ColorTextureID);
            screenShader.LoadTexture(ScreenShader.UNIFORM_NAME.depthTexture2, TextureUnit.Texture3, _drawFrameBuffer.DepthTextureID);
            screenShader.LoadUniform(ScreenShader.UNIFORM_NAME.backgroundColor, new Vertex3f(r, g, b));

            Gl.DepthMask(false); // don't write a depth-buffer.
            Gl.BindVertexArray(Renderer3d.Quad.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            screenShader.Unbind();
            Gl.DepthMask(true); // restore a depth mask to writable in depthbuffer.



            /*
            //-------------------------------------------------------------------------------------------
            // 2-pass rendering
            //-------------------------------------------------------------------------------------------
           
            */

            // 카메라 중심점 렌더링
            Renderer3d.RenderPoint(_shaders.ColorShader, camera.Position, camera, new Vertex4f(1, 1, 0, 1), 0.02f);
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
    }
}
