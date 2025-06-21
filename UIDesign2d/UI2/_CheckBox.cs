using OpenGL;
using System;

namespace UIDesign2d
{
    class CheckBox : Panel
    {
        bool _checked;

        ImageButton2 _checkButton;
        Label _label;

        Action<CheckBox, bool> _checkChanged;

        /// <summary>
        /// control, bool
        /// </summary>
        public Action<CheckBox, bool> CheckChanged
        {
            get => _checkChanged;
            set => _checkChanged = value;
        }

        public bool Checked
        {
            get => _checked; 
            set => _checked = value;
        }

        public string Text
        {
            set => _label.Text = value;
            get => _label.Text;
        }

        public uint[] CheckImage
        {
            set => _checkButton.Image = value;
        }

        public CheckBox(string name, FontFamily fontFamily) : base(name)
        {
            _backColor = new Vertex3f(1, 1, 0);
            _alpha = 0.0f;
            _width = 1;
            _height = 1;
            _isSelectable = false;

            _checkButton = new ImageButton2($"{name}_checkbox")
            {
                Align = CONTROL_ALIGN.ML,
                Margin = 0.0f,
                Width = 0.2f,
                Height = 0.8f,
                Padding = 0.2f,
                BorderColor = Vertex3f.Zero,
                BorderWidth = 1.0f,
                IsSelectable = true,
                Image = UITextureLoader.GetTexture("btnDef"),
            };
            AddChild(_checkButton);
            _checkButton.TextureImageMode = (_checked) ? MOUSE_IMAGE_MODE.CHECKED : MOUSE_IMAGE_MODE.NORMAL;

            _label = new Label($"{name}_label", fontFamily)
            {
                Align = CONTROL_ALIGN.RIGHTTOP_START,
                AdjontControl = _checkButton,
                Margin = 0.2f,
                Text = "hms",
                Alpha = 0.0f,
                IsSelectable = false,
                IsRendeable = true,
                IsVisible = true,
            };
            AddChild(_label);

            _checkButton.MouseUp += (o, x, y) =>
            {
                _checked = !_checked;
                _checkButton.TextureImageMode = (_checked) ? MOUSE_IMAGE_MODE.CHECKED : MOUSE_IMAGE_MODE.NORMAL;
                if (_checkChanged != null) _checkChanged(this, _checked);
            };

            _checkButton.MouseOver = (obj, x, y) =>
            {
                _checkButton.BorderWidth = _checkButton.BorderWidth * 4.0f;
            };

            _checkButton.MouseOut = (obj, x, y) =>
            {
                _checkButton.BorderWidth = _checkButton.BorderWidth * 0.25f;
            };

        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            // 체크박스의 텍스트로 인한 크기 변화로 Pannel and CheckButton의 크기를 수정한다.
            _width = (_label.TextWidth + _label.TextHeight +_label.RenderingPaddingX * 3) / Parent.RenderingWidth;
            _height = (_label.TextHeight + _label.RenderingPaddingY * 2) / Parent.RenderingHeight;

            _checkButton.Width = (0.8f * _label.TextHeight) / renderingWidth;
            _checkButton.Height = 1.0f;

        }

    }
}
