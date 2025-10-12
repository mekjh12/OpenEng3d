using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 캐릭터 이름표
    /// </summary>
    public class CharacterNamePlate : Billboard3D
    {
        // 텍스처 크기 상수
        private const int TEXTURE_WIDTH = 256;
        private const int TEXTURE_HEIGHT = 64;

        // 기본 색상 상수
        private static readonly Color DEFAULT_NAME_COLOR = Color.FromArgb(255, 255, 220, 20);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(255, 50, 50, 50);

        // 기본 크기 상수
        private const float DEFAULT_WIDTH = 0.3f;
        private const float DEFAULT_HEIGHT = 0.1f;
        private const float DEFAULT_Z_OFFSET = 0.1f;

        // 기본 폰트 상수
        private const string DEFAULT_FONT_FAMILY = "Consolas";

        private string _characterName;
        private Color _nameColor;
        private Color _backgroundColor;
        private string _fontFamily;
        private Font _nameFont;
        private bool _isAutoFit = true;

        // 재사용 가능한 비트맵 추가
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;

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

        public bool IsAutoFit { get => _isAutoFit; set => _isAutoFit = value; }

        // 색상 속성
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

        public CharacterNamePlate(Camera camera, string characterName)
            : base(camera)
        {
            _characterName = characterName;

            // 색상 설정 - 상수 사용
            _nameColor = DEFAULT_NAME_COLOR;
            _backgroundColor = DEFAULT_BACKGROUND_COLOR;

            // 폰트 - 상수 사용
            _fontFamily = DEFAULT_FONT_FAMILY;
            _nameFont = FontManager.GetFont(_fontFamily);

            // 크기 설정 - 상수 사용
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;

            // 오프셋 (머리 위) - 상수 사용
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);

            // ✅ 재사용할 비트맵 미리 생성
            _reusableBitmap = new Bitmap(TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);

            // Graphics 설정 (한 번만)
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        public CharacterNamePlate(
            Camera camera,
            string characterName,
            Color nameColor,
            Color backgroundColor,
            string fontFamily = DEFAULT_FONT_FAMILY)
            : base(camera)
        {
            _characterName = characterName;
            _nameColor = nameColor;
            _backgroundColor = backgroundColor;

            _fontFamily = fontFamily;
            _nameFont = FontManager.GetFont(_fontFamily);

            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);
        }

        protected override void UpdateTexture()
        {
            // ✅ using 없이 재사용
            Graphics g = _reusableGraphics;

            // 배경 클리어
            g.Clear(_backgroundColor);

            // 캐릭터 이름
            using (SolidBrush nameBrush = new SolidBrush(_nameColor))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Far,
                };

                if (_nameFont != null)
                {
                    g.DrawString(_characterName, _nameFont, nameBrush,
                        new RectangleF(0, 0, TEXTURE_WIDTH, TEXTURE_HEIGHT), format);
                }
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        public override void Dispose()
        {
            // ✅ 재사용 리소스 정리
            _reusableGraphics?.Dispose();
            _reusableBitmap?.Dispose();
            _nameFont?.Dispose();
            base.Dispose();
        }
    }
}