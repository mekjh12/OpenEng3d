using Geometry;
using OpenGL;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// OBB의 S, T, U 벡터를 이용하여 4x4행렬을 만든다.
    /// 
    ///      __________  
    ///     /         /| 
    ///    /         / | 
    ///   /________ /  | 
    ///  |         |u  |
    ///  |    u t  |   |
    ///  |    |/_s |   |
    ///  |    c    |  / t
    ///  |         | /
    ///  |_________|/
    ///  p         s 
    ///  
    /// c = Obb의 중심
    /// s, t, u는 방향벡터로서 중심으로 부터 OBB의 크기의 반인 벡터이다.
    ///   
    /// 모델행렬은 X=s, Y=t, Z=u, P=c이다.
    /// </summary>
    public class BoxOccluder
    {
        Vertex3f _size;

        /// <summary>
        /// 자신의 로컬모델행렬
        /// </summary>
        Matrix4x4f _matOccluder;

        /// <summary>
        /// OBB의 크기 벡터
        /// </summary>
        public Vertex3f Size => _size;

        /// <summary>
        /// 자신의 OBB 로컬모델행렬 <br/>
        /// Column0=s normalized, <br/>
        /// Column1=t normalized, <br/>
        /// Column2=u normalized, <br/>
        /// Column3=p <br/>
        /// </summary>
        public Matrix4x4f ModelMatrix => _matOccluder;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="obb">OBB</param>
        public BoxOccluder(OBB obb)
        {
            if (obb == null) return;

            _size = obb.Size * 2.0f;

            Vertex3f size = obb.Size;
            Vertex3f s = obb.Axis[0].Normalized;
            Vertex3f t = obb.Axis[1].Normalized;
            Vertex3f u = obb.Axis[2].Normalized;

            Vertex3f leftBottomBelowPoint = obb.Center - s * size.x - t * size.y - u * size.z;

            _matOccluder = Matrix4x4f.Identity;
            _matOccluder[0, 0] = s.x;
            _matOccluder[0, 1] = s.y;
            _matOccluder[0, 2] = s.z;

            _matOccluder[1, 0] = t.x;
            _matOccluder[1, 1] = t.y;
            _matOccluder[1, 2] = t.z;

            _matOccluder[2, 0] = u.x;
            _matOccluder[2, 1] = u.y;
            _matOccluder[2, 2] = u.z;

            _matOccluder[3, 0] = leftBottomBelowPoint.x;
            _matOccluder[3, 1] = leftBottomBelowPoint.y;
            _matOccluder[3, 2] = leftBottomBelowPoint.z;
        }

        /// <summary>
        /// 월드공간의 AABB의 8개의 버텍스
        /// </summary>
        /// <returns></returns>
        public Vertex3f[] AABB()
        {
            Vertex3f[] vertices = new Vertex3f[8];

            Vertex3f p = _matOccluder.Column3.xyz();
            Vertex3f x = _matOccluder.Column0.xyz() * _size.x;
            Vertex3f y = _matOccluder.Column1.xyz() * _size.y;
            Vertex3f z = _matOccluder.Column2.xyz() * _size.z;

            vertices[0] = p;
            vertices[1] = p + x;
            vertices[2] = p + x + y;
            vertices[3] = p + y;

            vertices[4] = p + z;
            vertices[5] = p + x + z;
            vertices[6] = p + x + y + z;
            vertices[7] = p + y + z;

            return new Vertex3f[] { Vertex3f.Min(vertices), Vertex3f.Max(vertices) };
        }

        public Vertex3f[] AABB(Vertex3f tightedVector, Vertex3f translated)
        {
            Vertex3f[] vertices = new Vertex3f[8];

            Vertex3f p = _matOccluder.Column3.xyz();
            Vertex3f x = _matOccluder.Column0.xyz() * _size.x * tightedVector.x;
            Vertex3f y = _matOccluder.Column1.xyz() * _size.y * tightedVector.y;
            Vertex3f z = _matOccluder.Column2.xyz() * _size.z * tightedVector.z;
            Vertex3f t = (_matOccluder.Column0.xyz() * _size.x + _matOccluder.Column1.xyz() * _size.y) * 0.5f;

            vertices[0] = p + t + translated;
            vertices[1] = p + x + t + translated;
            vertices[2] = p + x + y + t + translated;
            vertices[3] = p + y + t + translated;

            vertices[4] = p + z + t + translated;
            vertices[5] = p + x + z + t + translated;
            vertices[6] = p + x + y + z + t + translated;
            vertices[7] = p + y + z + t + translated;

            return new Vertex3f[] { Vertex3f.Min(vertices), Vertex3f.Max(vertices) };
        }

        public Vertex3f[] AABB(Matrix4x4f model, Vertex3f tightedVector, Vertex3f translated)
        {
            Vertex3f[] vertices = new Vertex3f[8];

            Vertex3f p = model.Column3.xyz();
            Vertex3f x = model.Column0.xyz() * _size.x * tightedVector.x;
            Vertex3f y = model.Column1.xyz() * _size.y * tightedVector.y;
            Vertex3f z = model.Column2.xyz() * _size.z * tightedVector.z;
            Vertex3f t = (model.Column0.xyz() * _size.x + model.Column1.xyz() * _size.y) * 0.5f;

            vertices[0] = p + t + translated;
            vertices[1] = p + x + t + translated;
            vertices[2] = p + x + y + t + translated;
            vertices[3] = p + y + t + translated;

            vertices[4] = p + z + t + translated;
            vertices[5] = p + x + z + t + translated;
            vertices[6] = p + x + y + z + t + translated;
            vertices[7] = p + y + z + t + translated;

            return new Vertex3f[] { Vertex3f.Min(vertices), Vertex3f.Max(vertices) };
        }

    }
}