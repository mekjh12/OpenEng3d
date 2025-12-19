using Common;
using Common.Abstractions;
using OpenGL;
using System.Collections.Generic;
using System.Drawing.Printing;

namespace Model3d
{
    public class UnifiedTexturedModel
    {
        private uint[] _textureIds;
        private List<uint> _textureList;

        public string Name { get; set; }
        public uint VaoID { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }

        public List<Texture> Textures { get; set; }

        public List<uint> TextureIDs
        {
            get => _textureList;
            set
            {
                _textureList = value;
                _textureIds = TextureIDs.ToArray();
            }
        }

        public uint[] TextureIDArray => _textureIds;

        public bool EnableCullFace { get; set; } = false;
        public CullFaceMode CullFaceMode { get; set; } = CullFaceMode.Back;

        public AABB3f AABB { get; set; }

        public UnifiedTexturedModel(string name)
        {
            Textures = new List<Texture>();
            TextureIDs = new List<uint>();
            Name = name;
        }
    }
}