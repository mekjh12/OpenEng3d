using Camera3d;
using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Ui2d;
using ZetaExt;

namespace GlWindow
{
    public partial class GlControl3
    {
        // ==================================================================================
        //                              초기화 및 시작/정지
        // ==================================================================================

        /// <summary>
        /// 엔진 시작
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
                    throw new InvalidOperationException($"필수 이벤트가 설정되지 않았습니다. {eventPair.Key} 이벤트를 먼저 설정해주세요.");
                }
            }

            _isRunning = true;
        }

        /// <summary>
        /// 엔진 정지
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 카메라 설정
        /// </summary>
        public void SetCamera(float x, float y, float z, float distance, float pitch, float yaw)
        {
            _camera = new OrbitCamera("orbitCamera", x, y, z, distance);
            _camera.CameraPitch = pitch;
            _camera.CameraYaw = yaw;
            _camera.FAR = 10000.0f;
            _camera.NEAR = 1.0f;
        }

        /// <summary>
        /// 해상도 조절
        /// </summary>
        public void SetResolution(int w, int h)
        {
            _prevLocation = new Point(Parent.Left, Parent.Top);
            _prevSize = new Size(Width, Height);
            _width = w;
            _height = h;
            _camera.SetResolution(w, h);
            Gl.Viewport(0, 0, _width, _height);

            // 렌더 타겟 리사이즈
            if (_useRenderTarget && _gbuffer != null)
            {
                _gbuffer.Dispose();
                _gbuffer.Initialize(w, h);
                Console.WriteLine($"✅ 렌더 타겟 리사이즈: {w}x{h}");
            }
        }

        /// <summary>
        /// 그리드 셰이더 초기화
        /// </summary>
        public void InitGridShader(string path)
        {
            // grid를 초기화한다.
            _grid.Init(path, Width, Height);
        }

        /// <summary>
        /// UI 2D 대화상자 표시 여부 설정
        /// </summary>
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

        /// <summary>
        /// 전체화면/창모드 전환
        /// </summary>
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

        // ==================================================================================
        //                              메인 렌더 루프
        // ==================================================================================

        /// <summary>
        /// 매 프레임마다 호출되는 메인 렌더링 함수
        /// </summary>
        private void GlControl3_Render(object sender, GlControlEventArgs e)
        {
            if (!_isRunning) return;

            if (!_isInitiazed)
            {
                // GPU 정보 표기
                string vendor = Gl.GetString(StringName.Vendor);
                string renderer = Gl.GetString(StringName.Renderer);
                string version = Gl.GetString(StringName.Version);
                Console.WriteLine("---------------------------------------------------");
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

                string shaderPath = SHADER_UI2D_PATH;
                InitGlControl(shaderPath);

                // 렌더 타겟 초기화
                if (_useRenderTarget)
                {
                    _gbuffer = new GBuffer();
                    _gbuffer.Initialize(Width, Height);
                    Console.WriteLine($"✅ GlControl3 렌더 타겟 활성화: {Width}x{Height}");
                }

                if (_init2d != null) _init2d(Width, Height);

                // 카메라 UBO 생성하기
                _cameraUBO = new CameraUBO();
                _cameraUBO.UpdateViewProjMatrices(_camera.ViewMatrix, _camera.ProjectiveMatrix, _camera.Position);
            }

            // 프레임 카운트 증가
            _tick++;
            if (_tick >= uint.MaxValue) _tick = 0;

            GetMouseInputFromWinAPI(Parent.Left, Parent.Top, Width, Height);

            // 엔진 루프, 처음 로딩시 deltaTime이 커지는 것을 방지
            if (FramePerSecond.DeltaTime < 1000)
            {
                int deltaTime = FramePerSecond.DeltaTime;

                if (_update != null)
                {
                    Update(deltaTime);
                }

                // UI2D 업데이트
                Update2d(deltaTime, _mouseWheelValue);
                _mouseWheelValue = 0;

                if (_render != null)
                {
                    Render3d(deltaTime);
                }

                // 포스트 프로세싱 호출
                if (_blitScreen != null)
                {
                    _blitScreen(deltaTime, _camera);
                }
            }

            // FPS 업데이트
            FramePerSecond.Update();

            MemoryProfiler.CheckFrameGC();
        }

        // ==================================================================================
        //                              GL 컨트롤 초기화
        // ==================================================================================

        /// <summary>
        /// GL 컨트롤 초기화
        /// </summary>
        private void InitGlControl(string path)
        {
            // 난수 시스템 초기화
            Rand.InitSeed(500);

            // 사용자 정의 초기화 실행
            if (_init != null)
            {
                _init(Width, Height);
            }

            if (_init3d != null)
            {
                // 카메라 설정 로드
                float cx = float.Parse(IniFile.GetPrivateProfileString("camera", "x", "0.0"));
                float cy = float.Parse(IniFile.GetPrivateProfileString("camera", "y", "0.0"));
                float cz = float.Parse(IniFile.GetPrivateProfileString("camera", "z", "0.0"));
                float yaw = float.Parse(IniFile.GetPrivateProfileString("camera", "yaw", "0.0"));
                float pitch = float.Parse(IniFile.GetPrivateProfileString("camera", "pitch", "0.0"));
                float dist = float.Parse(IniFile.GetPrivateProfileString("camera", "dist", "10.0"));
                SetCamera(cx, cy, cz, dist, pitch, yaw);

                // 3D 초기화
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

            // 마우스 휠 이벤트
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

            // 마우스 이동 이벤트
            MouseMove += (s, e) =>
            {
                Mouse.CurrentPosition = new Vertex2i(e.X, e.Y);
                Vertex2i delta = Mouse.DeltaPosition;

                if (MouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT)
                {
                    // 카메라 회전
                    _camera?.Yaw(-delta.x);
                    _camera?.Pitch(delta.y);
                }
                else if (MouseMode == MOUSE_GAME_MODE.CAMERA_ROUND_ROT2)
                {
                    // 카메라 회전
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
                    // 전체화면 설정
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
                        // 종료 시 설정 저장
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

            // UIEngine 설정
            int w = Width;
            int h = Height;

            Console.WriteLine("========== UI2d Engine ==========");
            FontFamilySet.AddFonts(_FontResourceFileName);
            UIEngine.REOURCES_PATH = _Ui2dResourcePath;
            UITextureLoader.LoadTexture2d(UIEngine.REOURCES_PATH);

            UIEngine.Add(new UIEngine("sysInfo", w, h, path) { AlwaysRender = true }, w, h);

            UIEngine.DesignInit += (w1, h1) =>
            {
                /*
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
                    BackColor = new Vertex3f(1, 0, 0),
                    BorderColor = new Vertex3f(0, 0, 0),
                    BorderWidth = 1.0f,
                    IsBorder = true,
                    Padding = 0.01f,
                    MaxNumOfLine = 45,
                    AutoSize = false,
                });
                */
            };
            UIEngine.InitFrame(Width, Height);
            UIEngine.StartFrame();
        }
    }
}