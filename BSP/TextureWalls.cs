using glEng.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace glEng.BSP
{
    public class TextureWalls
    {
        List<Texture> textures;

        public Texture Texture(int index)
        {
            return textures[index];
        }

        public int Count
        {
            get { return textures.Count; }
        }

        public TextureWalls()
        {
            textures = new List<Texture>();
            /*
            string path = @"E:\CS\glEngine\WindowsFormsApp1\BSP\src\";
            foreach (string filename in System.IO.Directory.GetFiles(path))
            {
                Texture texture = new Texture(filename);
                textures.Add(texture);
            }
            */
        }

        public void Add(Texture texture)
        {
            textures.Add(texture);
        }
    }
}
