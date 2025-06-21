using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui2d
{
    public class NumberBox : PictureBox
    {
        int _value = 0;
        Label _text;
        FontFamily _fontFamily;
        int _max = 0;
        Control _refControl;

        public Control RefControl
        {
            get => _refControl;
            set => _refControl = value;
        }

        public int Max
        {
            get => _max; 
            set => _max = value;
        }

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_text != null)
                {
                    _text.Text = _value.ToString();
                }
            }
        }

        public NumberBox(string name) : base(name, "")
        {
            _text = new Label(name + "_text")
            {
                ForeColor = new OpenGL.Vertex3f(0.9f, 0.9f, 0.0f),
                FontSize = 1.5f,
                Text = _value.ToString(),
                Top = 0.29f,
                Left = 0.47f,
                IsSelectable = false,
            };            
            AddChild(_text);

            AddChild(new Label(name + "_label")
            {
                ForeColor = new OpenGL.Vertex3f(0.9f, 0.9f, 0.9f),
                FontSize = 1.6f,
                Text = "갯수",
                Top = 0.0f,
                Left = 0.37f,
                IsSelectable = false,
            });

            AddChild(new ImageButton(name + "_minus")
            {
                BackgroundImage = UITextureLoader.Texture("minus_button"),
                ItemImageID = UITextureLoader.Texture("Item_Null"),
                Bound = new OpenGL.Vertex4f(0.1f, 0.5f, 0.5f, 0.5f),
                MouseIn = (o, x, y) => o.Parent.IsVisible = true,
                MouseOut = (o, x, y) => o.Parent.IsVisible = false,
            });

            AddChild(new ImageButton(name + "_plus")
            {
                BackgroundImage = UITextureLoader.Texture("plus_button"),
                ItemImageID = UITextureLoader.Texture("Item_Null"),
                Bound = new OpenGL.Vertex4f(0.6f, 0.575f, 0.4f, 0.5f),
                MouseIn = (o, x, y) => o.Parent.IsVisible = true,
                MouseOut = (o, x, y) => o.Parent.IsVisible = false,
            });


            UIEngine.UI2d<ImageButton>(name + "_minus").MouseUp += (obj, fx, fy) =>
            {
                Value--;
                if (_value < 0) Value = 0;
            };

            UIEngine.UI2d<ImageButton>(name + "_plus").MouseUp += (obj, fx, fy) =>
            {
                Value++;
                if (_value > _max) Value = _max;
            };

        }

    }
}
