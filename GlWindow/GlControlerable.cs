using Common.Abstractions;
using OpenGL;
using System;
using System.Windows.Forms;

namespace GlWindow
{
    public interface GlControlerable
    {
        void Init(int width, int height);
        void Init2d(int width, int height);
        void Init3d(int width, int height);
        void UpdateFrame(int deltaTime, int width, int height, Camera camera);
        void RenderFrame(double deltaTime, Vertex4f backcolor, Camera camera);
        void Form_Load(object sender, EventArgs e);
        //void Form_Resize(object sender, EventArgs e);
        void KeyDownEvent(object sender, KeyEventArgs e);
        void KeyUpEvent(object sender, KeyEventArgs e);
        void MouseDnEvent(object sender, MouseEventArgs e);
        void MouseUpEvent(object sender, MouseEventArgs e);
    }
}
