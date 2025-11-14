using System.Collections.Generic;
using System.Drawing;

namespace Ui3d
{
    public static class FontManager
    {
        public static readonly string DefaultFontFamily = "돋움체";//
        public static readonly float DefaultFontSize = 23.0f;
        public static readonly FontStyle DefaultFontStyle = FontStyle.Bold;
        public static Font DefaultFont = new Font(DefaultFontFamily, DefaultFontSize, DefaultFontStyle);

        private static Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();

        static FontManager()
        {
            // 기본 폰트 등록
            AddFont(DefaultFontFamily, DefaultFont);
            AddFont("Consolas", new Font("Consolas", DefaultFontSize, FontStyle.Bold));
        }

        public static void AddFont(string fontName, Font font)
        {
            if (!_fontCache.ContainsKey(fontName))
                _fontCache.Add(fontName, font);
        }

        public static Font GetFont(string fontName)
        {
            if (_fontCache.TryGetValue(fontName, out Font font))
                return font;
            return null;
        }

        public static bool HasFont(string fontName)
        {
            return _fontCache.ContainsKey(fontName);
        }

        public static bool RemoveFont(string fontName)
        {
            // 기본 폰트는 제거할 수 없음
            if (fontName == DefaultFontFamily)
                return false;

            return _fontCache.Remove(fontName);
        }

        public static void Clear()
        {
            foreach (var font in _fontCache.Values)
            {
                if (font != DefaultFont)
                    font?.Dispose();
            }
            _fontCache.Clear();

            // 기본 폰트는 다시 추가
            AddFont(DefaultFontFamily, DefaultFont);
        }
    }
}