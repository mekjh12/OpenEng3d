using Common.Abstractions;
using OpenGL;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 상호작용 프롬프트
    /// NPC, 오브젝트 등과 상호작용 가능할 때 키 입력을 안내합니다.
    /// </summary>
    public class InteractionPrompt : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TEXTURE_WIDTH = 256;
        private const int TEXTURE_HEIGHT = 64;

        // 패딩
        private const float PADDING_HORIZONTAL = 12f;
        private const float PADDING_VERTICAL = 8f;

        // 기본 색상
        private static readonly Color DEFAULT_KEY_BG_COLOR = Color.FromArgb(255, 50, 50, 50);
        private static readonly Color DEFAULT_KEY_TEXT_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_ACTION_TEXT_COLOR = Color.FromArgb(255, 200, 200, 200);
        private static readonly Color DEFAULT_BORDER_COLOR = Color.FromArgb(255, 150, 150, 150);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(0, 0, 0, 0);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 0.3f;
        private const float DEFAULT_HEIGHT = 0.08f;
        private const float DEFAULT_Z_OFFSET = 0.2f;

        // 폰트
        private const string DEFAULT_FONT_FAMILY = "Arial";
        private static readonly Font _keyFont = new Font(DEFAULT_FONT_FAMILY, 16, FontStyle.Bold);
        private static readonly Font _actionFont = new Font(DEFAULT_FONT_FAMILY, 12, FontStyle.Regular);

        private string _keyText;
        private string _actionText;
        private Color _keyBgColor;
        private Color _keyTextColor;
        private Color _actionTextColor;
        private Color _borderColor;
        private bool _showBorder;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private int _pulseTime;
        private float _pulseAlpha;

        /// <summary>키 텍스트 (예: "E", "F", "Space")</summary>
        public string KeyText
        {
            get => _keyText;
            set
            {
                if (_keyText != value)
                {
                    _keyText = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>액션 텍스트 (예: "대화하기", "열기", "줍기")</summary>
        public string ActionText
        {
            get => _actionText;
            set
            {
                if (_actionText != value)
                {
                    _actionText = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>키 배경 색상</summary>
        public Color KeyBackgroundColor
        {
            get => _keyBgColor;
            set
            {
                if (_keyBgColor != value)
                {
                    _keyBgColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>키 텍스트 색상</summary>
        public Color KeyTextColor
        {
            get => _keyTextColor;
            set
            {
                if (_keyTextColor != value)
                {
                    _keyTextColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>액션 텍스트 색상</summary>
        public Color ActionTextColor
        {
            get => _actionTextColor;
            set
            {
                if (_actionTextColor != value)
                {
                    _actionTextColor = value;
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

        /// <summary>
        /// 상호작용 프롬프트를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="keyText">키 텍스트 (예: "E")</param>
        /// <param name="actionText">액션 텍스트 (예: "대화하기")</param>
        public InteractionPrompt(Camera camera, string keyText = "E", string actionText = "상호작용")
            : base(camera)
        {
            _keyText = keyText;
            _actionText = actionText;
            _keyBgColor = DEFAULT_KEY_BG_COLOR;
            _keyTextColor = DEFAULT_KEY_TEXT_COLOR;
            _actionTextColor = DEFAULT_ACTION_TEXT_COLOR;
            _borderColor = DEFAULT_BORDER_COLOR;
            _showBorder = true;
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);
            _pulseTime = 0;
            _pulseAlpha = 1.0f;

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        /// <summary>
        /// 업데이트 (펄스 애니메이션)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (!_isActive)
                return;

            // 펄스 애니메이션 (0.8 ~ 1.0)
            _pulseTime += deltaTime;
            float cycle = (_pulseTime % 2000) / 2000f;
            _pulseAlpha = 0.8f + 0.2f * (float)System.Math.Sin(cycle * System.Math.PI * 2);
            _alpha = _pulseAlpha;

            base.Update(deltaTime);
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 배경 클리어 (투명)
            _reusableGraphics.Clear(DEFAULT_BACKGROUND_COLOR);

            // 키 버튼 크기 계산
            SizeF keySize = _reusableGraphics.MeasureString(_keyText, _keyFont);
            float keyBoxSize = System.Math.Max(keySize.Width, keySize.Height) + PADDING_HORIZONTAL * 2;
            float keyBoxX = PADDING_HORIZONTAL;
            float keyBoxY = (TEXTURE_HEIGHT - keyBoxSize) / 2;

            // 키 버튼 배경
            using (SolidBrush keyBgBrush = new SolidBrush(_keyBgColor))
            {
                _reusableGraphics.FillRectangle(keyBgBrush, keyBoxX, keyBoxY, keyBoxSize, keyBoxSize);
            }

            // 키 버튼 테두리
            if (_showBorder)
            {
                using (Pen borderPen = new Pen(_borderColor, 2f))
                {
                    _reusableGraphics.DrawRectangle(borderPen, keyBoxX, keyBoxY, keyBoxSize, keyBoxSize);
                }
            }

            // 키 텍스트
            using (SolidBrush keyTextBrush = new SolidBrush(_keyTextColor))
            using (StringFormat keyFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                RectangleF keyRect = new RectangleF(keyBoxX, keyBoxY, keyBoxSize, keyBoxSize);
                _reusableGraphics.DrawString(_keyText, _keyFont, keyTextBrush, keyRect, keyFormat);
            }

            // 액션 텍스트
            if (!string.IsNullOrEmpty(_actionText))
            {
                using (SolidBrush actionTextBrush = new SolidBrush(_actionTextColor))
                using (StringFormat actionFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                })
                {
                    float actionX = keyBoxX + keyBoxSize + PADDING_HORIZONTAL;
                    float actionY = TEXTURE_HEIGHT / 2;
                    RectangleF actionRect = new RectangleF(
                        actionX, 0,
                        TEXTURE_WIDTH - actionX - PADDING_HORIZONTAL,
                        TEXTURE_HEIGHT);
                    _reusableGraphics.DrawString(_actionText, _actionFont, actionTextBrush, actionRect, actionFormat);
                }
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        /// <summary>
        /// 프롬프트를 즉시 갱신합니다.
        /// </summary>
        public void Refresh()
        {
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