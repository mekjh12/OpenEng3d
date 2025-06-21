using OpenGL;

namespace UIDesign2d
{
    class CForm: Panel
    {
        protected Button _buttonClose;
        protected Label _titleLabel;
        protected Panel _sizableButton;
        
        protected string _title = "Form1";
        //private G3Engine _g3Engine;


        protected Vertex3f _colorTitleBackground = new Vertex3f(0.8f, 0.8f, 0.8f);
        protected Vertex3f _colorActive = new Vertex3f(0.8f, 0.5f, 0.5f);

        public Control TitleBar => _titleLabel;

        public Vertex3f TitleBackColor
        {
            get => _colorTitleBackground;
            set => _colorTitleBackground = value;
        }

        public string Title
        {
            get => _title;
            set => _title = value;
        }

        #region 속성

        public bool IsVisibleCloseButton
        {
            set => _buttonClose.IsVisible = value;
        }

        #endregion

        public CForm(string name, FontFamily fontFamily) : base(name)
        {
            // _titleLabel ==> _buttonClose
            // -----------------------------------
            _isVisible = false;
            _name = name;
            _width = 0.6f;
            _height = 0.6f;
            _backColor = new Vertex3f(0.9f, 0.9f, 0.9f);
            _borderColor = _backColor * 0.3f;
            _isBorderd = true;

            _sizableButton = new Panel($"{_name}_sizable")
            {
                Width = 0.03f,
                Height = 0.03f,
                Align = CONTROL_ALIGN.BR,
                IsBorder = false,
                Margin = 0.0f,
                Padding = 0.1f,
                FontSize = _fontSize,
                BackColor = new Vertex3f(1, 0, 0),
                ForeColor = Vertex3f.Zero,
            };
            _sizableButton.DragDrop += (obj, x, y, dx, dy) =>
            {
                Width += dx;
                Height += dy;                
            };
            AddChild(_sizableButton);



            // TitleBar
            _titleLabel = new Label($"{_name}_title", fontFamily)
            {
                Width = 1.0f,
                Align = CONTROL_ALIGN.TL,
                IsBorder = false,
                Margin = 0.0f,
                LineWidthMax = 1.0f,
                LineWidthMin = 1.0f,
                Padding = 0.1f,
                Text = _title,
                FontSize = _fontSize,
                IsTextCenter = false,
                BackColor = _colorTitleBackground,
                ForeColor = Vertex3f.Zero,
            };
            AddChild(_titleLabel);

            _titleLabel.MouseDown += (o, x, y) =>
            {
                _titleLabel.Parent.PushTopest();
            };

            _titleLabel.MouseUp += (o, x, y) =>
            {

            };
            
            _titleLabel.DragDrop += (obj, x, y, dx, dy) =>
            {
                float ox = (obj.RenderingPosition1.x - x);
                float oy = (obj.RenderingPosition1.y - y);
                obj.Parent.Align = CONTROL_ALIGN.NONE;
                obj.Parent.Left = x;
                obj.Parent.Top = y;
                obj.Align = CONTROL_ALIGN.NONE;
                Position += new Vertex2f(dx + ox, dy + oy);
            };

            // Window Title Close Button
            _buttonClose = new Button($"{name}_btnclose", fontFamily)
            {
                Align = CONTROL_ALIGN.TR,
                Padding = 0.07f,
                FontSize = _fontSize,
                Margin = 0.0f,
                IsTextCenter = true,
                BackColor = _colorActive,
                Text = "X",
                MouseUp = (obj, x, y) =>
                {
                    this.Hide();
                    //_g3Engine.EngineLoop.ChangeMode(EngineLoop.MAINLOOP_MODE.G3ENGINE);
                    //_g3Engine.Start();
                },
            };
            _titleLabel.AddChild(_buttonClose);
        }

        public override void Update(int deltaTime)
        {
            _titleLabel.Text = _title;
            base.Update(deltaTime);
        }

        public void Hide()
        {
            _isVisible = false;
            _isRenderable = false;
        }

        public virtual void ShowDialog(bool isCenter = false) //G3Engine g3Engine, 
        {
            //Picker2d.SelectedControl = this;
            //_g3Engine = g3Engine;
            _isVisible = true;
            _isRenderable = true;

            PushTopest();

            if (isCenter)
            {
                this.Location = new Vertex2f((Parent.Width - _width) * 0.5f,
                    (Parent.Height - _height) * 0.5f);
            }

            Start();
        }

    }
}
