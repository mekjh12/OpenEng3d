using OpenGL;

namespace Ui2d
{
    public class PictureBox : Panel
    {
        protected uint _texture = 0;
        protected uint _textureOver = 0;
        protected uint _textureClick = 0;
        protected uint _textureChecked = 0;

        protected bool _autoAspect = false;
        protected bool _horzFlip = false;

        public virtual bool HorzFlip
        {
            get => _horzFlip;
            set => _horzFlip = value;
        }

        public bool AutoAspect
        {
            get => _autoAspect;
            set
            {
                _autoAspect = value;
                if (_autoAspect)
                {
                    float imageAspect = UITextureLoader.TextureAspect(_name);
                    _width = _height * imageAspect / UIEngine.Aspect;
                }
            }
        }

        public uint[] Image
        {
            set
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (i == 0) _texture = value[0];
                        if (i == 1) _textureOver = value[1];
                        if (i == 2) _textureClick = value[2];
                        if (i == 3) _textureChecked = value[3];
                    }
                }
            }
        }

        public uint BackgroundImage
        {
            get => _texture;
            set => _texture = value;
        }

        public PictureBox(string name, string imageFileName = "") : base(name)
        {
            Bound = new Vertex4f(0, 0, 1, 1);
            Image = UITextureLoader.GetTexture(imageFileName == "" ? name : imageFileName);
        }

        public override void Render(UIShader uiShader, FontRenderer fontRenderer)
        {
            if (!IsVisible) return;

            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            uiShader.Bind();

            // 이미지가 로딩되면 이미지 버튼으로 인식하여 이미지를 텍스처로 선택하고
            // 텍스트버튼이면 색상을 사용하여 배경을 그린다.
            //Console.WriteLine($"{_name} {_textureImageMode} {_texture}");
            uint bindTextureId = _texture;

            switch (_textureImageMode)
            {
                case MOUSE_IMAGE_MODE.DRAGDROP:
                    bindTextureId = (_textureOver > 0) ? _textureOver : _texture;
                    break;
                case MOUSE_IMAGE_MODE.CHECKED:
                    bindTextureId = (_textureChecked > 0) ? _textureChecked : _texture;
                    break;
                case MOUSE_IMAGE_MODE.CLICK:
                    bindTextureId = (_textureClick > 0) ? _textureClick : _texture;
                    break;
                case MOUSE_IMAGE_MODE.OVER:
                    bindTextureId = (_textureOver > 0) ? _textureOver : _texture;
                    break;
                case MOUSE_IMAGE_MODE.NORMAL:
                    bindTextureId = _texture;
                    break;
            };

            //uiShader.LoadColor(new Vertex4f(1, 1, 1, _alpha));
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, (uint)bindTextureId);
            uiShader.LoadHorizonFlip(_horzFlip);
            uiShader.LoadEnableTexture((bindTextureId > 0));
            uiShader.LoadTexcoordModelTransform(0, 0, 1, 1);
            uiShader.LoadColor(new Vertex4f(_backColor.x, _backColor.y, _backColor.z, _alpha));

            uiShader.SetView(renderingPosition1.x, renderingPosition1.y, renderingWidth, renderingHeight);
            uiShader.LoadModelMatrix();

            Gl.BindVertexArray(uiShader.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, uiShader.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.DisableVertexAttribArray(1);
            Gl.BindVertexArray(0);

            uiShader.Unbind();
            Gl.Disable(EnableCap.Blend);

            // traversal childs.
            foreach (Control ctrl in _controls) ctrl.Render(uiShader, fontRenderer);
        }

        public override void Shutdown()
        {
            throw new System.NotImplementedException();
        }
    }
}
