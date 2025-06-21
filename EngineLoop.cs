using OpenGL;
using System;
using Camera3d;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Animate;
using static Animate.HumanAniModel;
using ZetaExt;
using System.Collections.Generic;

namespace OpenEng3d
{
    public partial class EngineLoop
    {
        /// <summary>
        /// 실행파일의 실행파일의 절대경로
        /// </summary>
        public static string EXECUTE_PATH;

        /// <summary>
        /// 프로젝트의 최상위 부모의 절대경로
        /// </summary>
        public static string PROJECT_PATH;

        private Camera _camera;

        private int _width;
        private int _height;

        private Action<int> _update;
        private Action<int> _render;
        private Action<int, int> _init;

        public static float cameraSpeed = 10.0f;


        ACTION prevAction = ACTION.IDLE;
        ACTION frameAction = ACTION.IDLE;
        Dictionary<Key, bool> _onPrevPressed = new Dictionary<Key, bool>();


        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        public int Width => _width;
        
        public int Height => _height;

        public Action<int, int> InitFrame
        {
            get => _init;
            set => _init = value;
        }

        public Action<int> UpdateFrame
        {
            get => _update;
            set => _update = value;
        }
        public Action<int> RenderFrame
        {
            get => _render;
            set => _render = value;
        }

        public EngineLoop()
        {
            EXECUTE_PATH = Application.StartupPath;
            EngineLoop.PROJECT_PATH = Application.StartupPath;
            EngineLoop.PROJECT_PATH = Directory.GetParent(EngineLoop.PROJECT_PATH).FullName;
            EngineLoop.PROJECT_PATH = Directory.GetParent(EngineLoop.PROJECT_PATH).FullName;

            _onPrevPressed[Key.W] = false;
            _onPrevPressed[Key.S] = false;
            _onPrevPressed[Key.D] = false;
            _onPrevPressed[Key.A] = false;
            _onPrevPressed[Key.Space] = false;
        }

        public void Init(int width, int height)
        {
            _width = width;
            _height = height;

            ShowCursor(false);
            Gl.Viewport(0, 0, _width, _height);

            if (_init != null) _init(_width, _height);
        }

        public void Update(int deltaTime, HumanAniModel humanAniModel = null)
        {
            if (_camera == null)
            {
                _camera = new OrbitCamera("fpsCam", -13, -1.5f, 3, 10);
                _camera.Init(_width, _height);
            }

            KeyCheck(deltaTime, humanAniModel);

            _camera.Update(deltaTime);

            if (_update != null) _update(deltaTime);

        }

        public void Render(int deltaTime)
        {
            if (_render != null) _render(deltaTime);
        }

        
        public void KeyCheck(int deltaTime, HumanAniModel humanAniModel)
        {
            float milliSecond = deltaTime * 0.001f;

            if (_camera is OrbitCamera)
            {
                OrbitCamera orbitCamera = (OrbitCamera)_camera;
                if (Keyboard.IsKeyDown(Key.Z)) orbitCamera.Distance = 900.0f;
                if (Keyboard.IsKeyDown(Key.X)) orbitCamera.Distance = 150.0f;
                if (Keyboard.IsKeyDown(Key.C)) orbitCamera.Distance = 10.0f;
            }

            KeyChk(Key.W, ACTION.SLOW_RUN);
            KeyChk(Key.D, ACTION.RIGHT_STRAFE_WALK);
            KeyChk(Key.A, ACTION.LEFT_STRAFE_WALK);
            KeyChk(Key.S, ACTION.WALK_BACK);
            KeyChk(Key.Space, ACTION.JUMP);

            if (humanAniModel != null)
            {
                if (prevAction != frameAction)
                {
                    humanAniModel.SetMotion(frameAction);
                    prevAction = frameAction;
                }

                if (!humanAniModel.Collider.Collided)
                {
                    switch (frameAction)
                    {
                        case ACTION.IDLE:
                            break;
                        case ACTION.WALK:
                            _camera.GoForward(milliSecond * cameraSpeed * 0.5f);
                            break;
                        case ACTION.RUN:
                            _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                            break;
                        case ACTION.SLOW_RUN:
                            _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                            break;
                        case ACTION.JUMP:
                            _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                            break;
                        case ACTION.WALK_BACK:
                            _camera.GoForward(-milliSecond * cameraSpeed * 0.3f);
                            break;
                        case ACTION.LEFT_STRAFE_WALK:
                            _camera.GoRight(-milliSecond * cameraSpeed * 0.5f);
                            break;
                        case ACTION.RIGHT_STRAFE_WALK:
                            _camera.GoRight(milliSecond * cameraSpeed * 0.5f);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    _camera.GoForward(-milliSecond * cameraSpeed * 0.3f);
                }
            }
            else
            {
                switch (frameAction)
                {
                    case ACTION.IDLE:
                        break;
                    case ACTION.WALK:
                        _camera.GoForward(milliSecond * cameraSpeed * 0.5f);
                        break;
                    case ACTION.RUN:
                        _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                        break;
                    case ACTION.SLOW_RUN:
                        _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                        break;
                    case ACTION.JUMP:
                        _camera.GoForward(milliSecond * cameraSpeed * 1.0f);
                        break;
                    case ACTION.WALK_BACK:
                        _camera.GoForward(-milliSecond * cameraSpeed * 0.3f);
                        break;
                    case ACTION.LEFT_STRAFE_WALK:
                        _camera.GoRight(-milliSecond * cameraSpeed * 0.5f);
                        break;
                    case ACTION.RIGHT_STRAFE_WALK:
                        _camera.GoRight(milliSecond * cameraSpeed * 0.5f);
                        break;
                    default:
                        break;
                }
            }

            void KeyChk(Key key, ACTION action)
            {
                bool IsKey = Keyboard.IsKeyDown(key);
                if (IsKey == true && _onPrevPressed[key] == false)
                {
                    frameAction = action;
                }
                if (IsKey == false && _onPrevPressed[key] == true)
                {
                    if (prevAction == frameAction)
                    {
                        frameAction = ACTION.IDLE;
                    }
                    else
                    {
                        frameAction = prevAction;
                    }
                }
                _onPrevPressed[key] = IsKey;
            }
        }

    }
}
