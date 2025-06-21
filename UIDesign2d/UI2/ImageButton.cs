using OpenGL;
using System;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Ui2d
{
    public class ImageButton : PictureBox
    {
        const float CLICK_SIZE_RATIO = 0.9f;
        PictureBox _itemImage;
        Label _text;
        FontFamily _fontFamily;
        Vertex2f _orginalSize = Vertex2f.One;
        Vertex2f _orginalLocation = Vertex2f.Zero;

        public override float FontSize
        {
            set
            {
                _fontSize = value;
                _text.FontSize = value;
            }
        }

        public string Text
        {
            get => _text.Text;
            set
            {
                if (_text == null)
                {
                    _text = new Label(_name + "_text", _fontFamily);
                    _text.Align = CONTROL_ALIGN.ROOT_MC;
                    _itemImage.AddChild(_text);
                }

                _text.ForeColor = _foreColor;
                _text.FontSize = _fontSize;
                _text.Text = value;
            }
        }

        public PictureBox ItemImage
        {
            get => _itemImage;
            set => _itemImage = value;
        }

        public uint ItemImageID
        {
            get => _itemImage.BackgroundImage;
            set => _itemImage.BackgroundImage = value;
        }

        public ImageButton(string name, FontFamily fontFamily = null) : base(name)
        {
            _fontFamily = fontFamily;

            _isSelectable = false;

            _itemImage = new PictureBox(name + "_itemImage")
            {
                Bound = new Vertex4f(0, 0, 1, 1),
                IsSelectable = true,
            };
            AddChild(_itemImage);

            _text = new Label(_name + "_text", _fontFamily)
            {
                Align = CONTROL_ALIGN.ROOT_MC,
                IsSelectable = false,
            };
            _itemImage.AddChild(_text);


            _itemImage.MouseIn = (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.OVER;
                UIEngine.MouseImageOver();
            };

            _itemImage.MouseOut = (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                UIEngine.MouseImageOut();
            };

            _itemImage.MouseDown = (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.CLICK;
                PictureBox obj = o as PictureBox;
                obj.Location += obj.Size * (1 - CLICK_SIZE_RATIO) * 0.5f;
                obj.Size *= CLICK_SIZE_RATIO;
            };

            _itemImage.MouseUp = (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                PictureBox obj = o as PictureBox;
                obj.Size /= CLICK_SIZE_RATIO;
                obj.Location -= obj.Size * (1 - CLICK_SIZE_RATIO) * 0.5f;
            };

            _itemImage.DragStart += (o, fx, fy, dx, dy) =>
            {
                if (DragStart != null) DragStart(this, fx, fy, dx, dy);
            };

            _itemImage.DragEnd += (o, fx, fy, dx, dy) =>
            {
                if (DragEnd != null) DragEnd(this, fx, fy, dx, dy);
            };

            _itemImage.DragDrop += (o, fx, fy, dx, dy) =>
            {
                if (DragDrop != null) DragDrop(this, fx, fy, dx, dy);
            };

        }

        public new Action<Control, float, float> MouseUp
        {
            get => _itemImage.MouseUp;
            set => _itemImage.MouseUp += value;
        }


    }
}
