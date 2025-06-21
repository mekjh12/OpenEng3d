using OpenGL;
using System;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Ui2d
{
    public class TextButton : Panel
    {
        const float CLICK_SIZE_RATIO = 0.95f;
        Label _text;
        PictureBox _pictureBox;
        FontFamily _fontFamily;

        public uint BackgroundImage
        {
            set
            {
                if (_pictureBox != null)
                {
                    _pictureBox.BackgroundImage = value;
                }
            }
        }

        public string Text
        {
            get => _text.Text;
            set
            {
                if (_text == null)
                {
                    _text = new Label(_name + "_text", _fontFamily)
                    {
                        IsSelectable = false,
                        Align = CONTROL_ALIGN.ROOT_MC,
                    };
                    //_text.AutoSize = true;
                    _text.Align = CONTROL_ALIGN.ROOT_MC;
                    AddChild(_text);
                }

                _text.ForeColor = _foreColor;
                _text.FontSize = _fontSize;
                _text.Text = value;
            }
        }

        public TextButton(string name, FontFamily fontFamily = null) : base(name)
        {
            _fontFamily = fontFamily;
            _alpha = 0.0f;

            _pictureBox = new PictureBox(name + "_background")
            {
                IsSelectable = false,
            };
            AddChild(_pictureBox);

            _mouseIn += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.OVER;
                UIEngine.MouseImageOver();
                _pictureBox.Alpha = 0.8f;
            };

            _mouseOut += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                UIEngine.MouseImageOut();
                _pictureBox.Alpha = 1.0f;
            };

            _mouseDown += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.CLICK;
                PictureBox obj = _pictureBox;
                obj.Location += obj.Size * (1 - CLICK_SIZE_RATIO) * 0.5f;
                obj.Size *= CLICK_SIZE_RATIO;
            };

            _mouseUp += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                PictureBox obj = _pictureBox;
                obj.Size /= CLICK_SIZE_RATIO;
                obj.Location -= obj.Size * (1 - CLICK_SIZE_RATIO) * 0.5f;
            };
        }

    }
}
