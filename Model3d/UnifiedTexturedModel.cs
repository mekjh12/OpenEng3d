using Common;
using Common.Abstractions;
using OpenGL;
using System.Collections.Generic;

namespace Model3d
{
    public class UnifiedTexturedModel
    {
        public uint VaoID { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }

        public List<Texture> Textures { get; set; }
        public List<uint> TextureIDs { get; set; }  // ✅ OpenGL 텍스처 ID

        public bool EnableCullFace { get; set; } = false;
        public CullFaceMode CullFaceMode { get; set; } = CullFaceMode.Back;

        public AABB3f AABB { get; set; }

        public UnifiedTexturedModel()
        {
            Textures = new List<Texture>();
            TextureIDs = new List<uint>();
        }
    }
}