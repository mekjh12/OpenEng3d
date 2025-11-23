using Common.Abstractions;
using OpenGL;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 캐릭터 이름표
    /// 텍스트 길이에 따라 자동으로 크기가 조절됩니다.
    /// </summary>
    public class CharacterNamePlate : Billboard3D
    {
        // 기본 텍스처 크기
        private int TExTURE_WIDTH = 256;
        private int TExTURE_HEIGHT = 64;

        // 텍스트 주변 여백
        private const float PADDING_HORIZONTAL = 16f;
        private const float PADDING_VERTICAL = 8f;

        // 기본 색상
        private static readonly Color DEFAULT_NAME_COLOR = Color.FromArgb(255, 255, 220, 20);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(255, 50, 50, 50);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 0.3f;
        private const float DEFAULT_HEIGHT = 0.1f;
        private const float DEFAULT_Z_OFFSET = 0.1f;

        // 기본 폰트
        private const string DEFAULT_FONT_FAMILY = "Consolas";

        private string _characterName;
        private Color _nameColor;
        private Color _backgroundColor;
        private string _fontFamily;
        private Font _nameFont;
        private bool _isAutoFit = true;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;

        /// <summary>표시할 캐릭터 이름</summary>
        public string CharacterName
        {
            get => _characterName;
            set
            {
                if (_characterName != value)
                {
                    _characterName = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>폰트 패밀리 이름</summary>
        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    _nameFont = FontManager.GetFont(_fontFamily);
                    _isDirty = true;
                }
            }
        }

        /// <summary>텍스트 크기에 맞춰 배경 크기를 자동 조절할지 여부</summary>
        public bool IsAutoFit { get => _isAutoFit; set => _isAutoFit = value; }

        /// <summary>이름 텍스트 색상</summary>
        public Color NameColor
        {
            get => _nameColor;
            set
            {
                if (_nameColor != value)
                {
                    _nameColor = value;
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

        /// <summary>
        /// 캐릭터 이름표를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="characterName">표시할 캐릭터 이름</param>
        public CharacterNamePlate(Camera camera, string characterName)
            : base(camera)
        {
            _characterName = characterName;
            _nameColor = DEFAULT_NAME_COLOR;
            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
            _fontFamily = DEFAULT_FONT_FAMILY;
            _nameFont = FontManager.GetFont(_fontFamily);
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        /// <summary>
        /// 이름표를 즉시 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            _isDirty = true;
        }

        // 텍스트 측정용 임시 비트맵 (재사용)
        private static Bitmap _measureBitmap;
        private static Graphics _measureGraphics;

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

            // 텍스트 크기 측정
            SizeF textSize = SizeF.Empty;
            if (_nameFont != null && !string.IsNullOrEmpty(_characterName))
            {
                textSize = _measureGraphics.MeasureString(_characterName, _nameFont);
            }

            // AutoFit이 활성화된 경우 빌보드 크기 자동 조절
            if (_isAutoFit && !textSize.IsEmpty)
            {
                int newWidth = (int)(textSize.Width + PADDING_HORIZONTAL * 2);
                int newHeight = (int)(textSize.Height + PADDING_VERTICAL * 2);

                // 텍스처 크기가 변경된 경우에만 재생성
                if (TExTURE_WIDTH != newWidth || TExTURE_HEIGHT != newHeight)
                {
                    TExTURE_WIDTH = newWidth;
                    TExTURE_HEIGHT = newHeight;

                    // 기존 리소스 해제 및 재생성
                    _reusableGraphics?.Dispose();
                    _reusableBitmap?.Dispose();
                    _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
                    _reusableGraphics = Graphics.FromImage(_reusableBitmap);
                    _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                    _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                }

                // 빌보드 크기 설정
                _width = 0.001f * TExTURE_WIDTH;
                _height = 0.001f * TExTURE_HEIGHT;
            }

            // 배경 클리어
            _reusableGraphics.Clear(_backgroundColor);

            // 캐릭터 이름 렌더링
            using (SolidBrush nameBrush = new SolidBrush(_nameColor))
            using (StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            })
            {
                if (_nameFont != null)
                {
                    _reusableGraphics.DrawString(_characterName, _nameFont, nameBrush,
                        new RectangleF(0, 0, TExTURE_WIDTH, TExTURE_HEIGHT), format);
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