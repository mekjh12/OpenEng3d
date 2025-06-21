using OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameEngine
{
    /// <summary>
    /// 게임 엔진의 핵심 기능을 담당하는 클래스
    /// </summary>
    public class GameEngine
    {
        private Window _window;
        private bool _isRunning = true;
        private Stopwatch _gameTimer = new Stopwatch();
        private int _frameCount = 0;
        private float _fpsTimer = 0;
        private const float FPS_UPDATE_INTERVAL = 0.2f;

        private Dictionary<KeyCode, bool> _previousKeyStates;
        private Dictionary<KeyCode, bool> _currentKeyStates;

        /// <summary>
        /// 키보드 입력을 위한 가상 키코드 정의
        /// </summary>
        public enum KeyCode
        {
            W = 0x57,
            S = 0x53,
            A = 0x41,
            D = 0x44,
            Space = 0x20,
            Escape = 0x1B,
            Enter = 0x0D
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);


        public GameEngine()
        {
            _previousKeyStates = new Dictionary<KeyCode, bool>();
            _currentKeyStates = new Dictionary<KeyCode, bool>();
        }

        /// <summary>
        /// 현재 키가 눌려있는지 확인
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>키가 눌려있으면 true</returns>
        private bool IsKeyDown(KeyCode key)
        {
            return GetAsyncKeyState((int)key) < 0;
        }

        /// <summary>
        /// 키가 이번 프레임에 처음 눌렸는지 확인
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>키가 처음 눌렸으면 true</returns>
        private bool IsKeyPressed(KeyCode key)
        {
            if (!_previousKeyStates.ContainsKey(key))
                _previousKeyStates[key] = false;
            if (!_currentKeyStates.ContainsKey(key))
                _currentKeyStates[key] = false;

            return _currentKeyStates[key] && !_previousKeyStates[key];
        }

        /// <summary>
        /// 키가 이번 프레임에 떼어졌는지 확인
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>키가 떼어졌으면 true</returns>
        private bool IsKeyReleased(KeyCode key)
        {
            if (!_previousKeyStates.ContainsKey(key))
                _previousKeyStates[key] = false;
            if (!_currentKeyStates.ContainsKey(key))
                _currentKeyStates[key] = false;

            return !_currentKeyStates[key] && _previousKeyStates[key];
        }

        /// <summary>
        /// 키보드 상태 업데이트
        /// </summary>
        private void UpdateKeyStates()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!_previousKeyStates.ContainsKey(key))
                    _previousKeyStates[key] = false;

                _previousKeyStates[key] = _currentKeyStates.ContainsKey(key) ? _currentKeyStates[key] : false;
                _currentKeyStates[key] = IsKeyDown(key);
            }
        }

        /// <summary>
        /// 게임 엔진 실행
        /// </summary>
        public void RunEngine()
        {
            _window = new Window();
            if (!_window.CreateWindow(1280, 768, "OpenGL Game Engine"))
            {
                Console.WriteLine("윈도우 생성 실패");
                return;
            }

            try
            {
                InitializeOpenGL();
                RunGameLoop();
            }
            finally
            {
                _window.Cleanup();
            }
        }

        /// <summary>
        /// OpenGL 초기화
        /// </summary>
        private void InitializeOpenGL()
        {
            Gl.Viewport(0, 0, 800, 600);
            Gl.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// 메인 게임 루프
        /// </summary>
        private void RunGameLoop()
        {
            _gameTimer.Start();
            long lastFrameTime = _gameTimer.ElapsedMilliseconds;

            while (_isRunning)
            {
                if (!_window.ProcessMessages())
                {
                    _isRunning = false;
                    break;
                }

                long currentTime = _gameTimer.ElapsedMilliseconds;
                float deltaTime = (currentTime - lastFrameTime) / 1000f;
                lastFrameTime = currentTime;

                Update(deltaTime);
                Render();
                UpdateFPS(deltaTime);
            }
        }

        /// <summary>
        /// FPS 업데이트 및 출력
        /// </summary>
        private void UpdateFPS(float deltaTime)
        {
            _frameCount++;
            _fpsTimer += deltaTime;
            if (_fpsTimer >= FPS_UPDATE_INTERVAL)
            {
                float currentFPS = _frameCount / _fpsTimer;
                Console.WriteLine($"FPS: {currentFPS:F1}");
                _frameCount = 0;
                _fpsTimer = 0;
            }
        }

        /// <summary>
        /// 무거운 작업 비동기 실행
        /// </summary>
        private async Task DoHeavyWorkAsync()
        {
            Console.WriteLine("무거운 작업 시작!");
            await Task.Run(() => Thread.Sleep(5000));
            Console.WriteLine("무거운 작업 완료!");
        }

        /// <summary>
        /// 게임 로직 업데이트
        /// </summary>
        private void Update(float deltaTime)
        {
            UpdateKeyStates();

            if (IsKeyPressed(KeyCode.Space))
            {
                _ = DoHeavyWorkAsync();
            }

            if (IsKeyDown(KeyCode.Escape))
            {
                Shutdown();
            }
        }

        /// <summary>
        /// 화면 렌더링
        /// </summary>
        private void Render()
        {
            try
            {
                Gl.ClearColor(0.9f, 0.3f, 0.3f, 1.0f);
                Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);







                if (!Wgl.SwapLayerBuffers(_window.DeviceContext, Wgl.SWAP_MAIN_PLANE))
                {
                    Console.WriteLine("버퍼 스왑 실패");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"렌더링 에러: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 엔진 종료
        /// </summary>
        private void Shutdown()
        {
            _isRunning = false;
            Console.WriteLine("게임 엔진을 종료합니다...");
            _window?.Cleanup();
            Environment.Exit(0);
        }

        /// <summary>
        /// 게임 엔진 중지
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }
    }
}