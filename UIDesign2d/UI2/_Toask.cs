using OpenGL;
using System.Timers;

namespace UIDesign2d
{
    public class Toask
    {
        public enum TOASK_TIME { LONG = 3000, MEDIUM = 2000, SHORT = 1000 }

        static Label _label;

        public static void Show(string msg, TOASK_TIME time = TOASK_TIME.MEDIUM)
        {
            if (_label == null) return;

            _label.IsVisible = true;
            _label.Text = msg;
            _label.Update();

            Timer _timer = new Timer();
            _timer.Interval = (int)time;
            _timer.Elapsed += (o, e) =>
            {
                _label.IsVisible = false;
                _timer.Stop();
            };

            _timer.Start();
        }

        public static void Init(Control root, FontFamily fontFamily)
        {
            _label = new Label("toask", fontFamily)
            {
                ForeColor = new Vertex3f(1.0f, 1.0f, 1.0f),
                BackColor = new Vertex3f(0.1f, 0.1f, 0.1f),
                BorderColor = new Vertex3f(1.0f, 1.0f, 1.0f),
                Text = "toask message!",
                FontSize = 1.0f,
                Padding = 0.2f,
                Margin = 0.1f,
                IsTextCenter = true,
                Align = Control.CONTROL_ALIGN.BC,
                Alpha = 0.7f,
                BorderWidth = 1.0f,
                IsBorder = true,
                LineWidthMin = 0.01f,
                LineWidthMax = 0.7f,
                IsVisible = false,
            };

            _label.Init();
            root.AddChild(_label);
        }

        public static void Update(int deltaTime)
        {
            if (_label.IsVisible)
                _label.Update(deltaTime);
        }

        public static void Render(UIShader uiShader, FontRenderer fontRenderer)
        {
            if (_label.IsVisible)
                _label.Render(uiShader, fontRenderer);
        }
    }
}
