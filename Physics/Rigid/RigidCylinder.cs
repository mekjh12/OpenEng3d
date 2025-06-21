using Geometry;
using OpenGL;
using System;
using ZetaExt;

namespace Physics
{
    public class RigidCylinder: RigidBody
    {
        float _radius;
        float _height;

        public float Radius => _radius;
        public float Height => _height;

        public RigidCylinder(Matter matter, float radius, float height) : base(matter)
        {
            _radius = radius;
            _height = height;

            // 부피를 계산한다.
            _volume = (float)(Math.PI * _radius * _radius * _height);
            CalculateMass();

            // 로컬공간 관성텐서를 지정한다.
            InitInertiaTensor();
        }

        public override void InitInertiaTensor()
        {
            _inverseInertiaTensor = InertiaTensor.Cylinder(Mass, _radius, _height).Inverse;
        }

        /// <summary>
        /// 강체의 모양에 따라 AABB 바운딩볼륨을 계산한다.
        /// </summary>
        public override void CalculateBoundingVolume()
        {
            float _xscale = _radius;
            float _yscale = _radius;
            float _zscale = _height;

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
            
        }
    }
}
