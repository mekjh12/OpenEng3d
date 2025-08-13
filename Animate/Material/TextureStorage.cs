using Common.Abstractions;
using System.Collections.Generic;

namespace Animate
{
    public static class TextureStorage
    {
        static Dictionary<string, Texture> _texturesLoaded = new Dictionary<string, Texture>();

        public static Texture GetTexture(string textureName)
        {
            if (_texturesLoaded.TryGetValue(textureName, out Texture texture))
            {
                return texture;
            }
            else
            {
                throw new KeyNotFoundException($"Texture '{textureName}' not found in storage.");
            }
        }

        public static void Add(string fileName, Texture texture)
        {
            _texturesLoaded[fileName] = texture;
        }

        public static bool ContainsKey(string key)
        {
            return _texturesLoaded.ContainsKey(key);
        }
    }
}
