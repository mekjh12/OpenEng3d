using OpenGL;
using System;

namespace Ui2d
{
    public class VerticalValueBar : Panel
    {
        int _round = 3;
        float _sensity = 0.05f;
        float _value = 5.0f;
        float _maxValue = 10.0f;
        float _minValue = 3.0f;
        Vertex3f _scrollColor = new Vertex3f(1, 1, 0);
        Panel _focus;

        event Action<VerticalValueBar, float, float> _scrollChanged;

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
            }
        }

        /// <summary>
        /// HValueBar, value, delta
        /// </summary>
        public Action<VerticalValueBar, float, float> ScrollChanged
        {
            get => _scrollChanged;
            set => _scrollChanged = value;
        }

        public Vertex3f ScrollColor
        {
            set => _scrollColor = value;
        }

        public VerticalValueBar(string name, FontFamily fontFamily) : base(name)
        {
            _isBorderd = true;
            _borderColor = _scrollColor;
            _height = 1.0f;

            _focus = new Panel(name + "_focus_button")
            {
                Align = CONTROL_ALIGN.NONE,
                Width = 1.0f,
                Height = 0.1f,
                Alpha = 0.5f,
                IsSelectable = false,
                IsRendeable = true,
                BackColor = _scrollColor,
            };
            AddChild(_focus);

            DragDrop += (o, x, y, dx, dy) =>
            {
                float rdy = dy / renderingHeight;
                float delta = rdy * (_maxValue - _minValue);
                Value += delta;
                if (_scrollChanged != null) _scrollChanged(this, _value, delta);
            };

            MouseOver += (obj, x, y) =>
            {
                BackColor = _backColor * 0.5f;
            };

            MouseOut += (obj, x, y) =>
            {
                BackColor = _backColor * 2.0f;
            };

            MouseDown += (o, x, y) =>
            {

            };
            
            MouseUp += (o, x, y) =>
            {
                
            };

            MouseWheel += (o,delta) =>
            {
                float d = _sensity * (_maxValue - _minValue);
                float del = (delta > 0) ? d : -d;
                Value += del; // 속성으로 접근해야 범위 안에서 움직인다.
                if (_scrollChanged != null) _scrollChanged(this, _value, del);
            };
        }

        public override void Update(int deltaTime)
        {
            float Height = (_maxValue - _minValue);
            float value = (_value - _minValue) / Height;
            _focus.Top = (1.0f - _focus.Height) * value;
            base.Update(deltaTime);
        }

    }
}
