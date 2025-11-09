using Geometry;
using OpenGL;
using System;
using ZetaExt;

namespace BoundingVolume
{
    public class AABB
    {
        const float TIGHT_SCALED_SIZE = 1.01f;

        //OcclusionEntity _occlusionEntity;
        Vertex3f _lower;
        Vertex3f _upper;
        //Node _node;
        bool _useEnhanceBox;
        Vertex3f _color;
        bool _collided = false;

        public bool Collided
        {
            get => _collided;
            set => _collided = value;
        }

        public override string ToString()
        {
            return $"{_lower} {_upper}";
        }

        public AABB Clone()
        {
            AABB result = new AABB();
            result._lower = _lower;
            result._upper = _upper;
            result._useEnhanceBox = _useEnhanceBox;
            //result._occlusionEntity = _occlusionEntity;
            return result;
        }

        /// <summary>
        /// AABB의 6개의 평면을 반환한다.
        /// </summary>
        public Plane[] Planes
        {
            get
            {
                Plane[] result = new Plane[6];
                result[0] = new Plane(Vertex3f.UnitX, _lower);
                result[1] = new Plane(-Vertex3f.UnitX, _upper);
                result[2] = new Plane(Vertex3f.UnitY, _lower);
                result[3] = new Plane(-Vertex3f.UnitY, _upper);
                result[4] = new Plane(Vertex3f.UnitZ, _lower);
                result[5] = new Plane(-Vertex3f.UnitZ, _upper);
                return result;
            }
        }

        public Vertex3f Color
        {
            get => _color;
            set => _color = value;
        }

        public Vertex3f Center => (_lower + _upper) * 0.5f;

        public Vertex3f Size => _upper - _lower;

        public Vertex3f HalfSize => (_upper - _lower) * 0.5f;

        public bool UseEnhanceBox
        {
            get => _useEnhanceBox;
            set => _useEnhanceBox = value;
        }

        /*
        public OcclusionEntity OcclusionEntity
        {
            get => _occlusionEntity;
            set => _occlusionEntity = value;
        }

        public Node Node
        {
            get => _node;
            set => _node = value;
        }
        */

        public Vertex3f LowerBound
        {
            get => _lower;
            set => _lower = value;
        }

        public Vertex3f UpperBound
        {
            get => _upper;
            set => _upper = value;
        }

        public float Area
        {
            get
            {
                Vertex3f delta = _upper - _lower;
                float x = Math.Abs(delta.x);
                float y = Math.Abs(delta.y);
                float z = Math.Abs(delta.z);
                return 2.0f * (x * y + y * z + z * x);
            }
        }

        public Matrix4x4f ModelMatrix
        {
            get
            {
                Vertex3f size = Size * 0.5f;
                Vertex3f center = Center;
                Matrix4x4f S = Matrix4x4f.Scaled(TIGHT_SCALED_SIZE * size.x, TIGHT_SCALED_SIZE * size.y, TIGHT_SCALED_SIZE * size.z);
                Matrix4x4f T = Matrix4x4f.Translated(center.x, center.y, center.z);
                Matrix4x4f R = Matrix4x4f.Identity;
                return T * R * S; // 순서는 S->R->T
            }
        }

        public AABB(bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _color = Rand.NextColor3f;
        }

        public AABB(float x1, float y1, float z1, float x2, float y2, float z2, bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _lower = new Vertex3f(x1, y1, z1);
            _upper = new Vertex3f(x2, y2, z2);
            _color = Rand.NextColor3f;
        }

        public AABB(Vertex3f lower, Vertex3f upper, bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _lower = lower;
            _upper = upper;
            _color = Rand.NextColor3f;
        }

        public static AABB operator +(AABB a, AABB b)
        {
            return new AABB(min(a.LowerBound, b.LowerBound), max(a.UpperBound, b.UpperBound));
            // 함수의 내부함수 부분이다.
            Vertex3f min(Vertex3f v1, Vertex3f v2) => new Vertex3f(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y), Math.Min(v1.z, v2.z));
            Vertex3f max(Vertex3f v1, Vertex3f v2) => new Vertex3f(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y), Math.Max(v1.z, v2.z));
        }

        /// <summary>
        /// 다면체 안에 AABB가 포함되는지 여부를 반환한다.
        /// </summary>
        /// <param name="planes">다면체 평면들</param>
        /// <returns></returns>
        public bool Included(Plane[] planes)
        {
            if (planes == null) return true;
            //Vertex3f size = Size;
            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                float rg = BoxRadius(plane.Normal);
                float distance = plane * Center;
                if (distance - rg < 0) return false;
            }
            return true;
        }

        /// <summary>
        /// 다면체 안에 AABB가 접촉(보이)하는지 판별한다.
        /// </summary>
        /// <param name="planes">다면체 평면들</param>
        /// <returns></returns>
        public bool Visible(Plane[] planes)
        {
            if (planes == null) return true;

            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                float rg = BoxRadius(plane.Normal);
                float distance = plane * Center;
                if (distance + rg < 0.0f) return false; // 속도를 위해 d<-g
            }
            return true;
        }

        /// <summary>
        /// AABB 상자가 충돌했는지 반환한다.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool CollisionTest(AABB box)
        {
            return Visible(box.Planes);
        }

        /// <summary>
        /// box중심으로부터 평면의 법선벡터 방향으로 가장 먼 점까지의 거리
        /// </summary>
        /// <param name="normal"></param>
        /// <returns></returns>
        protected float BoxRadius(Vertex3f normal)
        {
            Vertex3f[] axis = { Vertex3f.UnitX, Vertex3f.UnitY, Vertex3f.UnitZ };
            Vertex3f size = Size * 0.5f;

            return Math.Abs(normal.Dot(axis[0] * size.x)) +
                     Math.Abs(normal.Dot(axis[1] * size.y)) +
                      Math.Abs(normal.Dot(axis[2] * size.z));
        }

    }
}
