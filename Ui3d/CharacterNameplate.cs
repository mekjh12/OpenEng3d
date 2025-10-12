using Common.Abstractions;
using OpenGL;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 캐릭터 이름표
    /// </summary>
    public class CharacterNameplate : Billboard3D
    {
        private string _characterName;
        private string _guildName;
        private Color _nameColor;
        private Color _guildColor;
        private Color _backgroundColor;
        private Font _nameFont;
        private Font _guildFont;

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

        public string GuildName
        {
            get => _guildName;
            set
            {
                if (_guildName != value)
                {
                    _guildName = value;
                    _isDirty = true;
                }
            }
        }

        public CharacterNameplate(Camera camera, string characterName, string guildName = "")
            : base(camera)
        {
            _characterName = characterName;
            _guildName = guildName;

            // 색상 설정
            _nameColor = Color.FromArgb(255, 255, 220, 100);
            _guildColor = Color.FromArgb(255, 150, 200, 255);
            _backgroundColor = Color.FromArgb(200, 20, 20, 30);

            // 폰트
            _nameFont = new Font("맑은 고딕", 28, FontStyle.Bold);
            _guildFont = new Font("맑은 고딕", 18, FontStyle.Regular);

            // 크기 설정
            _width = 2.0f;
            _height = 0.5f;

            // 오프셋 (머리 위)
            _offset = new Vertex3f(0,  0, 2.0f);
        }

        protected override void UpdateTexture()
        {
            int width = 512;
            int height = 128;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // 고품질 렌더링 설정
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 배경
                using (SolidBrush bgBrush = new SolidBrush(_backgroundColor))
                {
                    g.FillRectangle(bgBrush, 0, 0, width, height);
                }

                // 캐릭터 이름
                using (SolidBrush nameBrush = new SolidBrush(_nameColor))
                {
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = string.IsNullOrEmpty(_guildName)
                            ? StringAlignment.Center
                            : StringAlignment.Far
                    };

                    float nameY = string.IsNullOrEmpty(_guildName)
                        ? height / 2f
                        : height * 0.4f;

                    g.DrawString(_characterName, _nameFont, nameBrush,
                        new RectangleF(0, 0, width, nameY), format);
                }

                // 길드 이름
                if (!string.IsNullOrEmpty(_guildName))
                {
                    using (SolidBrush guildBrush = new SolidBrush(_guildColor))
                    {
                        StringFormat format = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Near
                        };

                        g.DrawString($"<{_guildName}>", _guildFont, guildBrush,
                            new RectangleF(0, height * 0.55f, width, height * 0.45f), format);
                    }
                }

                // GPU에 업로드
                UploadTextureToGPU(bitmap);
            }
        }

        public override void Dispose()
        {
            _nameFont?.Dispose();
            _guildFont?.Dispose();
            base.Dispose();
        }
    }
}
