using System;
using Geometry;
using OpenGL;
using Physics.Collision;
using ZetaExt;

namespace Physics
{
    public class RigidPlane : RigidBody
    {
        Vertex3f _normal;
        float _width;
        float _height;
        bool _isHalfPlane;

        public Vertex3f Normal
        {
            get => _normal;
        }

        public float Width
        {
            get => _width; 
            set => _width = value;
        }

        public float Height
        {
            get => _height;
            set => _height = value;
        }

        public bool IsHalfPlane
        {
            get => _isHalfPlane;
            set => _isHalfPlane = value;
        }

        public RigidPlane(Matter matter, Vertex3f normal, Vertex3f position, float width, float height, bool isHalfPlane = true) : base(matter)
        {
            _normal = normal;
            _width = width;
            _height = height;
            _position = position;
            _isHalfPlane = isHalfPlane;

            Matrix3x3f rot = new Matrix3x3f();
            Vertex3f z = _normal.Normalized;
            Vertex3f x = z.Cross(Vertex3f.UnitZ).Normalized;
            Vertex3f y = z.Cross(x).Normalized;
            rot.Framed(x, y, z);
            _orientation = Quaternion4.ToQuaternion(rot);

            // 부피를 계산한다.
            _volume = _width * _height;
            CalculateMass();

            // 로컬공간 관성텐서를 지정한다.
            InitInertiaTensor();
            IsHalfPlane = isHalfPlane;
        }

        /// <summary>
        /// 강체의 모양에 따라 관성텐서를 지정한다.
        /// </summary>
        public override void InitInertiaTensor()
        {
            float w = _width * 0.5f;
            float h = _height * 0.5f;
            _inverseInertiaTensor = InertiaTensor.Cube(Mass, w, h, 0.01f).Inverse;
        }

        /// <summary>
        /// 강체의 모양에 따라 AABB 바운딩볼륨을 계산한다.
        /// </summary>
        public override void CalculateBoundingVolume()
        {
            Vertex3f[] p = new Vertex3f[8];
            float w = _width * 0.5f;
            float h = _height * 0.5f;
            p[0] = (_transformMatrix * new Vertex4f(-w, -h, -0.01f, 1.0f)).xyz();
            p[1] = (_transformMatrix * new Vertex4f(w, -h, -0.01f, 1.0f)).xyz();
            p[2] = (_transformMatrix * new Vertex4f(-w, h, -0.01f, 1.0f)).xyz();
            p[3] = (_transformMatrix * new Vertex4f(w, h, -0.01f, 1.0f)).xyz();
            p[4] = (_transformMatrix * new Vertex4f(-w, -h, 0.01f, 1.0f)).xyz();
            p[5] = (_transformMatrix * new Vertex4f(w, -h, 0.01f, 1.0f)).xyz();
            p[6] = (_transformMatrix * new Vertex4f(-w, h, 0.01f, 1.0f)).xyz();
            p[7] = (_transformMatrix * new Vertex4f(w, h, 0.01f, 1.0f)).xyz();

            Vertex3f minVec = Vertex3f.Min(p);
            Vertex3f maxVec = Vertex3f.Max(p);

            _aabb = new AABB(minVec, maxVec); 
        }

        public override void CalculatePrimitive()
        {
            CollisionPlane plane = _primitive as CollisionPlane;

            // 매 프레임마다 자세의 변환가 있을 수 있으므로 법선벡터를 업데이트한다.
            _normal = _transformMatrix.Column2.xyz();

            // 효율을 위해 반복적으로 생성하지 않는다.
            if (plane == null)
            {
                plane = new CollisionPlane(_normal, _position);
            }

            // 충돌 평면을 설정한다.
            plane.Normal = _normal;
            plane.Position = _position;
            plane.Distance = _position.Dot(_normal);
            _primitive = plane;
        }
    }
}
