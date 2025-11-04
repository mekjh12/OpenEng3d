using Common.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Model3d
{
    /// <summary>
    /// 
    /// </summary>
    public static class TextureStorage
    {
        public static Dictionary<string, Texture> _texturesLoaded = new Dictionary<string, Texture>();
        private static bool _isInit = false;
        public static string NullTextureFileName  = string.Empty;
        public static Texture DebugTexture;

        static TextureStorage()
        {
            
        }

        public static bool IsExistTexture(string filename)
        {
            return _texturesLoaded.ContainsKey(filename);
        }

        public static void Clear()
        {
            _texturesLoaded.Clear();
        }

        public static Texture Add(string filename, Texture.TextureMapType textureMapType = Texture.TextureMapType.Diffuse)
        {
            // 초기화가 진행되지 않았으면 초기화를 한다.
            if (!_isInit)
            {
                if (File.Exists(NullTextureFileName))
                {
                    Texture texture = new Texture(NullTextureFileName, Texture.TextureMapType.Diffuse);
                    _texturesLoaded.Add(NullTextureFileName, texture);
                    DebugTexture = texture;
                    _isInit = true;
                }
                else
                {
                    throw new Exception("사용하려면 초기화 디버깅 Null Texture의 파일경로를 지정하세요.");
                }
            }

            if (IsExistTexture(filename))
            {
                return _texturesLoaded[filename];
            }
            else
            {
                if (File.Exists(filename))
                {
                    Texture texture = new Texture(filename, textureMapType);
                    _texturesLoaded.Add(filename, texture);
                    return texture;
                }
                else
                {
                    // 파일이 없으면 Null텍스처를 읽어온다.
                    if (IsExistTexture(NullTextureFileName))
                    {
                        Console.WriteLine($"{filename} 텍스처 파일이 누락되어 null 파일로 대체하였습니다.");
                        return _texturesLoaded[NullTextureFileName];
                    }
                    return null;
                }
            }
        }
    }
}
