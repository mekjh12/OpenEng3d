using OpenGL;
using System;

namespace UIDesign2d
{
    class ColorPicker: Panel
    {
        uint _color;

        HValueBarT _barRed;
        HValueBarT _barGreen;
        HValueBarT _barBlue;
        Panel _panelColor;

        event Action<Control, Vertex3f> _valueChanged;

        /// <summary>
        /// 0xFFFFFF 흰색(r,g,b)
        /// </summary>
        public uint Color
        {
            set
            {
                _color = value;
                Vertex3f color = Color3f(_color);
                _barRed.Value = 255.0f * color.x;
                _barGreen.Value = 255.0f * color.y;
                _barBlue.Value = 255.0f * color.z;
                _panelColor.BackColor = Color3f(_color);
            }

            get => _color;
        }
        
        public Vertex3f ColorVertex3f
        {
            get => Color3f(_color);
        }

        /// <summary>
        /// control, vertex3f (0,0,0) ~ (1,1,1)
        /// </summary>
        public Action<Control, Vertex3f> ValueChanged
        {
            set => _valueChanged = value;
            get => _valueChanged;
        }

        public ColorPicker(string name, FontFamily fontFamily) : base(name)
        {
            _color = 0x00FF00;
            _backColor = new Vertex3f(0.6f, 0.6f, 0.6f);

            _panelColor = new Panel(name + "_pannel")
            {
                Align = CONTROL_ALIGN.NONE,
                BackColor = Color3f(_color),
                Width = 0.3f,
                Height = 1.0f,
                Margin = 0.03f,  
                IsBorder = true,
                BorderWidth = 1.0f,
                BorderColor = new Vertex3f(1, 1, 1),
            };
            AddChild(_panelColor);

            _barRed = new HValueBarT(name + "_red", fontFamily)
            {
                Align = CONTROL_ALIGN.NONE,
                Margin = 0.0f,
                Top = 0.0f,
                Left = 0.31f,
                Width = 0.68f,
                Height = 0.33f,
                MinValue = 0,
                MaxValue = 255,
                ScrollChanged = (bar, value, delta) =>
                {
                    _color = Color3f(_barRed.Value, _barGreen.Value, _barBlue.Value);
                    _panelColor.BackColor = Color3f(_color);
                    _valueChanged(this, Color3f(_color));
                },
            };
            AddChild(_barRed);

            _barGreen = new HValueBarT(name + "_green", fontFamily)
            {
                Align = CONTROL_ALIGN.NONE,
                Margin = 0.0f,
                Top = 0.333f,
                Left = 0.31f,
                Width = 0.68f,
                Height = 0.33f,
                MinValue = 0,
                MaxValue = 255,
                ScrollChanged = (bar, value, delta) =>
                {
                    _color = Color3f(_barRed.Value, _barGreen.Value, _barBlue.Value);
                    _panelColor.BackColor = Color3f(_color);
                    _valueChanged(this, Color3f(_color));
                },
            };
            AddChild(_barGreen);

            _barBlue = new HValueBarT(name + "_blue", fontFamily)
            {
                Align = CONTROL_ALIGN.NONE,
                Margin = 0.0f,
                Top = 0.666f,
                Left = 0.31f,
                Width = 0.68f,
                Height = 0.33f,
                MinValue = 0,
                MaxValue = 255,
                ScrollChanged = (bar, value, delta) =>
                {
                    _color = Color3f(_barRed.Value, _barGreen.Value, _barBlue.Value);
                    _panelColor.BackColor = Color3f(_color);
                    _valueChanged(this, Color3f(_color));
                },
            };
            AddChild(_barBlue);
        }

        private Vertex3f Color3f(uint color)
        {
            byte red = (byte)(color >> 16 & 0xFF);
            byte green = (byte)(color >> 8 & 0xFF);
            byte blue = (byte)(color & 0xFF);
            return new Vertex3f(red / 255.0f, green / 255.0f, blue / 255.0f);
        }

        private uint Color3f(float r, float g, float b)
        {
            byte rr = (byte)(1 * r);
            byte gg = (byte)(1 * g);
            byte bb = (byte)(1 * b);
            uint red = (uint)(rr << 16);
            uint green = (uint)(gg << 8);
            uint blue = (uint)(bb);
            return red + green + blue;
        }

    }
}
