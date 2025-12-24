using System;
using System.Runtime.InteropServices;
using Ui2d;
using ZetaExt;

namespace GlWindow
{
    public partial class GlControl3
    {
        // ==================================================================================
        //                              DllImport - 마우스 관련
        // ==================================================================================

        [DllImport("user32.dll")] private static extern int ShowCursor(bool bShow);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] public static extern Int32 SetCursorPos(Int32 x, Int32 y);

        private struct POINT
        {
            public int x;
            public int Y;
        }

        // ==================================================================================
        //                              입력 처리 메서드
        // ==================================================================================

        /// <summary>
        /// 시스템 마우스 커서 표시 여부 설정
        /// </summary>
        public void ShowSystemMouse(bool bShow)
        {
            ShowCursor(bShow);
        }

        /// <summary>
        /// 마우스의 표시여부를 설정한다.
        /// </summary>
        public void SetVisibleMouse(bool isVisible)
        {
            _isMouseVisible = isVisible;
            UIEngine.EnableMouse = isVisible;
        }

        /// <summary>
        /// 윈도우 API로부터 마우스 위치 및 델타값 가져오기
        /// </summary>
        private void GetMouseInputFromWinAPI(int ox, int oy, int width, int height)
        {
            // 기존 객체 재사용 (새 객체 생성 없음)
            _windowOffSet.x = ox;
            _windowOffSet.y = oy;

            POINT point;
            GetCursorPos(out point);

            float fx = (float)(point.x - ox) / (float)width;
            float fy = (float)(point.Y - oy) / (float)height;

            // 기존 객체에 값만 설정 (GC 없음)
            _currentMousePointFloat.x = fx;
            _currentMousePointFloat.y = fy;

            // 연산자 오버로딩 대신 직접 계산 (GC 없음)
            float deltax = fx - _prevMousePosition.x;
            float deltaY = fy - _prevMousePosition.y;

            // 기존 객체에 값 설정 (GC 없음)
            _mouseDeltaPos.x = deltax;
            _mouseDeltaPos.y = deltaY;

            _mousePosition.x = point.x;
            _mousePosition.y = point.Y;
        }
    }
}