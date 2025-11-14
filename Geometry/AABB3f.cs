using Common.Abstractions;
using FastMath;
using OpenGL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZetaExt;

namespace Geometry
{
    /// <summary>
    /// 3차원 축 정렬 바운딩 박스 (float 정밀도)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB3f
    {
        public Vertex3f Min;            // Lower -> Min (더 수학적)
        public Vertex3f Max;            // Upper -> Max
        public Vertex4f Color;          // 렌더링 색상
        public int EntityId;            // 바운딩볼륨에 포함된 BaseEntity
        private byte flags;             // 1 byte

        private Vertex3f _center;       // 중심점
        private Vertex3f _size;         // 크기
        private float _radius;          // 반지름
        private float _area;            // 겉넓이 
        private Matrix4x4f _model;      // 모델 행렬 캐시용
        private Vertex3f[] _vertices;   // 8개 꼭짓점 캐시용

        public Vertex3f Center => _center;
        public Vertex3f Size => _size;
        public float Radius => _radius;
        public float Area => _area;
        public Matrix4x4f ModelMatrix => _model;

        /// <summary>
        /// AABB을 느슨하게 감싸는 AABB 사용 여부
        /// </summary>
        public bool UseEnhanceBox
        {
            get => (flags & 0x01) != 0;
            set => flags = value ? (byte)(flags | 0x01) : (byte)(flags & ~0x01);
        }

        /// <summary>
        /// 렌더링 시 가시성 여부
        /// </summary>
        public bool IsVisible
        {
            get => (flags & 0x02) != 0;
            set => flags = value ? (byte)(flags | 0x02) : (byte)(flags & ~0x02);
        }

        /// <summary>
        /// AABB가 유효한지 검사 (Min <= Max)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid(in AABB3f aabb)
        {
            return aabb.Min.x <= aabb.Max.x &&
                   aabb.Min.y <= aabb.Max.y &&
                   aabb.Min.z <= aabb.Max.z;
        }

        /// <summary>
        /// 바운딩 박스의 UseEnhanceBox가 적용되지 8개의 꼭짓점의 월드 좌표
        /// </summary>
        public Vertex3f[] Vertices => _vertices;

        /// <summary>
        /// AABB를 감싸는 구의 지름
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDiameter(in AABB3f aabb)
        {
            return MathFast.Sqrt(aabb.Size.ModuleSquared());
        }

        /// <summary>
        /// AABB를 감싸는 구의 반지름 (Bounding Sphere Radius)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSphereRadius(in AABB3f aabb)
        {
            return MathFast.Sqrt(aabb.Size.ModuleSquared()) * 0.5f;
        }

        /// <summary>
        /// AABB의 부피
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVolume()
        {
            return _size.x * _size.y * _size.z;
        }

        /// <summary>
        /// AABB의 바닥 중심점 (지형 충돌 등에 유용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex3f GetBottomCenter(in AABB3f aabb)
        {
            return new Vertex3f(
                (aabb.Min.x + aabb.Max.x) * 0.5f,
                (aabb.Min.y + aabb.Max.y) * 0.5f,
                aabb.Min.z
            );
        }

        /// <summary>
        /// AABB의 8개 코너 점 반환
        /// </summary>
        public Vertex3f[] GetCorners()
        {
            return new Vertex3f[8]
            {
                new Vertex3f(Min.x, Min.y, Min.z), // 0: ---
                new Vertex3f(Max.x, Min.y, Min.z), // 1: +--
                new Vertex3f(Max.x, Max.y, Min.z), // 2: ++-
                new Vertex3f(Min.x, Max.y, Min.z), // 3: -+-
                new Vertex3f(Min.x, Min.y, Max.z), // 4: --+
                new Vertex3f(Max.x, Min.y, Max.z), // 5: +-+
                new Vertex3f(Max.x, Max.y, Max.z), // 6: +++
                new Vertex3f(Min.x, Max.y, Max.z)  // 7: -++
            };
        }

        /// <summary>
        /// AABB의 6개 평면 반환 (Frustum Culling 등에 사용)
        /// </summary>
        public Plane[] GetPlanes(in AABB3f aabb)
        {
            return new Plane[6]
            {
                new Plane(Vertex3f.UnitX, aabb.Min),   // Left
                new Plane(-Vertex3f.UnitX, aabb.Max),  // Right
                new Plane(Vertex3f.UnitY, aabb.Min),   // Bottom
                new Plane(-Vertex3f.UnitY, aabb.Max),  // Top
                new Plane(Vertex3f.UnitZ, aabb.Min),   // Back
                new Plane(-Vertex3f.UnitZ, aabb.Max)   // Front
            };
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public AABB3f(Vertex3f min, Vertex3f max)
        {
            Min = min;
            Max = max;
            Color = Rand.NextColor3f;
            flags = 0x01;
            EntityId = -1;

            _center = (Min + Max) * 0.5f;
            _size = Max - Min;
            _radius = MathFast.Sqrt(_size.x * _size.x + _size.y * _size.y + _size.z * _size.z) * 0.5f;

            float x = MathFast.Abs(_size.x);
            float y = MathFast.Abs(_size.y);
            float z = MathFast.Abs(_size.z);
            _area = 2.0f * (x * y + y * z + z * x);

            _model = new Matrix4x4f(
                _size.x, 0, 0, 0,
                0, _size.y, 0, 0,
                0, 0, _size.z, 0,
                _center.x, _center.y, _center.z, 1
            );

            // 8개 꼭지점 계산
            Vertex3f d = Max - Min;
            Vertex3f s = new Vertex3f(d.x, 0, 0);
            Vertex3f t = new Vertex3f(0, d.y, 0);
            Vertex3f u = new Vertex3f(0, 0, d.z);

            _vertices = new Vertex3f[8];
            _vertices[0] = Min;
            _vertices[1] = Min + s;
            _vertices[2] = Min + s + t;
            _vertices[3] = Min + t;
            _vertices[4] = Min + u;
            _vertices[5] = Min + u + s;
            _vertices[6] = Min + u + s + t;
            _vertices[7] = Min + u + t;

            // 기본값 설정
            IsVisible = true;
            UseEnhanceBox = false;
        }

        /// <summary>
        /// AABB를 특정 방향으로 투영했을 때의 최대 반지름 계산
        /// (SAT - Separating Axis Theorem에 사용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetProjectedRadius(Vertex3f normal)
        {
            Vertex3f halfSize = _size * 0.5f;

            return MathFast.Abs(normal.x * halfSize.x) +
                   MathFast.Abs(normal.y * halfSize.y) +
                   MathFast.Abs(normal.z * halfSize.z);
        }

        /// <summary>
        /// 다면체(Frustum) 평면들에 대해 AABB가 보이는지 검사
        /// </summary>
        public bool IsVisibleInFrustum(Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            Vertex3f center = _center;

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                Plane plane = frustumPlanes[i];
                float projectedRadius = GetProjectedRadius(plane.Normal);
                float distance = plane * center;

                // 평면 뒤쪽에 완전히 있으면 안 보임
                if (distance + projectedRadius < 0.0f)
                    return false;
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
        /// AABB3f를 뷰 공간으로 변환합니다.
        /// </summary>
        /// <param name="ViewProjMatrix">뷰-투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <returns>뷰 공간의 AABB</returns>
        public void TransformViewSpace(Matrix4x4f ViewProjMatrix, Matrix4x4f view, ref AABB3f viewAABB)
        {
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
            viewAABB.Min = Vertex3f.Min(cns);
            viewAABB.Max = Vertex3f.Max(cns);
        }

        /// <summary>
        /// 다면체(Frustum) 평면들에 대해 AABB가 완전히 포함되는지 검사
        /// </summary>
        public bool IsIncludedInFrustum(Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                Plane plane = frustumPlanes[i];
                float projectedRadius = GetProjectedRadius(plane.Normal);
                float distance = plane * _center;
                if (distance - projectedRadius < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 점이 AABB 내부에 있는지 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vertex3f point)
        {
            return point.x >= Min.x && point.x <= Max.x &&
                   point.y >= Min.y && point.y <= Max.y &&
                   point.z >= Min.z && point.z <= Max.z;
        }

        /// <summary>
        /// 다른 AABB가 완전히 포함되는지 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in AABB3f other)
        {
            return other.Min.x >= Min.x && other.Max.x <= Max.x &&
                   other.Min.y >= Min.y && other.Max.y <= Max.y &&
                   other.Min.z >= Min.z && other.Max.z <= Max.z;
        }

        /// <summary>
        /// 두 AABB가 교차하는지 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in AABB3f a, in AABB3f b)
        {
            return a.Min.x <= b.Max.x && a.Max.x >= b.Min.x &&
                   a.Min.y <= b.Max.y && a.Max.y >= b.Min.y &&
                   a.Min.z <= b.Max.z && a.Max.z >= b.Min.z;
        }

        /// <summary>
        /// 점 배열로부터 최소 AABB 생성
        /// </summary>
        public AABB3f FromPoints(params Vertex3f[] points)
        {
            if (points == null || points.Length == 0)
                return AABB3f.Zero;

            Vertex3f min = points[0];
            Vertex3f max = points[0];

            for (int i = 1; i < points.Length; i++)
            {
                min.x = MathFast.Min(min.x, points[i].x);
                min.y = MathFast.Min(min.y, points[i].y);
                min.z = MathFast.Min(min.z, points[i].z);

                max.x = MathFast.Max(max.x, points[i].x);
                max.y = MathFast.Max(max.y, points[i].y);
                max.z = MathFast.Max(max.z, points[i].z);
            }

            return new AABB3f(min, max);
        }

        /// <summary>
        /// AABB를 뷰 공간으로 변환 (HZB 오클루전 테스트용)
        /// </summary>
        /// <param name="viewProjMatrix">뷰-투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <returns>뷰 공간의 AABB</returns>
        public AABB3f TransformToViewSpace(Matrix4x4f viewProjMatrix, Matrix4x4f view)
        {
            Vertex3f[] corners = GetCorners();
            Vertex3f[] transformedCorners = new Vertex3f[8];

            for (int i = 0; i < 8; i++)
            {
                Vertex4f corner = new Vertex4f(corners[i].x, corners[i].y, corners[i].z, 1f);

                // 뷰-투영 변환 및 원근 나눗셈
                Vertex4f viewCorner = viewProjMatrix * corner;
                transformedCorners[i] = new Vertex3f(
                    viewCorner.x / viewCorner.w,
                    viewCorner.y / viewCorner.w,
                    viewCorner.z / viewCorner.w
                );

                // Z값 보정 (TerrainDepthShader와 동기화)
                Vertex4f viewPoint = view * corner;
                transformedCorners[i].z = viewPoint.z * 0.0001f;
            }

            return FromPoints(transformedCorners);
        }

        /// <summary>
        /// AABB를 모델 행렬로 변환 (렌더링용)
        /// </summary>
        public Matrix4x4f GetModelMatrix(float tightScale = 1.0f)
        {
            Vertex3f size = _size * 0.5f * tightScale;
            Matrix4x4f scale = Matrix4x4f.Scaled(size.x, size.y, size.z);
            Matrix4x4f translation = Matrix4x4f.Translated(_center.x, _center.y, _center.z);

            return translation * scale;
        }

        /// <summary>
        /// AABB 상의 가장 가까운 점 찾기
        /// </summary>
        public Vertex3f ClosestPoint(Vertex3f point)
        {
            return new Vertex3f(
                MathFast.Clamp(point.x, Min.x, Max.x),
                MathFast.Clamp(point.y, Min.y, Max.y),
                MathFast.Clamp(point.z, Min.z, Max.z)
            );
        }

        /// <summary>
        /// 점에서 AABB까지의 최단 거리
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(Vertex3f point)
        {
            return MathFast.Sqrt(DistanceSquared(point));
        }

        /// <summary>
        /// 점에서 AABB까지의 최단 거리 제곱
        /// </summary>
        public float DistanceSquared(Vertex3f point)
        {
            float sqDist = 0.0f;

            // X축
            if (point.x < Min.x)
            {
                float d = Min.x - point.x;
                sqDist += d * d;
            }
            else if (point.x > Max.x)
            {
                float d = point.x - Max.x;
                sqDist += d * d;
            }

            // Y축
            if (point.y < Min.y)
            {
                float d = Min.y - point.y;
                sqDist += d * d;
            }
            else if (point.y > Max.y)
            {
                float d = point.y - Max.y;
                sqDist += d * d;
            }

            // Z축
            if (point.z < Min.z)
            {
                float d = Min.z - point.z;
                sqDist += d * d;
            }
            else if (point.z > Max.z)
            {
                float d = point.z - Max.z;
                sqDist += d * d;
            }

            return sqDist;
        }

        /// <summary>
        /// 여러 AABB의 합집합
        /// </summary>
        public AABB3f Union(params AABB3f[] aabbs)
        {
            if (aabbs == null || aabbs.Length == 0)
                return AABB3f.Zero;

            AABB3f result = aabbs[0];
            for (int i = 1; i < aabbs.Length; i++)
            {
                result = result.Union(aabbs[i]);
            }
            return result;
        }

        /// <summary>
        /// 행렬을 적용하여 AABB 변환 (회전 포함 시 확장됨)
        /// </summary>
        public AABB3f Transform(Matrix4x4f matrix)
        {
            Vertex3f[] corners = GetCorners();
            Vertex3f min = new Vertex3f(float.MaxValue, float.MaxValue, float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < 8; i++)
            {
                Vertex4f transformed4 = matrix * new Vertex4f(corners[i].x, corners[i].y, corners[i].z, 1f);
                Vertex3f transformed = new Vertex3f(
                    transformed4.x / transformed4.w,
                    transformed4.y / transformed4.w,
                    transformed4.z / transformed4.w
                );

                min.x = MathFast.Min(min.x, transformed.x);
                min.y = MathFast.Min(min.y, transformed.y);
                min.z = MathFast.Min(min.z, transformed.z);

                max.x = MathFast.Max(max.x, transformed.x);
                max.y = MathFast.Max(max.y, transformed.y);
                max.z = MathFast.Max(max.z, transformed.z);
            }

            return new AABB3f(min, max);
        }


        /// <summary>
        /// AABB를 이동
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABB3f Translate(Vertex3f offset)
        {
            return new AABB3f(Min + offset, Max + offset);
        }

        /// <summary>
        /// AABB를 스케일 (중심 기준)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABB3f Scale(float scale)
        {
            Vertex3f halfSize = _size * 0.5f * scale;
            return new AABB3f(_center - halfSize, _center + halfSize);
        }

        /// <summary>
        /// AABB를 확장 (여유 공간 추가)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABB3f Expand(float margin)
        {
            Vertex3f offset = new Vertex3f(margin, margin, margin);
            return new AABB3f(Min - offset, Max + offset);
        }


        /// <summary>
        /// 원점 중심의 크기 0인 AABB 반환
        /// </summary>
        public static AABB3f Zero => new AABB3f(Vertex3f.Zero, Vertex3f.Zero);

        /// <summary>
        /// 무한대 크기의 AABB 반환 (모든 공간 포함)
        /// </summary>
        public static AABB3f Infinite => new AABB3f(
            new Vertex3f(float.MinValue, float.MinValue, float.MinValue),
            new Vertex3f(float.MaxValue, float.MaxValue, float.MaxValue)
        );

        /// <summary>
        /// 중심점과 크기로 AABB 생성
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f FromCenterAndSize(Vertex3f center, Vertex3f size)
        {
            Vertex3f halfSize = size * 0.5f;
            return new AABB3f(center - halfSize, center + halfSize);
        }

        /// <summary>
        /// 중심점과 반지름으로 AABB 생성
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f FromCenterAndRadius(Vertex3f center, float radius)
        {
            Vertex3f offset = new Vertex3f(radius, radius, radius);
            return new AABB3f(center - offset, center + offset);
        }

        /// <summary>
        /// 두 AABB의 교집합 (Intersection)
        /// </summary>
        /// <param name="hasIntersection">교집합이 존재하는지 여부</param>
        public static AABB3f Intersect(in AABB3f a, in AABB3f b, out bool hasIntersection)
        {
            AABB3f result = new AABB3f(
                new Vertex3f(
                    MathFast.Max(a.Min.x, b.Min.x),
                    MathFast.Max(a.Min.y, b.Min.y),
                    MathFast.Max(a.Min.z, b.Min.z)
                ),
                new Vertex3f(
                    MathFast.Min(a.Max.x, b.Max.x),
                    MathFast.Min(a.Max.y, b.Max.y),
                    MathFast.Min(a.Max.z, b.Max.z)
                )
            );

            hasIntersection = result.IsValid(in result);
            return hasIntersection ? result : AABB3f.Zero;
        }

        /// <summary>
        /// 점을 포함하도록 AABB 확장
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABB3f ExpandToInclude(Vertex3f point)
        {
            return new AABB3f(
                new Vertex3f(
                    MathFast.Min(Min.x, point.x),
                    MathFast.Min(Min.y, point.y),
                    MathFast.Min(Min.z, point.z)
                ),
                new Vertex3f(
                    MathFast.Max(Max.x, point.x),
                    MathFast.Max(Max.y, point.y),
                    MathFast.Max(Max.z, point.z)
                )
            );
        }

        /// <summary>
        /// 두 AABB의 합집합 (Union)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f Union(in AABB3f a, in AABB3f b)
        {
            return new AABB3f(
                new Vertex3f(
                    MathFast.Min(a.Min.x, b.Min.x),
                    MathFast.Min(a.Min.y, b.Min.y),
                    MathFast.Min(a.Min.z, b.Min.z)
                ),
                new Vertex3f(
                    MathFast.Max(a.Max.x, b.Max.x),
                    MathFast.Max(a.Max.y, b.Max.y),
                    MathFast.Max(a.Max.z, b.Max.z)
                )
            );
        }


        /// <summary>
        /// 광선과 AABB의 교차 검사 (Ray-Box Intersection)
        /// </summary>
        /// <param name="rayOrigin">광선 시작점</param>
        /// <param name="rayDirection">광선 방향 (정규화 필요)</param>
        /// <param name="tMin">교차 시작 거리 (출력)</param>
        /// <param name="tMax">교차 종료 거리 (출력)</param>
        /// <returns>교차 여부</returns>
        public bool IntersectsRay(Vertex3f rayOrigin, Vertex3f rayDirection, out float tMin, out float tMax)
        {
            tMin = float.MinValue;
            tMax = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                float origin = i == 0 ? rayOrigin.x : (i == 1 ? rayOrigin.y : rayOrigin.z);
                float dir = i == 0 ? rayDirection.x : (i == 1 ? rayDirection.y : rayDirection.z);
                float min = i == 0 ? Min.x : (i == 1 ? Min.y : Min.z);
                float max = i == 0 ? Max.x : (i == 1 ? Max.y : Max.z);

                if (MathFast.Abs(dir) < 1e-6f)
                {
                    // 광선이 축에 평행
                    if (origin < min || origin > max)
                        return false;
                }
                else
                {
                    float invDir = 1.0f / dir;
                    float t1 = (min - origin) * invDir;
                    float t2 = (max - origin) * invDir;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    tMin = MathFast.Max(tMin, t1);
                    tMax = MathFast.Min(tMax, t2);

                    if (tMin > tMax)
                        return false;
                }
            }

            return true;
        }
    }
}

