using OpenGL;
using System;

namespace UIDesign2d
{
    class HValueBarT : Panel
    {
        protected Vertex3f _bgColor = new Vertex3f(63.0f / 256.0f, 63.0f / 256.0f, 63.0f / 256.0f);
        protected Vertex3f _overColor = new Vertex3f(84.0f / 256.0f, 84.0f / 256.0f, 92.0f / 256.0f);
        protected Vertex3f _focusColor = new Vertex3f(250.0f / 256.0f, 184.0f / 256.0f, 0.0f / 256.0f);

        public enum RateOfChnage { Linear, Exponential, Logarithm };
        int _round = 3;
        float _sensity = 0.05f;
        float _value = 5.0f;
        float _maxValue = 10.0f;
        float _minValue = 3.0f;
        Vertex3f _scrollColor = new Vertex3f(1, 1, 0);
        Panel _focus;
        Label _label;
        string _title = "";
        RateOfChnage _rateOfChnage = RateOfChnage.Linear;

        event Action<HValueBarT, float, float> _scrollChanged;

        public new Vertex3f BackColor
        {
            get => _bgColor;
            set => _bgColor = value;
        }

        public Vertex3f OverColor
        {
            get => _overColor;
            set => _overColor = value;
        }

        public Vertex3f FocusColor
        {
            get => _focusColor;
            set => _focusColor = value;
        }

        public RateOfChnage RateChange
        {
            set => _rateOfChnage = value;
        }

        public string Title
        {
            set => _title = value;
        }

        public float MaxValue
        {
            get=>_maxValue; 
            set => _maxValue = value;
        }

        public float MinValue
        {
            get => _minValue;
            set => _minValue = value;
        }

        /// <summary>
        /// 소숫점 아래 자리를 잘라서 표시해 준다.
        /// </summary>
        public int Round
        {
            get => _round;
            set => _round = value;
        }

        /// <summary>
        /// 마우스 휠의 민감도를 설정한다. 0.1은 10번의 휠로 전체를 움직일 수 있다.
        /// </summary>
        public float Sensity
        {
            get => _sensity;
            set => _sensity = value;
        }

        public float Value
        {
            get => _value;
            set
            {
                _value = value.Clamp(_minValue, _maxValue);
                _label.Text = _title + " " + _value.Round(3) + "f";
            }
        }

        /// <summary>
        /// HValueBar, value, delta
        /// </summary>
        public Action<HValueBarT, float, float> ScrollChanged
        {
            get => _scrollChanged;
            set => _scrollChanged = value;
        }

        public HValueBarT(string name, FontFamily fontFamily) : base(name)
        {
            _isBorderd = true;
            _backColor = _bgColor;            

            _label = new Label(name + "_label", fontFamily)
            {
                Text = _title + " " + _value + "f",
                Alpha = 0.0f,
                IsSelectable = false,
            };
            AddChild(_label);

            _focus = new Panel(name + "_focus_button")
            {
                Align = CONTROL_ALIGN.NONE,
                Width = 0.05f,
                Height = 1.0f,
                Alpha = 0.5f,
                IsSelectable = false,
                IsRendeable = true,
                BackColor = _focusColor,
            };
            AddChild(_focus);

            DragDrop += (o, x, y, rx, ry) =>
            {
                float rdx = rx / renderingWidth;
                float delta = rdx * (_maxValue - _minValue);
                Value += delta;
                if (_scrollChanged != null) _scrollChanged(this, _value, delta);
                _label.Text = _title + " " + _value.Round(3) + "f";
            };

            MouseOver += (obj, x, y) =>
            {
                
            };

            MouseIn = (o, x, y) =>
            {
                _backColor = _overColor;
                _borderColor = _overColor * 2.0f;
                //Console.WriteLine(_name + " mouse in");
            };

            MouseOut = (o, x, y) =>
            {
                _backColor = _bgColor;
                _borderColor = _bgColor * 2.0f;
                //Console.WriteLine(_name + " mouse out");
            };

            MouseDown = (o, x, y) =>
            {
            };

            MouseUp = (o, x, y) =>
            {
            };

            MouseWheel += (delta) =>
            {
                float d = _sensity * (_maxValue - _minValue);
                float del = (delta > 0) ? d : -d;
                Value += del; // 속성으로 접근해야 범위 안에서 움직인다.
                _label.Text = _title + " " + _value.Round(3) + "f";
                if (_scrollChanged != null) _scrollChanged(this, _value, del);
            };
        }

        public override void Update(int deltaTime)
        {
            float Width = (_maxValue - _minValue);
            float value = (_value - _minValue) / Width;
            _focus.Left = (1.0f - _focus.Width) * value;
            _height = _label.TextHeight / Parent.RenderingHeight;

            base.Update(deltaTime);
        }

    }
}
