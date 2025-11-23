using OpenGL;
using System;
using System.Text;

namespace Ui2d
{
    /// <summary>
    /// 
    /// </summary>
    public class Label : Panel
    {
        /// <summary>
        /// 화면 초기에 표시할 높이사이즈가 1.0f크기의 비율을 가지고 설정할 초기 픽셀 크기값
        /// </summary>
        private const float INIT_HEIGHT = 600.0f;

        protected Text _text;
        protected FontFamily _fontFamily;
        protected bool _isCenter = false;
        protected bool _autosize = true;

        /// <summary>
        /// 너비의 최소크기
        /// </summary>
        protected float _lineWidthMin = 0.001f;

        /// <summary>
        /// 너비의 최대크기
        /// </summary>
        protected float _lineWidthMax = 1.0f;
        protected float _alphaText = 1.0f;
        protected float _lineSpacing = 1.1f;
        protected string _txt = "";
        protected int _maxNumOfLine = 1; // _text Text객체에 적용할 최대 줄 길이
        protected int _indexOfLine = 0; // _txt의 줄 시작번호
        protected int _numOfLine = 0; // _txt의 줄 길이

        protected VScrollBar _vScrollBar;
        protected VerticalValueBar _verticalValueBar;
        protected event Action<Control, int> _valueChanged;

        // 재사용을 위한 변수들
        private readonly StringBuilder _reusableStringBuilder = new StringBuilder(512);
        private Vertex4f _reusableColor = new Vertex4f();

        #region ============ 속성================

        public bool AutoSize
        {
            get => _autosize; 
            set => _autosize = value;
        }

        /// <summary>
        /// 글자의 투명도를 지정한다.
        /// </summary>
        public float AlphaText
        {
            get => _alphaText;
            set => _alphaText = value;
        }

        public VerticalValueBar VerticalBar => _verticalValueBar;

        public Action<Control, int> ValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        public float TextWidth => _text.Width;

        public float TextHeight => _text.Height;

        /// <summary>
        /// 한 줄에서의 레벨만 텍스트 가운데 정렬을 할 수 있다.
        /// </summary>
        public bool IsCenter
        {
            get => _isCenter;
            set => _isCenter = value;
        }

        /// <summary>
        /// 최대 줄의 길이를 설정한다. 남은 부분의 글자는 스크롤을 처리한다.
        /// </summary>
        public int MaxNumOfLine
        {
            set
            {
                _maxNumOfLine = value;
                //_vScrollBar.MaxLine = value;
                _verticalValueBar.MaxValue = value;
            }
            get=> _maxNumOfLine;
        }

        /// <summary>
        /// 줄간격 1.0f은 글자 크기의 상대적 크기이다.<br/>
        /// 한 텍스트 안에서의 줄 간격<br/>
        /// </summary>
        public float LineSpacing
        {
            get=> _lineSpacing;
            set => _lineSpacing = value;
        }

        /// <summary>
        /// * 텍스트에 맞추어 줄의 최소, 최대크기를 고정하여 줄의 크기를 일정하게 유지한다. <br/>
        /// * 텍스트가 없다면 부모크기에 영향을 주지 않는다. <br/>
        /// </summary>
        public float LineFixedTextWidth
        {
            set
            {
                _lineWidthMin = value;
                _lineWidthMax = value;
            }
        }
        /// <summary>
        /// 줄의 최소 크기 1.0은 부모에 대한 상대적 크기이다.
        /// </summary>
        public float LineWidthMin
        {
            set=> _lineWidthMin = value;
        }

        /// <summary>
        /// 부모에 대한 상대적 크기로 줄의 최대 크기 1.0이나  <br/>
        /// 부모의 범위를 벗어나면 더 크게 설정해야 한다.
        /// </summary>
        public float LineWidthMax
        {
            set => _lineWidthMax = value;
        }

        /// <summary>
        /// 배경의 알파값을 지정한다. 값이 0이면 경계선은 자동으로 없어진다.
        /// </summary>
        public override float Alpha
        {
            get => _alpha;
            set
            {
                if (_alpha <= 0) IsBorder = false;
                _alpha = value.Clamp(0.0f, 1.0f);
                _text.Color = new Vertex4f(_text.Color.x, _text.Color.y, _text.Color.z, _alpha);
            }
        }

        /// <summary>
        /// 텍스트가 지정되면 폰트크기에 따라 컨트롤의 너비와 높이가 자동으로 조정된다.
        /// </summary>
        public string Text
        {
            get => _txt;
            set
            {
                if (value == null) return;

                if (_txt != value)
                {
                    _txt = value;
                    if (_txt.IndexOf(UI2.NewLine) > 0)
                    {
                        _numOfLine = _txt.NumOfLine(UI2.NewLine);
                    }

                    SetTextMesh(_txt);
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fontFamily"></param>
        public Label(string name, FontFamily fontFamily = null) : base(name)
        {
            _height = 0.001f;
            _padding = 0.0f;
            _margin = 0.0f;

            if (fontFamily == null) fontFamily = FontFamilySet.연성체;
            _fontFamily = fontFamily;

            _text = new Text(name, _name, _fontSize)
            {
                FontFamily = fontFamily,
                Color = new Vertex4f(_foreColor.x, _foreColor.y, _foreColor.z, 1.0f),
            };

            _vScrollBar = new VScrollBar(name + "_vscroll", fontFamily)
            {
                Align = CONTROL_ALIGN.ROOT_TR,
                Width = 0.05f,
                Height = 1.0f,
                BorderWidth = 3.0f,
                BorderColor = _borderColor * 0.3f,
                BackColor = _backColor,
                ScrollColor = ForeColor,
                MaxLine = _maxNumOfLine,
                Alpha = 0.3f,
            };
            //AddChild(_vScrollBar);

            _verticalValueBar = new VerticalValueBar(name + "_verticalScroll", fontFamily)
            {
                Align = CONTROL_ALIGN.ROOT_TR,
                Width = 0.1f,
                BorderWidth = 3.0f,
                BorderColor = _borderColor * 0.3f,
                BackColor = _backColor,
                ScrollColor = ForeColor,
                Alpha = 0.3f,
            };
            AddChild(_verticalValueBar);

            MouseWheel += (o, delta) => 
            {
                int del = (delta < 0) ? 1 : -1;
                _indexOfLine += del;
                int M = _maxNumOfLine;
                int L = _txt.NumOfLine(UI2.NewLine);
                _indexOfLine = (L <= M)? 0: _indexOfLine.Clamp(0, L - M);
                //_vScrollBar.Index = _indexOfLine;
                _verticalValueBar.Value = _indexOfLine;
                this.Text = _txt;
                if (_valueChanged != null) _valueChanged(this, _indexOfLine);
            };
        }

        public override void Render(UIShader uiShader, FontRenderer fontRenderer)
        {
            if (IsVisible)
            {
                if (!_autosize)
                {
                    _width = _orgSize.x / Parent.Width;
                    _height = _orgSize.y / Parent.Height;                    
                }

                // 배경을 렌더링한다.
                if (_alpha > 0.0f)
                {
                    base.Render(uiShader, fontRenderer);
                }
                
                // 글자를 렌더링한다.
                if (_txt != "")
                {
                    uiShader.Bind();
                    uiShader.LoadColor(_text.Color);

                    if (_isCenter)
                    {
                        Vertex2f pad = Vertex2f.Zero;
                        pad.y = renderingPaddingY;
                        pad.x = (renderingWidth - _text.Width) * 0.5f;
                        fontRenderer.Render(_fontFamily, text: _text, position: renderingPosition1 + pad);
                    }
                    else
                    {
                        fontRenderer.Render(_fontFamily, _text, renderingPosition1 + new Vertex2f(renderingPaddingx * 0.5f, renderingPaddingY));
                    }

                    // 스크롤 설정을 한다.
                    //_vScrollBar.IsVisible = (_numOfLine > _maxNumOfLine) ? true : false;
                    _verticalValueBar.IsVisible = (_numOfLine > _maxNumOfLine) ? true : false;
                    //Console.WriteLine($"{_numOfLine} / {_maxNumOfLine}");
                }

                // 자식을 그린다.
                if (_isRenderChild)
                {
                    foreach (Control ctrl in _controls)
                    {
                        if (ctrl.IsRendeable)
                        {
                            ctrl.Render(uiShader, fontRenderer);
                        }
                    }
                }

            }
        }

        public override void Update(int deltaTime = 0)
        {
            base.Update(deltaTime);

            if (_txt != "")
            {
                if (!_isInit)
                {
                    SetTextMesh(_txt);
                    _isInit = true;
                }
 
                if (_verticalValueBar.RenderingWidth > 0.005f)
                        _verticalValueBar.Width = _verticalValueBar.Width - 0.001f;
            }
            else
            {
                _txt = "null";
            }

            if (!_autosize)
            {
                _width = _orgSize.x;
                _height = _orgSize.y;
            }
        }

        public void UpdateText()
        {
            _isInit = false;
        }

        /// <summary>
        /// Update전에 정보를 처리하면 기본정보를 이용하여 업데이트 처리된다.
        /// </summary>
        /// <param name="txt"></param>
        protected void SetTextMesh(string txt)
        {
            float pWidth = (Parent == null) ? 1.0f : Parent.RenderingWidth;
            float pHeight = (Parent == null) ? 1.0f : Parent.RenderingHeight;

            // 1. 객체 재사용 (new Vertex4f 대신)
            _reusableColor.x = _foreColor.x;
            _reusableColor.y = _foreColor.y;
            _reusableColor.z = _foreColor.z;
            _reusableColor.w = _alphaText;
            _text.Color = _reusableColor;

            _text.FontSize = (Label.INIT_HEIGHT / UIEngine.Height) * _fontSize;
            float relLineWidthMax = pWidth * _lineWidthMax;

            // 2. StringBuilder 사용 (string += 대신)
            _txt = txt;
            _reusableStringBuilder.Clear();
            string[] lines = txt.Replace(UI2.NewLine, "$").Split('$');

            // 3. Math 함수 제거 (boxing 방지)
            int endOfLine = lines.Length < (_indexOfLine + _maxNumOfLine) ?
                           lines.Length : (_indexOfLine + _maxNumOfLine);

            for (int i = _indexOfLine; i < endOfLine; i++)
            {
                _reusableStringBuilder.Append(lines[i].Trim());
                if (i < endOfLine - 1)
                    _reusableStringBuilder.Append(UI2.NewLine);
                else
                    _reusableStringBuilder.Append(UI2.EndOfString);
            }

            string cuttingText = _reusableStringBuilder.ToString();

            // 4. 텍스트 설정
            _text.SetText(cuttingText, _lineSpacing, maxLineWidth: relLineWidthMax);

            // 5. 크기 계산 (Math.Max 대신 조건문)
            float aspect = UIEngine.Aspect;
            float pixel = _text.Height * UIEngine.Height;
            float cHeight = _text.Height / (1 - 2 * _padding);
            float cPadding = cHeight * _padding;
            float cWidth = _text.Width + 2 * cPadding;

            float minWidth = pWidth * _lineWidthMin;
            cWidth = cWidth > minWidth ? cWidth : minWidth;

            if (_autosize)
            {
                _width = cWidth / pWidth;
                _height = cHeight / pHeight;
            }
        }
    }
}
