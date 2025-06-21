using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    /// <summary>
    /// 평면을 나타내는 클래스이다.
    /// </summary>
    public class Plane: GeometricElement
    {        
        float w;

        public float W => w;


        public Vertex4f Vertex4f => new Vertex4f(x, y, z, w);

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
            Vertex3f normal = new Vertex3f(nx, ny, nz);
            normal = normal.Normalized;
            x = normal.x;
            y = normal.y;
            z = normal.z;
            w = distance;
        }

        /// <summary>
        /// 생성자<br/>
        /// 법선벡터(정규화할 필요는 없다.) 생성시 자동으로 정규화한다. 
        /// </summary>
        /// <param name="normal">법선벡터</param>
        /// <param name="distance">원점으로부터 평면이 법선벡터 방향으로 떨어진 거리</param>
        public Plane(Vertex3f normal, float distance)
        {
            normal = normal.Normalized;
            x = normal.x;
            y = normal.y;
            z = normal.z;
            w = distance;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="normal">법선벡터(정규화할 필요는 없다.) 생성시 자동으로 정규화한다.</param>
        /// <param name="position"></param>
        public Plane(Vertex3f normal, Vertex3f position)
        {
            normal = normal.Normalized;
            x = normal.x;
            y = normal.y;
            z = normal.z;
            w = -normal.Normalized.Dot(position);
        }

        /// <summary>
        /// 반시계 방향으로 지정한다.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public Plane(Vertex3f a, Vertex3f b, Vertex3f c)
        {
            Vertex3f normal = (b - a).Cross(c - b).Normalized;
            x = normal.x;
            y = normal.y;
            z = normal.z;
            w = -normal.Normalized.Dot(a);
        }

        public Vertex4f ToVertex4f => new Vertex4f(x, y, z, w);

        public Vertex3f Normal => new Vertex3f(x, y, z).Normalized;

        /// <summary>
        /// 평면의 앞뒤만 서로 바꾼다.
        /// </summary>
        public void Flip()
        {
            x = -x;
            y = -y;
            z = -z;
            w = -w;
        }

        /// <summary>
        /// 평면의 앞뒤를 뒤집은 평면을 반환한다.
        /// </summary>
        public Plane FlipPlane => new Plane(-x, -y, -z, -w);

        /// <summary>
        /// 한 점이 평면의 앞쪽에 있는지 여부를 반환한다.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsFront(Vertex3f point) => ((this * point) > 0.0f);

        public override string ToString() => $"plane n=({x},{y},{z}), w={w}";

        /// <summary>
        /// 평면의 
        /// </summary>
        /// <param name="a"></param>
        public static implicit operator float[](Plane a)
        {
            return new float[4] { a.x, a.y, a.z, a.w };
        }

        public static implicit operator Vertex4f(Plane a)
        {
            return new Vertex4f(a.x, a.y, a.z, a.w);
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
            return plane.x * point.x + plane.y * point.y + plane.z * point.z + plane.w * point.w;
        }

        public static float operator *(Plane plane, Vertex3f point)
        {
            return plane.x * point.x + plane.y * point.y + plane.z * point.z + plane.w;
        }

        [Obsolete("이 함수는 더 이상 사용하지 않습니다.")]
        public float Dot(Vertex4f vertex)
        {
            return Normal.Dot(vertex.xyz()) + w * vertex.w;
        }

    }
}
