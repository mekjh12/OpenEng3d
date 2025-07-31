using OpenGL;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ZetaExt;

namespace Ui2d
{
    /// <summary>
    /// 
    ///                         |---> HValueBar
    ///                         |---> Label  ---> ImageLabel ---> ImageButton
    ///  Control --> Pannel --- |---> CForm ---|---> PopForm
    ///                         |              |---> MessageBoxs
    ///                         |---> CheckBox
    ///                   
    ///           
    /// * 초기화할때 사이즈는 작동하지 않아 이후에 사이즈를 적용하도록 한다.
    /// </summary>
    public abstract class Control : IControlRenderable
    {
        protected bool _isRenderChild = true;
        protected bool _isRenderable = true;
        protected bool _isSelectable = true;

        private static uint GUID = 256 * 50;

        protected uint _maxPixelHeight = 1080;

        protected bool _isInit = false;

        protected bool _isIniWritable = false;

        /// <summary>
        /// 강제로 크기의 최대 높이를 설정할 수 있다. 가로와 세로의 비는 이전 크기의 비율을 그대로 사용한다.
        /// </summary>
        public uint MaxPixelHeight
        {
            get => _maxPixelHeight;
            set => _maxPixelHeight = value;
        }

        /// <summary>
        /// 현재 컨트롤을 렌더링 여부를 결정한다.
        /// </summary>
        public bool IsRendeable
        {
            get => _isRenderable;
            set => _isRenderable = value;
        }

        /// <summary>
        /// 현재 컨트롤를 제외한 하위 자식 컨트롤 렌더링 여부를 결정한다.
        /// </summary>
        public bool IsRenderChild
        {
            get => _isRenderChild;
            set => _isRenderChild = value;
        }

        /// <summary>
        /// 현재 컨트롤을 마우스 선택기능에서 사용할지 여부를 결정한다.
        /// </summary>
        public bool IsSelectable
        {
            get => _isSelectable;
            set => _isSelectable = value;
        }

        public static uint Gen
        {
            get
            {
                GUID++;
                return GUID;
            }
        }

        public enum CONTROL_ALIGN
        {
            NONE,
            ROOT_TL, ROOT_TC, ROOT_TR,
            ROOT_ML, ROOT_MC, ROOT_MR,
            ROOT_BL, ROOT_BC, ROOT_BR,

            /// <summary>
            /// 가운데 선을 기준으로 왼쪽으로 정렬, 위 아래는 Top을 이용하여 조정
            /// </summary>
            HALF_VERTICAL_LEFT,
            /// <summary>
            /// 가운데 선을 기준으로 오른쪽으로 정렬, 위 아래는 Top을 이용하여 조정
            /// </summary>
            HALF_VERTICAL_RIGHT,
            /// <summary>
            /// 가운데 선을 기준으로 가운데으로 정렬, 위 아래는 Top을 이용하여 조정
            /// </summary>
            HALF_VERTICAL_CENTER,
            
            RIGHTTOP_START, 
            RIGHTBOTTOM_START, 
            LEFTBOTTOM_MARGIN_Y_START,
            LEFTBOTTOM_MARGIN_XY_START,
            ADJOINT_LEFT,
            ADJOINT_RIGHT,
            ADJOINT_TOP,
            ADJOINT_BOTTOM,
        };

        public enum MOUSE_IMAGE_MODE { NORMAL, OVER, CLICK, CHECKED, CHECKED_OVER, DRAGDROP }

        protected MOUSE_IMAGE_MODE _textureImageMode;
        protected string _name;
        protected float _width;
        protected float _height;
        protected uint _guid = 0;
        protected float _depth = 0.0f;
        protected Vertex2f _orgSize = Vertex2f.Zero;

        protected bool _isVisible;

        protected float _alpha = 1.0f;

        protected bool _isFixedAspect = false;

        //protected FontFamily _fontFamily;
        protected float _fontSize = 0.8f;
        protected float _maxFontSize = 1.0f;
        protected bool _isDragDrop = false;
        protected bool _isMouseDown = false;
        //private bool _isActiveDragDrop = false;

        protected Control _adjointControl = null;

        //protected float _depth = 0;
        protected List<Control> _controls;
        protected List<Control> _topControls;

        public MOUSE_IMAGE_MODE TextureImageMode
        {
            get => _textureImageMode;
            set => _textureImageMode = value;
        }

        public uint Guid => _guid;

        public float Depth
        {
            get => _depth;
            set => _depth = value;
        }

        public bool IsFixedAspect
        {
            get => _isFixedAspect;
            set => _isFixedAspect = value;
        }

        /// <summary>
        /// * 상대적 위치를 지정할 참조할 인접한 컨트롤을 지정한다. <br/>
        /// * CONTROL_ALIGN을 사용하여 인접한 컨트롤을 기준으로 어떤 위치에 놓일지 지정한다.
        /// </summary>
        public Control AdjontControl
        {
            set => _adjointControl = value;
            get => _adjointControl;
        }

        protected Vertex2f _updateSize = Vertex2f.Zero;

        /// <summary>
        /// maxPixelHeight에 의하여 Update에 크기가 변하면 강제 변환된 크기를 가져온다. 부모와의 상대적 크기이다.
        /// </summary>
        public Vertex2f UpdateSize => _updateSize;

        public Vertex2f RenderingPosition1 => renderingPosition1;

        public Vertex2f RenderingPosition2 => renderingPosition2;

        /// <summary>
        /// Left-Top 절대좌표
        /// </summary>
        protected Vertex2f renderingPosition1;

        /// <summary>
        /// Right-Bottom 절대좌표
        /// </summary>
        protected Vertex2f renderingPosition2;

        /// <summary>
        /// 절대너비
        /// </summary>
        protected float renderingWidth;

        /// <summary>
        /// 절대크기
        /// </summary>
        protected float renderingHeight;

        /// <summary>
        /// 절대 MarginY
        /// </summary>
        protected float renderingMarginY;

        /// <summary>
        /// 절대 MarginX
        /// </summary>
        protected float renderingMarginX;

        /// <summary>
        /// 절대 PaddingX
        /// </summary>
        protected float renderingPaddingX;

        /// <summary>
        /// 절대 PaddingY
        /// </summary>
        protected float renderingPaddingY;

        protected Vertex2f _position;
        protected Vertex3f _backColor;
        protected Vertex3f _foreColor;

        protected event Action<Control> _start;
        protected event Action<Control> _stop;
        protected event Action<Control> _resume;
        protected event Action<Control, int, int> _init;

        protected event Action<Control, float, float> _mouseDown;
        protected event Action<Control, float, float> _mouseUp;
        protected event Action<Control, float, float> _mouseOver;
        protected event Action<Control, float, float> _mouseOut;
        protected event Action<Control, float, float> _mouseIn;
        protected event EventHandler _mouseMove;
        protected event Action<Control, float> _mouseWheel;
        protected event Action<Control, float, float, float, float> _dragdrop;
        protected event Action<Control, float, float, float, float> _dragStart;
        protected event Action<Control, float, float, float, float> _dragEnd;
        protected event Action<Control> _keyDown;

        private bool _isMouseOver = false;

        private Control _parent = null;
        protected bool _isAllowClickOnly = true;

        protected CONTROL_ALIGN _align = CONTROL_ALIGN.NONE;

        protected float _padding = 0.00f;
        protected float _margin = 0.000f;

        #region ================== 속성======================

        /// <summary>
        /// 절대너비 0.0부터 1.0까지
        /// </summary>
        public float RenderingWidth => renderingWidth;

        /// <summary>
        /// 절대높이 0.0부터 1.0까지
        /// </summary>
        public float RenderingHeight => renderingHeight;

        public float RenderingPaddingX => renderingPaddingX;

        public float RenderingPaddingY => renderingPaddingY;

        /// <summary>
        /// 0.0은 투명, 1.0은 불투명이다.
        /// </summary>
        public virtual float Alpha
        {
            get => _alpha;
            set => _alpha = value;
        }

        /// <summary>
        /// 자신의 높이에서 상대적인 크기
        /// </summary>
        public float Padding
        {
            set
            {
                _padding = value;
                _padding = value;
            }
        }

        /// <summary>
        /// 자신의 높이에서의 상대적인 크기
        /// </summary>
        public float Margin
        {
            set=> _margin = value;
        }


        public float Left
        {
            get => _position.x;
            set => _position.x = value;
        }

        public float Top
        {
            get => _position.y;
            set => _position.y = value;
        }

        public virtual CONTROL_ALIGN Align
        {
            get => _align;
            set => _align = value;
        }

        public Control Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Action<Control> KeyDown
        {
            get => _keyDown;
            set => _keyDown = value;
        }

        /// <summary>
        /// 개별 클래스마다 Start할 때 발생하는 이벤트이다.
        /// </summary>
        public Action<Control> StartEvent
        {
            get => _start;
            set => _start = value;
        }

        public Action<Control> StopEvent
        {
            get => _stop;
            set => _stop = value;
        }

        public Action<Control> ResumeEvent
        {
            get => _resume;
            set => _resume = value;
        }

        /// <summary>
        /// object, x, y, rx, ry<br/>
        /// object = 자기자신 컨트롤<br/>
        /// x = fx로 0과 1사이의 값<br/>
        /// y = fy로 0과 1사이의 값<br/>
        /// dx = 0과 1로에서의 x의 변화값<br/>
        /// dx = 0과 1로에서의 y의 변화값<br/>
        /// </summary>
        public Action<Control, float, float, float, float> DragDrop
        {
            get => _dragdrop;
            set => _dragdrop = value;
        }

        public Action<Control, float, float, float, float> DragStart
        {
            get => _dragStart;
            set => _dragStart = value;
        }

        public Action<Control, float, float, float, float> DragEnd
        {
            get => _dragEnd;
            set => _dragEnd = value;
        }

        public Action<Control, float, float> MouseDown
        {
            get => _mouseDown;
            set => _mouseDown = value;
        }

        public Action<Control, float> MouseWheel
        {
            get => _mouseWheel;
            set => _mouseWheel = value;
        }

        public virtual Action<Control, float, float> MouseUp
        {
            get => _mouseUp;
            set => _mouseUp = value;
        }

        public Action<Control, float, float> MouseOver
        {
            get => _mouseOver;
            set => _mouseOver = value;
        }

        public EventHandler MouseMove
        {
            get => _mouseMove;
            set => _mouseMove = value;
        }

        public Action<Control, float, float> MouseOut
        {
            get => _mouseOut;
            set => _mouseOut = value;
        }

        public Action<Control, float, float> MouseIn
        {
            get => _mouseIn;
            set => _mouseIn = value;
        }

        public bool IsMouseOver
        {
            get => _isMouseOver;
            set => _isMouseOver = value;
        }

        public float Right => _position.x + _width;

        public float Bottom => _position.y + _height;

        public bool IsHaveChild => (_controls.Count > 0);

        public bool IsMouseDown
        {
            get => _isMouseDown;
            set => _isMouseDown = value;
        }


        public bool IsVisible
        {
            get => _isVisible; 
            set => _isVisible = value;
        }

        public Vertex3f ForeColor
        {
            get => _foreColor;
            set => _foreColor = value;
        }

        public virtual Vertex3f BackColor
        {
            get => _backColor;
            set
            {
                _backColor = value;
            }
        }

        public Vertex2f Position
        {
            get => _position;
            set => _position = value;
        }
        
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public Control AddCtrl
        {
            set
            {
                if (value != null)
                    AddChild(value);
            }
        }

        public Control FirstControl => (_controls.Count > 0) ? _controls[0] : null;

        public Control SecondControl => (_controls.Count > 1) ? _controls[1] : null;

        public Vertex4f Bound
        {
            get => new Vertex4f(_position.x, _position.y, _width, _height);
            set
            {
                _position = new Vertex2f(value.x, value.y);
                _width = value.z;
                _height = value.w;
            }
        }

        public Vertex2f Location
        {
            set => _position = value;
            get => _position;
        }


        /// <summary>
        /// 부모의 높이에서 상대적 높이로 지정한다. <br/>
        /// 단, 텍스트가 지정되면 너비와 높이는 자동으로 지정된다.
        /// </summary>
        public virtual float Height
        {
            get => _height;
            set => _height = value;
        }

        public virtual float FontSize
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        /// <summary>
        /// 부모의 너비에서 상대적 너비로 지정한다. <br/>
        /// 단, 텍스트가 지정되면 너비와 높이는 자동으로 지정된다.
        /// </summary>
        public virtual float Width
        {
            get => _width; 
            set => _width = value;
        }

        public virtual Vertex2f Size
        {
            get => new Vertex2f(_width, _height);
            set
            {
                _width = value.x; 
                _height = value.y;
                _orgSize = new Vertex2f(_width, _height);
            }
        }

        public bool IsIniWritable
        {
            get => _isIniWritable; 
            set => _isIniWritable = value;
        }

        #endregion


        public Control(string name)
        {
            _guid = Control.Gen;
            _name = name;
            //_fontFamily = fontFamily;
            _controls = new List<Control>();
            _topControls = new List<Control>();
            _backColor = new Vertex3f(0.2f, 0.2f, 0.2f);
            _foreColor = Vertex3f.One;
            _width = 0.001f;
            _height = 0.001f;
            _position = Vertex2f.Zero;
            _fontSize = 0.8f;
            _isVisible = true;
            _isMouseDown = false;
            _isDragDrop = false;
            _isSelectable = true;

            _mouseDown = (o, x, y) => { }; // this.Init();
            _mouseUp = (o, x, y) => { }; // this.Init();
            _mouseOver = (o, x, y) => { }; // this.Init();
            _mouseMove = (o, e) => { }; // this.Init();
            _dragdrop = (o, x, y, rx, ry) => { }; // this.Init();
            _dragStart = (o, x, y, rx, ry) => { }; // this.Init();
            _dragEnd = (o, x, y, rx, ry) => { }; // this.Init();

        }

        public abstract void Shutdown();

        public abstract void Render(UIShader uiShader, FontRenderer fontRenderer);

        /// <summary>
        ///  하위 개체까지 포함하여 렌더링 여부를 전환한다.
        /// </summary>
        public void PopRenderable(bool renderable)
        {
            _isRenderable = renderable;

            foreach (Control ctrl in _controls)
            {
                ctrl.PopRenderable(renderable);
            }
        }

        /// <summary>
        /// UI2d PickUp을 위한 색상 렌더링을 한다.
        /// </summary>
        /// <param name="uiShader"></param>
        /// <param name="fontRenderer"></param>
        public void Render(UIColorShader uiShader, FontRenderer fontRenderer)
        {
            if (!IsVisible) return;

            uint r = 0, g = 0, b = 0;

            if (Parent == null)
            {
                _depth = 0.99f;
                //루트패널은 선택하지 않는다.
            }
            else
            {
                // rgb로 변환한다.
                uint rgb = (uint)_guid;
                r = rgb >> 16;
                g = (rgb >> 8) & 255;
                b = rgb & 255;
                _depth = Parent.Depth - 0.01f;
            }

            if (_isSelectable)
            {
                Gl.Enable(EnableCap.Blend);
                Gl.BlendEquation(BlendEquationMode.FuncAdd);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                uiShader.Bind();
                uiShader.LoadColor(new Vertex4f((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, 1));
                uiShader.SetView(renderingPosition1.x, renderingPosition1.y, _depth, renderingWidth, renderingHeight);
                uiShader.LoadModelMatrix();

                Gl.BindVertexArray(uiShader.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);

                uiShader.Unbind();
                Gl.Disable(EnableCap.Blend);
            }

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Render(uiShader, fontRenderer);

            // top 컨트롤을 맨 위에 그린다. 마우스는 픽업에서 제외되어야 하므로 그리지 않는다.
            foreach (Control ctrl in _topControls) ctrl.Render(uiShader, fontRenderer);
        }

        /// <summary>
        /// 해당 컨트롤을 UI시스템의 가장 위로 배치한다.
        /// </summary>
        public void PushTopest()
        {
            // 제거 후 더해야 가장 최상위로 위치함.
            if (Parent != null)
            {
                Parent._controls.Remove(this);
                Parent._controls.Add(this);
            }
        }

        public Control AddTopControl(Control ctrl)
        {
            if (_topControls.Contains(ctrl))
            {
                throw new Exception($"{ctrl.Name}키가 이미 존재하여 컨트롤의 Name을 변경해 주세요.");
            }
            else
            {
                ctrl.Parent = this;
                _topControls.Add(ctrl);
            }
            return ctrl;
        }

        public void Remove(Control ctrl)
        {
            if (UIEngine.Controls(ctrl.Name) != null)
            {
                Control loc = null;
                foreach (Control item in _controls)
                {
                    if (ctrl == item)
                    {
                        loc = item;
                        break;
                    }
                }
                _controls.Remove(loc);
            }
        }

        public Control AddChild(Control ctrl)
        {
            if (UIEngine.Controls(ctrl.Name) != null)
            {
                //throw new Exception($"{ctrl.Name}키가 이미 존재하여 컨트롤의 Name을 변경해 주세요.");
            }
            else
            {
                ctrl.Parent = this;
                _controls.Add(ctrl);
                //Console.WriteLine($"{ctrl.Parent.Name} ---> {ctrl.Name}");
                UIEngine.RegistryControl(ctrl);
            }
            return ctrl;
        }

        public void Print()
        {
            Console.WriteLine($"Control Name: {_name}, Type: {this.GetType().ToString()}");
            foreach (Control ctrl in _controls) ctrl.Print();
        }

        public virtual void Update(int deltaTime)
        {
            // calculate a absolutly position of child from position of parent.
            Vertex2f delta = Vertex2f.Zero;
            float aspect = UIEngine.Aspect;

            return;

            // 최상위 컨트롤이 아닌 경우에
            if (Parent != null)
            {
                float X1 = Parent.renderingPosition1.x;
                float Y1 = Parent.renderingPosition1.y;

                float W = Parent.RenderingWidth;
                float H = Parent.RenderingHeight;

                float x1 = W * _position.x;
                float y1 = H * _position.y;
                
                float h = H * _height;
                float my = h * _margin; //marginX

                float w = W * _width;
                float mx = my / aspect;

                float py = h * _padding; //paddingX
                float px = w * _padding / aspect;

                int absPixelHeight = (int)(h * UIEngine.Height);
                if (absPixelHeight > _maxPixelHeight)
                {
                    float asp = w / h;
                    h = (float)_maxPixelHeight / UIEngine.Height;
                    w = h * asp;
                }

                _updateSize.x = w / W;
                _updateSize.y = h / H;

                switch (_align)
                {
                    // none
                    case CONTROL_ALIGN.NONE:
                        delta.x = x1 + px;
                        delta.y = y1 + py;
                        break;

                    // top
                    case CONTROL_ALIGN.ROOT_TL:
                        delta.x = mx;
                        delta.y = my;

                        break;

                    case CONTROL_ALIGN.ROOT_TR:
                        delta.x = W - mx - w;
                        delta.y = my;
                        break;

                    case CONTROL_ALIGN.ROOT_TC:
                        delta.x = 0.5f * (W - w);
                        delta.y = my;
                        break;

                    // bottom
                    case CONTROL_ALIGN.ROOT_BR:
                        delta.x = W - mx - w;
                        delta.y = H - my - h;
                        break;

                    case CONTROL_ALIGN.ROOT_BL:
                        delta.x = mx;
                        delta.y = H - my - h;
                        break;

                    case CONTROL_ALIGN.ROOT_BC:
                        delta.x = 0.5f * (W - w);
                        delta.y = H - my - h;
                        break;

                    // middle
                    case CONTROL_ALIGN.ROOT_ML:
                        delta.x = mx;
                        delta.y = 0.5f * (H - h);
                        break;

                    case CONTROL_ALIGN.ROOT_MR:
                        delta.y = 0.5f * (H - h);
                        break;

                    case CONTROL_ALIGN.ROOT_MC:
                        delta.x = 0.5f * (W - w);
                        delta.y = 0.5f * (H - h);
                        break;

                    case CONTROL_ALIGN.HALF_VERTICAL_LEFT:
                        delta.x = 0.5f * W - w - mx;
                        delta.y = y1;
                        break;

                    case CONTROL_ALIGN.HALF_VERTICAL_RIGHT:
                        delta.x = 0.5f * W + mx;
                        delta.y = y1;
                        break;

                    case CONTROL_ALIGN.HALF_VERTICAL_CENTER:
                        delta.x = 0.5f * (W - w);
                        delta.y = y1;
                        break;

                    case CONTROL_ALIGN.RIGHTTOP_START:
                        delta.x = mx;
                        delta.y = 0;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition2.x;
                            Y1 = _adjointControl.renderingPosition1.y;
                        }
                        break;

                    case CONTROL_ALIGN.LEFTBOTTOM_MARGIN_Y_START:
                        delta.x = 0;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition1.x;
                            Y1 = _adjointControl.renderingPosition2.y;
                        }
                        break;

                    case CONTROL_ALIGN.LEFTBOTTOM_MARGIN_XY_START:
                        delta.x = mx;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition1.x;
                            Y1 = _adjointControl.renderingPosition2.y;
                        }
                        break;

                    case CONTROL_ALIGN.RIGHTBOTTOM_START:
                        delta.x = mx;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition2.x;
                            Y1 = _adjointControl.renderingPosition2.y;
                        }
                        break;

                    case CONTROL_ALIGN.ADJOINT_LEFT:
                        delta.x = mx;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition1.x - renderingWidth;
                            //Y1 = _adjointControl.renderingPosition1.y;
                        }
                        break;

                    case CONTROL_ALIGN.ADJOINT_RIGHT:
                        delta.x = mx;
                        //delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition2.x;
                            Y1 = _adjointControl.renderingPosition1.y;
                        }
                        break;

                    case CONTROL_ALIGN.ADJOINT_TOP:
                        //delta.x = mx;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition1.x;
                            Y1 = _adjointControl.renderingPosition1.y - renderingHeight;
                        }
                        break;

                    case CONTROL_ALIGN.ADJOINT_BOTTOM:
                        //delta.x = mx;
                        delta.y = my;
                        if (_adjointControl != null)
                        {
                            X1 = _adjointControl.renderingPosition1.x;
                            Y1 = _adjointControl.renderingPosition2.y;
                        }
                        break;

                    default:
                        break;
                }

                renderingPosition1.x = X1 + delta.x;
                renderingPosition1.y = Y1 + delta.y;
                renderingPosition2.x = renderingPosition1.x + w;
                renderingPosition2.y = renderingPosition1.y + h;

                if (_isFixedAspect)
                {
                    float rH = renderingPosition2.y - renderingPosition1.y;
                    float pixelHeight = UIEngine.Height * rH;
                    renderingPosition2 = new Vertex2f(renderingPosition1.x + (pixelHeight/UIEngine.Width), renderingPosition2.y);
                }

                renderingMarginY = my;
                renderingMarginX = mx;
                renderingPaddingX = px;
                renderingPaddingY = py;
                renderingWidth = w;
                renderingHeight = h;
            }
            // 최상위 컨트롤인 경우에
            else
            {
                // 마우스를 위한 컨트롤의 조정------------------------------------------------
                if (_name == "sysInfoMouse")
                {
                    float orginHeight = UIEngine.Height;
                    float h = 17.0f / orginHeight;
                    float w = h * UIEngine.MouseAspect / UIEngine.Aspect;

                    renderingPosition1.x = _position.x;
                    renderingPosition1.y = _position.y;
                    renderingPosition2.x = renderingPosition1.x + w;
                    renderingPosition2.y = renderingPosition1.y + h;

                    renderingWidth = w;
                    renderingHeight = h;
                    renderingMarginX = 0.0f;
                    renderingMarginY = 0.0f;
                    renderingPaddingX = 0.0f;
                    renderingPaddingY = 0.0f;
                }
                //---------------------------------------------------------------------------
                else
                {
                    renderingPosition1.x = 0;
                    renderingPosition1.y = 0;
                    renderingPosition2.x = renderingPosition1.x + _width;
                    renderingPosition2.y = renderingPosition1.y + _height;

                    renderingMarginX = 0.0f;
                    renderingMarginY = 0.0f;
                    renderingPaddingX = 0.0f;
                    renderingPaddingY = 0.0f;
                    renderingWidth = _width;
                    renderingHeight = _height;
                }
            }

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Update(deltaTime);
        }

        public virtual void Init()
        {
            // 초기화 코드는 아래에 구성한다.           
            if (_init != null) _init(this, 0, 0);

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Init();
        }

        public virtual void Start()
        {
            if (_start != null) _start(this);

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Start();
        }

        public virtual void Stop()
        {
            if (_stop != null) _stop(this);

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Stop();
        }

        public virtual void Resume()
        {
            if (_resume != null) _resume(this);

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Resume();
        }

        /// <summary>
        /// 자기자신이 부모로부터의 상대적 위치를 가져온다.
        /// </summary>
        /// <param name="curCtrl"></param>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        /// <returns></returns>
        public Vertex2f GetRelativeCoordinate2f(float fx, float fy)
        {
            Control curCtrl = this;
            Stack<Control> stack = new Stack<Control>();
            while (curCtrl.Parent != null)
            {
                stack.Push(curCtrl);
                curCtrl = curCtrl.Parent;
            }

            Vertex2f pos = new Vertex2f(fx, fy);
            Vertex2f ctrlPos = new Vertex2f(fx, fy);

            while (stack.Count > 0) // 하나는 남겨놓아야 한다.
            {
                Control ctrl = stack.Pop();
                pos.x = (pos.x - ctrl.Left) / ctrl.UpdateSize.x;
                pos.y = (pos.y - ctrl.Top) / ctrl.UpdateSize.y;
            }

            return pos;
        }
    }
}
