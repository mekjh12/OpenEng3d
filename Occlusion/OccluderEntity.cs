using Geometry;
using Model3d;

namespace Occlusion
{
    public class OccluderEntity : Entity, IOcclusionable
    {
        OcclusionComponent _occlusionComponent;

        public OccluderEntity(string entityName, string modelName, RawModel3d[] rawModel3D) : base(entityName, modelName, rawModel3D)
        {
            // 컴포넌트 구성
            _occlusionComponent = new OcclusionComponent();
        }

        public bool IsOccluder
        {
            get => ((IOcclusionable)_occlusionComponent).IsOccluder;
        }

        public BoxOccluder BoxOccluder
        {
            get => ((IOcclusionable)_occlusionComponent).BoxOccluder; 
            set => ((IOcclusionable)_occlusionComponent).BoxOccluder = value;
        }

        public void GenBoxOccluder(OBB obb)
        {
            ((IOcclusionable)_occlusionComponent).GenBoxOccluder(obb);
        }
    }
}
