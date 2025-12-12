using Common.Abstractions;
using System.Collections.Generic;

namespace Model3d
{
    public class UnifiedModel
    {
        public uint VaoID { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }

        public List<Texture> Textures { get; set; }
        public List<uint> TextureIDs { get; set; }  // ✅ OpenGL 텍스처 ID

        public UnifiedModel()
        {
            Textures = new List<Texture>();
            TextureIDs = new List<uint>();
        }
    }
}