using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillBoard
{
    public static class ImposterSaver
    {
        static ImposterSaver()
        {
        }


        /// <summary>
        /// 생성된 임포스터 노멀 아틀라스를 Bitmap 형태로 반환한다.
        /// </summary>
        /// <param name="settings">임포스터 설정</param>
        /// <param name="drawBorders">테두리 그리기 여부</param>
        /// <returns>임포스터 노멀 아틀라스 Bitmap</returns>
        public static Bitmap GetImpostorNormalTexture(RenderTarget2D atlasRenderTarget, ImpostorSettings settings, bool drawBorders = false)
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                // 텍스처 메모리 할당
                int size = settings.AtlasSize * settings.AtlasSize * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                // 현재 바인딩된 프레임버퍼 저장
                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // 렌더 타겟 프레임버퍼 바인딩
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, atlasRenderTarget.FrameBuffer);

                // ✅ ColorAttachment1(Normal)에서 읽기
                Gl.ReadBuffer(ReadBufferMode.ColorAttachment1);

                // 픽셀 데이터 읽기
                Gl.ReadPixels(
                    0, 0,
                    settings.AtlasSize,
                    settings.AtlasSize,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixelsPtr
                );

                // 이전 프레임버퍼로 복구
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                // 픽셀 데이터를 관리되는 배열로 복사
                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                // Bitmap 생성
                Bitmap bitmap = new Bitmap((int)settings.AtlasSize, settings.AtlasSize,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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

                        for (int y = 0; y < settings.AtlasSize; y++)
                        {
                            for (int x = 0; x < settings.AtlasSize; x++)
                            {
                                // OpenGL 데이터는 아래에서 위로 저장
                                int srcIndex = (((settings.AtlasSize - 1 - y) * settings.AtlasSize) + x) * 4;
                                int dstIndex = (y * stride) + (x * 4);

                                // RGBA를 BGRA로 변환
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
                    bitmap.UnlockBits(bitmapData);
                }

                // 테두리 그리기
                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Lime), 2))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            int horizontalCells = settings.AtlasSize / settings.IndividualSize;
                            int verticalCells = settings.AtlasSize / settings.IndividualSize;

                            for (int v = 0; v < verticalCells; v++)
                            {
                                for (int h = 0; h < horizontalCells; h++)
                                {
                                    int x = h * settings.IndividualSize;
                                    int y = v * settings.IndividualSize;

                                    g.DrawRectangle(pen,
                                        x + 1, y + 1,
                                        settings.IndividualSize - 3,
                                        settings.IndividualSize - 3);
                                }
                            }
                        }
                    }
                }

                return bitmap;
            }
            finally
            {
                if (pixelsPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelsPtr);
                }
            }
        }

        public static Bitmap GetImpostorDepthTexture(RenderTarget2D atlasRenderTarget, ImpostorSettings settings, bool drawBorders = false)
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                // ✅ RGBA 포맷으로 읽기 (fragDepth는 R 채널에 저장됨)
                int size = settings.AtlasSize * settings.AtlasSize * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, atlasRenderTarget.FrameBuffer);

                // ✅ ColorAttachment2(Depth)에서 읽기
                Gl.ReadBuffer(ReadBufferMode.ColorAttachment2);

                Gl.ReadPixels(
                    0, 0,
                    settings.AtlasSize,
                    settings.AtlasSize,
                    PixelFormat.Rgba,          // ✅ RGBA로 읽기
                    PixelType.UnsignedByte,    // ✅ byte로 읽기
                    pixelsPtr
                );

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                Bitmap bitmap = new Bitmap((int)settings.AtlasSize, settings.AtlasSize,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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

                        for (int y = 0; y < settings.AtlasSize; y++)
                        {
                            for (int x = 0; x < settings.AtlasSize; x++)
                            {
                                int srcIndex = (((settings.AtlasSize - 1 - y) * settings.AtlasSize) + x) * 4;
                                int dstIndex = (y * stride) + (x * 4);

                                // ✅ R 채널만 사용 (fragDepth 값)
                                byte depthByte = pixels[srcIndex + 0];  // R 채널

                                // 그레이스케일로 변환
                                bitmapPtr[dstIndex + 0] = depthByte; // B
                                bitmapPtr[dstIndex + 1] = depthByte; // G
                                bitmapPtr[dstIndex + 2] = depthByte; // R
                                bitmapPtr[dstIndex + 3] = 255;       // A
                            }
                        }
                    }

                    // 테두리 그리기
                    if (drawBorders)
                    {
                        unsafe
                        {
                            byte* bitmapPtr = (byte*)bitmapData.Scan0;
                            int stride = bitmapData.Stride;

                            int horizontalCells = settings.AtlasSize / settings.IndividualSize;
                            int verticalCells = settings.AtlasSize / settings.IndividualSize;

                            for (int v = 0; v < verticalCells; v++)
                            {
                                for (int h = 0; h < horizontalCells; h++)
                                {
                                    int startx = h * settings.IndividualSize;
                                    int startY = v * settings.IndividualSize;
                                    int endx = startx + settings.IndividualSize - 1;
                                    int endY = startY + settings.IndividualSize - 1;

                                    // ✅ 파란색 테두리 (Color, Normal과 구분)
                                    for (int x = startx; x <= endx; x++)
                                    {
                                        for (int offset = 0; offset < 2; offset++)
                                        {
                                            int y1 = startY + offset;
                                            int y2 = endY - offset;

                                            int idx1 = (y1 * stride) + (x * 4);
                                            int idx2 = (y2 * stride) + (x * 4);

                                            bitmapPtr[idx1 + 0] = 255;  // B ✅
                                            bitmapPtr[idx1 + 1] = 0;    // G
                                            bitmapPtr[idx1 + 2] = 0;    // R
                                            bitmapPtr[idx1 + 3] = 255;  // A

                                            bitmapPtr[idx2 + 0] = 255;  // B ✅
                                            bitmapPtr[idx2 + 1] = 0;    // G
                                            bitmapPtr[idx2 + 2] = 0;    // R
                                            bitmapPtr[idx2 + 3] = 255;  // A
                                        }
                                    }

                                    for (int y = startY; y <= endY; y++)
                                    {
                                        for (int offset = 0; offset < 2; offset++)
                                        {
                                            int x1 = startx + offset;
                                            int x2 = endx - offset;

                                            int idx1 = (y * stride) + (x1 * 4);
                                            int idx2 = (y * stride) + (x2 * 4);

                                            bitmapPtr[idx1 + 0] = 255;  // B ✅
                                            bitmapPtr[idx1 + 1] = 0;    // G
                                            bitmapPtr[idx1 + 2] = 0;    // R
                                            bitmapPtr[idx1 + 3] = 255;  // A

                                            bitmapPtr[idx2 + 0] = 255;  // B ✅
                                            bitmapPtr[idx2 + 1] = 0;    // G
                                            bitmapPtr[idx2 + 2] = 0;    // R
                                            bitmapPtr[idx2 + 3] = 255;  // A
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            finally
            {
                if (pixelsPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelsPtr);
                }
            }
        }


        /// <summary>
        /// 생성된 임포스터 아틀라스를 Bitmap 형태로 반환한다.
        /// 옵션에 따라 각 뷰의 경계에 테두리를 그릴 수 있다.
        /// </summary>
        /// <param name="drawBorders">테두리 그리기 여부</param>
        /// <returns>임포스터 아틀라스 Bitmap</returns>
        public static Bitmap GetImpostorTexture(RenderTarget2D atlasRenderTarget, ImpostorSettings settings, bool drawBorders = false)
        {
            // 픽셀 데이터를 저장할 포인터
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                // 텍스처 메모리 할당
                int size = settings.AtlasSize * settings.AtlasSize * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                // 현재 바인딩된 프레임버퍼 저장
                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // 렌더 타겟 프레임버퍼 바인딩
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, atlasRenderTarget.FrameBuffer);

                // 픽셀 데이터 읽기
                Gl.ReadPixels(
                    0, 0,                          // x, y 좌표
                    settings.AtlasSize,           // 너비
                    settings.AtlasSize,           // 높이
                    PixelFormat.Rgba,             // 픽셀 포맷
                    PixelType.UnsignedByte,       // 데이터 타입
                    pixelsPtr                     // 저장할 메모리 위치
                );

                // 이전 프레임버퍼로 복구
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                // 픽셀 데이터를 관리되는 배열로 복사
                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                // Bitmap 생성
                Bitmap bitmap = new Bitmap((int)settings.AtlasSize, settings.AtlasSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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

                        for (int y = 0; y < settings.AtlasSize; y++)
                        {
                            for (int x = 0; x < settings.AtlasSize; x++)
                            {
                                // OpenGL 데이터는 아래에서 위로 저장되어 있으므로 y좌표를 뒤집어서 읽음
                                int srcIndex = (((settings.AtlasSize - 1 - y) * settings.AtlasSize) + x) * 4;
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
                }

                // 현재 시각 표시
                /*
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    // 현재 시각 문자열 생성
                    string timeText = DateTime.Now.ToString("HH:mm:ss");

                    // 폰트 설정
                    using (Font font = new Font("Arial", 20, FontStyle.Regular))
                    using (Brush brush = new SolidBrush(Color.Red))
                    {
                        // 문자열 중앙 정렬을 위한 포맷 설정
                        StringFormat stringFormat = new StringFormat();
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Far;

                        // 하단 중앙에 텍스트 그리기
                        RectangleF textRect = new RectangleF(0, 0, bitmap.Width, bitmap.Height - 10);
                        g.DrawString(timeText, font, brush, textRect, stringFormat);
                    }
                }
                */

                // 테두리 그리기
                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 합성 모드를 SourceOver로 설정하여 기존 내용 위에 그리기
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Red))) // 완전 불투명한 빨간색
                        {
                            // 선 품질 설정
                            pen.Width = 2;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // 전체 영역을 IndividualSize로 나누어 모든 그리드에 테두리 그리기
                            int horizontalCells = settings.AtlasSize / settings.IndividualSize;
                            int verticalCells = settings.AtlasSize / settings.IndividualSize;

                            for (int v = 0; v < verticalCells; v++)
                            {
                                for (int h = 0; h < horizontalCells; h++)
                                {
                                    int x = h * settings.IndividualSize;
                                    int y = v * settings.IndividualSize;

                                    // 테두리 그리기 (1픽셀 안쪽으로)
                                    g.DrawRectangle(pen,
                                        x + 1, y + 1,
                                        settings.IndividualSize - 3,  // 양쪽 1픽셀씩 줄임
                                        settings.IndividualSize - 3); // 양쪽 1픽셀씩 줄임
                                }
                            }
                        }
                    }
                }

                return bitmap;
            }
            finally
            {
                // 할당된 메모리 해제
                if (pixelsPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelsPtr);
                }
            }
        }

    }
}
