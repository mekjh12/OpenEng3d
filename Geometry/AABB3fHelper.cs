using FastMath;
using OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace Geometry
{
    /// <summary>
    /// AABB3f 구조체를 위한 정적 유틸리티 클래스
    /// 충돌 검사, 변환, 공간 쿼리 등의 기능 제공
    /// </summary>
    public static class AABB3fHelper
    {
        #region 생성 및 초기화

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
        /// 점 배열로부터 최소 AABB 생성
        /// </summary>
        public static AABB3f FromPoints(params Vertex3f[] points)
        {
            if (points == null || points.Length == 0)
                return Zero;

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

        #endregion

        #region 검증 및 속성

        /// <summary>
        /// AABB가 유효한지 검사 (Min <= Max)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(in AABB3f aabb)
        {
            return aabb.Min.x <= aabb.Max.x &&
                   aabb.Min.y <= aabb.Max.y &&
                   aabb.Min.z <= aabb.Max.z;
        }

        /// <summary>
        /// AABB를 감싸는 구의 지름
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDiameter(in AABB3f aabb)
        {
            return aabb.Size.Module();
        }

        /// <summary>
        /// AABB를 감싸는 구의 반지름 (Bounding Sphere Radius)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSphereRadius(in AABB3f aabb)
        {
            return aabb.Size.Module() * 0.5f;
        }

        /// <summary>
        /// AABB의 표면 겉넓이
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSurfaceArea(in AABB3f aabb)
        {
            Vertex3f size = aabb.Size;
            float x = MathFast.Abs(size.x);
            float y = MathFast.Abs(size.y);
            float z = MathFast.Abs(size.z);
            return 2.0f * (x * y + y * z + z * x);
        }

        /// <summary>
        /// AABB의 부피
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetVolume(in AABB3f aabb)
        {
            Vertex3f size = aabb.Size;
            return size.x * size.y * size.z;
        }

        /// <summary>
        /// AABB의 바닥 중심점 (지형 충돌 등에 유용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vertex3f GetBottomCenter(in AABB3f aabb)
        {
            return new Vertex3f(
                (aabb.Min.x + aabb.Max.x) * 0.5f,
                (aabb.Min.y + aabb.Max.y) * 0.5f,
                aabb.Min.z
            );
        }

        #endregion

        #region 8개 코너 및 평면

        /// <summary>
        /// AABB의 8개 코너 점 반환
        /// </summary>
        public static Vertex3f[] GetCorners(in AABB3f aabb)
        {
            return new Vertex3f[8]
            {
                new Vertex3f(aabb.Min.x, aabb.Min.y, aabb.Min.z), // 0: ---
                new Vertex3f(aabb.Max.x, aabb.Min.y, aabb.Min.z), // 1: +--
                new Vertex3f(aabb.Max.x, aabb.Max.y, aabb.Min.z), // 2: ++-
                new Vertex3f(aabb.Min.x, aabb.Max.y, aabb.Min.z), // 3: -+-
                new Vertex3f(aabb.Min.x, aabb.Min.y, aabb.Max.z), // 4: --+
                new Vertex3f(aabb.Max.x, aabb.Min.y, aabb.Max.z), // 5: +-+
                new Vertex3f(aabb.Max.x, aabb.Max.y, aabb.Max.z), // 6: +++
                new Vertex3f(aabb.Min.x, aabb.Max.y, aabb.Max.z)  // 7: -++
            };
        }

        /// <summary>
        /// AABB의 6개 평면 반환 (Frustum Culling 등에 사용)
        /// </summary>
        public static Plane[] GetPlanes(in AABB3f aabb)
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

        #endregion

        #region 포함 및 교차 테스트

        /// <summary>
        /// 점이 AABB 내부에 있는지 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(in AABB3f aabb, Vertex3f point)
        {
            return point.x >= aabb.Min.x && point.x <= aabb.Max.x &&
                   point.y >= aabb.Min.y && point.y <= aabb.Max.y &&
                   point.z >= aabb.Min.z && point.z <= aabb.Max.z;
        }

        /// <summary>
        /// 다른 AABB가 완전히 포함되는지 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(in AABB3f aabb, in AABB3f other)
        {
            return other.Min.x >= aabb.Min.x && other.Max.x <= aabb.Max.x &&
                   other.Min.y >= aabb.Min.y && other.Max.y <= aabb.Max.y &&
                   other.Min.z >= aabb.Min.z && other.Max.z <= aabb.Max.z;
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
        /// 광선과 AABB의 교차 검사 (Ray-Box Intersection)
        /// </summary>
        /// <param name="rayOrigin">광선 시작점</param>
        /// <param name="rayDirection">광선 방향 (정규화 필요)</param>
        /// <param name="tMin">교차 시작 거리 (출력)</param>
        /// <param name="tMax">교차 종료 거리 (출력)</param>
        /// <returns>교차 여부</returns>
        public static bool IntersectsRay(in AABB3f aabb, Vertex3f rayOrigin,
            Vertex3f rayDirection, out float tMin, out float tMax)
        {
            tMin = float.MinValue;
            tMax = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                float origin = i == 0 ? rayOrigin.x : (i == 1 ? rayOrigin.y : rayOrigin.z);
                float dir = i == 0 ? rayDirection.x : (i == 1 ? rayDirection.y : rayDirection.z);
                float min = i == 0 ? aabb.Min.x : (i == 1 ? aabb.Min.y : aabb.Min.z);
                float max = i == 0 ? aabb.Max.x : (i == 1 ? aabb.Max.y : aabb.Max.z);

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

        #endregion

        #region Frustum Culling

        /// <summary>
        /// 다면체(Frustum) 평면들에 대해 AABB가 완전히 포함되는지 검사
        /// </summary>
        public static bool IsIncludedInFrustum(in AABB3f aabb, Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            Vertex3f center = aabb.Center;

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                Plane plane = frustumPlanes[i];
                float projectedRadius = GetProjectedRadius(in aabb, plane.Normal);
                float distance = plane * center;

                if (distance - projectedRadius < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 다면체(Frustum) 평면들에 대해 AABB가 보이는지 검사
        /// </summary>
        public static bool IsVisibleInFrustum(in AABB3f aabb, Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            Vertex3f center = aabb.Center;

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                Plane plane = frustumPlanes[i];
                float projectedRadius = GetProjectedRadius(in aabb, plane.Normal);
                float distance = plane * center;

                // 평면 뒤쪽에 완전히 있으면 안 보임
                if (distance + projectedRadius < 0.0f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// AABB를 특정 방향으로 투영했을 때의 최대 반지름 계산
        /// (SAT - Separating Axis Theorem에 사용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetProjectedRadius(in AABB3f aabb, Vertex3f normal)
        {
            Vertex3f halfSize = aabb.Size * 0.5f;

            return MathFast.Abs(normal.x * halfSize.x) +
                   MathFast.Abs(normal.y * halfSize.y) +
                   MathFast.Abs(normal.z * halfSize.z);
        }

        #endregion

        #region 변환 (Transform)

        /// <summary>
        /// 행렬을 적용하여 AABB 변환 (회전 포함 시 확장됨)
        /// </summary>
        public static AABB3f Transform(in AABB3f aabb, Matrix4x4f matrix)
        {
            Vertex3f[] corners = GetCorners(in aabb);
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
        /// AABB를 뷰 공간으로 변환 (HZB 오클루전 테스트용)
        /// </summary>
        /// <param name="viewProjMatrix">뷰-투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <returns>뷰 공간의 AABB</returns>
        public static AABB3f TransformToViewSpace(in AABB3f aabb,
            Matrix4x4f viewProjMatrix, Matrix4x4f view)
        {
            Vertex3f[] corners = GetCorners(in aabb);
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
        /// AABB를 이동
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f Translate(in AABB3f aabb, Vertex3f offset)
        {
            return new AABB3f(aabb.Min + offset, aabb.Max + offset);
        }

        /// <summary>
        /// AABB를 스케일 (중심 기준)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f Scale(in AABB3f aabb, float scale)
        {
            Vertex3f center = aabb.Center;
            Vertex3f halfSize = aabb.Size * 0.5f * scale;
            return new AABB3f(center - halfSize, center + halfSize);
        }

        /// <summary>
        /// AABB를 확장 (여유 공간 추가)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f Expand(in AABB3f aabb, float margin)
        {
            Vertex3f offset = new Vertex3f(margin, margin, margin);
            return new AABB3f(aabb.Min - offset, aabb.Max + offset);
        }

        #endregion

        #region 합집합 및 교집합

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
        /// 여러 AABB의 합집합
        /// </summary>
        public static AABB3f Union(params AABB3f[] aabbs)
        {
            if (aabbs == null || aabbs.Length == 0)
                return Zero;

            AABB3f result = aabbs[0];
            for (int i = 1; i < aabbs.Length; i++)
            {
                result = Union(in result, in aabbs[i]);
            }
            return result;
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

            hasIntersection = IsValid(in result);
            return hasIntersection ? result : Zero;
        }

        /// <summary>
        /// 점을 포함하도록 AABB 확장
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB3f ExpandToInclude(in AABB3f aabb, Vertex3f point)
        {
            return new AABB3f(
                new Vertex3f(
                    MathFast.Min(aabb.Min.x, point.x),
                    MathFast.Min(aabb.Min.y, point.y),
                    MathFast.Min(aabb.Min.z, point.z)
                ),
                new Vertex3f(
                    MathFast.Max(aabb.Max.x, point.x),
                    MathFast.Max(aabb.Max.y, point.y),
                    MathFast.Max(aabb.Max.z, point.z)
                )
            );
        }

        #endregion

        #region 거리 계산

        /// <summary>
        /// 점에서 AABB까지의 최단 거리 제곱
        /// </summary>
        public static float DistanceSquared(in AABB3f aabb, Vertex3f point)
        {
            float sqDist = 0.0f;

            // X축
            if (point.x < aabb.Min.x)
            {
                float d = aabb.Min.x - point.x;
                sqDist += d * d;
            }
            else if (point.x > aabb.Max.x)
            {
                float d = point.x - aabb.Max.x;
                sqDist += d * d;
            }

            // Y축
            if (point.y < aabb.Min.y)
            {
                float d = aabb.Min.y - point.y;
                sqDist += d * d;
            }
            else if (point.y > aabb.Max.y)
            {
                float d = point.y - aabb.Max.y;
                sqDist += d * d;
            }

            // Z축
            if (point.z < aabb.Min.z)
            {
                float d = aabb.Min.z - point.z;
                sqDist += d * d;
            }
            else if (point.z > aabb.Max.z)
            {
                float d = point.z - aabb.Max.z;
                sqDist += d * d;
            }

            return sqDist;
        }

        /// <summary>
        /// 점에서 AABB까지의 최단 거리
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(in AABB3f aabb, Vertex3f point)
        {
            return MathFast.Sqrt(DistanceSquared(in aabb, point));
        }

        /// <summary>
        /// AABB 상의 가장 가까운 점 찾기
        /// </summary>
        public static Vertex3f ClosestPoint(in AABB3f aabb, Vertex3f point)
        {
            return new Vertex3f(
                MathFast.Clamp(point.x, aabb.Min.x, aabb.Max.x),
                MathFast.Clamp(point.y, aabb.Min.y, aabb.Max.y),
                MathFast.Clamp(point.z, aabb.Min.z, aabb.Max.z)
            );
        }

        #endregion

        #region 디버그 및 시각화

        /// <summary>
        /// AABB를 모델 행렬로 변환 (렌더링용)
        /// </summary>
        public static Matrix4x4f GetModelMatrix(in AABB3f aabb, float tightScale = 1.0f)
        {
            Vertex3f size = aabb.Size * 0.5f * tightScale;
            Vertex3f center = aabb.Center;

            Matrix4x4f scale = Matrix4x4f.Scaled(size.x, size.y, size.z);
            Matrix4x4f translation = Matrix4x4f.Translated(center.x, center.y, center.z);

            return translation * scale;
        }

        #endregion
    }
}