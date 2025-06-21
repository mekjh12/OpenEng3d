using Geometry;
using OpenGL;
using Physics.Collision;
using ZetaExt;

namespace Physics
{
    public class RigidCube : RigidBody
    {
        float _xscale;
        float _yscale;
        float _zscale;

        public float Xscale => _xscale;

        public float Yscale => _yscale;

        public float Zscale => _zscale;

        public RigidCube(Matter matter, float xscale = 1.0f, float yscale = 1.0f, float zscale = 1.0f) : base(matter)
        {
            _xscale = xscale;
            _yscale = yscale;
            _zscale = zscale;

            // 부피를 계산한다.
            _volume = 2.0f * _xscale * 2.0f * _yscale * 2.0f * _zscale;
            CalculateMass();

            // 로컬공간 관성텐서를 지정한다.
            InitInertiaTensor();
        }

        /// <summary>
        /// 강체의 모양에 따라 관성텐서를 지정한다.
        /// </summary>
        public override void InitInertiaTensor()
        {
            _inverseInertiaTensor = InertiaTensor.Cube(Mass, _xscale, _yscale, _zscale).Inverse;
        }

        /// <summary>
        /// 강체의 회전과 이동을 고려하여 AABB(Axis-Aligned Bounding Box) 바운딩 볼륨을 계산한다.
        /// 8개의 꼭지점을 변환행렬로 변환한 후 최소/최대 좌표를 구해 AABB를 생성한다.
        /// </summary>
        public override void CalculateBoundingVolume()
        {
            Vertex3f[] p = new Vertex3f[8];
            p[0] = (_transformMatrix * new Vertex4f(-_xscale, -_yscale, -_zscale, 1.0f)).xyz();
            p[1] = (_transformMatrix * new Vertex4f(_xscale, -_yscale, -_zscale, 1.0f)).xyz();
            p[2] = (_transformMatrix * new Vertex4f(-_xscale, _yscale, -_zscale, 1.0f)).xyz();
            p[3] = (_transformMatrix * new Vertex4f(_xscale, _yscale, -_zscale, 1.0f)).xyz();
            p[4] = (_transformMatrix * new Vertex4f(-_xscale, -_yscale, _zscale, 1.0f)).xyz();
            p[5] = (_transformMatrix * new Vertex4f(_xscale, -_yscale, _zscale, 1.0f)).xyz();
            p[6] = (_transformMatrix * new Vertex4f(-_xscale, _yscale, _zscale, 1.0f)).xyz();
            p[7] = (_transformMatrix * new Vertex4f(_xscale, _yscale, _zscale, 1.0f)).xyz();

            Vertex3f minVec = Vertex3f.Min(p);
            Vertex3f maxVec = Vertex3f.Max(p);

            _aabb = new AABB(minVec, maxVec);
        }

        /// <summary>
        /// 직육면체 강체의 충돌 검사를 위한 CollisionBox 프리미티브를 생성하거나 갱신한다.
        /// 매 프레임마다 새로운 CollisionBox를 생성하지 않고 기존 것을 재사용하여 성능을 개선한다.
        /// </summary>
        public override void CalculatePrimitive()
        {
            CollisionBox collisionBox = _primitive as CollisionBox;

            // 효율을 위해 반복적으로 생성하지 않는다.
            if (collisionBox == null)
            {
                collisionBox = new CollisionBox(_position, new Vertex3f(_xscale, _yscale, _zscale));
                collisionBox.RigidBody = this;
            }

            // 충돌 박스를 설정한다.
            collisionBox.Position = _position;
            _primitive = collisionBox;
        }
    }
}
