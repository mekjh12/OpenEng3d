using Geometry;
using OpenGL;
using Physics.Collision;
using System;
using ZetaExt;

namespace Physics
{
    public class RigidSphere: RigidBody
    {
        float _radius;

        public float Radius => _radius;

        public RigidSphere(Matter matter, float radius) : base(matter)
        {
            _radius = radius;

            // 부피를 계산한다.
            _volume = (float)(1.3333333f * Math.PI * _radius * _radius * _radius);
            CalculateMass();

            // 로컬공간 관성텐서를 지정한다.
            InitInertiaTensor();
        }

        public override void InitInertiaTensor()
        {
            _inverseInertiaTensor = InertiaTensor.Sphere(Mass, _radius).Inverse;
        }

        /// <summary>
        /// 강체의 모양에 따라 AABB 바운딩볼륨을 계산한다.
        /// </summary>
        public override void CalculateBoundingVolume()
        {
            float _xscale = _radius;
            float _yscale = _radius;
            float _zscale = _radius;

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

        public override void CalculatePrimitive()
        {
            CollisionSphere sphere = _primitive as CollisionSphere;

            // 효율을 위해 반복적으로 생성하지 않는다.
            if (sphere == null)
                sphere = new CollisionSphere(_position);

            // 충돌구를 설정한다.
            sphere.Radius = _radius;
            sphere.Position = _position;
            _primitive = sphere;
        }
    }
}
