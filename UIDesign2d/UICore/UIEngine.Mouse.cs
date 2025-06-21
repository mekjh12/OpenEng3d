using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ui2d
{
    partial class UIEngine
    {
        // 마우스 구현을 위한 변수
        float _mx = 0.5f;
        float _my = 0.5f;

        public static float MouseAspect = 1.0f;

        bool _isVisibleMouse = true;
        
        static PictureBox _mouseImage;

        static string _mouseImageFileName = "";

        private static Vertex2f _prevMousePosition;

        public static readonly float MOUSE_ORGINAL_WIDTH = 0.01f;
        public static readonly float MOUSE_ORGINAL_HEIGHT = 0.025f;
        public static readonly string MOUSE_IMAGE = "cursor.png";
        public static readonly string MOUSE_OVER_IMAGE = "cursor_over.png";

        public static void MouseImageOver()
        {
            UIEngine.MouseImageFileName = UIEngine.REOURCES_PATH + UIEngine.MOUSE_OVER_IMAGE;
            UIEngine.MouseImage.Width = 0.17f;
            UIEngine.MouseImage.Height = 0.4f;
        }

        public static void MouseImageOut()
        {
            UIEngine.MouseImageFileName = UIEngine.REOURCES_PATH + UIEngine.MOUSE_IMAGE;
            UIEngine.MouseImage.Width = UIEngine.MOUSE_ORGINAL_WIDTH;
            UIEngine.MouseImage.Height = UIEngine.MOUSE_ORGINAL_HEIGHT;
        }

        public static string MouseImageFileName
        {
            get => _mouseImageFileName;
            set
            {
                _mouseImageFileName = value;
                string name = Path.GetFileNameWithoutExtension(value);
                _mouseImage.BackgroundImage = UITextureLoader.ContainTexture(name) ? UITextureLoader.Texture(name)
                    : UITextureLoader.AddOneFile(_mouseImageFileName);
            }
        }

        public static PictureBox MouseImage
        {
            get => _mouseImage;
            set => _mouseImage = value;
        }



        public bool VisibleMouse
        {
            get => _isVisibleMouse;
            set => _isVisibleMouse = value;
        }

        public float MouseX
        {
            get => _mx;
            set => _mx = value;
        }

        public float MouseY
        {
            get => _my;
            set => _my = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ox">창의 전체화면에서의 왼쪽상단의 X 절대좌표</param>
        /// <param name="oy">창의 전체화면에서의 왼쪽상단의 Y 절대좌표</param>
        /// <param name="width">창의 전체화면에서의 절대너비</param>
        /// <param name="height">창의 전체화면에서의 절대높이</param>
        /// <param name="mouseWheelValue">마우스휠의 변화된 값</param>
        public static void MouseUpdateFrame(int ox, int oy, int width, int height, float mouseWheelValue)
        {
            UIEngine.GetCursorPos(out UIEngine.POINT point);
            int mx = point.X;
            int my = point.Y;

            float fx = (float)(mx - ox) / (float)width;
            float fy = (float)(my - oy) / (float)height;
            UIEngine.CurrentMousePointFloat = new Vertex2f(fx, fy);

            Vertex2f currentPoint = new Vertex2f(fx, fy);
            Vertex2f delta = currentPoint - _prevMousePosition;
            
            float dx = (float)delta.x;
            float dy = (float)delta.y;

            bool Lbutton = UIEngine.GetAsyncKeyState((int)UIEngine.VKeys.VK_LBUTTON) != 0;

            foreach (UIEngine uIEngine in UIEngine.UIEngineList)
            {
                if (uIEngine.Enable && uIEngine.Visible)
                {
                    uIEngine.UpdateInput(Lbutton, mx, my, fx, fy, dx, dy, mouseWheelValue);
                }
            }

            _prevMousePosition = currentPoint;
        }
    }
}
