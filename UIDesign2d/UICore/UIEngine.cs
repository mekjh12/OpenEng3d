using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Ui2d
{
    /// <summary>
    /// ##########################################  <br/>
    ///               UIEngine 사용법  <br/>
    /// ##########################################  <br/>
    /// 1. FontFamilySet 생성한다.  <br/>
    /// 2. UIEngine 전역변수(REOURCES_PATH)를 설정한다.  <br/>
    /// 3. UITextureLoader.LoadTexture2d로 모든 텍스처를 읽어온다.  <br/>
    /// 4. UIEngine.Add(new UIEngine("mainUI", w, h) { AlwaysRender = true }, w, h);  <br/>
    /// 5. DesignInit 구성한다.  <br/>
    ///   <br/>
    /// --------------------시작시-------------------------  <br/>
    /// a. UIEngine.InitFrame(w, h)  <br/>
    /// b. UIEngine.StartFrame()  <br/>
    ///   <br/>
    /// --------------------LOOP-------------------------  <br/>
    /// c. UIEngine.MouseUpdateFrame()  <br/>
    /// d. UIEngine.UpdateFrame(deltaTime)  <br/>
    /// e. UIEngine.Render(deltaTime)  <br/>
    /// --------------------------------------------  <br/>
    /// 
    /// </summary>
    public partial class UIEngine
    {
        // -------------------------------------------------------------------------------------------
        /// * UIEngin을 모아서 관리하는 부분이다. <br/>
        /// * 정적클래스이다. <br/>
        // -------------------------------------------------------------------------------------------
        #region Static UIEngine을 위한 부분
        static Dictionary<string, UIEngine> _uiEngineList = new Dictionary<string, UIEngine>();

        static string _currentUIEngineName = "";

        public static T UI2d<T>(string name) => (T)Convert.ChangeType(UIEngine.Controls(name), typeof(T));

        public static bool ContainsKey(string name) => _uiEngineList.ContainsKey(name);

        public static List<UIEngine> UIEngineList => _uiEngineList.Values.ToList<UIEngine>();

        public static UIEngine GetUIEngine(string name) => _uiEngineList.ContainsKey(name) ? _uiEngineList[name] : null;

        public static void SetCurrentUIEngine(string name) => _currentUIEngineName = name;

        public static void AddUIEngine(string name, UIEngine engine) => _uiEngineList.Add(name, engine);

        public static UIEngine CurrentUIEngine => GetUIEngine(_currentUIEngineName);

        #endregion

        private bool _isInitialize = false;

        /// <summary>
        /// 지정하는 UI패널엔진에 컨트를 붙인다.
        /// </summary>
        /// <param name="formName"></param>
        /// <param name="control"></param>
        public static Control AddControl(string formName, Control control)
        {
            UIEngine.GetUIEngine(formName).Root.AddChild(control);
            return control;
        }

        /// <summary>
        /// 전역 컨트롤 리스트에 컨트롤을 등록한다.
        /// </summary>
        /// <param name="control"></param>
        public static void RegistryControl(Control control)
        {
            /// AddControl은 컨트롤의 자식으로 추가하는 것이고,
            /// 컨트롤의 등록은 전역리스트에 추가하는 함수이다.
            if (!_controlList.ContainsKey(control.Name))
            {
                _controlList.Add(control.Name, control);
            }
        }

        /// <summary>
        /// UIEngine을 추가한다.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void Add(UIEngine engine, int width, int height)
        {
            if (engine != null)
            {
                if (FontFamilySet.Count > 0)
                {
                    UIEngine.AddUIEngine(engine.Name, engine);
                    engine.Init(width, height);
                    engine.SetResolution(width, height);
                }
                else
                {
                    Console.WriteLine("UIEngine을 등록하지 못했습니다. FontFamilySet이 Null입니다.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        /// * Control를 모아서 관리하는 Static부분이다. <br/>
        /// * 정적클래스이다. <br/>
        // -------------------------------------------------------------------------------------------
        static Dictionary<string, Control> _controlList = new Dictionary<string, Control>();

        public static string REOURCES_PATH = @"";

        public static List<Control> ControlList => _controlList.Values.ToList();

        

        /// <summary>
        /// 전역 컨트롤 리스트에서 이름으로 컨트롤를 가져온다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Control Controls(string name)
        {
            if (_controlList.ContainsKey(name))
            {
                if (_controlList[name] is Control)
                {
                    return (Control)_controlList[name];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // -------------------------------------------------------------------------------------------
        //
        //
        // -------------------------------------------------------------------------------------------
        bool _started = false;
        bool _isVisible = true;
        bool _isEnable = true;
        static bool _isMouseEnable = false;
        bool _isAlways = false;

        string _name = "";
        FontRenderer _fontRenderer;
        UIShader _uiShader;
        UIColorShader _uiColorShader;
        Control _rootPannel;  // 화면의 가장 부모가 되는 컨트롤
        //Control recentAddCtrl = null;

        // _width, _height를 설정하면 자동으로 static 변수는 변경된다.
        public static float Width = 800; // 800x600은 초기설정일 뿐이다.
        public static float Height = 600;
        public const byte PERIOD_TICK_UICAPTURE = 2;

        /// <summary>
        /// Width / Height
        /// </summary>
        public static float Aspect => (float)UIEngine.Width / (float)UIEngine.Height;

        public string Name => _name;

        public bool AlwaysRender
        {
            get => _isAlways;
            set => _isAlways = value;
        }

        public static bool EnableMouse
        {
            get => _isMouseEnable;
            set => _isMouseEnable = value;
        }

        public bool Visible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public bool Enable
        {
            get => _isEnable;
            set => _isEnable = value;
        }

        public UIShader UIShader => _uiShader;

        public Control Root => _rootPannel;

        public Control FocusControl
        {
            get => _focusControl;
            set => _focusControl = value;
        }

        public UIColorShader UIColorShader => _uiColorShader;

        public UIEngine(string name, int width, int height, string path)
        {
            //_engineLoop = engineLoop;
            _name = name;
            Width = width;
            Height = height;

            _fontRenderer = new FontRenderer(path);
            _uiShader = new UIShader(path);
            _uiColorShader = new UIColorShader(path);
        }

        public static void InitFrame(int w, int h)
        {
            // UI엔진 초기화
            foreach (UIEngine uIEngine in UIEngine.UIEngineList)
            {
                uIEngine.Init(w, h);
            }

            // 초기화한 후에 w, h를 가지고 디자인을 작업한다.
            if (_designInit != null)
            {
                _designInit(w, h);
            }
        }

        public static void StartFrame()
        {
            foreach (UIEngine uIEngine in UIEngine.UIEngineList)
            {
                uIEngine.Start();
            }
        }

        public static void RenderFrame(int deltaTime)
        {
            // UIEngine을 렌더링한다.
            foreach (UIEngine uIEngine in UIEngine.UIEngineList)
            {
                uIEngine.Render(deltaTime);
            }
        }

        public static void UpdateFrame(int deltaTime)
        {
            // UIEngine을 업데이트한다.
            foreach (UIEngine uIEngine in UIEngine.UIEngineList)
            {
                float fx = UIEngine.CurrentMousePointFloat.x;
                float fy = UIEngine.CurrentMousePointFloat.y;
                uIEngine.Update(deltaTime, fx, fy);
            }
        }

        /// <summary>
        /// 창의 크기 변화에 따라 가로, 세로를 설정할 수 있다.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Init(int width, int height)
        {
            if (_isInitialize) { return; }

            Width = width;
            Height = height;

            // 이벤트 처리를 위하여 기본 기능을 구성한다.
            RegistryMouseEvent();

            // 초기 실행시 UI구성을 초기화한다.
            Intialize();

            // 루트패널 자식으로 탐색하여 컨트롤을 초기화한다.
            _rootPannel.Init(); 
            
            //Toask.Init(_rootPannel, Font_Hangul);
            Console.WriteLine($"* UI2d Engine {_name} Init!");

            _mouseImage = new PictureBox($"{_name}Mouse")
            {
                Left = 0.5f,
                Top = 0.5f,
                Width = MOUSE_ORGINAL_WIDTH,
                Height = MOUSE_ORGINAL_HEIGHT,
                BackColor = new Vertex3f(1, 1, 1),
                IsSelectable = false,
                TextureImageMode = Control.MOUSE_IMAGE_MODE.NORMAL,
                BackgroundImage = UITextureLoader.Cursor,
            };

            _isInitialize = true;
            //_rootPannel.AddChild(_mouseImage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void Render(int deltaTime)
        {
            if (!_isAlways)
            {
                if (!_isVisible) return;
            }

            Gl.Disable(EnableCap.DepthTest);
            //Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            Gl.FrontFace(FrontFaceDirection.Ccw);
            Gl.Enable(EnableCap.CullFace); 
            Gl.CullFace(CullFaceMode.Back);

            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 주 컨트롤과 자식 컨트롤 렌더링한다.
            // 마우스는 주패널에 마우스를 렌더링한다.
            _rootPannel.Render(_uiShader, _fontRenderer);

            // 추가 렌더링 코드를 실행한다.
            if (_render != null) _render(deltaTime);

            //Toask.Render(_uiShader, _fontRenderer);
            if (_isMouseEnable)
            {
                _mouseImage.Render(_uiShader, _fontRenderer);
            }

            Gl.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void Update(int deltaTime, float fx, float fy)
        {
            if (!_started) return;

            if (!_isAlways)
            {
                if (!_isEnable) return;
            }

            // 포커스 컨트롤에 대하여 
            _focusControl?.KeyDown(_focusControl);

            // 조상으로부터 자손으로 업데이트한다.
            _rootPannel.Update(deltaTime);

            _mouseImage.Update(deltaTime);

            // 추가 업데이트 코드를 실행한다.
            if (_update != null) _update(deltaTime);

        }

        public void ShutDown()
        {
            _rootPannel.Shutdown();
            Console.WriteLine($"UIEngine {_name} ShutDown!");
        }

        public void Start()
        {
            _started = true;
            _rootPannel.Start();
        }

        public void Stop()
        {
            _started = false;
            _rootPannel.Stop();  
        }

        public void Resume()
        {
            _started = true;
            _isMouseDown = false;
            _dragControl = null;
            _tickClick = 0;
            _tickDrag = 0;
            _isDragdrop = false;

            _rootPannel.Resume();
        }

    }
}
