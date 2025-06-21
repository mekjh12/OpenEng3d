using OpenGL;
using System;
using System.IO;
using System.Windows.Input;
using System.Xml;

namespace Ui2d
{
    partial class UIEngine
    {
        Control _focusControl; // 선택된 컨트롤을 저장하는 변수
        Control _prevMouseOverControl;
        Control _currentOverControl;
        Control _currentDragControl;
        Control _keyDownSelectedControl;

        bool _isMouseDown = false;
        Control _dragControl = null;
        private int _tickClick = 0;
        private int _tickDrag = 0;
        private bool _isDragdrop = false;

        private event Action<float, float> _mouseDown;
        private event Action<float, float> _mouseUp;
        private event Action<float, float> _mouseOver;
        private event Action<float, float> _mouseOut;
        private event Action<int, int, float, float, float, float> _mouseMove;
        private event Action<float> _mouseWheel;


        private event Action<float, float, float, float> _dragStart; // x, y, dx, dy
        private event Action<float, float, float, float> _dragMove;
        private event Action<float, float, float, float> _dragEnd;

        private event Action<int> _update;
        private event Action<int> _render;
        private event Action _start;
        private event Action _init;

        #region 속성을 위한 부분

        public Action<int> UpdateEvent
        {
            get => _update;
            set => _update = value;
        }

        public Action<int> RenderEvent
        {
            get => _render;
            set => _render = value;
        }

        public Action StartEvent
        {
            get => _start;
            set => _start = value;
        }

        public Action InitEvent
        {
            get => _init;
            set => _init = value;
        }

        public Control CurrentOverControl => _currentOverControl;

        public Control PrevMouseOverControl => _prevMouseOverControl;


        public Control KeyDownSelectedControl
        {
            get => _keyDownSelectedControl;
            set => _keyDownSelectedControl = value;
        }

        public Action<int, int, float, float, float, float> MouseMove
        {
            get => _mouseMove;
            set => _mouseMove = value;
        }

        public Action<float, float> MouseDown
        {
            get => _mouseDown;
            set => _mouseDown = value;
        }

        public Action<float, float> MouseOver
        {
            get => _mouseOver;
            set => _mouseOver = value;
        }

        public Action<float, float> MouseOut
        {
            get => _mouseOut;
            set => _mouseOut = value;
        }

        public Action<float, float> MouseUp
        {
            get => _mouseUp;
            set => _mouseUp = value;
        }

        public Action<float> MouseWheel
        {
            get => _mouseWheel;
            set => _mouseWheel = value;
        }

        /// <summary>
        /// x,y,dx,dy로 모두 절대비율로 (x,y)는 절대위치값, (dx,dy)절대변화값
        /// </summary>
        public Action<float, float, float, float> DragMove
        {
            get => _dragMove;
            set => _dragMove = value;
        }

        public Action<float, float, float, float> DragStart
        {
            get => _dragStart;
            set => _dragStart = value;
        }

        public Action<float, float, float, float> DragEnd
        {
            get => _dragEnd;
            set => _dragEnd = value;
        }

        #endregion

        public void UpdateInput(bool Lbutton, int mx, int my, float fx, float fy, float dx, float dy, float wheelDelta)
        {          
            if (!_isVisible) return;
            if (!_isEnable) return;

            if (fx < 0 || fx > 1.0f) return;
            if (fy < 0 || fy > 1.0f) return;

            // 마우스휠이 작동할때
            if (wheelDelta != 0)
            {
                if (_currentOverControl != null)
                {
                    if (_currentOverControl.MouseWheel != null)
                    {
                        _currentOverControl.MouseWheel(_currentOverControl, wheelDelta);
                    }
                }
            }

            // 왼쪽 마우스 누를 때
            if (Lbutton)
            {
                _tickClick++;
                _tickClick = _tickClick > 3 ? 3 : _tickClick;

                // Mouse Down
                if (_tickClick == 1)
                {
                    if (_currentOverControl != null)
                    {
                        if (_currentOverControl.MouseDown != null)
                        {
                            _currentOverControl.MouseDown(_currentOverControl, fx, fy);
                            //Console.WriteLine($"{_currentOverControl.Name} click");
                        }
                    }
                    _isMouseDown = true;
                    //Console.WriteLine($"ui2d {_name} engine mouse down.");
                }
            }
            // 왼쪽 마우스 누르지 않을 때
            else
            {
                _tickClick--;
                _tickClick = _tickClick <= 0 ? 0 : _tickClick;

                if (_tickClick == 1)
                {
                    if (_currentOverControl != null)
                    {
                        if (_currentOverControl.MouseUp != null)
                        {
                            _currentOverControl.MouseUp(_currentOverControl, fx, fy);
                        }
                    }
                    _isMouseDown = false;
                    //Console.WriteLine("mainloop mouse up.");

                    // drag-end
                    if (_isDragdrop)
                    {
                        _isDragdrop = false;

                        if (_currentDragControl != null)
                        {
                            if (_currentDragControl.DragEnd != null)
                            {
                                _currentDragControl.DragEnd(_currentDragControl, fx, fy, dx, dy);
                            }
                        }

                        _tickDrag = 0;
                        //Console.WriteLine("mainloop dragdrop end.");
                    }
                }
            }

            if (dx != 0 || dy != 0)
            {
                // Mouse Move
                if (_mouseMove != null)
                {
                    _mouseMove(mx, my, fx, fy, dx, dy);
                    
                    _mouseImage.IsVisible = _isMouseEnable;

                    // 속도개선을 위하여 tick마다 개체를 선택하지 않고 마우스가 움직일 때에만 선택한다.
                    // 선택을 위한 컨트롤 Id의 색상 렌더링
                    Picker2d.RenderControlId(_uiColorShader, _fontRenderer, _rootPannel, Width, Height);
                    Control ctrl = Picker2d.PickUpControl(this, fx, fy);

                    if (ctrl != _currentOverControl)
                    {
                        _prevMouseOverControl = _currentOverControl;
                        _currentOverControl = ctrl;

                        if (_prevMouseOverControl != null)
                        {
                            if (_prevMouseOverControl.MouseOut != null)
                                _prevMouseOverControl?.MouseOut(_prevMouseOverControl, fx, fy);
                        }
                        if (_currentOverControl != null)
                        {
                            if (_currentOverControl.MouseIn != null)
                                _currentOverControl?.MouseIn(_currentOverControl, fx, fy);
                        }
                    }

                }
                //Console.WriteLine("mainloop mouse move move.");

                if (_isMouseDown)
                {
                    // drag-drop
                    if (_currentDragControl != null)
                    {
                        if (_currentDragControl.DragDrop != null)
                        {
                            _currentDragControl.DragDrop(_currentDragControl, fx, fy, dx, dy);
                        }
                    }

                    _tickDrag++;
                    _isDragdrop = true;
                    //Console.WriteLine("mainloop dragdrop move.");

                    // 드래그 드롭이 시작하면
                    if (_tickDrag == 1)
                    {
                        // drag-drop이 시작하면 mouseup
                        if (_currentOverControl != null)
                        {
                            if (_currentOverControl.MouseUp != null)
                            {
                                //_currentOverControl.MouseUp(_currentOverControl, fx, fy);
                            }
                        }
                        _isMouseDown = true;

                        // process drag-start.
                        _isDragdrop = true;

                        if (_currentDragControl != null)
                        {
                            if (_currentDragControl.MouseUp != null)
                            {                                 
                                _currentDragControl.MouseUp(_currentDragControl, fx, fy);
                            }
                        }

                        _currentDragControl = _currentOverControl;
                        if (_currentDragControl != null)
                        {
                            if (_currentDragControl.DragStart != null)
                            {
                                _currentDragControl.DragStart(_currentDragControl, fx, fy, dx, dy);
                            }
                        }
                        //Console.WriteLine("mainloop dragdrop start.");
                    }
                }
            }
        }

        public void KeyCheck(int deltaTime) //EngineLoop engineLoop, 
        {
            float milliSecond = deltaTime * 0.001f;
            float speed = milliSecond * 100.0f;

            //UIEngine uIEngine = engineLoop.UIEngine;

            // 키보드를 누를때 사용할 컨트롤에 이벤트를 넘긴다.
            if (_keyDownSelectedControl != null)
            {
                if (_keyDownSelectedControl.KeyDown != null)
                {
                    _keyDownSelectedControl.KeyDown(_keyDownSelectedControl);
                }
            }

            if (Picker2d.SelectedControl != null)
            {
                // 선택된 컨트롤이 있는 경우

            }
            else
            {
                // 선택된 컨트롤이 없는 경우의 키처리
                /*
                if (KeyBoard.IsKeyPress(Key.Escape))
                {
                    EngineLoop.ChangeMode(EngineLoop.MAINLOOP_MODE.G3ENGINE);
                    engineLoop.UIEngine.HideAllForm();
                    KeyDownSelectedControl = null;
                }
                if (KeyBoard.IsKeyPress(Key.H))
                {
                    EngineLoop.ChangeMode(EngineLoop.MAINLOOP_MODE.G3ENGINE);
                    (Controls("debug") as Label).IsVisible = false;
                }

                if (KeyBoard.IsKeyPress(Key.X)) EventControls.SelectionControl = uIEngine.Controls("xpos");
                if (KeyBoard.IsKeyPress(Key.Y)) EventControls.SelectionControl = uIEngine.Controls("ypos");
                if (KeyBoard.IsKeyPress(Key.Z)) EventControls.SelectionControl = uIEngine.Controls("zpos");
                if (KeyBoard.IsKeyPress(Key.C)) EventControls.SelectionControl = uIEngine.Controls("Shininess");
                
                if (Picker3d.SelectedEntity != null)
                {
                    if (Keyboard.IsKeyDown(Key.W)) Picker3d.SelectedEntity.Pitch(speed);
                    if (Keyboard.IsKeyDown(Key.S)) Picker3d.SelectedEntity.Pitch(-speed);
                    if (Keyboard.IsKeyDown(Key.D)) Picker3d.SelectedEntity.Roll(speed);
                    if (Keyboard.IsKeyDown(Key.A)) Picker3d.SelectedEntity.Roll(-speed);
                    if (Keyboard.IsKeyDown(Key.E)) Picker3d.SelectedEntity.Yaw(speed);
                    if (Keyboard.IsKeyDown(Key.Q)) Picker3d.SelectedEntity.Yaw(-speed);
                }
                */
            }        
        }

        private void RegistryMouseEvent()
        {
            _mouseMove = (mx, my, fx, fy, dx, dy) =>
            {                
                _mx += dx;
                _my += dy;

                // 사용자 마우스를 처리한다.
                if (_isMouseEnable)
                {
                    _mouseImage.Left = fx;
                    _mouseImage.Top = fy;
                }

                // 마우스를 누르지 않고(드래그드롭하지 않고) 움직일 때,
                if (!_isMouseDown)
                {
                    if (_currentOverControl != null)
                    {
                        _currentOverControl?.MouseOver(_currentOverControl, fx, fy);
                        //_mouseImage.BackgroundImage = UIE
                    }
                }
            };

            _mouseDown = (x, y) =>
            {
                _isMouseDown = true;

                Control ctrl = Picker2d.PickUpControl(this, x, y);
                _dragControl = ctrl;

                if (ctrl?.MouseDown != null)
                {
                    ctrl.MouseDown(ctrl, x, y);
                    Console.WriteLine(ctrl.Name + " mouse down [ui]");
                }
            };

            _mouseUp = (x, y) =>
            {
                _isMouseDown = false;
                //Console.WriteLine($"{_name} mouse up.");

                Control ctrl = Picker2d.PickUpControl(this, x, y);
                //_dragControl = null;

                if (ctrl != null)
                {
                    if (ctrl.MouseUp != null)
                    {
                        ctrl.MouseUp(ctrl, x, y);
                        //Console.WriteLine(ctrl.Name + " mouse up");
                    }
                }
            };

            _mouseWheel = (delta) =>
            {
                Control ctrl = Picker2d.SelectedControl;
                if (ctrl != null && ctrl.MouseWheel != null)
                {
                    ctrl.MouseWheel(ctrl, delta);
                    Console.WriteLine(ctrl.Name + " MouseWheel");
                }
                Console.WriteLine(ctrl.Name);
            };

            _dragMove = (x, y, dx, dy) =>
            {
                //_dragControl = Picker2d.SelectedControl; 중간에 선택되면 안된다.
                if (_dragControl != null && _dragControl.DragDrop != null)
                {
                    _dragControl.DragDrop(_dragControl, x, y, dx, dy);
                    //Console.WriteLine(_dragControl.Name + " drag drop. [UI2d]");
                }
            };

            _dragStart = (x, y, dx, dy) =>
            {
                _dragControl = Picker2d.SelectedControl;
                if (_dragControl != null && _dragControl.DragStart != null)
                {
                    _dragControl.DragStart(_dragControl, x, y, dx, dy);
                    //Console.WriteLine(_dragControl.Name + " drag start. [UI2d]");
                }
            };

            _dragEnd = (x, y, dx, dy) =>
            {
                _dragControl = Picker2d.SelectedControl;
                if (_dragControl != null && _dragControl.DragEnd != null)
                {
                    _dragControl.DragEnd(_dragControl, x, y, dx, dy);
                    //Console.WriteLine(_dragControl.Name + " drag end. [UI2d]");
                }
            };

        }

    }
}
