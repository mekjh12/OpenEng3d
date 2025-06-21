using Common.Abstractions;

namespace Geometry
{
    public class BoundBoxComponent
    {
        AABB _aabb;
        OBB _obb;
        AABB _modelAABB;
        OBB _modelOBB;

        private bool _visible;
        private readonly BaseEntity _baseEntity;

        public BoundBoxComponent(BaseEntity baseEntity, AABB modelAABB, OBB modelOBB)
        {
            _baseEntity = baseEntity;
            _modelAABB = modelAABB;
            _modelOBB = modelOBB;
        }

        public AABB AABB
        {
            get => _aabb;
            set => _aabb = value;
        }

        public OBB OBB
        {
            get => _obb; 
            set => _obb = value;
        }

        public bool IsVisible
        {
            get => _visible;
            set => _visible = value;
        }

        public AABB ModelAABB
        {
            get => _modelAABB; 
            set => _modelAABB = value;
        }
        
        public OBB ModelOBB
        {
            get => _modelOBB;
            set => _modelOBB = value;
        }

        public virtual void UpdateBoundingBox()
        {
            
        }
    }
}
