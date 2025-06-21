using OpenGL;
using System;

namespace Ui2d
{
    /// <summary>
    /// 이미지에 글자로 이루어진 버튼이다.
    /// </summary>
    public class ImageButton2 : PictureBox
    {
        Vertex2f _orginalSize = Vertex2f.One;

        /// <summary>
        /// <b>이미지버튼 또는 텍스트버튼을 만든다. </b><br/>
        /// * 이미지버튼으로 마우스 over, out, click에 따라 버튼의 action를 지정할 수 있다.<br/> 
        /// * 버튼의 속성으로 이미지, 텍스트, 너비, 높이를 지정한다.<br/>
        /// * 기본이미지를 <b>imagename_out.png</b>이고 나머지가 지정되지 않으면 기본 이미지로 자동 지정된다.<br/>
        /// * 텍스트를 지정하지 않으면 글자는 표시되지 않는다.<br/>
        /// * 텍스트가 지정이 되면 가로, 세로의 크기가 padding에 맞추어 자동 설정된다. <br/>
        /// </summary>
        /// <param name="name"></param>
        public ImageButton2(string name) : base(name)
        {
            _name = name;
            _borderColor = _backColor * 0.8f;
            _margin = 0.0f;
            _padding = 0;
            _borderWidth = 1.0f;
            _isBorderd = false;
            _width = 1;
            _height = 1;
            _isDragDrop = false;

            MouseIn += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.OVER;
                UIEngine.MouseImageOver();
            };

            MouseOut += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                UIEngine.MouseImageOut();
            };

            MouseDown += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.CLICK;
                PictureBox obj = o as PictureBox;
                obj.Size *= 0.98f;
            };

            MouseUp += (o, x, y) =>
            {
                _textureImageMode = MOUSE_IMAGE_MODE.NORMAL;
                PictureBox obj = o as PictureBox;
                (o as ImageButton2).Size /= 0.98f;
            };

        }

    }
}
