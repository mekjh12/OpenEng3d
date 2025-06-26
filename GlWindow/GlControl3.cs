using Camera3d;
using Common.Abstractions;
using GlWindow.Properties;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Ui2d;
using ZetaExt;

namespace GlWindow
{
    /// <summary>
    /// 호출순서 
    /// formload->init->init3d->init2d->loop(update->render)
    /// </summary>
    public class GlControl3 : GlControl
    {
        private string _name = "glcontrol";

        private uint _tick = 0;

        [DllImport("user32.dll")] private static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")] public static extern Int32 SetCursorPos(Int32 x, Int32 y);

        public void ShowSystemMouse(bool bShow) { ShowCursor(bShow); }

        private Dictionary<Key, bool> _onPrevPressed = new Dictionary<Key, bool>();

        public enum MOUSE_GAME_MODE
        {
            NONE, CAMERA_ROUND_ROT, CAMERA_ROUND_ROT2
        };

        private Vertex3f _backColor = Vertex3f.Zero;

        private PolygonMode _polygonMode = PolygonMode.Fill;
        private MOUSE_GAME_MODE _mouseMode = MOUSE_GAME_MODE.NONE;
        private Vertex3f _prevPos = Vertex3f.Zero;

        private Camera _camera;
        private int _width = 0;
        private int _height = 0;
        private Size _prevSize = new Size(0, 0);
        private Point _prevLocation = new Point(0, 0);

        private bool _isInitiazed = false;
        private bool _isRunning = false;
        private bool _isUi2dMode = false;
        private bool _isEnableCameraMove = true;
        private float _cameraStepLength = 0.1f;

        private ColorShader _colorShader;

        protected event Action<int, int> _init;
        protected event Action<int, int> _init3d;
        protected event Action<int, int, int, Camera> _update;
        protected event Action<int, int> _init2d;
        protected event Action<int, float, float, Vertex4f, Camera> _render;

        protected Action<object, System.Windows.Forms.KeyEventArgs> _keyUp;
        protected Action<object, System.Windows.Forms.KeyEventArgs> _keyDown;

        private string _FontResourceFileName = "";
        private string _Ui2dResourcePath = "";
        private string _RootPath = "";

        private bool _isMouseVisible = true;
        private bool _isVisibleGrid = false;
        private bool _isVisibleUi2d = true;
        private string _helpText = "<HELP><br>";

        private Vertex2i _mousePosition = Vertex2i.Zero;
        private Vertex2f _mouseDeltaPos = Vertex2f.Zero;
        private float _mouseWheelValue = 0.0f;

        private struct POINT
        { public int X; public int Y; }

        private static Vertex2i _windowOffSet = Vertex2i.Zero;
        private static Vertex2f _currentMousePointFloat = Vertex2f.Zero;
        private Vertex2f _prevMousePosition = Vertex2f.Zero;

        private InfiniteGrid _grid;

        private bool _isFullscreen = false;

        Ui2d.Control _lastControl;

        private Vertex2f _cameraPrevAngle = Vertex2f.Zero;

        /// <summary>
        /// 초기화 전에 설정해야 반영된다.
        /// </summary>
        public string HelpText
        {
            set => _helpText += value.Replace("/", "<br>");
        }

        public bool IsUi2dMode
        {
            get => _isUi2dMode;
            set
            {
                _isUi2dMode = value;
                _isMouseVisible = value;
            }
        }

        public bool IsEnableCameraMove
        {
            get => _isEnableCameraMove;
            set => _isEnableCameraMove = value;
        }

        public bool IsVisibleGrid
        {
            get => _isVisibleGrid;
            set => _isVisibleGrid = value;
        }

        public Vertex3f BackClearColor
        {
            get => _backColor;
            set => _backColor = value;
        }

        public PolygonMode PolygonMode
        {
            get => _polygonMode;
            set => _polygonMode = value;
        }

        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        public Action<int, int> Init
        {
            set => _init += value;
            get => _init;
        }

        public Action<int, int> Init3d
        {
            set => _init3d = value;
            get => _init3d;
        }

        public Action<int, int> Init2d
        {
            set => _init2d = value;
            get => _init2d;
        }

        public Action<int, int, int, Camera> UpdateFrame
        {
            get => _update;
            set => _update = value;
        }

        public Action<int, float, float, Vertex4f, Camera> RenderFrame
        {
            get => _render;
            set => _render = value;
        }

        public bool IsMouseVisible
        {
            get => _isMouseVisible;
        }

        public bool IsVisibleDebug
        {
            get => CLabel("debug") == null ? false : CLabel("debug").IsVisible;
            set => CLabel("debug").IsVisible = value;
        }

        public MOUSE_GAME_MODE MouseMode
        {
            get => _mouseMode; 
            set => _mouseMode = value;
        }

        public uint Tick
        {
            get => _tick; 
            set => _tick = value;
        }
        public ColorShader ColorShader
        {
            get => _colorShader; 
            set => _colorShader = value;
        }

        public bool IsVisibleUi2d
        {
            get => _isVisibleUi2d; 
            set => _isVisibleUi2d = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="rootPath">실행파일의 경로</param>
        /// <param name="fontResourceFileName">실행파일에서의 폰트리소트 상대경로(파일리스트+폰트)</param>
        /// <param name="ui2dResourcePath">UI2d를 위한 리소스 상대경로</param>
        /// <param name="colorShader">ColorShader</param>
        public GlControl3(string name, string rootPath, string fontResourceFileName, string ui2dResourcePath, bool mouseUsed = false)
        {
            _name = name;

            // 경로를 설정한다.
            _RootPath = rootPath;
            _FontResourceFileName = rootPath + fontResourceFileName;
            _Ui2dResourcePath = rootPath + ui2dResourcePath;
            IniFile.s_PATH_ROOT = rootPath;
            IniFile.SetFileName($"setup_{_name}.ini");

            // system-mouse setup
            ShowCursor(mouseUsed);
            UIEngine.EnableMouse = mouseUsed;

            // setup
            Animation = true;
            MultisampleBits = ((uint)(0u));
            ColorBits = 24;
            DepthBits = 24;
            StencilBits = 8;
            SwapInterval = 10;
            DoubleBuffer = true;
            Render += GlControl3_Render;
            MouseWheel += (o, e) => { _mouseWheelValue = e.Delta; };

            // 그리드를 생성한다.
            _grid = new InfiniteGrid();

            // 시스템 정보 출력
            Console.WriteLine("========================================================");
            Console.WriteLine("                  OpenGL3d 시스템 정보");
            Console.WriteLine("========================================================");
            Console.WriteLine(" * 오른손 좌표계 사용: x축 right, y축 forward, z축 up.");
            Console.WriteLine(" * 지형시스템: x축 양의 방향 동쪽, y축 양의 방향 북쪽");
            Console.WriteLine("========================================================");

            // GPU 정보 출력
            string vendor = Gl.GetString(StringName.Vendor);
            string renderer = Gl.GetString(StringName.Renderer);
            string version = Gl.GetString(StringName.Version);
            Console.WriteLine($"GPU 제조사: {vendor}");
            Console.WriteLine($"렌더러: {renderer}");
            Console.WriteLine($"OpenGL 버전: {version}");
            Console.WriteLine("========================================================");
        }

        /// <summary>
        /// 부모컨트롤의 상속으로 매프레임의 주요 콜함수이다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlControl3_Render(object sender, GlControlEventArgs e)
        {
            if (!_isRunning) return;
            if (!_isInitiazed)
            {
                // GPU 정보 표기
                string vendor = Gl.GetString(StringName.Vendor);
                string renderer = Gl.GetString(StringName.Renderer);
                string version = Gl.GetString(StringName.Version);
                Console.WriteLine($"GPU 제조사: {vendor}");
                Console.WriteLine($"렌더러: {renderer}");
                Console.WriteLine($"OpenGL 버전: {version}");
                
                // 버전 파싱 (예: "4.5.0" -> 메이저 버전 4, 마이너 버전 5)
                string[] parts = version.Split('.');
                if (parts.Length >= 2)
                {
                    int major = int.Parse(parts[0]);
                    int minor = int.Parse(parts[1]);

                    if (major < 4 || (major == 4 && minor < 3))
                    {
                        Console.WriteLine("경고: 컴퓨트 셰이더는 OpenGL 4.3 이상이 필요합니다!");
                    }
                }
                Console.WriteLine(" ");

                string shaderPath = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\UIDesign2d\Shader\";
                InitGlControl(shaderPath);
                if (_init2d != null) _init2d(Width, Height);
            }

            _tick++;
            if (_tick >= uint.MaxValue) _tick = 0;

            // 다음 프레임에서 프레임텍스트를 쓰기 위해서 이전 프레임 문자를 지운다.
            Debug.ClearFrameText();
            Debug.ClearPointAndLine();

            GetMouseInputFromWinAPI(Parent.Left, Parent.Top, Width, Height);

            // 엔진 루프, 처음 로딩시 deltaTime이 커지는 것을 방지
            if (FramePerSecond.DeltaTime < 1000)
            {
                int deltaTime = FramePerSecond.DeltaTime;

                if (_update != null)
                {
                    Update(deltaTime);
                }

                // ui2d를 업데이트한다.
                Update2d(deltaTime, _mouseWheelValue);
                _mouseWheelValue = 0;

                if (_render != null)
                {
                    Render3d(deltaTime);
                }
            }

            // fps업데이트
            FramePerSecond.Update();
        }

        /// <summary>
        /// 시작한다.
        /// </summary>
        public void Start()
        {
            // 필수 이벤트들이 설정되어 있는지 확인
            var requiredEvents = new Dictionary<string, object>
            {
                { "Init", _init },
                { "Init3d", _init3d },
                { "Init2d", _init2d },
                { "UpdateFrame", _update },
                { "RenderFrame", _render },
            };

            foreach (var eventPair in requiredEvents)
            {
                if (eventPair.Value == null)
                {
                    //throw new InvalidOperationException($"필수 이벤트가 설정되지 않았습니다. {eventPair.Key} 이벤트를 먼저 설정해주세요.");
                }
            }

            _isRunning = true;
        }

        /// <summary>
        /// 멈춘다.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 카메라를 설정한다.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="distance"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        public void SetCamera(float x, float y, float z, float distance, float pitch, float yaw)
        {
            _camera = new OrbitCamera("orbitCamera", x, y, z, distance);
            _camera.CameraPitch = pitch;
            _camera.CameraYaw = yaw;
            _camera.FAR = 10000.0f;
            _camera.NEAR = 1.0f;
        }

        /// <summary>
        /// 매 프레임마다 키를 검사하여 지정된 행동을 수행한다.
        /// </summary>
        private void CheckKeyBoardToDo()
        {
            if (_camera is OrbitCamera)
            {
                OrbitCamera orbitCamera = (OrbitCamera)_camera;
                if (Keyboard.IsKeyDown(Key.Z)) orbitCamera.Distance = 900.0f;
                if (Keyboard.IsKeyDown(Key.X)) orbitCamera.Distance = 150.0f;
                if (Keyboard.IsKeyDown(Key.C)) orbitCamera.Distance = 10.0f;

                if (Keyboard.IsKeyDown(Key.W)) orbitCamera.GoForward(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.S)) orbitCamera.GoForward(-_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.D)) orbitCamera.GoRight(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.A)) orbitCamera.GoRight(-_cameraStepLength);

                if (Keyboard.IsKeyDown(Key.E)) orbitCamera.GoUp(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.Q)) orbitCamera.GoUp(-_cameraStepLength);
            }
        }

        /// <summary>
        /// 윈도우api로부터 마우스의 위치좌표와 위치변화량을 가져온다.
        /// </summary>
        /// <param name="ox">컨트롤의 화면의 절대위치 X</param>
        /// <param name="oy">컨트롤의 화면의 절대위치 Y</param>
        /// <param name="width">컨트롤의 화면너비</param>
        /// <param name="height">컨트롤의 화면높이</param>
        private void GetMouseInputFromWinAPI(int ox, int oy, int width, int height)
        {
            _windowOffSet = new Vertex2i(ox, oy);

            POINT point;
            GetCursorPos(out point);
            float fx = (float)(point.X - ox) / (float)width;
            float fy = (float)(point.Y - oy) / (float)height;
            _currentMousePointFloat = new Vertex2f(fx, fy);
            Vertex2f currentPoint = new Vertex2f(fx, fy);
            Vertex2f delta = currentPoint - _prevMousePosition;
            _mouseDeltaPos.x = (float)delta.x;
            _mouseDeltaPos.y = (float)delta.y;
            _mousePosition.x = point.X;
            _mousePosition.y = point.Y;
        }

        public void Update(int deltaTime)
        {
            if (_camera == null)
            {
                _camera = new OrbitCamera("OrbitCamera001", 0, 0, 0, 10);
            }

            if (_camera.Width * _camera.Height == 0)
            {
                _camera?.Init(Width, Height);
            }

            // 키보드를 체크하여 실행한다.
            CheckKeyBoardToDo();

            // 카메라를 업데이트한다.
            if (_isEnableCameraMove)
            {
                _camera?.Update(deltaTime);
            }

            // 컨트롤에 부과된 액션을 실행한다.
            _update(deltaTime, _width, _height, _camera);

            // 그리드 보임유무를 업데이트한다.
            if (_isVisibleGrid)
            {
                _grid?.Update(deltaTime);
            }
        }

        public void Render3d(int deltaTime)
        {
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);
            Gl.ClearColor(_backColor.x, _backColor.y, _backColor.z, 1.0f);
            Gl.Enable(EnableCap.DepthTest);
            Gl.Viewport(0, 0, Width, Height);
            Gl.PolygonMode(MaterialFace.Front, _polygonMode);

            // 3d렌더링
            if (_render != null)
            {
                _render(deltaTime, _width, _height, _backColor, _camera);
            }

            // 그리드를 렌더링한다.
            if (_isVisibleGrid)
            {
                _grid?.Render(_camera);
            }

            // UI렌더링
            if (IsVisibleUi2d)
            {
                Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
                UIEngine.RenderFrame(deltaTime);
            }
        }

        /// <summary>
        /// ui2d를 업데이트한다.
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="mouseWheelValue"></param>
        private void Update2d(int deltaTime, float mouseWheelValue)
        {
            int glLeftMargin = Parent.Width - this.Width;
            int glTopMargin = Parent.Height - this.Height;


            CLabel("fps").Text = "프레임율(" + FramePerSecond.FPS + $"FPS {_tick}) {_camera.Direction}";
            CLabel("debug").Text = Debug.Text;

            UIEngine.MouseUpdateFrame(Parent.Left + glLeftMargin, Parent.Top + glTopMargin, Width, Height, mouseWheelValue);
            UIEngine.UpdateFrame(deltaTime);
        }

        public void InitGridShader(string path)
        {
            // grid를 초기화한다.
            _grid.Init(path, Width, Height);
        }

        /// <summary>
        /// 해상도를 조절한다.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public void SetResolution(int w, int h)
        {
            _prevLocation = new Point(Parent.Left, Parent.Top);
            _prevSize = new Size(Width, Height);
            _width = w;
            _height = h;
            _camera.SetResolution(w, h);
            Gl.Viewport(0, 0, _width, _height);
        }

        private void InitGlControl(string path)
        {
            // random system init
            Rand.InitSeed(500);

            // 초기화를 한다.
            if (_init != null)
            {
                _init(Width, Height);
            }

            if (_init3d != null)
            {
                // 카메라 설정
                float cx = float.Parse(IniFile.GetPrivateProfileString("camera", "x", "0.0"));
                float cy = float.Parse(IniFile.GetPrivateProfileString("camera", "y", "0.0"));
                float cz = float.Parse(IniFile.GetPrivateProfileString("camera", "z", "0.0"));
                float yaw = float.Parse(IniFile.GetPrivateProfileString("camera", "yaw", "0.0"));
                float pitch = float.Parse(IniFile.GetPrivateProfileString("camera", "pitch", "0.0"));
                float dist = float.Parse(IniFile.GetPrivateProfileString("camera", "dist", "10.0"));
                SetCamera(cx, cy, cz, dist, pitch, yaw);

                // 3d를 초기화한다.
                _init3d(Width, Height);
            }

            _isInitiazed = true;

            MouseLeave += (s, e) => ShowSystemMouse(true);

            MouseEnter += (s, e) => ShowSystemMouse(false);

            Resize += (s, e) =>
            {
                UIEngine.Width = this.Width;
                UIEngine.Height = this.Height;
            };

            // 마우스 이동에 관한 기능
            MouseWheel += (s, e) =>
            {
                OrbitCamera camera = _camera as OrbitCamera;

                if (_isUi2dMode) return;

                if (UIEngine.GetUIEngine("sysInfo").CurrentOverControl == null)
                {
                    if (camera is OrbitCamera)
                    {
                        camera?.FarAway(-(float)(0.001f * camera.Distance * e.Delta));
                    }
                }
            };

            MouseMove += (s, e) =>
            {
                Mouse.CurrentPosition = new Vertex2i(e.X, e.Y);
                Vertex2i delta = Mouse.DeltaPosition;

                if (MouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT)
                {
                    // 카메라를 회전
                    _camera?.Yaw(-delta.x);
                    _camera?.Pitch(delta.y);
                }
                else if (MouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT2)
                {
                    // 카메라를 회전
                    _camera?.Yaw(-delta.x);
                    _camera?.Pitch(delta.y);
                }

                Mouse.PrevPosition = new Vertex2i(e.X, e.Y);
            };

            KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.F)
                {
                    _polygonMode = _polygonMode == PolygonMode.Fill ? PolygonMode.Line : PolygonMode.Fill;
                }
                else if (e.KeyCode == Keys.G)
                {
                    _isVisibleGrid = !_isVisibleGrid;
                    IniFile.WritePrivateProfileString("sysInfo", "visibleGrid", _isVisibleGrid.ToString());
                }
                else if (e.KeyCode == Keys.F2)
                {
                    IsVisibleDebug = !IsVisibleDebug;
                    IniFile.WritePrivateProfileString("sysInfo", "visibleDebugWindow", IsVisibleDebug.ToString());
                }
                else if (e.KeyCode == Keys.P)
                {
                    _isRunning = !_isRunning;
                }
                else if (e.KeyCode == Keys.Tab)
                {
                    ShowUi2dDialog(!_isUi2dMode);
                }
                else if (e.KeyCode == Keys.D0)
                {
                    _camera.Position = Vertex3f.Zero;
                    if (_camera is OrbitCamera)
                    {
                        (_camera as OrbitCamera).Distance = 1.0f;
                    }
                }
                else if (e.KeyCode == Keys.F3)
                {
                    CaptureScreen();
                }
                else if (e.KeyCode == Keys.F1)
                {
                    // 전체화면을 설정한다.
                    Form frm = (Form)Parent;
                    _isFullscreen = !_isFullscreen;
                    FullScreen(_isFullscreen);
                }

                if (_keyUp != null)
                {
                    _keyUp(s, e);
                }
            };

            KeyDown += (s, e) =>
            {
                OrbitCamera camera = (OrbitCamera)_camera;
                if (e.KeyCode == Keys.Escape)
                {
                    _isMouseVisible = false;
                    _isEnableCameraMove = false;
                    ShowSystemMouse(true);

                    _isUi2dMode = !_isUi2dMode;
                    SetVisibleMouse(_isUi2dMode);
                    _isEnableCameraMove = !_isUi2dMode;

                    if (MessageBox.Show("정말로 끝내시겠습니까?", "종료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        // 종료 설정 저장
                        IniFile.WritePrivateProfileString("camera", "x", camera.PivotPosition.x);
                        IniFile.WritePrivateProfileString("camera", "y", camera.PivotPosition.y);
                        IniFile.WritePrivateProfileString("camera", "z", camera.PivotPosition.z);
                        IniFile.WritePrivateProfileString("camera", "yaw", camera.CameraYaw);
                        IniFile.WritePrivateProfileString("camera", "pitch", camera.CameraPitch);
                        IniFile.WritePrivateProfileString("camera", "dist", (camera as OrbitCamera).Distance);

                        FileHashManager.SaveHashes();

                        Application.Exit();
                    }
                    else
                    {
                        ShowSystemMouse(false);
                        _isEnableCameraMove = true;
                    }
                }

                if (_keyDown != null)
                {
                    _keyDown(s, e);
                }
            };

            // -----------------------------------------------------------------
            //                           UIEngine Add
            // -----------------------------------------------------------------
            int w = Width;
            int h = Height;

            Console.WriteLine("========== UI2d Engine ==========");
            FontFamilySet.AddFonts(_FontResourceFileName);
            UIEngine.REOURCES_PATH = _Ui2dResourcePath;
            UITextureLoader.LoadTexture2d(UIEngine.REOURCES_PATH);

            UIEngine.Add(new UIEngine("sysInfo", w, h, path) { AlwaysRender = true }, w, h);

            UIEngine.DesignInit += (w1, h1) =>
            {
                UIEngine.AddControl("sysInfo", new Ui2d.Label("fps", FontFamilySet.연성체)
                {
                    Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TC,
                    IsCenter = true,
                    Margin = 0.2f,
                    Padding = 0.1f,
                    FontSize = 1.3f,
                    Alpha = 0.9f,
                    ForeColor = new Vertex3f(1, 1, 1),
                    BackColor = new Vertex3f(0, 0, 0),
                    BorderColor = new Vertex3f(1, 0, 0),
                    BorderWidth = 1.0f,
                    IsBorder = false,
                });

                UIEngine.AddControl("sysInfo", new Ui2d.Label("debug", FontFamilySet.연성체)
                {
                    Align = Ui2d.Control.CONTROL_ALIGN.NONE,
                    Location = new Vertex2f(0.75f, 0.0f),
                    Size = new Vertex2f(0.23f, 0.875f),
                    LineWidthMax = 0.2f,
                    FontSize = 1.0f,
                    IsCenter = false,
                    Margin = 0.0f,
                    Alpha = 0.3f,
                    ForeColor = new Vertex3f(1, 1, 1),
                    BackColor = new Vertex3f(0, 0, 0),
                    BorderColor = new Vertex3f(0, 0, 0),
                    BorderWidth = 1.0f,
                    IsBorder = true,
                    Padding = 0.01f,
                    MaxNumOfLine = 45,
                    AutoSize = false,
                });

                _lastControl = Ctrl("fps");
            };
            UIEngine.InitFrame(Width, Height);
            UIEngine.StartFrame();
        }
        
        public void WriteLine(string txt)
        {
            if (CLabel("debug") != null)
            {
                CLabel("debug").Text = txt + UI2.NewLine + CLabel("debug").Text;
            }
        }

        public void CaptureScreen()
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            int size = _width * _height * 4;
            pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

            // 렌더 타겟 프레임버퍼 바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 픽셀 데이터 읽기
            Gl.ReadPixels(
                0, 0,                          // x, y 좌표
                _width,           // 너비
                _height,           // 높이
                PixelFormat.Rgba,             // 픽셀 포맷
                PixelType.UnsignedByte,       // 데이터 타입
                pixelsPtr                      // 저장할 메모리 위치
            );

            // 픽셀 데이터를 관리되는 배열로 복사
            byte[] pixels = new byte[size];
            Marshal.Copy(pixelsPtr, pixels, 0, size);

            // Bitmap 생성
            Bitmap bitmap = new Bitmap((int)_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Bitmap 데이터를 직접 조작하기 위해 락
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* bitmapPtr = (byte*)bitmapData.Scan0;
                    int stride = bitmapData.Stride;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            // OpenGL 데이터는 아래에서 위로 저장되어 있으므로 y좌표를 뒤집어서 읽음
                            int srcIndex = (((_height - 1 - y) * _width) + x) * 4;
                            int dstIndex = (y * stride) + (x * 4);

                            // RGBA를 BGRA로 변환 (GDI+의 Format32bppArgb는 BGRA 형식임)
                            bitmapPtr[dstIndex + 0] = pixels[srcIndex + 2]; // B
                            bitmapPtr[dstIndex + 1] = pixels[srcIndex + 1]; // G
                            bitmapPtr[dstIndex + 2] = pixels[srcIndex + 0]; // R
                            bitmapPtr[dstIndex + 3] = pixels[srcIndex + 3]; // A
                        }
                    }
                }
            }
            finally
            {
                // 비트맵 언락
                bitmap.UnlockBits(bitmapData);

                bitmap = ResizeImage(bitmap, _width, _height);
                bitmap.Save(@"C:\Users\mekjh\OneDrive\바탕 화면\a.png");
            }
        }

        // 방법 1: Graphics 사용
        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, 0, 0, width, height);
            }
            return resized;
        }


        public void ShowUi2dDialog(bool isUi2d)
        {
            _isUi2dMode = isUi2d;
            SetVisibleMouse(isUi2d);
            _isEnableCameraMove = !_isUi2dMode;

            if (_isEnableCameraMove)
            {
                _camera.CameraYaw = _cameraPrevAngle.x;
                _camera.CameraPitch = _cameraPrevAngle.y;
                SetCursorPos((int)(Width * 0.5f), (int)(Height * 0.5f));
            }
            else
            {
                _cameraPrevAngle = new Vertex2f(_camera.CameraYaw, _camera.CameraPitch);
            }
        }

        public void FullScreen(bool isFullScreen)
        {
            // 전체화면을 설정한다.
            Form frm = (Form)Parent;
            if (isFullScreen)
            {
                SetResolution(Screen.PrimaryScreen.Bounds.Size.Width, Screen.PrimaryScreen.Bounds.Size.Height);
                frm.Width = _width;
                frm.Height = _height;
                frm.Location = new System.Drawing.Point(0, 0);
                frm.ControlBox = false;
                frm.FormBorderStyle = FormBorderStyle.None;
                frm.WindowState = FormWindowState.Maximized;
                if (!_isInitiazed) _init(_width, _height);
            }
            // 창모드로 전환한다.
            else
            {
                SetResolution(_width, _height);
                Parent.Location = _prevLocation;
                Parent.Size = _prevSize;
                frm.ControlBox = true;
                frm.WindowState = FormWindowState.Normal;
                frm.FormBorderStyle = FormBorderStyle.FixedSingle;
            }
        }

        /// <summary>
        /// 마우스의 표시여부를 설정한다.
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetVisibleMouse(bool isVisible)
        {
            _isMouseVisible = isVisible;
            UIEngine.EnableMouse = isVisible;
        }

        /// <summary>
        /// ui2d main에 컨트롤을 추가한다.
        /// </summary>
        public void AddControl2d(Ui2d.Control control)
        {
            UIEngine.AddControl("mainUI",  control);
        }

        #region ####### 컨트롤을 추가하거나 반환하는 영역 ########
        public void AddCheckBar(string name, string text, string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, bool value = false)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            SimpCheckbox chkBox = new SimpCheckbox(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                FontSize = fontSize,
                Text = text,                
                Margin = 0.5f,
                Checked = value,
                Alpha = 0.0f,
                MouseDown = (o, mx, my) => { }
            };
            AddControlWith(chkBox, adjoint, align, formName);
        }

        public void AddControlWith(Ui2d.Control ctrl, Ui2d.Control adjoint, 
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START, string formName = "sysInfo")
        {
            if (_lastControl != null)
            {
                ctrl.AdjontControl = _lastControl;
            }

            if (adjoint != null)
            {
                ctrl.AdjontControl = adjoint;
            }

            if (_lastControl == CLabel("fps"))
            {
                ctrl.Align = Ui2d.Control.CONTROL_ALIGN.ROOT_TL;
                ctrl.Margin = 0.5f;
            }
            else
            {
                ctrl.Align = align;
            }

            UIEngine.AddControl(formName, ctrl);
            _lastControl = ctrl;
        }

        public SimpCheckList AddCheckList(string name, string text, string[] items,
            string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null,
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, bool value = false)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            SimpCheckList chkList = new SimpCheckList(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                Alpha = 0.6f,
                Margin = 0.01f,
                FontSize = fontSize,
                Items = items,
            };
            AddControlWith(chkList, adjoint, align, formName);

            _lastControl = chkList;
            return chkList;
        }

        /// <summary>
        /// 가장 최근에 추가한 컨트롤과 어울려 아래로 배치한다.
        /// </summary>
        /// <param name="formName"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="foreColor"></param>
        /// <param name="fontSize"></param>
        /// <param name="fontFamily"></param>
        /// <param name="align"></param>
        public Ui2d.Label AddLabel(string name,  string text, string formName = "sysInfo",
            Vertex3f? foreColor = null, float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null, 
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, Vertex2f? location = null)
        {
            if (location == null) location = Vertex2f.Zero;
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 1);

            Ui2d.Label lbl = new Ui2d.Label(name, fontFamily)
            {
                ForeColor = (Vertex3f)foreColor,
                FontSize = fontSize,
                Text = text,
                Alpha = 0.0f,
                Margin = 0.05f,
                BackColor = Vertex3f.UnitY,                
                Location = (Vertex2f)location,                
                MouseDown = (o, mx, my) => { }
            };

            AddControlWith(lbl, adjoint, align, formName);
            return lbl;
        }

        public SimpHValueBar AddValueBar(string name, string formName = "sysInfo",
            Vertex3f? foreColor = null, Vertex3f? backColor = null,
            float fontSize = 1.0f, Ui2d.FontFamily fontFamily = null, 
            Ui2d.Control.CONTROL_ALIGN align = Ui2d.Control.CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START,
            Ui2d.Control adjoint = null, float width = 0.2f
            , float maxValue = 1.0f, float minValue = 0.0f, float value = 0.5f, float stepValue = 0.1f)
        {
            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            if (foreColor == null) foreColor = new Vertex3f(1, 1, 0);
            if (backColor == null) backColor = new Vertex3f(0, 0, 1);

            SimpHValueBar vbar = new SimpHValueBar(name)
            {
                ForeColor = (Vertex3f)foreColor,
                ValueColor = (Vertex3f)foreColor,
                BackColor = (Vertex3f)backColor,
                FontSize = fontSize,
                MaxValue = maxValue,
                MinValue = minValue,
                StepValue = stepValue,
                Margin = 0.2f,
                Value = value,
                Height = 0.1f * width,
                Width = width,
                Round = 3,
                IsIniWritable = true,
            };

            vbar.MouseWheel += (o, delta) =>
            {
                if (o.IsIniWritable)
                {
                    IniFile.WritePrivateProfileString(formName, name, (o as SimpHValueBar).Value);
                }
            };

            AddControlWith(vbar, adjoint, align, formName);
            return vbar;
        }

        /// <summary>
        /// 이름에 맞는 라벨을 가져온다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Ui2d.Label CLabel(string name)
        {
            return (Ui2d.Label)UIEngine.Controls(name);
        }

        public Ui2d.SimpCheckList CheckList(string name)
        {
            return (Ui2d.SimpCheckList)UIEngine.Controls(name);
        }

        public Ui2d.SimpHValueBar SimpHValueBar(string name)
        {
            return (SimpHValueBar)UIEngine.Controls(name);
        }

        public Ui2d.SimpCheckbox SimpCheckBox(string name)
        {
            return (SimpCheckbox)UIEngine.Controls(name);
        }

        /// <summary>
        /// 이름에 맞는 컨트롤을 가져온다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Ui2d.Control Ctrl(string name)
        {
            return UIEngine.Controls(name);
        }
        #endregion
    }
}