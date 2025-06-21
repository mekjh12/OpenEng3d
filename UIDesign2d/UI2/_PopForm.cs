using System;

namespace UIDesign2d
{
    class PopForm : CForm
    {
        protected bool _isPoped = true;
        float _prevHeight = 0.1f;
        protected Button _btnPop;
        string _titleText;
        protected bool _isAutoSize = true;

        event Action<Control, bool> _onPoped;

        public float PrevHeight
        {
            get => _prevHeight;
            set => _prevHeight = value;
        }

        public Action<Control, bool> OnPoped
        {
            get => _onPoped;
            set => _onPoped = value;
        }

        public bool AutoSize
        {
            get => _isAutoSize;
            set => _isAutoSize = value;
        }

        public bool IsPoped
        {
            get => _isPoped;
            set => _isPoped = value;
        }

        public PopForm(string name, FontFamily fontFamily, string title = "", float width = 0.3f, float height = 0.5f)
            : base(name, fontFamily)
        {
            _height = height;
            _width = width;
            _prevHeight = _height;

            _titleText = title;
            if (_titleText == "") _titleText = name;

            // Window Pop Menu
            _btnPop = new Button($"{name}_btnPop", fontFamily)
            {
                Align = CONTROL_ALIGN.TL,
                Padding = 0.07f,
                FontSize = _fontSize,
                Margin = 0.0f,
                MouseDown = (obj, x, y) =>
                {
                    _isPoped = !_isPoped;
                    PopBody(_isPoped);
                },
            };
            _btnPop.Text = "*";
            _titleLabel.AddChild(_btnPop);
            _titleLabel.Padding = 0.1f;

            _onPoped = (o, b) => { };

            _sizableButton.DragDrop += (obj, x, y, dx, dy) =>
            {
                Width += dx;
                Height += dy;
                _prevHeight = _height;
            };


        }

        public void PopBody(bool poped)
        {
            PopRenderable(poped);
            _onPoped(this, poped);

            _isRenderable = true;
            _titleLabel.PopRenderable(poped);
            _titleLabel.IsRendeable = true;
            _btnPop.IsRendeable = true;
        }

        public override void Update(int deltaTime)
        {
            _title = "  " + _titleText;
            if (_isPoped)
            {
                _height = _prevHeight;
            }
            else
            {
                float foldedHeight = _titleLabel.RenderingHeight / Parent.RenderingHeight;
                _height = foldedHeight;
            }

            base.Update(deltaTime);            
        }

    }
}
