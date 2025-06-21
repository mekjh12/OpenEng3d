using OpenGL;
using System;
using System.Windows.Forms;

namespace Ui2d
{
    public class Button : Label
    {
        protected Vertex3f _clickColor = new Vertex3f(0, 122.0f / 255.0f, 204.0f / 255.0f);
        protected Vertex3f _bgColor = new Vertex3f(63.0f / 255.0f, 63.0f / 255.0f, 70.0f / 255.0f);
        protected Vertex3f _overColor = new Vertex3f(84.0f / 255.0f, 84.0f / 255.0f, 92.0f / 255.0f);

        public Vertex3f ClickColor
        {
            get => _clickColor;
            set => _clickColor = value;
        }

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

        public Button(string name, FontFamily fontFamily) : base(name, fontFamily)
        {
            _name = name;
            _margin = 0.0f;
            _padding = 0;
            _isCenter = true;
            _lineWidthMax = 1.0f;
            _lineWidthMin = 0.001f;
            _width = 0.001f;
            _height = 0.001f;
            _maxNumOfLine = 1;
            _isBorderd = true;
            _borderWidth = 2.0f;
            _vScrollBar.IsVisible = false;
            _isDragDrop = false;

            _borderColor = _bgColor;
            _borderColor = _bgColor * 2.0f;
            _borderWidth = 1.0f;
            _isBorderd = true;

            MouseIn = (o, x, y) =>
            {
                _backColor = _overColor;
                _borderColor = _overColor * 2.0f;
                Console.WriteLine(_name + " mouse in");
            };

            MouseOut = (o, x, y) =>
            {
                _backColor = _bgColor;
                _borderColor = _bgColor * 2.0f;
                Console.WriteLine(_name + " mouse out");
            };

            MouseDown = (o, x, y) =>
            {
                _backColor = _clickColor;
                _borderColor = _clickColor * 2.0f;
                //Console.WriteLine(_name + " mouse down");
            };

            MouseUp = (o, x, y) =>
            {
                _backColor = _bgColor;
                _borderColor = _bgColor * 2.0f;
                //Console.WriteLine(_name + " mouse up!");
            };

            _mouseOut += (o, x, y) =>
            {
                UIEngine currentUIEngine = UIEngine.CurrentUIEngine;
                UIEngine.MouseImageOut();
            };

            _mouseIn += (o, x, y) =>
            {
                UIEngine currentUIEngine = UIEngine.CurrentUIEngine;
                UIEngine.MouseImageOver();
            };
        }
    }
}
