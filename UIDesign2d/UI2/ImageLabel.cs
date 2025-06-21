using OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Ui2d
{
    public class ImageLabel : PictureBox
    {
        public enum ALIGN { LEFT, CENTER, RIGHT, CUSTOM };

        public enum VALIGN { TOP, MIDDLE, BOTTOM, CUSTOM };

        /// <summary>
        /// 화면 초기에 표시할 1.0f크기의 비율을 가지고 설정할 초기 크기값
        /// </summary>
        private const float INIT_HEIGHT = 600.0f;

        protected Text _text;
        protected FontFamily _fontFamily;
        protected ALIGN _horizonAlign = ALIGN.LEFT;
        protected VALIGN _verticalAlign = VALIGN.TOP;
        protected float _lineWidthMin = 0.001f;
        protected float _lineWidthMax = 1.0f;
        protected bool _isCursorMouseOver = false;
        protected bool _autoSize = false;
        protected Vertex2f _location = Vertex2f.Zero;
        protected float _alphaText = 1.0f;

        public float AlphaText
        {
            get => _alphaText;
            set => _alphaText = value;
        }

        public float LineWidthMax
        {
            get => _lineWidthMax;
            set => _lineWidthMax = value;
        }

        public float LineWidthMin
        {
            get => _lineWidthMin;
            set => _lineWidthMin = value;
        }
        
        /// <summary>
        /// TextHorizonAlign, TextVerticalAlign CUSTOM인 경우에 사용할 수 있다.
        /// </summary>
        public Vertex2f TextLocation
        {
            get => _location;
            set => _location = value;
        }

        /// <summary>
        /// 줄간격
        /// </summary>
        protected float _lineSpacing = 1.1f;
        protected string _txt = "";
        protected string _prevTxt = "";

        public bool AutoSize
        {
            get => _autoSize;
            set => _autoSize = value;
        }

        public ALIGN TextHorizonAlign
        {
            get => _horizonAlign;
            set => _horizonAlign = value;
        }

        public VALIGN TextVerticalAlign
        {
            get => _verticalAlign;
            set => _verticalAlign = value;
        }

        /// <summary>
        /// 텍스트가 지정되면 폰트크기에 따라 컨트롤의 너비와 높이가 자동으로 조정된다.
        /// </summary>
        public string Text
        {
            get => _txt;
            set
            {
                if (_txt != value)
                {
                    _txt = value;
                    SetTextMesh(_txt);
                }
            }
        }


        public ImageLabel(string name, FontFamily fontFamily) : base(name)
        {
            _fontFamily = fontFamily;

            _text = new Text(name, _name, _fontSize)
            {
                FontFamily = fontFamily
            };

            MouseIn += (o, x, y) =>
            {
                if (_isCursorMouseOver)
                {
                    _textureImageMode = MOUSE_IMAGE_MODE.OVER;
                    UIEngine.MouseImageOver();
                }
            };

            MouseOut += (o, x, y) =>
            {
                if (_isCursorMouseOver)
                {
                    _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                    UIEngine currentUIEngine = UIEngine.CurrentUIEngine;
                    UIEngine.MouseImageOut();
                }
            };

            MouseDown += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.CLICK;
            };

            MouseUp += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
            };

        }

        public override Vertex3f BackColor
        {
            get => _backColor;
            set => _backColor = value;
        }

        public override void Update(int deltaTime = 0)
        {
            if (_txt != "")
            {
                SetTextMesh(_txt);
            }

            base.Update(deltaTime);
        }

        public override void Render(UIShader uiShader, FontRenderer fontRenderer)
        {
            if (IsVisible)
            {
                // 패널 배경을 그린다.
                base.Render(uiShader, fontRenderer);

                if (_txt != "")
                {
                    Vertex2f offset = new Vertex2f(renderingPaddingX, renderingPaddingY);

                    // 배경 위에 글자를 그린다.
                    if (_horizonAlign == ALIGN.CENTER)
                        offset.x = (renderingWidth - _text.Width) * 0.5f;
                    else if (_horizonAlign == ALIGN.RIGHT)
                        offset.x = renderingWidth - _text.Width - renderingPaddingX;
                    else if (_horizonAlign == ALIGN.LEFT)
                        offset.x = renderingPaddingX;
                    else if (_horizonAlign == ALIGN.CUSTOM)
                        offset.x = _location.x * renderingWidth;

                    if (_verticalAlign == VALIGN.MIDDLE)
                        offset.y = (renderingHeight - _text.Height) * 0.5f;
                    else if (_verticalAlign == VALIGN.BOTTOM)
                        offset.y = _text.Height + renderingPaddingY;
                    else if (_verticalAlign == VALIGN.TOP)
                        offset.y = renderingPaddingY;
                    else if (_verticalAlign == VALIGN.CUSTOM)
                        offset.y = _location.y * renderingHeight;

                    fontRenderer.Render(_fontFamily, text: _text, position: renderingPosition1 + offset);
                }
            };
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="txt"></param>
        protected void SetTextMesh(string txt)
        {
            float pWidth = (Parent == null) ? 1.0f : Parent.RenderingWidth;
            float pHeight = (Parent == null) ? 1.0f : Parent.RenderingHeight;

            _text.Color = new Vertex4f(_foreColor.x, _foreColor.y, _foreColor.z, _alphaText);
            _text.FontSize = (ImageLabel.INIT_HEIGHT / UIEngine.Height) * _fontSize;

            float lineWidthMax = pWidth * _width * _lineWidthMax;

            // Max라인에 맞게 글자를 자른다.
            // IndexOfLine, MaxNumOfLine으로 텍스트를 자른다.
            _txt = txt;
            _text.SetText(txt, _lineSpacing, maxLineWidth: lineWidthMax);

            // 자신의 상대 크기를 계산한다. 자신의 상대적 크기를 text.width의 절대크기를 비로 하여 
            float aspect = UIEngine.Aspect;
            float cHeight = _text.Height / (1 - 2 * _padding);
            float cPadding = cHeight * _padding;
            float cWidth = _text.Width + 2 * cPadding;
            cWidth = Math.Max(_lineWidthMin, cWidth);
            cWidth = Math.Min(_lineWidthMax, cWidth);

            if (txt == "아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.아이템 설명자료입니다.")
            {
                Console.WriteLine("s");
            }            

            if (_autoSize)
            {
                _width = cWidth / pWidth; //상대크기
                _height = cHeight / pHeight; //상대높이
            }
        }

    }
}
