using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 고정되어 표시되는 FPS 카운터
    /// 화면에 항상 표시되며 성능 모니터링에 사용됩니다.
    /// </summary>
    public class FPSDisplay : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TExTURE_WIDTH = 256;
        private const int TExTURE_HEIGHT = 64;

        // 패딩
        private const float PADDING = 0.0f;

        // 기본 색상
        private static readonly Color DEFAULT_TExT_COLOR = Color.FromArgb(255, 0, 255, 0);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(180, 0, 0, 0);
        private static readonly Color DEFAULT_WARNING_COLOR = Color.FromArgb(255, 255, 200, 0);
        private static readonly Color DEFAULT_CRITICAL_COLOR = Color.FromArgb(255, 255, 50, 50);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 1.5f;
        private const float DEFAULT_HEIGHT = 0.6f;

        // FPS 임계값
        private const float WARNING_FPS = 30f;
        private const float CRITICAL_FPS = 15f;

        // 폰트
        private const string DEFAULT_FONT_FAMILY = "Consolas";
        private static readonly Font _fpsFont = new Font(DEFAULT_FONT_FAMILY, 14, FontStyle.Bold);

        private float _currentFPS;
        private Color _textColor;
        private Color _backgroundColor;
        private bool _showBackground;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private int _frameCount;
        private int _elapsedTime;
        private int _updateInterval;

        /// <summary>현재 FPS 값</summary>
        public float CurrentFPS => _currentFPS;

        /// <summary>텍스트 색상</summary>
        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>배경 색상</summary>
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>배경 표시 여부</summary>
        public bool ShowBackground
        {
            get => _showBackground;
            set
            {
                if (_showBackground != value)
                {
                    _showBackground = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>FPS 업데이트 간격 (밀리초)</summary>
        public int UpdateInterval
        {
            get => _updateInterval;
            set => _updateInterval = Math.Max(100, value);
        }

        /// <summary>
        /// FPS 디스플레이를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="updateInterval">FPS 업데이트 간격 (밀리초, 기본 500ms)</param>
        public FPSDisplay(Camera camera, int updateInterval = 100)
            : base(camera)
        {
            _textColor = DEFAULT_TExT_COLOR;
            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
            _showBackground = true;
            _updateInterval = updateInterval;
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _currentFPS = 0f;
            _frameCount = 0;
            _elapsedTime = 0;

            // 카메라를 향하지 않고 고정
            _faceCamera = true;
            _scaleWithDistance = false;
            _fadeWithDistance = false;

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        /// <summary>
        /// 업데이트 (FPS 계산)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (!_isActive)
                return;

            _frameCount++;
            _elapsedTime += deltaTime;

            // 일정 간격마다 FPS 계산
            if (_elapsedTime >= _updateInterval)
            {
                _currentFPS = _frameCount * 1000f / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0;
                _isDirty = true;
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 배경 클리어
            if (_showBackground)
            {
                _reusableGraphics.Clear(_backgroundColor);
            }
            else
            {
                _reusableGraphics.Clear(Color.FromArgb(0, 0, 0, 0));
            }

            // FPS에 따른 색상 결정
            Color currentColor = _textColor;
            if (_currentFPS < CRITICAL_FPS)
            {
                currentColor = DEFAULT_CRITICAL_COLOR;
            }
            else if (_currentFPS < WARNING_FPS)
            {
                currentColor = DEFAULT_WARNING_COLOR;
            }

            // FPS 텍스트
            string fpsText = $"FPS: {_currentFPS:F1}";

            using (SolidBrush textBrush = new SolidBrush(currentColor))
            using (StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                _reusableGraphics.DrawString(fpsText, _fpsFont, textBrush,
                    new RectangleF(0, 0, TExTURE_WIDTH, TExTURE_HEIGHT), format);
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        /// <summary>
        /// FPS 카운터를 리셋합니다.
        /// </summary>
        public void Reset()
        {
            _frameCount = 0;
            _elapsedTime = 0;
            _currentFPS = 0f;
            _isDirty = true;
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public override void Dispose()
        {
            _reusableGraphics?.Dispose();
            _reusableBitmap?.Dispose();
            base.Dispose();
        }
    }
}