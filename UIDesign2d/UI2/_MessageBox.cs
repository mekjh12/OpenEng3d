using OpenGL;
using System.Windows.Forms;

namespace UIDesign2d
{
    class MessageBox : CForm
    {
        private Label _lblContent;
        private Button _btnOK;
        private Button _btnCancel;
        private string _questionText;

        public MessageBox(UIEngine uIEngine, FontFamily fontFamily, string name, string questionText, string title = "") : base(name, fontFamily)
        {
            _width = 0.3f;
            _height = 0.15f;
            _buttonClose.IsVisible = true;
            _title = title;

            _questionText = questionText;
            _lblContent = new Label("lblcontent", fontFamily)
            {
                Align = CONTROL_ALIGN.MC,
                Margin = 0.05f,
                Alpha = 0.0f,
                Text = questionText,
                ForeColor = Vertex3f.Zero,
                IsTextCenter = true,
                FontSize = 1.0f,
            };
            AddChild(_lblContent);

            // OK-Button
            _btnOK = new Button("btnOK", fontFamily)
            {
                Text = "OK[Space]",
                LineWidthMin = 0.3f,
                Margin = 0.05f,
                Padding = 0.1f,
                FontSize = 0.9f,
                Align = CONTROL_ALIGN.HALF_VERTICAL_LEFT,
                Top = 0.7f,
                IsTextCenter = true,
                MouseDown = (obj, x, y) =>
                {
                    Application.Exit();
                },
            };
            AddChild(_btnOK);

            // Cancel-Button
            _btnCancel = new Button("btnCancel", fontFamily)
            {
                Text = "Cancel[Esc]",
                LineWidthMin = 0.3f,
                Margin = 0.05f,
                Padding = 0.1f,
                FontSize = 0.9f,
                Align = CONTROL_ALIGN.HALF_VERTICAL_RIGHT,
                Top = 0.7f,
                IsTextCenter = true,
                MouseDown = (obj, x, y) =>
                {
                    //uIEngine.EngineLoop.ChangeMode(EngineLoop.MAINLOOP_MODE.G3ENGINE);
                    this.Hide();
                    //uIEngine.EngineLoop.G3Engine.Start();
                },
            };
            AddChild(_btnCancel);

            KeyDown += (obj) =>
            {
                if (KeyBoard.IsKeyPress(System.Windows.Input.Key.Escape))
                    _btnCancel.MouseDown(_btnCancel, 0, 0);
                if (KeyBoard.IsKeyPress(System.Windows.Input.Key.Space))
                    _btnOK.MouseDown(_btnOK, 0, 0);
            };
        }

        public override void ShowDialog(bool isCenter = false) //G3Engine g3Engine,
        {
            base.ShowDialog(isCenter); //g3Engine, 
            Picker2d.SelectedControl = this;
        }


        public override void Update(int deltaTime)
        {
            _lblContent.Text = _questionText;
            base.Update(deltaTime);
        }

    }
}
