using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    public class AABB : BoundVolume
    {
        const float TIGHT_SCALED_SIZE = 1.1f;

        Vertex3f _lower;
        Vertex3f _upper;
        bool _collided = false;

        Vertex3f _index;


        /// <summary>
        /// 
        /// </summary>
        public bool Collided
        {
            get => _collided;
            set => _collided = value;
        }

        /// <summary>
        /// AABB를 감싸는 구의 지름
        /// </summary>
        public float Diameter
        {
            get => (_upper - _lower).Length();
        }

        /// <summary>
        /// AABB를 감싸는 구의 반지름
        /// </summary>
        public float SphereRadius
        {
            get
            {
                // 중심점에서 AABB의 꼭지점까지의 거리가 구의 반지름
                Vertex3f extent = _upper - _lower;
                return extent.Distance() * 0.5f;
            }
        }

        /// <summary>
        /// 바운딩 박스의 중심점
        /// </summary>
        public override Vertex3f Center
        {
            get => (_lower + _upper) * 0.5f;
            set
            {
                // 바운딩 박스를 이동한다.
                Vertex3f size = Size * 0.5f;
                _lower = value - size;
                _upper = value + size;
            }
        }

        public Vertex3f BottomCenter => new Vertex3f((_lower.x + _upper.x) * 0.5f, (_lower.y + _upper.y) * 0.5f, _lower.z);

        /// <summary>
        /// 바운딩 박스의 크기 (Upper - Lower)
        /// </summary>
        public override Vertex3f Size
        {
            get => _upper - _lower;
            set
            {
                Vertex3f size = value * 0.5f;
                Vertex3f center = Center;
                _upper = center + size;
                _lower = center - size;
            }
        } 

        /// <summary>
        /// 바운딩 박스의 반의 크기
        /// </summary>
        public Vertex3f HalfSize => (_upper - _lower) * 0.5f;
        
        /// <summary>
        /// 텍스트로 출력한다.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{_lower},{_upper}";
        }


        /// <summary>
        /// 바운딩 박스의 8개의 꼭짓점의 최소바운딩 꼭짓점
        /// </summary>
        public Vertex3f LowerBound
        {
            get => _lower;
            set => _lower = value;
        }

        /// <summary>
        /// 바운딩 박스의 8개의 꼭짓점의 최대바운딩 꼭짓점
        /// </summary>
        public Vertex3f UpperBound
        {
            get => _upper;
            set => _upper = value;
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

        /// <summary>
        /// 바운딩 박스의 표면 겉넓이
        /// </summary>
        public override float Area
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

        /// <summary>
        /// 바운딩 박스의 월드변환 행렬
        /// </summary>
        public override Matrix4x4f ModelMatrix
        {
            get
            {
                Vertex3f size = Size * 0.5f;
                Vertex3f center = Center;
                float tight_size = _useEnhanceBox ? TIGHT_SCALED_SIZE :  1.0f;
                Matrix4x4f S = Matrix4x4f.Scaled(tight_size * size.x, tight_size * size.y, tight_size * size.z);
                Matrix4x4f T = Matrix4x4f.Translated(center.x, center.y, center.z);
                return T * S; // 순서는 S->R->T  (R==I)
            }
        }

        /// <summary>
        /// 바운딩 박스의 UseEnhanceBox가 적용되지 8개의 꼭짓점의 월드 좌표
        /// </summary>
        public override Vertex3f[] Vertices
        {
            get
            {
                float tight_size = _useEnhanceBox ? TIGHT_SCALED_SIZE : 1.0f;

                Vertex3f d = _upper - _lower;
                Vertex3f s = new Vertex3f(d.x, 0, 0);
                Vertex3f t = new Vertex3f(0, d.y, 0);
                Vertex3f u = new Vertex3f(0, 0, d.z);

                Vertex3f[] vertices = new Vertex3f[8];
                vertices[0] = _lower;
                vertices[1] = _lower + s;
                vertices[2] = _lower + s + t;
                vertices[3] = _lower + t;
                vertices[4] = _lower + u;
                vertices[5] = _lower + u + s;
                vertices[6] = _lower + u + s + t;
                vertices[7] = _lower + u + t;

                return vertices;
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="useEnhanceBox"></param>
        public AABB(bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _color = Rand.NextColor3f;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <param name="useEnhanceBox"></param>
        public AABB(float x1, float y1, float z1, float x2, float y2, float z2, bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _lower = new Vertex3f(x1, y1, z1);
            _upper = new Vertex3f(x2, y2, z2);
            _color = Rand.NextColor3f;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="useEnhanceBox"></param>
        public AABB(Vertex3f lower, Vertex3f upper, bool useEnhanceBox = false)
        {
            _useEnhanceBox = useEnhanceBox;
            _lower = lower;
            _upper = upper;
            _color = Rand.NextColor3f;
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
                float rg = GetProjectedRadius(plane.Normal);
                float distance = plane * Center;
                if (distance - rg < 0) return false;
            }
            return true;
        }

        /// <summary>
        /// 다면체 안에 AABB가 접촉(보이)하는지 판별한다.
        /// </summary>
        /// <param name="planes">다면체 평면들</param>
        /// <remarks>평면의 위쪽은 다면체의 안쪽을 바라보아야 한다.</remarks>
        /// <returns></returns>
        public bool Visible(Plane[] planes)
        {
            if (planes == null) return true;

            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                float rg = GetProjectedRadius(plane.Normal);
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
        /// 박스의 중심으로부터 지정된 방향으로의 최대 투영 거리를 계산합니다.
        /// 박스의 모서리들을 주어진 방향으로 투영했을 때 가장 먼 거리를 반환합니다.
        /// SAT(Separating Axis Theorem) 충돌 검사에 사용됩니다.
        /// </summary>
        /// <param name="normal">투영할 방향 벡터 (정규화된 벡터)</param>
        /// <returns>박스 중심에서 normal 방향으로의 최대 투영 거리</returns>
        protected float GetProjectedRadius(Vertex3f normal)
        {
            Vertex3f size = Size * 0.5f;
            Vertex3f[] axis = { Vertex3f.UnitX * size.x, Vertex3f.UnitY * size.y, Vertex3f.UnitZ * size.z };

            return Math.Abs(normal.Dot(axis[0] )) +
                     Math.Abs(normal.Dot(axis[1] )) +
                      Math.Abs(normal.Dot(axis[2] ));
        }

        /// <summary>
        /// 깊은 복사를 한다.
        /// </summary>
        /// <returns></returns>
        public override BoundVolume Clone()
        {
            AABB result = new AABB();
            result._lower = _lower;
            result._upper = _upper;
            result._useEnhanceBox = _useEnhanceBox;
            result._baseEntity = _baseEntity;
            result._collided = _collided;
            return result;
        }

        /// <summary>
        /// AABB를 뷰 공간으로 변환합니다.
        /// </summary>
        /// <param name="ViewProjMatrix">뷰-투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <returns>뷰 공간의 AABB</returns>
        public AABB TransformViewSpace(Matrix4x4f ViewProjMatrix, Matrix4x4f view)
        {
            AABB viewAABB = new AABB();

            // AABB의 8개 꼭지점 가져오기
            Vertex3f[] vertices = Vertices;

            // 각 꼭지점을 뷰 공간으로 변환
            Vertex3f[] cns = new Vertex3f[8];
            for (int i = 0; i < 8; i++)
            {
                // 꼭지점을 4차원 좌표로 변환
                Vertex4f corner = vertices[i].Vertex4f();

                // 뷰-투영 변환 수행 및 원근 나눗셈
                Vertex4f viewCorner = ViewProjMatrix * corner;
                cns[i] = viewCorner.Vertex3fDivideW();

                // z값 보정 (깊이 값 스케일링) 0.001 중요
                // (TerrainDepthShader에서 null.frag 에서 1000.0f)를 사용하므로 역변환
                // 1000.0f 가 가장 적당함.
                Vertex4f point = view * corner;
                cns[i].z = point.z * 0.0001f;
            }

            // 변환된 꼭지점들로 새로운 AABB 생성
            viewAABB.LowerBound = Vertex3f.Min(cns);
            viewAABB.UpperBound = Vertex3f.Max(cns);

            return viewAABB;
        }

        /// <summary>
        /// 점을 추가하여 상자를 바운딩을 확장한다.
        /// </summary>
        /// <param name="point"></param>
        public void Expand(Vertex3f point)
        {
            _lower = new Vertex3f(
                Math.Min(_lower.x, point.x),
                Math.Min(_lower.y, point.y),
                Math.Min(_lower.z, point.z)
            );

            _upper = new Vertex3f(
                Math.Max(_upper.x, point.x),
                Math.Max(_upper.y, point.y),
                Math.Max(_upper.z, point.z)
            );
        }

        public override BoundVolume Union(BoundVolume boundVolume)
        {
            AABB a = this;
            AABB b = boundVolume as AABB;
            return new AABB(min(a.LowerBound, b.LowerBound), max(a.UpperBound, b.UpperBound));
            // 함수의 내부함수 부분이다.
            Vertex3f min(Vertex3f v1, Vertex3f v2) => new Vertex3f(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y), Math.Min(v1.z, v2.z));
            Vertex3f max(Vertex3f v1, Vertex3f v2) => new Vertex3f(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y), Math.Max(v1.z, v2.z));
        }

        public override BoundVolume Intersect(BoundVolume boundVoulume)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 원점(0,0,0)에 위치하고 크기가 0인 AABB를 반환합니다.
        /// </summary>
        public static AABB ZeroSizeAABB
        {
            get
            {
                return new AABB(Vertex3f.Zero, Vertex3f.Zero);
            }
        }

        public Vertex3f Index { get => _index; set => _index = value; }
    }
}
