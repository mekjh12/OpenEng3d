using OpenGL;
using System;
using ZetaExt;

namespace Ui2d
{
    public class SimpHValueBar : ValueBar
    {
        public enum ProcessAlign { Left, Right }

        Label _label;
        Label _valueLabel;
        Panel _valueBar;
        Vertex3f _valueColor = Vertex3f.One;
        int _round = 3;

        protected event Action<Control, float> _valueChange;

        public Action<Control, float> ValueChange
        {
            get => _valueChange;
            set => _valueChange = value;
        }

        public override float Alpha
        {
            get => base.Alpha;
            set 
            {
                _alpha = value;
                _valueBar.Alpha = value;
                _valueLabel.Alpha = value;
            }
        }

        public Vertex3f ForeColor
        {
            set
            {
                _valueLabel.ForeColor = value;
                _label.ForeColor = value;
            }
        }

        public override Vertex3f BackColor
        {
            get => base.BackColor; 
            set
            {
                _valueBar.BackColor = value;
            }
        }

        public Vertex3f ValueColor
        {
            get => _valueColor;
            set
            {
                _valueColor = value;
                _valueBar.BackColor = value;
            }
        }

        public SimpHValueBar(string name): base(name)
        {
            // 값 그래프
            _valueBar = new Panel($"{_name}_valuebar")
            {
                Align = CONTROL_ALIGN.NONE,
                IsSelectable = false,
                Location = new Vertex2f(0.4f, 0.2f),
                Size = new Vertex2f(0.6f, 0.6f),
                BackColor = _valueColor,
            };
            AddChild(_valueBar);

            // 레이블
            _label = new Label($"{_name}_label")
            {
                Align = CONTROL_ALIGN.NONE,
                IsSelectable = false,
                IsBorder = false,
                FontSize = 0.8f,
                Text = _name,
                Location = new Vertex2f(0.01f, 0),
                Width = 0.4f,
                Alpha = 0.0f,
            };
            AddChild(_label);

            _valueLabel = new Label($"{_name}_value")
            {
                Align = CONTROL_ALIGN.NONE,
                IsSelectable = false,
                FontSize = 0.8f,
                Text = _value.ToString(),
                Left = 0.01f,
                Alpha = 0.0f,
            };
            AddChild(_valueLabel);

            // 배경
            _position = new Vertex2f(0, 0);
            _width = 1.0f;
            _height = 1.0f;
            _alpha = 0.5f;
            _backColor = new Vertex3f(0, 0, 0);

            // 이벤트
            MouseWheel = (o, delta) =>
            {
                SimpHValueBar me = (o as SimpHValueBar);
                me.Value += delta > 0 ? me.StepValue : -me.StepValue;
                _valueBar.Width = (_value / _maxValue) * 0.6f;
                _valueLabel.Text = _value.Round(_round).ToString();
                _valueLabel.Left = _valueBar.Right + 0.02f;
                //Debug.PrintLine(me.Name + "=" + _valueLabel.FontSize.ToString());

                if (_valueChange != null)
                {
                    _valueChange(o, me.Value);

                    if (_isIniWritable)
                    {
                        IniFile.WritePrivateProfileString("sysInfo", _name, _value.ToString());
                    }
                }
            };

            MouseWheel(this, 0);
        }

        public override float Value
        {
            get => _value;
            set
            {
                _value = value.Clamp(_minValue, _maxValue);
                UpdateValue();
            }
        }

        public int Round
        {
            get => _round; 
            set => _round = value;
        }

        public void UpdateValue()
        {
            _valueBar.Width = (_value / _maxValue) * 0.6f;
            _valueLabel.Text = _value.Round(_round).ToString();
            _valueLabel.Left = _valueBar.Right + 0.02f;
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            if (!_isInit)
            {
                _isInit = true;
                _label.UpdateText();
                _valueLabel.UpdateText();
            }
        }

    }
}
