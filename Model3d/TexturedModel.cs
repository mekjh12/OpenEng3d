using Common.Abstractions;

namespace Model3d
{
    public class TexturedModel : RawModel3d
    {
        private Texture _texture;
        private Texture.TextureMapType _textureType;

        public Texture.TextureMapType TextureMapType
        {
            get => _textureType;
            set => _textureType = value;
        }

        public Texture Texture
        {
            get => _texture;
        }

        public Texture.TextureMapType RemoveMap
        {
            set => _textureType &= ~value;
        }

        public Texture.TextureMapType AppendMap
        {
            set => _textureType |= value;
        }

        public Texture.TextureMapType AttachMap
        {
            set => _textureType = value;
        }

        public TexturedModel(RawModel3d model, Texture texture): base(model.VAO, model.Vertices)
        {
            // 생성자와 속성초기화에서 (1) 생성자 호출 (2) 속성초기화 순으로 진행된다.
            _texture = texture;
            _ibo = model.IBO;
            _indexCount = model.IndexCount;
            _vertexCount = model.VertexCount;
            _obb = model.OBB;
            _aabb = model.AABB;

            if (texture != null)
                _textureType = texture.TextureType;
        }
    }
}
