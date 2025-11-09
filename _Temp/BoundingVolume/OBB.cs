using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace BoundingVolume
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
    public class OBB
    {
        // 물체와 완전히 동일한 바운딩 상자는 지그재그가 발생하여 조금 여유가 있는 상자로 만들기 위한 상수
        const float TIGHT_SCALED_SIZE = 1.01f;

        Vertex3f _center;
        Vertex3f _size;
        Vertex3f[] _axis;
        Vertex3f _color;

        /// <summary>
        /// 오브젝트 공간에서의 모델행렬이다.
        /// </summary>
        public Matrix4x4f ModelMatrix
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
        protected Vertex3f[] Vertices
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

        public Vertex3f Color => _color;

        public Vertex3f Center
        {
            get => _center;
            set => _center = value;
        }

        public Vertex3f Size => _size;

        public Vertex3f[] Axis => _axis;

        public float Radius =>
            (float)Math.Sqrt(_size.x * _size.x + _size.y * _size.y + _size.z * _size.z) * 0.5f;

        public float Area =>
            2.0f * (_size.x * _size.y + _size.y * _size.z + _size.z * _size.x);

        /// <summary>
        /// OBB 바운딩 상자를 생성한다.
        /// </summary>
        /// <param name="center">바운딩 상자의 오브젝트 원점의 월드좌표</param>
        /// <param name="size">가로, 세로, 높이</param>
        /// <param name="axis">오브젝트 공간의 원점에서의 직교하는 세 축</param>
        public OBB(Vertex3f center, Vertex3f size, Vertex3f[] axis)
        {
            _center = center;
            _size = size;
            _axis = axis;
            _color = Rand.NextColor3f;
        }

        public override string ToString()
        {
            return $"center {_center}, size {_size}";
        }

        /// <summary>
        /// 입력된 버텍스에서 중심과 oriented aligned axis 3축, 축에 대한 크기를 반환한다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool CalculateOBB(Vertex3f[] vertices, out Vertex3f center, out Vertex3f size, out Vertex3f[] axis)
        {
            center = Vertex3f.Zero;
            size = Vertex3f.One;
            axis = new Vertex3f[3];

            CalculateDiameter(vertices, out int a, out int b);
            GetPrimaryBoxDirection(vertices, a, b, out Vertex3f[] primatryDirection);

            float area = float.MaxValue;

            // Loop over all candidates for primary axis.
            for (int k = 0; k < 9; k++)
            {
                Vertex3f s = primatryDirection[k].Normalized;
                CalculateSecondaryDiameter(vertices, s, out int sa, out int sb);
                GetSecondaryBoxDirection(vertices, s, sa, sb, out Vertex3f[] secondaryDirection);

                // Loop over all candidates for secondary axis.
                for (int j = 0; j < 5; j++)
                {
                    Vertex3f t = secondaryDirection[j].Normalized;
                    Vertex3f u = s.Cross(t);

                    float smin = s.Dot(vertices[0]), smax = smin;
                    float tmin = t.Dot(vertices[0]), tmax = tmin;
                    float umin = u.Dot(vertices[0]), umax = umin;

                    for (int i = 1; i < vertices.Length; i++)
                    {
                        float ds = s.Dot(vertices[i]);
                        float dt = t.Dot(vertices[i]);
                        float du = u.Dot(vertices[i]);
                        smin = Math.Min(smin, ds);
                        tmin = Math.Min(tmin, dt);
                        umin = Math.Min(umin, du);
                        smax = Math.Max(smax, ds);
                        tmax = Math.Max(tmax, dt);
                        umax = Math.Max(umax, du);
                    }

                    float hx = (smax - smin) * 0.5f;
                    float hy = (tmax - tmin) * 0.5f;
                    float hz = (umax - umin) * 0.5f;

                    // Calculate one-eighth surface area and see if it's better.
                    float m = hx * hy + hy * hz + hz * hx;
                    if (m < area && m > 0)
                    {
                        center = (s * (smin + smax) + t * (tmin + tmax) + u * (umin + umax)) * 0.5f;
                        size = new Vertex3f(hx, hy, hz);
                        axis[0] = s.Normalized;
                        axis[1] = t.Normalized;
                        axis[2] = u.Normalized;
                        area = m;
                    }
                }
            }

            // Checked oriented bounding box is validiate.
            return CheckCalculateOBB(vertices, center, size * 1.01f, axis);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float CalculateDiameter(Vertex3f[] vertices, out int a, out int b)
        {
            int vertexCount = vertices.Length;
            const int kDirectionCount = 13;
            Vertex3f[] directions = new Vertex3f[kDirectionCount]
            {
                new Vertex3f(1,0,0),  new Vertex3f(0,1,0),  new Vertex3f(0,0,1),
                new Vertex3f(1,1,0),  new Vertex3f(1,0,1),  new Vertex3f(0,1,1),
                new Vertex3f(1,-1,0), new Vertex3f(1,0,-1), new Vertex3f(0,1,-1),
                new Vertex3f(1,1,1),  new Vertex3f(1,-1,1), new Vertex3f(1,1,-1), new Vertex3f(1,-1,-1)
            };

            float[] dmin = new float[kDirectionCount];
            float[] dmax = new float[kDirectionCount];
            int[] imin = new int[kDirectionCount];
            int[] imax = new int[kDirectionCount];

            // Find min and max dot product for each direction and record vertex indices.
            for (int j = 0; j < kDirectionCount; j++)
            {
                dmin[j] = dmax[j] = vertices[0].Dot(directions[j]);
                imin[j] = imax[j] = 0;

                for (int i = 1; i < vertexCount; i++)
                {
                    float d = vertices[i].Dot(directions[j]);
                    if (d < dmin[j]) { dmin[j] = d; imin[j] = i; }
                    else if (d > dmax[j]) { dmax[j] = d; imax[j] = i; }
                }
            }

            // Find direction for which vertices at min and max extents are furthest apart.
            float d2 = (vertices[imax[0]] - vertices[imin[0]]).Norm();
            int k = 0;
            for (int j = 1; j < kDirectionCount; j++)
            {
                float m2 = (vertices[imax[j]] - vertices[imin[j]]).Norm();
                if (m2 > d2) { d2 = m2; k = j; }
            }

            a = imin[k];
            b = imax[k];

            return d2;
        }

        /// <summary>
        /// 입력된 버텍스들로 만들어진 OBB가 유효한지 검사한다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool CheckCalculateOBB(Vertex3f[] vertices, Vertex3f center, Vertex3f size, Vertex3f[] axis)
        {
            Plane[] planes = new Plane[6];
            Vertex3f ax = axis[0].Normalized * size.x;
            Vertex3f ay = axis[1].Normalized * size.y;
            Vertex3f az = axis[2].Normalized * size.z;

            planes[0] = new Plane(-ax, center + ax);
            planes[2] = new Plane(ax, center);
            planes[1] = new Plane(-ay, center + ay);
            planes[3] = new Plane(ay, center);
            planes[4] = new Plane(-az, center + az);
            planes[5] = new Plane(az, center);

            float epsilon = 0.001f;
            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                for (int j = 0; j < vertices.Length; j++)
                {
                    float dot = plane * vertices[j];
                    if (dot < -epsilon)
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        private static void CalculateSecondaryDiameter(Vertex3f[] vertices, Vertex3f axis, out int a, out int b)
        {
            const int kDirectionCount = 4;
            Vertex2f[] direction = new Vertex2f[kDirectionCount]
            {
                new Vertex2f(1, 0), new Vertex2f(0, 1), new Vertex2f(1, 1), new Vertex2f(1, -1)
            };

            float[] dmin = new float[kDirectionCount];
            float[] dmax = new float[kDirectionCount];
            int[] imin = new int[kDirectionCount];
            int[] imax = new int[kDirectionCount];

            // Create vectors x and y perpendicular to the primary axis.
            Vertex3f x = MakePerpendicularVector(axis);
            Vertex3f y = axis.Cross(x);

            // Find min and max dot products for each direction and record vertex indices.
            for (int j = 0; j < kDirectionCount; j++)
            {
                Vertex3f t = x * direction[j].x + y * direction[j].y;
                dmin[j] = dmax[j] = t.Dot(vertices[0]);
                imin[j] = imax[j] = 0;

                for (int i = 1; i < vertices.Length; i++)
                {
                    float d = t.Dot(vertices[j]);
                    if (d < dmin[j]) { dmin[j] = d; imin[j] = i; }
                    else if (d > dmax[j]) { dmax[j] = d; imax[j] = i; }
                }
            }

            // Find diameter in plane perpendicular to primary axis.
            Vertex3f dv = vertices[imax[0]] - vertices[imin[0]];
            float d2 = (dv - axis * dv.Dot(axis)).Norm();
            int k = 0;
            for (int j = 0; j < kDirectionCount; j++)
            {
                dv = vertices[imax[j]] - vertices[imin[j]];
                float m2 = (dv - axis * dv.Dot(axis)).Norm();
                if (m2 > d2) { d2 = m2; k = j; }
            }

            a = imin[k];
            b = imax[k];

            Vertex3f MakePerpendicularVector(Vertex3f v)
            {
                float px = Math.Abs(v.x);
                float py = Math.Abs(v.y);
                float pz = Math.Abs(v.z);
                if (pz < Math.Min(px, py)) return new Vertex3f(v.y, -v.x, 0.0f);
                if (py < px) return new Vertex3f(-v.z, 0, v.x);
                return new Vertex3f(0, v.z, -v.y);
            }

        }

        private static void FindExtremalVertices(Vertex3f[] vertices, Plane plane, out int e, out int f)
        {
            e = 0;
            f = 0;
            float dmin = plane * vertices[0];
            float dmax = dmin;
            for (int i = 1; i < vertices.Length; i++)
            {
                float m = plane * vertices[i];
                if (m < dmin) { dmin = m; e = i; }
                else if (m > dmax) { dmax = m; f = i; }
            }
        }

        private static void GetSecondaryBoxDirection(Vertex3f[] vertices, Vertex3f axis, int a, int b, out Vertex3f[] direction)
        {
            direction = new Vertex3f[5];
            direction[0] = vertices[b] - vertices[a];
            Vertex3f normal = axis.Cross(direction[0]).Normalized;
            Plane plane = new Plane(normal, -normal.Dot(vertices[a]));

            FindExtremalVertices(vertices, plane, out int e, out int f);
            direction[1] = vertices[e] - vertices[a];
            direction[2] = vertices[e] - vertices[b];
            direction[3] = vertices[f] - vertices[a];
            direction[4] = vertices[f] - vertices[b];

            for (int j = 0; j < 5; j++) direction[j] -= axis * direction[j].Dot(axis);
        }

        private static void GetPrimaryBoxDirection(Vertex3f[] vertices, int a, int b, out Vertex3f[] direction)
        {
            int c = 0;
            direction = new Vertex3f[9];
            direction[0] = vertices[b] - vertices[a];
            float dmax = DistancePointLine(vertices[0], vertices[a], direction[0]);
            for (int i = 1; i < vertices.Length; i++)
            {
                float m = DistancePointLine(vertices[i], vertices[a], direction[0]);
                if (m > dmax) { dmax = m; c = i; }
            }

            direction[1] = vertices[c] - vertices[a];
            direction[2] = vertices[c] - vertices[b];
            Vertex3f normal = direction[0].Cross(direction[1]);
            Plane plane = new Plane(normal, -normal.Dot(vertices[a]));

            FindExtremalVertices(vertices, plane, out int e, out int f);
            direction[3] = vertices[e] - vertices[a];
            direction[4] = vertices[e] - vertices[b];
            direction[5] = vertices[e] - vertices[c];
            direction[6] = vertices[f] - vertices[a];
            direction[7] = vertices[f] - vertices[b];
            direction[8] = vertices[f] - vertices[c];

            float DistancePointLine(Vertex3f point, Vertex3f linePoint, Vertex3f lineDirection)
            {
                Vertex3f areaVector = (linePoint - point).Cross(lineDirection);
                return areaVector.Norm() / lineDirection.Norm();
            }
        }

        /// <summary>
        /// 두 OBB의 오브젝트 공간의 원점이 서로 같을 때 새로운 두 OBB의 합의 OBB를 반환한다.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static OBB operator +(OBB left, OBB right)
        {
            List<Vertex3f> list = new List<Vertex3f>();
            list.AddRange(left.Vertices);
            list.AddRange(right.Vertices);

            OBB obb = null;
            OBB.CalculateOBB(list.ToArray(), out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);
            obb = new OBB(center, size, axis);
            return obb;
        }
    }
}
