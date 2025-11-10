using Common.Abstractions;
using FastMath;
using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;


namespace Geometry
{
    /// <summary>
    /// 
    /// 오브젝트 공간에서의 좌표공간이다.
    /// 
    ///       ------------
    ///      /           /|
    ///     /           / |
    ///     -----------/  | 
    ///    |           |  |
    ///    |     Z Y   |  |
    ///    |     |/__  |  /
    ///    |     C   X | / 
    ///    |___________|/
    /// 
    /// </summary>
    public class OBB: BoundVolume
    {
        // 물체와 완전히 동일한 바운딩 상자는 지그재그가 발생하여 조금 여유가 있는 상자로 만들기 위한 상수
        const float TIGHT_SCALED_SIZE = 1.01f;

        Vertex3f _center;
        Vertex3f _size;
        Vertex3f[] _axis;

        /// <summary>
        /// 오브젝트 공간에서의 모델행렬이다.
        /// </summary>
        public override Matrix4x4f ModelMatrix
        {
            get
            {
                Matrix4x4f S = Matrix4x4f.Scaled(TIGHT_SCALED_SIZE * _size.x, TIGHT_SCALED_SIZE * _size.y, TIGHT_SCALED_SIZE * _size.z);
                Matrix4x4f T = Matrix4x4f.Translated(_center.x, _center.y, _center.z);
                Matrix4x4f R = Matrix4x4f.Identity;
                Matrix4x4F.Column(ref R, 0, _axis[0].Normalized);
                Matrix4x4F.Column(ref R, 1, _axis[1].Normalized);
                Matrix4x4F.Column(ref R, 2, _axis[2].Normalized);
                return T * R * S; // 순서는 S->R->T
            }
        }

        /// <summary>
        /// 오브젝트 공간에서의 버텍스들이다.
        /// </summary>
        public override Vertex3f[] Vertices
        {
            get
            {
                Vertex3f s = _axis[0] * _size.x;
                Vertex3f t = _axis[1] * _size.y;
                Vertex3f u = _axis[2] * _size.z;

                Vertex3f[] vertices = new Vertex3f[8];
                vertices[0] = _center - s - t - u;
                vertices[1] = _center + s - t - u;
                vertices[2] = _center + s + t - u;
                vertices[3] = _center - s + t - u;
                vertices[4] = _center - s - t + u;
                vertices[5] = _center + s - t + u;
                vertices[6] = _center + s + t + u;
                vertices[7] = _center - s + t + u;
                return vertices;
            }
        }

        /// <summary>
        /// OBB로부터 AABB를 생성합니다.
        /// </summary>
        /// <returns>AABB 객체</returns>
        public AABB ToAABB()
        {
            // OBB의 모든 꼭지점을 가져옴
            Vertex3f[] vertices = Vertices;

            // 초기값 설정
            Vertex3f min = vertices[0];
            Vertex3f max = vertices[0];

            // 모든 꼭지점을 순회하면서 최소/최대 좌표 갱신
            for (int i = 1; i < vertices.Length; i++)
            {
                min.x = MathFast.Min(min.x, vertices[i].x);
                min.y = MathFast.Min(min.y, vertices[i].y);
                min.z = MathFast.Min(min.z, vertices[i].z);

                max.x = MathFast.Max(max.x, vertices[i].x);
                max.y = MathFast.Max(max.y, vertices[i].y);
                max.z = MathFast.Max(max.z, vertices[i].z);
            }

            // AABB의 중심점과 크기 계산
            Vertex3f center = (min + max) * 0.5f;
            Vertex3f size = (max - min) * 0.5f;

            // AABB 생성 및 반환
            return new AABB(center, size);
        }

        public override Vertex3f Center
        {
            get => _center;
            set => _center = value;
        }

        public override Vertex3f Size
        {
            get => _size;
            set => _size = value;
        }

        public Vertex3f[] Axis
        {
            get => _axis;
            set => _axis = value;
        }

        public float Radius =>
            (float)MathFast.Sqrt(_size.x * _size.x + _size.y * _size.y + _size.z * _size.z) * 0.5f;

        public override float Area =>
            2.0f * (_size.x * _size.y + _size.y * _size.z + _size.z * _size.x);

        /// <summary>
        /// OBB 바운딩 상자를 생성한다.
        /// </summary>
        /// <param name="center">바운딩 상자의 오브젝트 원점의 월드좌표</param>
        /// <param name="size">가로, 세로, 높이</param>
        /// <param name="axis">오브젝트 공간의 원점에서의 직교하는 세 단위축</param>
        public OBB(Vertex3f center, Vertex3f size, Vertex3f[] axis)
        {
            _center = center;
            _size = size;
            _axis = axis;
            _color = Rand.NextColor3f;
        }

        public static OBB ZeroSizeOBB
        {
            get
            {
                return new OBB(Vertex3f.Zero, Vertex3f.One * 0.0001f, new Vertex3f[3] { Vertex3f.UnitX, Vertex3f.UnitY, Vertex3f.UnitZ });
            }
        }

        public override string ToString()
        {
            return $"center {_center}, size {_size}";
        }

        /// <summary>
        /// 공간의 점들로 부터 OBB를 만든다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static OBB GenerateOBB(Vertex3f[] vertices)
        {
            OBBUtility.CalculateOBB(vertices, out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);
            return new OBB(center, size, axis);
        }

        public override BoundVolume Clone()
        {            
            return new OBB(_center, _size, _axis);
        }

        /// <summary>
        /// 두 OBB의 오브젝트 공간의 원점이 서로 같을 때 새로운 두 OBB의 합의 OBB를 반환한다.
        /// </summary>
        public override BoundVolume Union(BoundVolume boundVoulume)
        {
            List<Vertex3f> list = new List<Vertex3f>();
            BoundVolume right = boundVoulume as BoundVolume;
            list.AddRange(this.Vertices);
            list.AddRange(right.Vertices);

            OBB obb = null;
            OBBUtility.CalculateOBB(list.ToArray(), out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);
            obb = new OBB(center, size, axis);
            return obb;
        }

        public override BoundVolume Intersect(BoundVolume boundVoulume)
        {
            throw new NotImplementedException("추후 구현");
        }
    }
}
