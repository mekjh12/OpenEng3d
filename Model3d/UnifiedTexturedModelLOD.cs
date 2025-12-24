using System;

namespace Model3d
{
    public class UnifiedTexturedModelLOD: UnifiedTexturedModel
    {
        UnifiedTexturedModel _modelLod1 = null;

        public UnifiedTexturedModel ModelLod1 => _modelLod1;

        public UnifiedTexturedModelLOD(UnifiedTexturedModel source, UnifiedTexturedModel lod1) : base(source.Name)
        {
            // 부모 클래스 데이터 복사
            this.VaoID = source.VaoID;
            this.VertexCount = source.VertexCount;
            this.IndexCount = source.IndexCount;
            this.Textures = source.Textures; // 참조 복사 (같은 텍스처 공유)
            this.TextureIDs = source.TextureIDs; // 프로퍼티 세터가 배열도 자동 생성
            this.EnableCullFace = source.EnableCullFace;
            this.CullFaceMode = source.CullFaceMode;
            this.AABB = source.AABB;
            _modelLod1 = lod1;
        }
    }
}
