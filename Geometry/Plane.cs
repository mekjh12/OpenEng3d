using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    public struct Plane
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        private Vertex3f _normal;

        public Vertex4f Vertex4f => new Vertex4f(X, Y, Z, W);
        public Vertex4f ToVertex4f => new Vertex4f(X, Y, Z, W);
        public Vertex3f Normal => _normal;
        public Plane FlipPlane => new Plane(-X, -Y, -Z, -W);

        /// <summary>
        /// 생성자<br/>
        /// 법선벡터(정규화할 필요는 없다.) 생성시 자동으로 정규화한다.
        /// </summary>
        /// <param name="nx">법선벡터x</param>
        /// <param name="ny">법선벡터y</param>
        /// <param name="nz">법선벡터z</param>
        /// <param name="distance">원점으로부터 평면이 법선벡터 방향으로 떨어진 거리</param>
        public Plane(float nx, float ny, float nz, float distance)
        {
            _normal = new Vertex3f(nx, ny, nz).Normalized;
            X = _normal.x;
            Y = _normal.y;
            Z = _normal.z;
            W = distance;
        }

        /// <summary>
        /// 생성자<br/>
        /// 법선벡터(정규화할 필요는 없다.) 생성시 자동으로 정규화한다. 
        /// </summary>
        /// <param name="normal">법선벡터</param>
        /// <param name="distance">원점으로부터 평면이 법선벡터 방향으로 떨어진 거리</param>
        public Plane(Vertex3f normal, float distance)
        {
            _normal = normal.Normalized;
            X = _normal.x;
            Y = _normal.y;
            Z = _normal.z;
            W = distance;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="normal">법선벡터(정규화할 필요는 없다.) 생성시 자동으로 정규화한다.</param>
        /// <param name="position"></param>
        public Plane(Vertex3f normal, Vertex3f position)
        {
            _normal = normal.Normalized;
            X = _normal.x;
            Y = _normal.y;
            Z = _normal.z;
            W = -_normal.Dot(position);          
        }

        /// <summary>
        /// 반시계 방향으로 지정한다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public Plane(Vertex3f a, Vertex3f b, Vertex3f c)
        {
            Vertex3f ab = b - a;
            Vertex3f ac = c - a;
            _normal = ab.Cross(ac).Normalized;
            X = _normal.x;
            Y = _normal.y;
            Z = _normal.z;
            W = -_normal.Dot(a);
        }

        /// <summary>
        /// 평면의 앞뒤만 서로 바꾼다.
        /// </summary>
        public void Flip()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
            W = -W;
            _normal = new Vertex3f(X, Y, Z);
        }

        /// <summary>
        /// 한 점이 평면의 앞쪽에 있는지 여부를 반환한다.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsFront(Vertex3f point)
        {
            return ((this * point) > 0.0f);
        }

        public override string ToString() => $"plane n=({X},{Y},{Z}), w={W}";

        public static implicit operator float[](Plane a)
        {
            return new float[4] { a.X, a.Y, a.Z, a.W };
        }

        public static implicit operator Vertex4f(Plane a)
        {
            return new Vertex4f(a.X, a.Y, a.Z, a.W);
        }

        public static Plane operator *(Plane plane, Matrix4x4f mat)
        {
            // plane * M  (1,4)*(4,4)=(1,4)
            // M^t * plane  (4,4)*(4,1)=(4,1)
            mat = mat.Transposed;
            Vertex4f v = mat * plane.ToVertex4f;
            return new Plane(v.x, v.y, v.z, v.w);
        }

        public static float operator *(Plane plane, Vertex4f point)
        {
            return plane.X * point.x + plane.Y * point.y + plane.Z * point.z + plane.W * point.w;
        }

        public static float operator *(Plane plane, Vertex3f point)
        {
            return plane.X * point.x + plane.Y * point.y + plane.Z * point.z + plane.W;
        }
    }
}
