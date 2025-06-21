using System;
using System.Runtime.InteropServices;
using OpenGL;

namespace GameEngine
{
    public class Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
           uint dwExStyle,
           string lpClassName,
           string lpWindowName,
           uint dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("gdi32.dll")]
        private static extern int ChoosePixelFormat(IntPtr hdc, [In] ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll")]
        private static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, [In] ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private IntPtr windowHandle;
        private IntPtr deviceContext;
        private IntPtr renderContext;

        public IntPtr DeviceContext => deviceContext;

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // 시스템 메트릭스 상수
        private const int SM_CXSCREEN = 0;  // 화면 가로 해상도
        private const int SM_CYSCREEN = 1;  // 화면 세로 해상도

        private const uint CS_HREDRAW = 0x0002;
        private const uint CS_VREDRAW = 0x0001;
        private const uint CS_OWNDC = 0x0020;
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_VISIBLE = 0x10000000;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const uint WM_DESTROY = 0x0002;
        private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        private const uint PFD_SUPPORT_OPENGL = 0x00000020;
        private const uint PFD_DOUBLEBUFFER = 0x00000001;
        private const byte PFD_TYPE_RGBA = 0;
        private const byte PFD_MAIN_PLANE = 0;
        public const uint PM_REMOVE = 0x0001;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_QUIT = 0x0012;

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProc wndProcDelegate;
        
        public bool CreateWindow(int width, int height, string title)
        {
            try
            {
                Gl.Initialize();

                wndProcDelegate = WndProcFunction;
                var wndClass = new WNDCLASSEX
                {
                    cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                    style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
                    lpfnWndProc = wndProcDelegate,
                    hInstance = GetModuleHandle(null),
                    lpszClassName = "OpenGLWindow"
                };

                RegisterClassEx(ref wndClass);

                // 화면 해상도 가져오기
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                // 중앙 위치 계산
                int x = (screenWidth - width) / 2;
                int y = (screenHeight - height) / 2;

                windowHandle = CreateWindowEx(
                    0,
                    "OpenGLWindow",
                    title,
                    WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                    x, y,  // CW_USEDEFAULT 대신 계산된 x, y 사용
                    width, height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    GetModuleHandle(null),
                    IntPtr.Zero);

                if (windowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("윈도우 생성 실패");
                    return false;
                }

                deviceContext = GetDC(windowHandle);
                if (deviceContext == IntPtr.Zero)
                {
                    Console.WriteLine("DC 생성 실패");
                    return false;
                }

                var pfd = new PIXELFORMATDESCRIPTOR
                {
                    nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                    nVersion = 1,
                    dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
                    iPixelType = PFD_TYPE_RGBA,
                    cColorBits = 32,
                    cDepthBits = 24,
                    cStencilBits = 8,
                    iLayerType = PFD_MAIN_PLANE
                };

                int pixelFormat = ChoosePixelFormat(deviceContext, ref pfd);
                if (pixelFormat == 0)
                {
                    Console.WriteLine("픽셀 포맷 선택 실패");
                    return false;
                }

                if (!SetPixelFormat(deviceContext, pixelFormat, ref pfd))
                {
                    Console.WriteLine("픽셀 포맷 설정 실패");
                    return false;
                }

                renderContext = Wgl.CreateContext(deviceContext);
                if (renderContext == IntPtr.Zero)
                {
                    Console.WriteLine("렌더링 컨텍스트 생성 실패");
                    return false;
                }

                if (!Wgl.MakeCurrent(deviceContext, renderContext))
                {
                    Console.WriteLine("렌더링 컨텍스트 활성화 실패");
                    return false;
                }

                try
                {
                    string version = Gl.GetString(StringName.Version);
                    string renderer = Gl.GetString(StringName.Renderer);
                    string vendor = Gl.GetString(StringName.Vendor);

                    if (version != null)
                        Console.WriteLine($"OpenGL 버전: {version}");
                    else
                        Console.WriteLine("OpenGL 버전 정보 가져오기 실패");

                    if (renderer != null)
                        Console.WriteLine($"GPU: {renderer}");
                    else
                        Console.WriteLine("GPU 정보 가져오기 실패");

                    if (vendor != null)
                        Console.WriteLine($"벤더: {vendor}");
                    else
                        Console.WriteLine("벤더 정보 가져오기 실패");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OpenGL 정보 가져오기 실패: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"윈도우 생성 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        private IntPtr WndProcFunction(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public bool ProcessMessages()
        {
            MSG msg;
            while (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
            {
                if (msg.message == WM_QUIT)
                    return false;

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            return true;
        }

        public void Cleanup()
        {
            if (renderContext != IntPtr.Zero)
            {
                Wgl.MakeCurrent(IntPtr.Zero, IntPtr.Zero);
                Wgl.DeleteContext(renderContext);
                renderContext = IntPtr.Zero;
            }

            if (deviceContext != IntPtr.Zero)
            {
                ReleaseDC(windowHandle, deviceContext);
                deviceContext = IntPtr.Zero;
            }

            if (windowHandle != IntPtr.Zero)
            {
                DestroyWindow(windowHandle);
                windowHandle = IntPtr.Zero;
            }
        }
    }
}
