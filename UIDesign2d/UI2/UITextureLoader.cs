using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;

namespace Ui2d
{
    public static class UITextureLoader
    {
        [Flags]
        public enum UITextureControlBrunch
        {
            NORMAL, OVER, CLICK, CHECKED, CHECKED_OVER
        }

        public static uint Transparent => _textures["transparent"];

        public static uint Cursor => _textures["cursor"];

        public static uint CursorOver => _textures["cursor_over"];

        public static uint NULL => 1;

        public static uint Texture(string name)
        {
            if (_textures.ContainsKey(name))
            {
                return _textures[name];
            }
            return 0;
        }

        public static float TextureAspect(string name)
        {
            if (_textureAspect.ContainsKey(name))
            {
                return _textureAspect[name];
            }
            return 0;
        }

        private static Dictionary<string, uint> _textures = new Dictionary<string, uint>();
        private static Dictionary<string, float> _textureAspect = new Dictionary<string, float>();

        public static bool ContainTexture(string textureName) => _textures.ContainsKey(textureName);

        public static uint[] GetTexture(string name)
        {
            if (_textures.ContainsKey(name))
            {
                List<uint> result = new List<uint>();
                if (_textures.ContainsKey(name)) result.Add(_textures[name]);
                if (_textures.ContainsKey($"{name}_over")) result.Add(_textures[$"{name}_over"]);
                if (_textures.ContainsKey($"{name}_click")) result.Add(_textures[$"{name}_click"]);
                if (_textures.ContainsKey($"{name}_checked")) result.Add(_textures[$"{name}_checked"]);
                return result.ToArray();                
            }
            return null;
        }

        public static uint[] Texture(string name, UITextureControlBrunch mode)
        {
            uint[] textures = new uint[5];
            if ((mode & UITextureControlBrunch.NORMAL) == UITextureControlBrunch.NORMAL)
                textures[0] = (_textures.ContainsKey(name)) ? _textures[name] : 0;
            if ((mode & UITextureControlBrunch.OVER) == UITextureControlBrunch.OVER)
                textures[1] = (_textures.ContainsKey($"{name}_over")) ? _textures[$"{name}_over"] : 0;
            if ((mode & UITextureControlBrunch.CLICK) == UITextureControlBrunch.CLICK)
                textures[2] = (_textures.ContainsKey($"{name}_click")) ? _textures[$"{name}_click"] : 0;
            if ((mode & UITextureControlBrunch.CHECKED) == UITextureControlBrunch.CHECKED)
                textures[3] = (_textures.ContainsKey($"{name}_checked")) ? _textures[$"{name}_checked"] : 0;
            if ((mode & UITextureControlBrunch.CHECKED_OVER) == UITextureControlBrunch.CHECKED_OVER)
                textures[4] = (_textures.ContainsKey($"{name}_checkedOver")) ? _textures[$"{name}_checked"] : 0;
            return textures;
        }

        /// <summary>
        /// 지정된 경로의 png파일을 텍스처로 모두 로딩한다.<br/>
        /// *파일의 확장자는 png만 지원됩니다.<br/>
        /// </summary>
        /// <param name="path"></param>
        public static void LoadTexture2d(string path)
        {
            string mouseFileImage = UIEngine.REOURCES_PATH + "cursor.png";
            Image image = Bitmap.FromFile(mouseFileImage);
            float mouseAspect = (float)image.Width / (float)image.Height;
            UIEngine.MouseAspect = mouseAspect;

            AddOneFile(UIEngine.REOURCES_PATH + "transparent.png");
            AddOneFile(UIEngine.REOURCES_PATH + "cursor.png");
            AddOneFile(UIEngine.REOURCES_PATH + "cursor_over.png");

            AddOneFile(UIEngine.REOURCES_PATH + "hvalue.png");

            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                if (Path.GetExtension(filename) == ".png")
                    UITextureLoader.AddOneFile(filename);
            }
        }

        public static uint[] Add(string fileName)
        {
            List<uint> res = new List<uint>();
            res.Add(AddOneFile(fileName));
            res.Add(AddOneFile(fileName, "_over"));
            res.Add(AddOneFile(fileName, "_click"));
            res.Add(AddOneFile(fileName, "_checked"));
            res.Add(AddOneFile(fileName, "_checkedOver"));
            return res.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="optionName">_click, _over, _checked</param>
        public static uint AddOneFile(string fileName, string optionName = "")
        {
            if (!File.Exists(fileName)) return 0;
            string name = Path.GetFileNameWithoutExtension(fileName);
            if (UITextureLoader.ContainTexture(name)) 
                return UITextureLoader.Texture(name);

            if (!_textures.ContainsKey(name + optionName))
            {
                string path = Path.GetDirectoryName(fileName);
                string fn = path + "\\" + Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                string newFileName = $"{fn}{optionName}{ext}";
                if (File.Exists(newFileName))
                {
                    Image bitmap = Bitmap.FromFile(newFileName);
                    _textureAspect.Add(name + optionName, (float)bitmap.Width / (float)bitmap.Height);
                    _textures.Add(name + optionName, LoaderFont.LoadTexture2($"{fn}{optionName}{ext}"));
                    return _textures[name + optionName];
                }
            }
            return 0;
        }
    }
}
