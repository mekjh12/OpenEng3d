using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 체력바
    /// 현재 체력과 최대 체력에 따라 자동으로 표시됩니다.
    /// </summary>
    public class HealthBar : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TEXTURE_WIDTH = 256;
        private const int TEXTURE_HEIGHT = 32;

        // 바 여백 및 크기
        private const float BAR_PADDING = 4f;
        private const float BAR_BORDER_WIDTH = 2f;

        // 기본 색상
        private static readonly Color DEFAULT_HP_COLOR = Color.FromArgb(255, 0, 200, 0);
        private static readonly Color DEFAULT_LOST_HP_COLOR = Color.FromArgb(255, 100, 100, 100);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(255, 30, 30, 30);
        private static readonly Color DEFAULT_BORDER_COLOR = Color.FromArgb(255, 200, 200, 200);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 0.3f;
        private const float DEFAULT_HEIGHT = 0.03f;
        private const float DEFAULT_Z_OFFSET = 0.15f;

        private float _currentHP;
        private float _maxHP;
        private Color _hpColor;
        private Color _lostHPColor;
        private Color _backgroundColor;
        private Color _borderColor;
        private bool _showBorder = true;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;

        /// <summary>현재 체력</summary>
        public float CurrentHP
        {
            get => _currentHP;
            set
            {
                float clampedValue = Math.Max(0, Math.Min(_maxHP, value));
                if (Math.Abs(_currentHP - clampedValue) > 0.001f)
                {
                    _currentHP = clampedValue;
                    _isDirty = true;
                }
            }
        }

        /// <summary>최대 체력</summary>
        public float MaxHP
        {
            get => _maxHP;
            set
            {
                if (_maxHP != value && value > 0)
                {
                    _maxHP = value;
                    _currentHP = Math.Min(_currentHP, _maxHP);
                    _isDirty = true;
                }
            }
        }

        /// <summary>체력바 색상</summary>
        public Color HPColor
        {
            get => _hpColor;
            set
            {
                if (_hpColor != value)
                {
                    _hpColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>손실된 체력 색상</summary>
        public Color LostHPColor
        {
            get => _lostHPColor;
            set
            {
                if (_lostHPColor != value)
                {
                    _lostHPColor = value;
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

        /// <summary>테두리 색상</summary>
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>테두리 표시 여부</summary>
        public bool ShowBorder
        {
            get => _showBorder;
            set
            {
                if (_showBorder != value)
                {
                    _showBorder = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>체력 비율 (0.0 ~ 1.0)</summary>
        public float HPRatio => _maxHP > 0 ? _currentHP / _maxHP : 0f;

        /// <summary>
        /// 체력바를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="maxHP">최대 체력</param>
        /// <param name="currentHP">현재 체력 (기본값: 최대 체력)</param>
        public HealthBar(Camera camera, float maxHP, float currentHP = -1)
            : base(camera)
        {
            _maxHP = maxHP;
            _currentHP = currentHP < 0 ? maxHP : Math.Min(currentHP, maxHP);
            _hpColor = DEFAULT_HP_COLOR;
            _lostHPColor = DEFAULT_LOST_HP_COLOR;
            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
            _borderColor = DEFAULT_BORDER_COLOR;
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        /// <summary>
        /// 체력바를 즉시 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 체력을 설정합니다.
        /// </summary>
        /// <param name="currentHP">현재 체력</param>
        /// <param name="maxHP">최대 체력 (선택적)</param>
        public void SetHP(float currentHP, float? maxHP = null)
        {
            if (maxHP.HasValue && maxHP.Value > 0)
            {
                _maxHP = maxHP.Value;
            }
            CurrentHP = currentHP;
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 배경 클리어
            _reusableGraphics.Clear(_backgroundColor);

            // 바 영역 계산
            float barX = BAR_PADDING;
            float barY = BAR_PADDING;
            float barWidth = TEXTURE_WIDTH - BAR_PADDING * 2;
            float barHeight = TEXTURE_HEIGHT - BAR_PADDING * 2;

            // 테두리 그리기
            if (_showBorder)
            {
                using (Pen borderPen = new Pen(_borderColor, BAR_BORDER_WIDTH))
                {
                    _reusableGraphics.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);
                }
                barX += BAR_BORDER_WIDTH;
                barY += BAR_BORDER_WIDTH;
                barWidth -= BAR_BORDER_WIDTH * 2;
                barHeight -= BAR_BORDER_WIDTH * 2;
            }

            // 손실된 체력 배경 (회색 바)
            using (SolidBrush lostHPBrush = new SolidBrush(_lostHPColor))
            {
                _reusableGraphics.FillRectangle(lostHPBrush, barX, barY, barWidth, barHeight);
            }

            // 현재 체력 바
            float hpRatio = HPRatio;
            if (hpRatio > 0)
            {
                float hpBarWidth = barWidth * hpRatio;
                using (SolidBrush hpBrush = new SolidBrush(_hpColor))
                {
                    _reusableGraphics.FillRectangle(hpBrush, barX, barY, hpBarWidth, barHeight);
                }
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
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