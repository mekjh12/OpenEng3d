using Camera3d;
using System.Windows.Input;
using Ui2d;

namespace GlWindow
{
    public partial class GlControl3
    {
        // ==================================================================================
        //                              업데이트 메서드
        // ==================================================================================

        /// <summary>
        /// 매 프레임 업데이트
        /// </summary>
        public void Update(int deltaTime)
        {
            if (_camera == null)
            {
                _camera = new OrbitCamera("OrbitCamera001", 0, 0, 0, 10);
            }

            if (_camera.Width * _camera.Height == 0)
            {
                _camera?.Init(Width, Height);
            }

            // 키보드 입력 처리
            CheckKeyBoardToDo();

            // 카메라 업데이트
            if (_isEnableCameraMove)
            {
                _camera?.Update(deltaTime);
            }

            // 사용자 정의 업데이트 함수 실행
            _update(deltaTime, _width, _height, _camera);

            // 그리드 업데이트
            if (_isVisibleGrid)
            {
                _grid?.Update(deltaTime);
            }
        }

        /// <summary>
        /// UI2D 업데이트
        /// </summary>
        private void Update2d(int deltaTime, float mouseWheelValue)
        {
            int glLeftMargin = Parent.Width - this.Width;
            int glTopMargin = Parent.Height - this.Height;

            // 디버깅 텍스트 업데이트
            if (_isVisibleDebug)
            {
                //CLabel("debug").Text = Debug.Text;
            }

            // 마우스 위치 업데이트
            UIEngine.MouseUpdateFrame(Parent.Left + glLeftMargin, Parent.Top + glTopMargin, Width, Height, mouseWheelValue);

            // UI2d 컨트롤 업데이트
            UIEngine.UpdateFrame(deltaTime);
        }

        /// <summary>
        /// 키보드 입력 검사 및 처리
        /// </summary>
        private void CheckKeyBoardToDo()
        {
            if (_camera is OrbitCamera)
            {
                OrbitCamera orbitCamera = (OrbitCamera)_camera;
                if (Keyboard.IsKeyDown(Key.Z)) orbitCamera.Distance = 900.0f;
                if (Keyboard.IsKeyDown(Key.X)) orbitCamera.Distance = 150.0f;
                if (Keyboard.IsKeyDown(Key.C)) orbitCamera.Distance = 10.0f;

                if (Keyboard.IsKeyDown(Key.W)) orbitCamera.GoForward(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.S)) orbitCamera.GoForward(-_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.D)) orbitCamera.GoRight(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.A)) orbitCamera.GoRight(-_cameraStepLength);

                if (Keyboard.IsKeyDown(Key.E)) orbitCamera.GoUp(_cameraStepLength);
                if (Keyboard.IsKeyDown(Key.Q)) orbitCamera.GoUp(-_cameraStepLength);
            }
        }
    }
}