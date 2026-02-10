using Camera3d;
using Common;
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using Ui2d;
using ZetaExt;

namespace GlWindow
{
    /// <summary>
    /// 호출순서: formload->init->init3d->init2d->loop(update->render)
    /// </summary>
    public partial class GlControl3 : GlControl
    {
        // ==================================================================================
        //                              상수
        // ==================================================================================
        
        private const string SHADER_UI2D_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\UIDesign2d\Shader\";

        // ==================================================================================
        //                              열거형 및 구조체
        // ==================================================================================

        public enum MOUSE_GAME_MODE
        {
            NONE, 
            CAMERA_ROUND_ROT, 
            CAMERA_ROUND_ROT2
        }

        // ==================================================================================
        //                              멤버 변수 - 기본 설정
        // ==================================================================================

        private string _formName = "glcontrol";
        private string _FontResourceFileName = "";
        private string _Ui2dResourcePath = "";
        private string _RootPath = "";
        private string _helpText = "<HELP><br>";

        // ==================================================================================
        //                              멤버 변수 - 상태 플래그
        // ==================================================================================

        private uint _tick = 0;
        private bool _isInitiazed = false;
        private bool _isRunning = false;
        private bool _isUi2dMode = false;
        private bool _isEnableCameraMove = true;
        private bool _isMouseVisible = true;
        private bool _isVisibleGrid = true;
        private bool _isVisibleUi2d = true;
        private bool _isVisibleDebug = false;
        private bool _isFullscreen = false;

        // ==================================================================================
        //                              멤버 변수 - 렌더링 설정
        // ==================================================================================

        private Vertex3f _backColor = Vertex3f.Zero;
        private PolygonMode _polygonMode = PolygonMode.Fill;
        private MOUSE_GAME_MODE _mouseMode = MOUSE_GAME_MODE.NONE;

        // ==================================================================================
        //                              멤버 변수 - 카메라 및 뷰포트
        // ==================================================================================

        private Camera _camera;
        private int _width = 0;
        private int _height = 0;
        private Size _prevSize = new Size(0, 0);
        private Point _prevLocation = new Point(0, 0);
        private Vertex2f _cameraPrevAngle = Vertex2f.Zero;
        private CameraUBO _cameraUBO;

        // ==================================================================================
        //                              멤버 변수 - 마우스 입력
        // ==================================================================================

        private Vertex2i _mousePosition = Vertex2i.Zero;
        private Vertex2f _mouseDeltaPos = Vertex2f.Zero;
        private float _mouseWheelValue = 0.0f;
        private Vertex3f _prevPos = Vertex3f.Zero;
        private Vertex2f _prevMousePosition = Vertex2f.Zero;
        private static Vertex2i _windowOffSet = Vertex2i.Zero;
        private static Vertex2f _currentMousePointFloat = Vertex2f.Zero;

        // ==================================================================================
        //                              멤버 변수 - 키보드 입력
        // ==================================================================================

        private Dictionary<Key, bool> _onPrevPressed = new Dictionary<Key, bool>();

        // ==================================================================================
        //                              멤버 변수 - 셰이더 및 렌더링 객체
        // ==================================================================================

        private ColorShader _colorShader;
        private InfiniteGrid _grid;

        // ==================================================================================
        //                              멤버 변수 - UI 및 디버그
        // ==================================================================================

        private FpsStringCache _fpsCache;
        private GameInfoStringCache _gameInfoCache;
        private string _fps;
        private Ui2d.Control _lastControl;

        // ==================================================================================
        //                              멤버 변수 - 렌더 타겟
        // ==================================================================================

        private GBuffer _gbuffer;
        private bool _useRenderTarget = false;
        private bool _autoBlitToScreen = true;

        // ==================================================================================
        //                              보호된 이벤트
        // ==================================================================================

        protected event Action<int, int> _init;
        protected event Action<int, int> _init3d;
        protected event Action<int, int, int, Camera> _update;
        protected event Action<int, int> _init2d;
        protected event Action<int, float, float, Vertex4f, Camera> _render;
        protected Action<object, System.Windows.Forms.KeyEventArgs> _keyUp;
        protected Action<object, System.Windows.Forms.KeyEventArgs> _keyDown;
        protected Action<int, Camera> _blitScreen;

        // ==================================================================================
        //                              공개 속성
        // ==================================================================================

        public string HelpText { set => _helpText += value.Replace("/", "<br>"); }
        public float CameraStepLength { get => Constants.CAMERA_MOVE_DELTA; set => Constants.CAMERA_MOVE_DELTA = value; }
        public bool IsUi2dMode { get => _isUi2dMode; set { _isUi2dMode = value; _isMouseVisible = value; } }
        public bool IsEnableCameraMove { get => _isEnableCameraMove; set => _isEnableCameraMove = value; }
        public bool IsVisibleGrid { get => _isVisibleGrid; set => _isVisibleGrid = value; }
        public Vertex3f BackClearColor { get => _backColor; set => _backColor = value; }
        public PolygonMode PolygonMode { get => _polygonMode; set => _polygonMode = value; }
        public Camera Camera { get => _camera; set => _camera = value; }
        public Action<int, int> Init { set => _init += value; get => _init; }
        public Action<int, int> Init3d { set => _init3d = value; get => _init3d; }
        public Action<int, int> Init2d { set => _init2d = value; get => _init2d; }
        public Action<int, int, int, Camera> UpdateFrame { get => _update; set => _update = value; }
        public Action<int, float, float, Vertex4f, Camera> RenderFrame { get => _render; set => _render = value; }
        public bool IsMouseVisible { get => _isMouseVisible; }
        public bool IsVisibleDebug { get => _isVisibleDebug; set => _isVisibleDebug = value; }
        public MOUSE_GAME_MODE MouseMode { get => _mouseMode; set => _mouseMode = value; }
        public uint Tick { get => _tick; set => _tick = value; }
        public ColorShader ColorShader { get => _colorShader; set => _colorShader = value; }
        public bool IsVisibleUi2d { get => _isVisibleUi2d; set => _isVisibleUi2d = value; }
        public bool AutoBlitToScreen { get => _autoBlitToScreen; set => _autoBlitToScreen = value; }
        public uint DepthTextureId => _gbuffer?.DepthTextureId ?? 0;
        public uint AlbedoTextureId => _gbuffer?.AlbedoTextureId ?? 0;
        public uint PositionTextureId => _gbuffer?.PositionTextureId ?? 0;
        public uint NormalTextureId => _gbuffer?.NormalTextureId ?? 0;

        public GBuffer GBuffer => _gbuffer;
        public bool UseRenderTarget => _useRenderTarget;
        public Action<int, Camera> BlitToScreen { get => _blitScreen; set => _blitScreen = value; }

        // ==================================================================================
        //                              생성자
        // ==================================================================================

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="name">컨트롤 이름</param>
        /// <param name="rootPath">실행파일의 경로</param>
        /// <param name="fontResourceFileName">폰트 리소스 상대경로</param>
        /// <param name="ui2dResourcePath">UI2d 리소스 상대경로</param>
        /// <param name="winMouseUsed">윈도우 마우스 사용 여부</param>
        /// <param name="isEnableMouse">마우스 활성화 여부</param>
        /// <param name="useRenderTarget">렌더 타겟 사용 여부 (깊이 버퍼 접근 필요시)</param>
        public GlControl3(string name, string rootPath, string fontResourceFileName, string ui2dResourcePath, 
            bool winMouseUsed = false, bool isEnableMouse = true, bool useRenderTarget = true)
        {
            // GL 컨트롤 기본설정
            Location = new Point(0, 0);
            Dock = DockStyle.Fill;

            // 이름 설정
            _formName = name;

            // 렌더 타겟 사용 설정
            _useRenderTarget = useRenderTarget;

            // 경로 설정
            _RootPath = rootPath;
            _FontResourceFileName = rootPath + fontResourceFileName;
            _Ui2dResourcePath = rootPath + ui2dResourcePath;
            IniFile.s_PATH_ROOT = rootPath;
            IniFile.SetFileName($"setup_{_formName}.ini");

            // 시스템 마우스 설정
            ShowCursor(winMouseUsed);
            UIEngine.EnableMouse = isEnableMouse;

            // OpenGL 설정
            Animation = true;
            MultisampleBits = ((uint)(0u));
            ColorBits = 24;
            DepthBits = 24;
            StencilBits = 8;
            SwapInterval = 10;
            DoubleBuffer = true;
            Render += GlControl3_Render;
            MouseWheel += (o, e) => { _mouseWheelValue = e.Delta; };

            // 그리드 생성
            _grid = new InfiniteGrid();

            // FPS 문자열 캐시 생성 (0~144 FPS 지원)
            _fpsCache = new FpsStringCache(maxFps: 144, prefix: "FPS: ");
            _gameInfoCache = new GameInfoStringCache(maxFps: 144);

            // 메모리 사용량 확인 (144개 * 평균 8글자 * 2byte = 약 2.3KB)
            Console.WriteLine($"FPS 캐시 메모리: {_fpsCache.EstimatedMemoryUsage} bytes");

            PrintSystemInfo();
        }

        // ==================================================================================
        //                              유틸리티 메서드
        // ==================================================================================

        /// <summary>
        /// 시스템 정보 출력
        /// </summary>
        private void PrintSystemInfo()
        {
            return;

            Console.WriteLine("=======================================================");
            Console.WriteLine("  OpenGL3D 시스템 정보");
            Console.WriteLine("=======================================================");
            Console.WriteLine();
            Console.WriteLine("[ 좌표계 ]");
            Console.WriteLine("  오른손 좌표계: x(right) Y(forward) Z(up)");
            Console.WriteLine();
            Console.WriteLine("[ 지형 시스템 ]");
            Console.WriteLine("  x축(+) → 동쪽 | Y축(+) → 북쪽");
            Console.WriteLine();
            Console.WriteLine("[ 행렬 정보 - OpenGL.Net ]");
            Console.WriteLine("  ㅇ 열 우선(Column-Major) 저장");
            Console.WriteLine("  ㅇ 인덱싱: [열, 행]");
            Console.WriteLine("  ㅇ M × N 연산 = 일반적인MN연산");
            Console.WriteLine("  ㅇ 벡터 v를 MNv는 N 변환 후 M 변환");
            Console.WriteLine();
            Console.WriteLine("=======================================================");
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine("  C# 행렬 복합 대입 연산자 (M *= N) <=> M=MN");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("[ 기본 의미 ]");
            Console.WriteLine("  M *= N;  →  M = M * N;");
            Console.WriteLine("  (M에 N을 오른쪽에서 곱한 결과를 M에 저장)");
            Console.WriteLine();
            Console.WriteLine("[ 변환 적용 순서 ]");
            Console.WriteLine("  M *= N; <=> M = M * N");
            Console.WriteLine("  → 1단계: N 변환 적용 (먼저)");
            Console.WriteLine("  → 2단계: M 변환 적용 (나중)");
            Console.WriteLine(" (예시)");
            Console.WriteLine("  Matrix4x4 M = CreateTranslation(10, 0, 0);  // 이동");
            Console.WriteLine("  Matrix4x4 N = CreateRotationZ(45);          // 회전");
            Console.WriteLine("  결과: 45도 회전(N) → (10,0,0) 이동(M)");
            Console.WriteLine("-------------------------------------------------------");
        }
    }
}