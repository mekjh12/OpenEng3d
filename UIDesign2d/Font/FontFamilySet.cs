using System.Collections.Generic;

namespace ZetaEngine
{
    public class FontFamilySet
    {
        Dictionary<string, FontFamily> fonts;

        public Dictionary<string, FontFamily> Fonts => fonts;

        public FontFamily this[string fontName]
        {
            get
            {
                if (fonts.ContainsKey(fontName))
                {
                    return fonts[fontName];
                }
                return null;
            }
        }

        public FontFamilySet()
        {
            fonts = new Dictionary<string, FontFamily>();
        }

        public bool Contains(string fontName)
        {
            return fonts.ContainsKey(fontName);
        }

        public void Add(string fontName, string pngFileName, string fntFileName, float aspectRatio)
        {
            fonts.Add(fontName, CreateFontType(pngFileName, fntFileName, aspectRatio));
        }

        private FontFamily CreateFontType(string pngFileName, string fntFileName, float aspectRatio)
        {
            string pngFileFullName = EngineLoop.EXECUTE_PATH + @"\fonts\" + pngFileName;
            string fontFileFullName = EngineLoop.EXECUTE_PATH + @"\fonts\" + fntFileName;

            return new FontFamily(
                textureAtlas: (int)Loader.LoadTexture2(pngFileFullName.Replace("\\", "/")),
                fontMetaFile: fontFileFullName.Replace("\\", "/"),
                aspectRatio: aspectRatio);
        }
    }
}
