using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에서 위로 떠오르면서 사라지는 아이템 표시
    /// 아이템 획득 시 시각적 피드백을 제공합니다.
    /// </summary>
    public class FloatingItem : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TExTURE_WIDTH = 128;
        private const int TExTURE_HEIGHT = 128;

        // 기본 색상
        private static readonly Color DEFAULT_TExT_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_COUNT_COLOR = Color.FromArgb(255, 255, 220, 0);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(0, 0, 0, 0);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 0.35f;
        private const float DEFAULT_HEIGHT = 0.35f;

        // 애니메이션 설정
        private const float FLOAT_SPEED = 2.0f;
        private const float FLOAT_DISTANCE = 1.0f;
        private const int LIFETIME_MS = 1500;
        private const int FADE_START_MS = 600;

        // 폰트
        private const string DEFAULT_FONT_FAMILY = "Arial";
        private static readonly Font _countFont = new Font(DEFAULT_FONT_FAMILY, 14, FontStyle.Bold);

        private Bitmap _itemIcon;
        private string _itemName;
        private int _count;
        private Color _textColor;
        private Color _countColor;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private int _elapsedTime;
        private Vertex3f _startPosition;
        private bool _isComplete;

        /// <summary>아이템 아이콘 (비트맵)</summary>
        public Bitmap ItemIcon
        {
            get => _itemIcon;
            set
            {
                if (_itemIcon != value)
                {
                    _itemIcon = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>아이템 이름</summary>
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>획득 개수</summary>
        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = Math.Max(0, value);
                    _isDirty = true;
                }
            }
        }

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

        /// <summary>개수 텍스트 색상</summary>
        public Color CountColor
        {
            get => _countColor;
            set
            {
                if (_countColor != value)
                {
                    _countColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>애니메이션이 완료되었는지 여부</summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// 떠오르는 아이템을 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="itemIcon">아이템 아이콘 비트맵</param>
        /// <param name="itemName">아이템 이름</param>
        /// <param name="count">획득 개수</param>
        public FloatingItem(Camera camera, Bitmap itemIcon, string itemName = "", int count = 1)
            : base(camera)
        {
            _itemIcon = itemIcon;
            _itemName = itemName;
            _count = count;
            _textColor = DEFAULT_TExT_COLOR;
            _countColor = DEFAULT_COUNT_COLOR;
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _elapsedTime = 0;
            _isComplete = false;

            // 시작 위치 저장
            _startPosition = _worldPosition;

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            _reusableGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (_isComplete)
            {
                _isActive = false;
                return;
            }

            _elapsedTime += deltaTime;

            // 수명 체크
            if (_elapsedTime >= LIFETIME_MS)
            {
                _isComplete = true;
                _isActive = false;
                return;
            }

            // 떠오르는 애니메이션 (FLOAT_SPEED 적용)
            float progress = (float)_elapsedTime / LIFETIME_MS;
            float easedProgress = progress * progress; // 가속 효과 (선택적)
            float floatOffset = FLOAT_DISTANCE * easedProgress * FLOAT_SPEED;
            _worldPosition.z = _startPosition.z + floatOffset;

            // 페이드 아웃 처리
            if (_elapsedTime >= FADE_START_MS)
            {
                float fadeProgress = (float)(_elapsedTime - FADE_START_MS) / (LIFETIME_MS - FADE_START_MS);
                _alpha = 1.0f - fadeProgress;
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 배경 클리어 (투명)
            _reusableGraphics.Clear(DEFAULT_BACKGROUND_COLOR);

            if (_itemIcon != null)
            {
                // 아이콘을 중앙에 그리기
                int iconSize = Math.Min(TExTURE_WIDTH, TExTURE_HEIGHT) - 20;
                int iconx = (TExTURE_WIDTH - iconSize) / 2;
                int iconY = (TExTURE_HEIGHT - iconSize) / 2 - 10;

                _reusableGraphics.DrawImage(_itemIcon,
                    new Rectangle(iconx, iconY, iconSize, iconSize),
                    new Rectangle(0, 0, _itemIcon.Width, _itemIcon.Height),
                    GraphicsUnit.Pixel);
            }

            // 개수 표시 (우측 하단)
            if (_count > 1)
            {
                using (SolidBrush countBrush = new SolidBrush(_countColor))
                using (StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Far
                })
                {
                    string countText = $"+{_count}";
                    _reusableGraphics.DrawString(countText, _countFont, countBrush,
                        new RectangleF(0, 0, TExTURE_WIDTH - 5, TExTURE_HEIGHT - 5), format);
                }
            }

            // 아이템 이름 표시 (하단 중앙) - 선택적
            if (!string.IsNullOrEmpty(_itemName))
            {
                using (SolidBrush textBrush = new SolidBrush(_textColor))
                using (StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Far
                })
                using (Font nameFont = new Font(DEFAULT_FONT_FAMILY, 10, FontStyle.Regular))
                {
                    _reusableGraphics.DrawString(_itemName, nameFont, textBrush,
                        new RectangleF(0, 0, TExTURE_WIDTH, TExTURE_HEIGHT - 5), format);
                }
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        /// <summary>
        /// 애니메이션을 리셋합니다.
        /// </summary>
        public void Reset()
        {
            _elapsedTime = 0;
            _isComplete = false;
            _isActive = true;
            _alpha = 1.0f;
            _startPosition = _worldPosition;
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