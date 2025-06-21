using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Ui2d
{
    /// <summary>
    /// # 선택 FrameBuffer 이용하여 컨트롤을 선택한다.<br/>
    /// ============== 구현 순서 =============<br/>
    /// 1. LoadBuffer: FrameBuffer를 해상도에 맞게 생성한다.<br/>
    /// 2. RenderSelectionBuffer: 버퍼에 고유ID를 가진 컨트롤을 렌더링한다.<br/>
    /// 3. PickUpControl: 버퍼에서 선택한 점의 컨트롤을 반환한다. <br/>
    /// ----------------------------------------------------- <br/>
    /// [주의] 해상도가 바뀌면 버퍼를 다시 해상도에 맞게 생성한다.
    /// </summary>
    public class Picker2d
    {
        private static FrameBuffer _frameBuffer;
        private static int _width = 0;
        private static int _height = 0;
        private static bool _isStarted = false;
        private static Control _selectedControl = null;

        public static FrameBuffer FrameBuffer
        {
            get => _frameBuffer;
            set => _frameBuffer = value;
        }

        public static Control SelectedControl
        {
            get => _selectedControl;
            set => _selectedControl = value;
        }

        /// <summary>
        /// 선택 버퍼를 해상도에 맞게 다시 생성한다.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        static void CreateBuffer(float width, float height)
        {
            _width = (int)width;
            _height = (int)height;
            _frameBuffer = new FrameBuffer(_width, _height);
            _frameBuffer.CreateBuffer();
            _isStarted = true;
        }

        /// <summary>
        /// 해당 픽셀의 색상을 가져와 guid를 복원하여 selectionObjectGuid에 기록한다.<br/>
        /// </summary>
        /// <param name="px"></param>
        /// <param name="py"></param>
        public static Control PickUpControl(UIEngine uIEngine, float x, float y)
        {
            if (_frameBuffer == null) return null;
            int px = (int)(x * _width).Clamp(0, _width);
            int py = (int)(y * _height).Clamp(0, _height);

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer.FrameBufferID);

            IntPtr buffer = Marshal.AllocHGlobal(_width * _height * 3);
            // GPU 공간(좌하~우상) --> 화면 공간(좌하-우하) 때문에 y값을 flip해야 한다.
            Gl.ReadPixels(px, _height - 1 - py, 1, 1, OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, buffer);
            byte red = 0, green = 0, blue = 0;
            unsafe
            {
                byte* ptr = (byte*)buffer.ToPointer();
                blue = *ptr++;
                green = *ptr++;
                red = *ptr++;
            }
            Marshal.FreeHGlobal(buffer); // Free HGlobal memory

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            //bitmap.Save(@"C:\Users\mekjh\Desktop\2d.bmp");

            uint selId = (uint)((red << 16) + (green << 8) + blue);
            foreach (Control ctrl in UIEngine.ControlList)
            {
                if (selId == ctrl.Guid)
                {
                    _selectedControl = ctrl;
                    return ctrl;
                }
            }
            _selectedControl = null;
            return null;
        }

        /// <summary>
        /// framebuffer에 entity의 guid를 색상으로 기록한다.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="root"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void RenderControlId(UIColorShader shader, FontRenderer fontRenderer, Control root, float width, float height)
        {
            // 만약 초기화가 되어 있지 않으면 초기화한다.
            if (!_isStarted) CreateBuffer(width, height);

            Picker2d.FrameBuffer.Bind();
            Picker2d.FrameBuffer.Viewport(_width, _height);
            Gl.ClearColor(0, 0, 0, 1);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // no-reversing depth-buffer test
            Gl.DepthFunc(DepthFunction.Always);
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);

            shader.Bind();
            root.Render(shader, fontRenderer);
            shader.Unbind();

            Gl.DepthFunc(DepthFunction.Lequal);

            Picker2d.FrameBuffer.Unbind();
        }

    }
}
