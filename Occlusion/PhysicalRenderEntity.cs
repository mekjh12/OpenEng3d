using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using System.Collections.Generic;
using ZetaExt;

namespace Occlusion
{
    public class PhysicalRenderEntity : Entity, IOcclusionable
    {
        static float DISTANCE_LOD2 = 300.0f;
        static float DISTANCE_LOD1 = 150.0f;

        protected bool _isVisibleLOD = false;
        protected bool _isLod = false;
        protected int _lod = 0;
        
        protected Entity _entityLevel1;
        protected Entity _entityLevel2;
        protected Entity _renderEntity;

        public Entity RenderEntity => _renderEntity;

        protected bool _isVisibleRigidBody = false;
        protected bool _isCollisioTest = true;

        AABB _aabb = null;
        AABB _rigidBody = null;

        OcclusionComponent _occlusionComponent;
        
        public bool IsVisibleRigidBody
        {
            get => _isVisibleRigidBody; 
            set => _isVisibleRigidBody = value;
        }

        public bool IsCollisioTest
        {
            get => _isCollisioTest;
            set => _isCollisioTest= value;
        }

        public bool IsVisibleLOD
        {
            get => _isVisibleLOD;
            set => _isVisibleLOD = value;
        }

        public bool IsLOD
        {
            get => _isLod;
            set => _isLod = value;
        }

        public Entity EntityLevel1
        {
            get => _entityLevel1;
            set => _entityLevel1= value;
        }

        public Entity EntityLevel2
        {
            get => _entityLevel2;
            set => _entityLevel2= value;
        }

        public int LOD => _lod;

        public AABB RigidBody => _aabb;

        public void SetRigidBody(BoxOccluder boxOccluder, Vertex3f tightedVector, Vertex3f translated)
        {
            Vertex3f[] res = boxOccluder.AABB(tightedVector, translated);

            Matrix4x4f model =  base.LocalBindMatrix * boxOccluder.ModelMatrix;

            res = boxOccluder.AABB(model, tightedVector, translated);

            // 속도를 위해 기존것을 업데이트 하는 것으로 처리
            if (_rigidBody == null)
            {
                _rigidBody = new AABB(res[0], res[1]);
            }
            else
            {
                _rigidBody.LowerBound = res[0];
                _rigidBody.UpperBound = res[1];
            }
        }

        public AABB AABB
        {
            get
            {
                //Vertex3f[] res = _boxOccluder.AABB();
                throw new System.Exception();
                /*
                // 속도를 위해 기존것을 업데이트 하는 것으로 처리
                if (_aabb == null)
                {
                    _aabb = new AABB(res[0], res[1]);
                }
                else
                {
                    _aabb.LowerBound = res[0];
                    _aabb.UpperBound = res[1];
                }
                return _aabb;
                */
            }

            set
            {
                _aabb = value;
            }
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

        public PhysicalRenderEntity(string name, RawModel3d rawModel3D) : this(name, new RawModel3d[] { rawModel3D })
        {

        }

        public PhysicalRenderEntity(string name, List<TexturedModel> texturedModels) : this(name, texturedModels.ToArray())
        {

        }

        public PhysicalRenderEntity(string name, RawModel3d[] rawModel3Ds) : base(name, "",  rawModel3Ds)
        {
            // RawModel3d의 물체의 합집합으로 OBB를 만든다.
            OBB res = null;
            foreach (RawModel3d rawModel3D in rawModel3Ds)
            {
                res = (OBB)((res == null) ? rawModel3D.OBB : res.Union(rawModel3D.OBB));
            }

            // 컴포넌트 생성
            _occlusionComponent = new OcclusionComponent();
        }
        
        /// <summary>
        /// LOD를 업데이트한다.
        /// </summary>
        /// <param name="cameraPos"></param>
        public void Update(Camera camera)
        {
            // lod update
            Vertex3f cameraPos = camera.Position;

            if (_isLod)
            {
                float dist = (cameraPos - Pose.Position).Length();
                if (dist > DISTANCE_LOD2)
                {
                    _lod = 2;
                }
                else if (dist > DISTANCE_LOD1)
                {
                    _lod = 1;
                }
                else
                {
                    _lod = 0;
                }

                if (_lod == 0) _renderEntity = this;
                if (_lod == 1) _renderEntity = _entityLevel2;
                if (_lod == 2) _renderEntity = _entityLevel2;
            }
            else
            {
                _renderEntity = this;
                _lod = 0;
            }
        }

        public void GenBoxOccluder(OBB obb)
        {
            ((IOcclusionable)_occlusionComponent).GenBoxOccluder(obb);
        }
    }
}
