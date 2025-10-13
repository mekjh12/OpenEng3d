using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 데미지/힐량 숫자
    /// 위로 떠오르면서 페이드아웃 애니메이션이 적용됩니다.
    /// </summary>
    public class DamageNumber : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TEXTURE_WIDTH = 256;
        private const int TEXTURE_HEIGHT = 128;

        // 애니메이션 설정
        private const float RISE_SPEED = 0.5f;              // 초당 상승 속도 (월드 단위)
        private const int DISPLAY_DURATION = 1500;          // 표시 시간 (밀리초)
        private const int FADE_START_TIME = 500;            // 페이드 시작 시간 (밀리초)

        // 기본 색상
        private static readonly Color DEFAULT_NORMAL_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_CRITICAL_COLOR = Color.FromArgb(255, 255, 50, 50);
        private static readonly Color DEFAULT_HEAL_COLOR = Color.FromArgb(255, 100, 255, 100);
        private static readonly Color DEFAULT_OUTLINE_COLOR = Color.FromArgb(255, 0, 0, 0);

        // 기본 크기
        private const float DEFAULT_WIDTH = 0.8f;
        private const float DEFAULT_HEIGHT = 0.4f;
        private const float CRITICAL_SCALE = 1.5f;

        // 기본 폰트
        private const string DEFAULT_FONT_FAMILY = "Consolas";

        private string _displayText;
        private Color _textColor;
        private Color _outlineColor;
        private string _fontFamily;
        private Font _normalFont;
        private Font _criticalFont;
        private bool _isCritical;
        private bool _isHeal;
        private int _elapsedTime;
        private float _startZ;
        private bool _isFinished;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private static Bitmap _measureBitmap;
        private static Graphics _measureGraphics;

        /// <summary>표시할 텍스트</summary>
        public string DisplayText => _displayText;

        /// <summary>애니메이션이 완료되었는지 여부</summary>
        public bool IsFinished => _isFinished;

        /// <summary>크리티컬 여부</summary>
        public bool IsCritical => _isCritical;

        /// <summary>힐 여부</summary>
        public bool IsHeal => _isHeal;

        /// <summary>
        /// 데미지 숫자를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="damage">표시할 데미지 값</param>
        /// <param name="position">표시할 월드 위치</param>
        /// <param name="isCritical">크리티컬 여부</param>
        /// <param name="isHeal">힐 여부</param>
        public DamageNumber(Camera camera, float damage, Vertex3f position, bool isCritical = false, bool isHeal = false)
            : base(camera)
        {
            _displayText = isHeal ? $"+{(int)damage}" : $"{(int)damage}";
            _isCritical = isCritical;
            _isHeal = isHeal;
            _worldPosition = position;
            _startZ = position.z;
            _offset = Vertex3f.Zero;
            _elapsedTime = 0;
            _isFinished = false;

            // 색상 설정
            if (_isHeal)
                _textColor = DEFAULT_HEAL_COLOR;
            else if (_isCritical)
                _textColor = DEFAULT_CRITICAL_COLOR;
            else
                _textColor = DEFAULT_NORMAL_COLOR;

            _outlineColor = DEFAULT_OUTLINE_COLOR;

            // 폰트 설정
            _fontFamily = DEFAULT_FONT_FAMILY;
            _normalFont = FontManager.GetFont(_fontFamily);

            // 크리티컬용 큰 폰트 (임시로 생성)
            if (_isCritical && _normalFont != null)
            {
                _criticalFont = new Font(_normalFont.FontFamily, _normalFont.Size * CRITICAL_SCALE, _normalFont.Style);
            }

            // 크기 설정
            _width = DEFAULT_WIDTH * (_isCritical ? CRITICAL_SCALE : 1.0f);
            _height = DEFAULT_HEIGHT * (_isCritical ? CRITICAL_SCALE : 1.0f);

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // 초기 텍스처 생성
            UpdateTexture();
        }

        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (_isFinished)
            {
                _isVisible = false;
                return;
            }

            _elapsedTime += deltaTime;

            // 표시 시간이 지나면 완료 처리
            if (_elapsedTime >= DISPLAY_DURATION)
            {
                _isFinished = true;
                _isVisible = false;
                return;
            }

            // 위로 상승
            float deltaSeconds = deltaTime / 1000.0f;
            _worldPosition.z = _startZ + (RISE_SPEED * _elapsedTime / 1000.0f);

            // 페이드 아웃 계산
            if (_elapsedTime >= FADE_START_TIME)
            {
                int fadeTime = _elapsedTime - FADE_START_TIME;
                int fadeDuration = DISPLAY_DURATION - FADE_START_TIME;
                _alpha = 1.0f - ((float)fadeTime / fadeDuration);
            }
            else
            {
                _alpha = 1.0f;
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 정적 측정용 리소스 초기화 (최초 1회만)
            if (_measureBitmap == null)
            {
                _measureBitmap = new Bitmap(2048, 256);
                _measureGraphics = Graphics.FromImage(_measureBitmap);
                _measureGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            }

            // 배경 투명하게
            _reusableGraphics.Clear(Color.Transparent);

            // 사용할 폰트 선택
            Font useFont = _isCritical && _criticalFont != null ? _criticalFont : _normalFont;

            if (useFont != null && !string.IsNullOrEmpty(_displayText))
            {
                // 텍스트 크기 측정
                SizeF textSize = _measureGraphics.MeasureString(_displayText, useFont);

                // 중앙 위치 계산
                float x = (TEXTURE_WIDTH - textSize.Width) / 2;
                float y = (TEXTURE_HEIGHT - textSize.Height) / 2;

                // 외곽선 그리기 (여러 방향으로 그려서 두껍게)
                using (SolidBrush outlineBrush = new SolidBrush(_outlineColor))
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (dx != 0 || dy != 0)
                            {
                                _reusableGraphics.DrawString(_displayText, useFont, outlineBrush, x + dx, y + dy);
                            }
                        }
                    }
                }

                // 메인 텍스트 그리기
                using (SolidBrush textBrush = new SolidBrush(_textColor))
                {
                    _reusableGraphics.DrawString(_displayText, useFont, textBrush, x, y);
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
            _criticalFont?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// 정적 리소스를 정리합니다. (앱 종료 시 호출)
        /// </summary>
        public static void CleanupStaticResources()
        {
            _measureGraphics?.Dispose();
            _measureBitmap?.Dispose();
            _measureGraphics = null;
            _measureBitmap = null;
        }
    }
}