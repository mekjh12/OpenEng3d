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

        /// <summary>
        /// 로딩을 모두 마치고 초기화가 끝났을 때 호출됩니다.
        /// </summary>
        void Start();
        void KeyDownEvent(object sender, KeyEventArgs e);
        void KeyUpEvent(object sender, KeyEventArgs e);
        void MouseDnEvent(object sender, MouseEventArgs e);
        void MouseUpEvent(object sender, MouseEventArgs e);
    }
}
