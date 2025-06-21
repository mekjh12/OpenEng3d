using Common.Abstractions;
using OpenGL;

namespace Geometry
{
    public class OBBComponent : IOBBComponent
    {
        private OBB _obb;
        private OBB _localUnionObb;
        private bool _isVisibleOBB;
        private readonly BaseEntity _owner;

        public OBBComponent(BaseEntity owner, BaseModel3d[] models)
        {
            _owner = owner;
            InitializeOBB(models);
        }

        private void InitializeOBB(BaseModel3d[] models)
        {
            OBB res = null;
            foreach (var model in models)
            {
                //res = (res == null) ? model.OBB : res + model.OBB;
            }
            _localUnionObb = res;
        }

    }
}
