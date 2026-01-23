using BillBoard;
using Camera3d;
using Common;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormImposterBaker : Form
    {
        private const string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\Res";

        private ImpostorBaker _impostorBaker;
        private ImpostorShader _impShader;
        private ImpostorBakingShader _shader;
        private AABBBoxShader _aabbShader;

        private UnifiedTexturedModel _model;
        private WorldAxisRenderer _axisRenderer;

        private ImpostorSettings _settings;
        private AABB3f _aabbBox;
        private Matrix4x4f _transform;
        ImpostorBakeResult _result;
        private OrbitCamera _camera;
        PolygonMode _polygonMode = PolygonMode.Fill;
        bool _isVisibleAABB = true;
        bool _isVisibleAxis = true;
        bool _isImposterMode = false;

        Vertex3f _pos;
        float _scale;
        float _rotZ;

        private bool _isInitialized = false;
        private string _openFilePath = "";

        public FormImposterBaker()
        {
            InitializeComponent();

            // 파일 해시 매니저 초기화
            FileHashManager.ROOT_FILE_PATH = StrRes.PROJECT_PATH;
        }

        private void FormImposterBaker_Load(object sender, EventArgs e)
        {
            _impostorBaker = new ImpostorBaker();
            _shader = new ImpostorBakingShader(StrRes.PROJECT_PATH);
            _aabbShader = new AABBBoxShader(StrRes.PROJECT_PATH);
            _axisRenderer = new WorldAxisRenderer(StrRes.PROJECT_PATH);
            _axisRenderer.SetThick(1.0f);
            _impShader = new ImpostorShader(StrRes.PROJECT_PATH);

            glControl1.MouseWheel += glControl1_MouseWheel;
            glControl1.MouseDown += glControl1_MouseDown;
            glControl1.MouseMove += glControl1_MouseMove;
            glControl1.MouseUp += glControl1_MouseUp;

            this.textBox1.AppendText("=== 임포스터 베이커 도구 ===\r\n");
            this.textBox1.AppendText("파일 > 열기로 3D 모델을 선택하세요.\r\n\r\n");

            LoadModel(Path.Combine(PROJECT_PATH, "palm4.obj")); // MedievalHouse01
        }


        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.Filter = "3D Model Files|*.obj;*.fbx;*.dae|All Files|*.*";
            this.openFileDialog1.InitialDirectory = PROJECT_PATH;

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = this.openFileDialog1.FileName;
                LoadModel(filename);
            }
        }

        private void LoadModel(string filename)
        {
            try
            {
                this.textBox1.Clear();
                this.Text = $"임포스터 베이커 - {Path.GetFileName(filename)}";

                string outputDir = Path.Combine(Path.GetDirectoryName(filename), "imposter");
                _openFilePath = outputDir;

                // 출력 디렉토리 생성
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                string baseName = Path.GetFileNameWithoutExtension(filename);
                string metadataPath = Path.Combine(outputDir, baseName + ".json");

                // 모델 로드
                _model = ObjLoaderEx.LoadObjUnified(filename);

                _pos = new Vertex3f(Rand.NextFloat * 200, Rand.NextFloat * 200, Rand.NextFloat * 200);
                _rotZ = Rand.NextFloat * 360.0f;
                _scale = Rand.NextFloat * 10.0f;

                _transform = Matrix4x4f.Translated(_pos.x, _pos.y, _pos.z) *
                    Matrix4x4f.RotatedZ(_rotZ) *
                    Matrix4x4f.Scaled(_scale, _scale, _scale);

                _aabbBox = _model.AABB;

                this.textBox1.AppendText($"모델 로드 완료: {_model.Name}\r\n");
                this.textBox1.AppendText($"  - Vertices: {_model.VertexCount}\r\n");
                this.textBox1.AppendText($"  - AABB Center: {_model.AABB.Center}\r\n");
                this.textBox1.AppendText($"  - AABB Size: {_model.AABB.Size}\r\n");
                this.textBox1.AppendText($"  - AABB Max: {_model.AABB.Max}\r\n");
                this.textBox1.AppendText($"  - AABB Min: {_model.AABB.Min}\r\n");
                this.textBox1.AppendText($"  - Bounding Radius: {_model.AABB.Radius:F3}\r\n\r\n");

                // 카메라 초기화 (모델 중심 기준)
                InitializeCamera();
                _camera.PivotPosition = _pos;

                // 텍스처 저장
                string albedoPath = Path.Combine(outputDir, baseName + "_albedo.png");
                string normalPath = Path.Combine(outputDir, baseName + "_normal.png");
                string depthPath = Path.Combine(outputDir, baseName + "_depth.png");

                if (File.Exists(albedoPath) && File.Exists(normalPath) 
                    && File.Exists(depthPath) && File.Exists(metadataPath))
                {
                    if (_result == null) _result = new ImpostorBakeResult();
                    _result.AlbedoTextureID = new Texture(albedoPath, flipY: true).TextureID;
                    _result.NormalTextureID = new Texture(normalPath, flipY: true).TextureID;
                    _result.DepthTextureID = new Texture(depthPath, flipY: true).TextureID;
                    _result.Metadata = ImpostorMetadataLoader.LoadFromFile(metadataPath);
                    this.textBox1.AppendText("임포스터 가져오기 완료!\r\n");
                }
                else
                {
                    // 베이킹 시작
                    _settings = ImpostorSettings.CreateHighQuality(_model.Name);
                    _result = _impostorBaker.BakeAtlas(_model, _settings, _shader, metadataPath);
                    this.textBox1.AppendText("베이킹 완료!\r\n");
                }

                // 이미지 표시
                if (File.Exists(albedoPath))
                    this.pictureBox1.Image = Bitmap.FromFile(albedoPath);
                if (File.Exists(normalPath))
                    this.pictureBox2.Image = Bitmap.FromFile(normalPath);
                if (File.Exists(depthPath))
                    this.pictureBox3.Image = Bitmap.FromFile(depthPath);

                // 메타데이터 표시
                if (File.Exists(metadataPath))
                {
                    this.textBox1.AppendText("\r\n=== 메타데이터 ===\r\n");
                    this.textBox1.AppendText(File.ReadAllText(metadataPath) + "\r\n\r\n");
                }

                // GLControl 갱신
                glControl1.Invalidate();
            }
            catch (Exception ex)
            {
                this.textBox1.AppendText($"오류 발생: {ex.Message}\n");
                this.textBox1.AppendText($"{ex.StackTrace}\n\n");
                MessageBox.Show($"베이킹 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void glControl1_Render(object sender, OpenGL.GlControlEventArgs e)
        {
            // 초기화
            if (!_isInitialized)
            {
                InitializeOpenGL();
                _isInitialized = true;
            }

            // 기본 렌더링 설정
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);
            Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.Viewport(0, 0, glControl1.Width, glControl1.Height);
            Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 모델이 로드되지 않았으면 종료
            if (_model == null || _camera == null)
                return;

            // 카메라 업데이트
            _camera.Update(0);

            if (_isImposterMode)
            {
                Vector3f center = _result.Metadata.AABBCenter;
                _impShader.Bind();
                {
                    // 매 프레임 변경 데이터
                    _impShader.LoadVPMatrix(_camera.VPMatrix);
                    _impShader.LoadCameraPosition(_camera.Position);

                    // 불변 데이터 (내부 캐시)
                    _impShader.LoadImpostorAtlas(TextureUnit.Texture0, _result.AlbedoTextureID);
                    _impShader.LoadAABBSphereRadius(_result.Metadata.BoundingSphereRadius);
                    _impShader.LoadAABBCenterPosition(_result.Metadata.AABBCenter.X, _result.Metadata.AABBCenter.Y, _result.Metadata.AABBCenter.Z);
                    _impShader.LoadModelMatrix(_transform);
                    _impShader.LoadVerticalBoundAngle(_result.Metadata.VerticalAngleMin, _result.Metadata.VerticalAngleMax);
                    _impShader.LoadAtlasSize(_result.Metadata.AtlasSize);
                    _impShader.LoadIndividualSize(_result.Metadata.IndividualSize);
                    _impShader.LoadHorizontalFrames(_result.Metadata.HorizontalAngles);
                    _impShader.LoadVerticalFrames(_result.Metadata.VerticalAngles);
                    _impShader.LoadEnableEdgeLine(true, 3.0f);

                    // 렌더링
                    Gl.BindVertexArray(Renderer3d.Point.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
                _impShader.Unbind();
            }
            else
            {
                // 셰이더 바인딩
                _shader.Bind();
                {
                    // 행렬 계산
                    Matrix4x4f model = _transform;
                    Matrix4x4f view = _camera.ViewMatrix;
                    Matrix4x4f proj = _camera.ProjectiveMatrix;

                    Matrix4x4f mv = view * model;
                    Matrix4x4f mvp = proj * mv;

                    // Uniform 설정
                    _shader.LoadTransforms(mvp, mv, model);
                    _shader.LoadTextureArray(_model.TextureIDArray);

                    // 모델 렌더링
                    RenderModel(_model);
                }
                _shader.Unbind();
            }
            
            if (_isVisibleAABB)
            {
                Gl.Enable(EnableCap.Blend);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _aabbShader.RenderAABB(_aabbBox, _camera, new Vertex4f(1.0f, 0.0f, 0.0f, 0.3f));
            }

            if (_isVisibleAxis)
            {
                _axisRenderer.Render(_camera.VPMatrix);
            }
        }

        /// <summary>
        /// OpenGL 초기화
        /// </summary>
        private void InitializeOpenGL()
        {
            // 기본 OpenGL 상태 설정
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);
            Gl.FrontFace(FrontFaceDirection.Ccw);
        }

        /// <summary>
        /// 카메라 초기화
        /// </summary>
        private void InitializeCamera()
        {
            if (_model == null)
                return;

            // 모델 AABB 기준 카메라 설정
            Vertex3f center = _model.AABB.Center;
            float radius = _model.AABB.Radius;

            // OrbitCamera 초기화 (타겟을 모델 중심으로)
            _camera = new OrbitCamera(
                "camera",
                center.x,      // ✅ 타겟 X
                center.y,      // ✅ 타겟 Y
                center.z,      // ✅ 타겟 Z
                radius * 3.0f   // 거리
            );
            _camera.FOV = 45.0f;
            _camera.SetResolution(glControl1.Width, glControl1.Height);
            _camera.Start();
        }

        /// <summary>
        /// 모델 렌더링
        /// </summary>
        private void RenderModel(UnifiedTexturedModel model)
        {
            // CullFace 설정
            if (model.EnableCullFace)
            {
                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(model.CullFaceMode);
            }
            else
            {
                Gl.Disable(EnableCap.CullFace);
            }

            // VAO 바인딩
            Gl.BindVertexArray(model.VaoID);

            // 인덱스 버퍼로 렌더링
            Gl.DrawElements(
                PrimitiveType.Triangles,
                model.IndexCount,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero  // ✅ null 대신 IntPtr.Zero 사용
            );

            // VAO 언바인딩
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// OpenGL 텍스처를 PNG로 저장
        /// </summary>
        private void SaveTextureAsPNG(uint textureID, int width, int height, string path)
        {
            // 텍스처 데이터 읽기
            byte[] pixels = new byte[width * height * 4]; // RGBA
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // Bitmap 생성 및 고속 저장
            using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + y * stride;
                        int srcY = height - 1 - y; // 상하 반전

                        for (int x = 0; x < width; x++)
                        {
                            int srcIndex = (srcY * width + x) * 4;
                            int dstIndex = x * 4;

                            // RGBA -> BGRA 변환 (Bitmap은 BGRA 순서)
                            row[dstIndex + 0] = pixels[srcIndex + 2]; // B
                            row[dstIndex + 1] = pixels[srcIndex + 1]; // G
                            row[dstIndex + 2] = pixels[srcIndex + 0]; // R
                            row[dstIndex + 3] = pixels[srcIndex + 3]; // A
                        }
                    }
                }

                bitmap.UnlockBits(bmpData);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        /// <summary>
        /// 마우스 휠 - 줌
        /// </summary>
        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_camera == null)
                return;

            float delta = e.Delta > 0 ? 0.9f : 1.1f;
            _camera.Distance *= delta;

            glControl1.Invalidate();
        }

        /// <summary>
        /// 마우스 이동 - 회전
        /// </summary>
        private Point _lastMousePos;
        private bool _isRotating = false;

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isRotating = true;
                _lastMousePos = e.Location;
            }
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_camera == null || !_isRotating)
                return;

            int dx = e.X - _lastMousePos.X;
            int dy = e.Y - _lastMousePos.Y;

            _camera.CameraYaw -= dx * 0.5f;
            _camera.CameraPitch += dy * 0.5f;

            _lastMousePos = e.Location;
            glControl1.Invalidate();
        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isRotating = false;
            }
        }

        /// <summary>
        /// 폼 닫힐 때 리소스 정리
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            _impostorBaker?.Dispose();
        }

        private void fillModeFillLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_polygonMode == PolygonMode.Fill) 
                _polygonMode = PolygonMode.Line;
            else 
                _polygonMode = PolygonMode.Fill;
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void glControl1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("종료하시겠습니까?", "종료", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            }
            else if (e.KeyCode == Keys.F)
            {
                if (_polygonMode == PolygonMode.Fill)
                    _polygonMode = PolygonMode.Line;
                else
                    _polygonMode = PolygonMode.Fill;
            }
            else if (e.KeyCode == Keys.W)
            {
                _isImposterMode = !_isImposterMode;
            }
            else if (e.KeyCode == Keys.B)
            {
                _isVisibleAABB = !_isVisibleAABB;
            }
        }

        private void 지정폴더열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(_openFilePath))
            {
                MessageBox.Show("펄더를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 탐색기에서 파일을 선택한 상태로 열기
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_openFilePath}\"");
        }

        private void aABBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _isVisibleAABB = !_isVisibleAABB;
        }

        private void axisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _isVisibleAxis = !_isVisibleAxis;
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}