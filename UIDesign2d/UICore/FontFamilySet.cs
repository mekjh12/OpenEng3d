using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ui2d
{
    public class FontFamilySet
    {
        static Dictionary<string, FontFamily> fonts = new Dictionary<string, FontFamily>();

        public static FontFamily DefaultHangul => FontFamilySet.Contains("hangul") ? FontFamilySet.Font("hangul") : null;

        public static FontFamily D2Coding => FontFamilySet.Contains("d2coding") ? FontFamilySet.Font("d2coding") : null;

        public static FontFamily Consalas => FontFamilySet.Contains("consalas") ? FontFamilySet.Font("consalas") : null;

        public static FontFamily 연성체 => FontFamilySet.Contains("yeonsung") ? FontFamilySet.Font("yeonsung") : null;

        public static FontFamily 조선100년체 => FontFamilySet.Contains("ChosunCentennial") ? FontFamilySet.Font("ChosunCentennial") : null;

        public static Dictionary<string, FontFamily> Fonts => fonts;

        public static int Count => fonts.Count;

        public static FontFamily Font(string fontName)
        {
            if (fonts.ContainsKey(fontName))
            {
                return fonts[fontName];
            }
            return null;
        }

        public static bool Contains(string fontName)
        {
            return fonts.ContainsKey(fontName);
        }

        public static void AddFonts(string fileName)
        {
            // 폰트패밀리세트 설정하기, 폰트등록 (기본폰트: consalas)
            string fontListFileName = fileName;
            string path = Path.GetDirectoryName(fontListFileName) + "\\";
            StreamReader sr = new StreamReader(fontListFileName);
            while (!sr.EndOfStream)
            {
                string txt = sr.ReadLine();
                if (txt.StartsWith("//")) continue;
                string[] items = txt.Split(new char[] { ',' });
                if (items.Length != 4) continue;
                float aspectRatio = float.Parse(items[3].Trim());
                FontFamilySet.Add(items[0].Trim(), path + items[1].Trim(), path + items[2].Trim(), aspectRatio);
                Console.WriteLine($"* FONT {items[0].Trim()}폰트를 등록하였습니다.");
            }
            sr.Close();
        }

        public static bool Add(string fontName, string pngFileName, string fntFileName, float aspectRatio)
        {
            FontFamily family = CreateFontType(pngFileName, fntFileName, aspectRatio);
            if (family != null)
            {
                fonts.Add(fontName, family);
                return true;
            }
            else
            {
                Console.WriteLine($"--->FONT {fontName}폰트을 등록에 실패하였습니다.");
                return false;
            }
        }

        private static FontFamily CreateFontType(string pngFileName, string fntFileName, float aspectRatio)
        {
            if (!File.Exists(pngFileName))
            {
                Console.WriteLine($"--->/누락/ FONT 폰트 PNG파일이 없습니다. {pngFileName}");
                return null;
            }

            if (!File.Exists(fntFileName))
            {
                Console.WriteLine($"--->/누락/ FONT 폰트의 메타 파일이 없습니다. {fntFileName}");
                return null;
            }

            return new FontFamily(
                textureAtlas: (int)LoaderFont.LoadTexture2(pngFileName.Replace("\\", "/")),
                fontMetaFile: fntFileName.Replace("\\", "/"),
                aspectRatio: aspectRatio);
        }
    }
}
