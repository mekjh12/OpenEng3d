using OpenGL;
using System.Collections.Generic;

namespace UIDesign2d
{
    class CForm2 : Panel
    {
        Button btnClose;
        float padding = 0.01f;

        public CForm2(string name, FontFamily font, ref List<Control> controls) : base(name)
        {
            this.Width = 0.5f;
            this.Height = 0.3f;
            this.BackColor = new Vertex3f(0.9f, 0.9f, 0.9f);
            this.IsVisible = false;
            this.btnClose = new Button("btnClose", font)
            {
                IsVisible = false,
                FontSize = 0.5f,
                Bound = new Vertex4f(0, 0, 0.1f, 0.1f),
                BackColor = Vertex3f.One,
                Text = "XXXXXXXXXXXxx",
                //ZIndex = -0.98f,
                //Image = EngineLoop.PROJECT_PATH + "\\UI2\\Res\\btnDefault.png"
            };
            controls.Add(this.btnClose);

        }

        public void ShowDialog()
        {
            this.IsVisible = true;
            this.btnClose.IsVisible = true;
            btnClose.Location = this.Location - new Vertex2f(btnClose.Width*0.5f, 0);
        }

    }
}
