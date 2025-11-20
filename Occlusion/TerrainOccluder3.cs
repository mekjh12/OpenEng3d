using Common;
using Geometry;
using OpenGL;
using ZetaExt;

namespace Occlusion
{
    public class TerrainOccluder3
    {
        static int Guid = 0;

        Vertex3f _left;
        Vertex3f _right;
        float _area;
        Matrix4x4f _model;
        int _guid = 0;

        public int ID => _guid;

        public Vertex3f Left
        {
            get => _left;
            set
            {
                _left = value;
                SetModelMatrix4x4f();
            }
        }

        public Vertex3f Right
        {
            get => _right;
            set
            {
                _right = value;
                SetModelMatrix4x4f();
            }
        }

        public float Area
        {
            get
            {
                float dist = (_left.xy() - _right.xy()).Norm();
                return (_left.z + _right.z) * dist * 0.5f;
            }
        }

        public Matrix4x4f ModelMatrix => _model;

        public TerrainOccluder3(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            _guid = Guid;
            Guid++;
            _left = new Vertex3f(x1, y1, z1);
            _right = new Vertex3f(x2, y2, z2);
            SetModelMatrix4x4f();
        }

        public TerrainOccluder3(Vertex3f left, Vertex3f right)
        {
            _guid = Guid;
            Guid++;
            _left = left;
            _right = right;
            SetModelMatrix4x4f();
        }

        private void SetModelMatrix4x4f()
        {
            // xy(-1,-1,0)--(1,1,0) 평면을 변환
            Vertex3f l = _left;
            Vertex3f r = _right;
            Vertex3f p = (l + r) * 0.5f;
            p.z -= l.z * 0.5f;

            Vertex3f nx = (r - l) * 0.5f;
            Vertex3f ny = new Vertex3f(0, 0, l.z * 0.5f);
            Vertex3f nz = nx.Cross(ny);

            Matrix4x4f model = Matrix4x4f.Identity;
            model = model.Column(0, nx);
            model = model.Column(1, ny);
            model = model.Column(2, nz);
            model = model.Column(3, p);

            _model = model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cameraPosition"></param>
        /// <returns></returns>
        public Plane[] GetOccluderPlane(Vertex3f cameraPosition)
        {
            // 안쪽을 바라보는 3개의 평면
            Vertex3f l1 = new Vertex3f(_left.x, _left.y, -100.0f);
            Vertex3f l2 = new Vertex3f(_left.x, _left.y, _left.z);
            Vertex3f r1 = new Vertex3f(_right.x, _right.y, -100.0f);
            Vertex3f r2 = new Vertex3f(_right.x, _right.y, _right.z);
            Vertex3f p = cameraPosition;

            Vertex3f n = (l2 - p).Cross(r2 - p);

            if (n.z > 0.0f)
            {
                Plane left = new Plane(p, l1, l2).FlipPlane;
                Plane right = new Plane(p, r2, r1).FlipPlane;
                Plane front = new Plane(l1, l2, r2).FlipPlane;
                Plane top = new Plane(p, l2, r2).FlipPlane;

                return new Plane[] { left, right, front, top };
            }
            else
            {
                Plane left = new Plane(p, l1, l2);
                Plane right = new Plane(p, r2, r1);
                Plane front = new Plane(l1, l2, r2);
                Plane top = new Plane(p, l2, r2);

                return new Plane[] { left, right, front, top };
            }
        }
    }
}
