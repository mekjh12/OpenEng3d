using OpenGL;
using System;

namespace Ui2d
{
    public class SimpCheckbox : Panel
    {
        Label _label;
        Panel _checkPanel;
        bool _checked;
        protected event Action<SimpCheckbox, bool> _changeValue;

        public float Width
        {
            get
            {
                return _label.TextWidth + (_checkPanel.Width) * _width;
            }
        }

        public Action<SimpCheckbox, bool> ChangeValue
        {
            get => _changeValue;
            set => _changeValue = value;
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                _checkPanel.BackColor = _checked ? new Vertex3f(0.6f, 0.3f, 0.3f) : new Vertex3f(0.2f, 0.2f, 0.2f);
            }
        }

        public string Text
        {
            get => _label.Text;
            set
            {
                _label.Text = value;
                _label.Update(0);
            }
        }

        public override float Alpha
        {
            get => base.Alpha;
            set 
            {
                _alpha = value;
                //_label.Alpha = value;
                //_checkPanel.Alpha = value;
            }
        }

        public Vertex3f ForeColor
        {
            set
            {
                _checkPanel.ForeColor = value;
            }
        }

        public Panel Panel => _checkPanel;

        public SimpCheckbox(string name, FontFamily fontFamily): base(name)
        {
            Alpha = 0.3f;

            _checkPanel = new Panel($"{_name}_panel")
            {
                Align = CONTROL_ALIGN.NONE,
                Padding = 0.2f,
                Location = new Vertex2f(0.01f, 0.1f),
                Size = new Vertex2f(0.04f, 0.8f),
                Alpha = 1.0f,
                IsBorder = true,
                BorderWidth = 1.0f,
                BorderColor = Vertex3f.Zero,
                IsFixedAspect = true,
                MouseDown = (o, fx, fy) =>
                {
                    Checked = !Checked;
                    _checkPanel.BackColor = _checked ? new Vertex3f(0.6f, 0.3f, 0.3f) : new Vertex3f(0.2f, 0.2f, 0.2f);
                    if (_changeValue != null) _changeValue(this, _checked);
                }
            };
            AddChild(_checkPanel);
            _checkPanel.BackColor = _checked ? new Vertex3f(0.6f, 0.3f, 0.3f) : new Vertex3f(0.2f, 0.2f, 0.2f);

            _label = new Label($"{_name}_label", fontFamily)
            {
                Align = CONTROL_ALIGN.NONE,
                Location = new Vertex2f(0.07f, 0.0f),
                IsSelectable = false,
                ForeColor = _foreColor,
                IsBorder = true,
                Margin = 0.0f,
                FontSize = _fontSize,
                Text = _name,
                Alpha = 0.0f,
                Width = 0.2f,
            };
            AddChild(_label);

            Size = _label.Size;
            _isSelectable = false;
            _backColor = new Vertex3f(0, 0, 0);
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            if (!_isInit)
            {
                _isInit = true;
                _label.UpdateText();
                _checkPanel.BackColor = _checked ? new Vertex3f(0.6f, 0.3f, 0.3f) : new Vertex3f(0.2f, 0.2f, 0.2f);
            }
        }
    }
}
