using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Camera3d;
using Ui2d;
using Shader;

namespace GlWindow
{
    public partial class GlControl3
    {
        // ==================================================================================
        //                              렌더링 메서드
        // ==================================================================================

        /// <summary>
        /// 3D 씬 렌더링
        /// </summary>
        public void Render3d(int deltaTime)
        {
            if (_useRenderTarget)
            {
                // 렌더 타겟에 3D 씬 렌더링
                _gbuffer.Bind();
                {
                    Gl.Enable(EnableCap.CullFace);
                    Gl.CullFace(CullFaceMode.Back);
                    Gl.ClearColor(_backColor.x, _backColor.y, _backColor.z, 1.0f);
                    Gl.Enable(EnableCap.DepthTest);
                    Gl.Viewport(0, 0, Width, Height);
                    Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
                    Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    // 3D 렌더링
                    if (_render != null)
                    {
                        _render(deltaTime, Width, Height, _backColor, _camera);
                    }

                    // 그리드 렌더링
                    if (_isVisibleGrid)
                    {
                        _grid?.Render(_camera);
                    }
                }
                _gbuffer.Unbind();

                // 자동 화면 복사 옵션
                if (_autoBlitToScreen)
                {
                    Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    Gl.Viewport(0, 0, Width, Height);
                    _gbuffer.BlitToScreen(Width, Height);
                }
            }
            else
            {
                // 기본 렌더링
                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(CullFaceMode.Back);
                Gl.ClearColor(_backColor.x, _backColor.y, _backColor.z, 1.0f);
                Gl.Enable(EnableCap.DepthTest);
                Gl.Viewport(0, 0, Width, Height);
                Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

                if (_render != null)
                {
                    _render(deltaTime, Width, Height, _backColor, _camera);
                }

                if (_isVisibleGrid)
                {
                    _grid?.Render(_camera);
                }
            }

            // UI 렌더링
            if (IsVisibleUi2d)
            {
                Gl.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
                UIEngine.RenderFrame(deltaTime);
            }
        }

        /// <summary>
        /// 외부에서 BlitToScreen 호출할 수 있도록
        /// </summary>
        public void BlitRenderTargetToScreen()
        {
            if (_useRenderTarget && _gbuffer != null)
            {
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Gl.Viewport(0, 0, Width, Height);
                _gbuffer.BlitToScreen(Width, Height);
            }
        }

        public void BlitDebugView(RenderDepthBufferShader shader)
        {
            if (_useRenderTarget && _gbuffer != null)
            {
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Gl.Viewport(0, 0, Width, Height);
                _gbuffer.BlitDebugView(Width, Height, shader);
            }
        }

        /// <summary>
        /// 화면 캡처 후 저장
        /// </summary>
        public void CaptureScreen()
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            int size = _width * _height * 4;
            pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

            // 렌더 타겟 프레임버퍼 바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 픽셀 데이터 읽기
            Gl.ReadPixels(
                0, 0,                          // x, y 좌표
                _width,                        // 너비
                _height,                       // 높이
                PixelFormat.Rgba,             // 픽셀 포맷
                PixelType.UnsignedByte,       // 데이터 타입
                pixelsPtr                      // 저장할 메모리 위치
            );

            // 픽셀 데이터를 관리되는 배열로 복사
            byte[] pixels = new byte[size];
            Marshal.Copy(pixelsPtr, pixels, 0, size);

            // Bitmap 생성
            Bitmap bitmap = new Bitmap((int)_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Bitmap 데이터를 직접 조작하기 위해 락
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* bitmapPtr = (byte*)bitmapData.Scan0;
                    int stride = bitmapData.Stride;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            // OpenGL 데이터는 아래에서 위로 저장되어 있으므로 y좌표를 뒤집어서 읽음
                            int srcIndex = (((_height - 1 - y) * _width) + x) * 4;
                            int dstIndex = (y * stride) + (x * 4);

                            // RGBA를 BGRA로 변환 (GDI+의 Format32bppArgb는 BGRA 형식임)
                            bitmapPtr[dstIndex + 0] = pixels[srcIndex + 2]; // B
                            bitmapPtr[dstIndex + 1] = pixels[srcIndex + 1]; // G
                            bitmapPtr[dstIndex + 2] = pixels[srcIndex + 0]; // R
                            bitmapPtr[dstIndex + 3] = pixels[srcIndex + 3]; // A
                        }
                    }
                }
            }
            finally
            {
                // 비트맵 언락
                bitmap.UnlockBits(bitmapData);

                bitmap = ResizeImage(bitmap, _width, _height);
                bitmap.Save(@"C:\Users\mekjh\OneDrive\바탕 화면\a.png");
            }
        }

        /// <summary>
        /// 이미지 리사이즈 (고품질 보간)
        /// </summary>
        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, 0, 0, width, height);
            }
            return resized;
        }

        /// <summary>
        /// 디버그 창에 텍스트 출력
        /// </summary>
        public void WriteLine(string txt)
        {
            if (CLabel("debug") != null)
            {
                CLabel("debug").Text = txt + UI2.NewLine + CLabel("debug").Text;
            }
        }
    }
}