using FastMath;
using OpenGL;
using System;
using System.Runtime.CompilerServices;
using ZetaExt;

namespace Geometry
{
    /// <summary>
    /// OBB3f 구조체를 위한 정적 유틸리티 클래스
    /// 충돌 검사, 변환, PCA 기반 OBB 생성 등의 기능 제공
    /// </summary>
    public static class OBB3fHelper
    {
        private const float TIGHT_SCALED_SIZE = 1.01f;

        #region 생성

        /// <summary>
        /// 원점 중심의 단위 OBB 반환
        /// </summary>
        public static OBB3f Identity => new OBB3f(
            Vertex3f.Zero,
            Vertex3f.One,
            Vertex3f.UnitX,
            Vertex3f.UnitY,
            Vertex3f.UnitZ
        );

        /// <summary>
        /// 크기가 거의 0인 OBB 반환
        /// </summary>
        public static OBB3f ZeroSize => new OBB3f(
            Vertex3f.Zero,
            Vertex3f.One * 0.0001f,
            Vertex3f.UnitX,
            Vertex3f.UnitY,
            Vertex3f.UnitZ
        );

        /// <summary>
        /// 점 배열로부터 최적의 OBB 생성 (PCA 알고리즘)
        /// </summary>
        public static OBB3f FromPoints(Vertex3f[] vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return ZeroSize;

            OBBUtility.CalculateOBB(vertices, out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);

            return new OBB3f(
                center,
                size,
                axis[0],
                axis[1],
                axis[2]
            );
        }

        /// <summary>
        /// 변환 행렬과 로컬 AABB로부터 OBB 생성
        /// </summary>
        public static OBB3f FromTransformAndLocalAABB(Matrix4x4f worldMatrix, in AABB3f localAABB)
        {
            // 로컬 AABB의 크기
            Vertex3f halfSize = localAABB.Size * 0.5f;

            // 월드 행렬에서 스케일 추출
            Vertex3f scaleX = new Vertex3f(worldMatrix[0, 0], worldMatrix[0, 1], worldMatrix[0, 2]) * halfSize.x;
            Vertex3f scaleY = new Vertex3f(worldMatrix[1, 0], worldMatrix[1, 1], worldMatrix[1, 2]) * halfSize.y;
            Vertex3f scaleZ = new Vertex3f(worldMatrix[2, 0], worldMatrix[2, 1], worldMatrix[2, 2]) * halfSize.z;

            // 로컬 중심을 월드 공간으로 변환
            Vertex3f center = localAABB.Center;
            Vertex4f worldCenter4 = worldMatrix * new Vertex4f(center.x, center.y, center.z, 1f);
            Vertex3f worldCenter = new Vertex3f(
                worldCenter4.x / worldCenter4.w,
                worldCenter4.y / worldCenter4.w,
                worldCenter4.z / worldCenter4.w
            );

            return new OBB3f(new Matrix4x4f(
                scaleX.x, scaleX.y, scaleX.z, 0,
                scaleY.x, scaleY.y, scaleY.z, 0,
                scaleZ.x, scaleZ.y, scaleZ.z, 0,
                worldCenter.x, worldCenter.y, worldCenter.z, 1
            ));
        }

        #endregion

        #region 8개 코너 및 변환

        /// <summary>
        /// OBB의 8개 코너 점 반환
        /// </summary>
        public static Vertex3f[] GetCorners(in OBB3f obb)
        {
            Vertex3f center = obb.Center;
            Vertex3f xAxis = obb.XAxis;
            Vertex3f yAxis = obb.YAxis;
            Vertex3f zAxis = obb.ZAxis;

            return new Vertex3f[8]
            {
                center - xAxis - yAxis - zAxis, // 0: ---
                center + xAxis - yAxis - zAxis, // 1: +--
                center + xAxis + yAxis - zAxis, // 2: ++-
                center - xAxis + yAxis - zAxis, // 3: -+-
                center - xAxis - yAxis + zAxis, // 4: --+
                center + xAxis - yAxis + zAxis, // 5: +-+
                center + xAxis + yAxis + zAxis, // 6: +++
                center - xAxis + yAxis + zAxis  // 7: -++
            };
        }

        /// <summary>
        /// OBB를 감싸는 AABB 계산
        /// </summary>
        public static AABB3f ToAABB(in OBB3f obb)
        {
            Vertex3f[] corners = GetCorners(in obb);
            Vertex3f min = corners[0];
            Vertex3f max = corners[0];

            for (int i = 1; i < 8; i++)
            {
                min.x = MathFast.Min(min.x, corners[i].x);
                min.y = MathFast.Min(min.y, corners[i].y);
                min.z = MathFast.Min(min.z, corners[i].z);

                max.x = MathFast.Max(max.x, corners[i].x);
                max.y = MathFast.Max(max.y, corners[i].y);
                max.z = MathFast.Max(max.z, corners[i].z);
            }

            return new AABB3f(min, max);
        }

        /// <summary>
        /// 행렬을 적용하여 OBB 변환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OBB3f Transform(in OBB3f obb, Matrix4x4f matrix)
        {
            return new OBB3f(matrix * obb.Transform);
        }

        /// <summary>
        /// OBB를 회전 (중심 기준)
        /// </summary>
        public static OBB3f Rotate(in OBB3f obb, Matrix4x4f rotation)
        {
            Vertex3f center = obb.Center;
            Vertex3f halfExtents = obb.HalfExtents;

            // 회전된 축
            Vertex3f newX =  new Vertex3f(rotation[0, 0], rotation[0, 1], rotation[0, 2]).Normalized * halfExtents.x;
            Vertex3f newY =  new Vertex3f(rotation[1, 0], rotation[1, 1], rotation[1, 2]).Normalized * halfExtents.y;
            Vertex3f newZ =  new Vertex3f(rotation[2, 0], rotation[2, 1], rotation[2, 2]).Normalized * halfExtents.z;

            return new OBB3f(new Matrix4x4f(
                newX.x, newX.y, newX.z, 0,
                newY.x, newY.y, newY.z, 0,
                newZ.x, newZ.y, newZ.z, 0,
                center.x, center.y, center.z, 1
            ));
        }

        #endregion

        #region 충돌 검사 (SAT - Separating Axis Theorem)

        /// <summary>
        /// 두 OBB가 교차하는지 검사 (SAT 알고리즘)
        /// </summary>
        public static bool Intersects(in OBB3f a, in OBB3f b)
        {
            // 15개의 분리축 테스트
            // - A의 3개 축
            // - B의 3개 축
            // - A와 B 축의 외적 9개

            Vertex3f[] axesA = { a.XAxisNormalized, a.YAxisNormalized, a.ZAxisNormalized };
            Vertex3f[] axesB = { b.XAxisNormalized, b.YAxisNormalized, b.ZAxisNormalized };

            Vertex3f centerA = a.Center;
            Vertex3f centerB = b.Center;
            Vertex3f halfExtentsA = a.HalfExtents;
            Vertex3f halfExtentsB = b.HalfExtents;

            // A의 축으로 테스트
            for (int i = 0; i < 3; i++)
            {
                if (!TestAxis(axesA[i], in a, in b, centerA, centerB, halfExtentsA, halfExtentsB, axesA, axesB))
                    return false;
            }

            // B의 축으로 테스트
            for (int i = 0; i < 3; i++)
            {
                if (!TestAxis(axesB[i], in a, in b, centerA, centerB, halfExtentsA, halfExtentsB, axesA, axesB))
                    return false;
            }

            // 외적 축으로 테스트
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Vertex3f axis = axesA[i].Cross(axesB[j]);
                    float lengthSq = axis.x * axis.x + axis.y * axis.y + axis.z * axis.z;

                    if (lengthSq < 1e-6f) continue; // 거의 평행한 축 스킵

                    axis = axis.Normalized;
                    if (!TestAxis(axis, in a, in b, centerA, centerB, halfExtentsA, halfExtentsB, axesA, axesB))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// SAT 분리축 테스트
        /// </summary>
        private static bool TestAxis(Vertex3f axis, in OBB3f a, in OBB3f b,
            Vertex3f centerA, Vertex3f centerB,
            Vertex3f halfExtentsA, Vertex3f halfExtentsB,
            Vertex3f[] axesA, Vertex3f[] axesB)
        {
            // 중심 간 거리 투영
            float distance = MathFast.Abs((centerB - centerA).Dot(axis));

            // A의 반지름 계산
            float radiusA = 0;
            for (int i = 0; i < 3; i++)
            {
                float extent = i == 0 ? halfExtentsA.x : (i == 1 ? halfExtentsA.y : halfExtentsA.z);
                radiusA += MathFast.Abs(axesA[i].Dot(axis)) * extent;
            }

            // B의 반지름 계산
            float radiusB = 0;
            for (int i = 0; i < 3; i++)
            {
                float extent = i == 0 ? halfExtentsB.x : (i == 1 ? halfExtentsB.y : halfExtentsB.z);
                radiusB += MathFast.Abs(axesB[i].Dot(axis)) * extent;
            }

            // 분리축이 발견되면 충돌하지 않음
            return distance <= radiusA + radiusB;
        }

        /// <summary>
        /// OBB와 점의 충돌 검사
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(in OBB3f obb, Vertex3f point)
        {
            // 점을 OBB의 로컬 공간으로 변환
            Vertex3f localPoint = point - obb.Center;

            // 각 축에 투영하여 범위 체크
            Vertex3f halfExtents = obb.HalfExtents;

            float projX = MathFast.Abs(localPoint.Dot(obb.XAxisNormalized));
            if (projX > halfExtents.x) return false;

            float projY = MathFast.Abs(localPoint.Dot(obb.YAxisNormalized));
            if (projY > halfExtents.y) return false;

            float projZ = MathFast.Abs(localPoint.Dot(obb.ZAxisNormalized));
            if (projZ > halfExtents.z) return false;

            return true;
        }

        /// <summary>
        /// 광선과 OBB의 교차 검사
        /// </summary>
        public static bool IntersectsRay(in OBB3f obb, Vertex3f rayOrigin,
            Vertex3f rayDirection, out float tMin, out float tMax)
        {
            tMin = float.MinValue;
            tMax = float.MaxValue;

            Vertex3f[] axes = { obb.XAxisNormalized, obb.YAxisNormalized, obb.ZAxisNormalized };
            Vertex3f halfExtents = obb.HalfExtents;
            Vertex3f delta = obb.Center - rayOrigin;

            for (int i = 0; i < 3; i++)
            {
                Vertex3f axis = axes[i];
                float extent = i == 0 ? halfExtents.x : (i == 1 ? halfExtents.y : halfExtents.z);

                float e = axis.Dot(delta);
                float f = axis.Dot(rayDirection);

                if (MathFast.Abs(f) > 1e-6f)
                {
                    float t1 = (e + extent) / f;
                    float t2 = (e - extent) / f;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    tMin = MathFast.Max(tMin, t1);
                    tMax = MathFast.Min(tMax, t2);

                    if (tMin > tMax || tMax < 0)
                        return false;
                }
                else if (-e - extent > 0 || -e + extent < 0)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 합집합 및 거리

        /// <summary>
        /// 두 OBB의 합집합 (새로운 OBB 생성)
        /// </summary>
        public static OBB3f Union(in OBB3f a, in OBB3f b)
        {
            Vertex3f[] cornersA = GetCorners(in a);
            Vertex3f[] cornersB = GetCorners(in b);

            Vertex3f[] allCorners = new Vertex3f[16];
            Array.Copy(cornersA, 0, allCorners, 0, 8);
            Array.Copy(cornersB, 0, allCorners, 8, 8);

            return FromPoints(allCorners);
        }

        /// <summary>
        /// 점에서 OBB까지의 최단 거리 제곱
        /// </summary>
        public static float DistanceSquared(in OBB3f obb, Vertex3f point)
        {
            Vertex3f closestPoint = ClosestPoint(in obb, point);
            Vertex3f diff = point - closestPoint;
            return diff.x * diff.x + diff.y * diff.y + diff.z * diff.z;
        }

        /// <summary>
        /// OBB 상의 가장 가까운 점 찾기
        /// </summary>
        public static Vertex3f ClosestPoint(in OBB3f obb, Vertex3f point)
        {
            Vertex3f localPoint = point - obb.Center;
            Vertex3f result = obb.Center;

            Vertex3f[] axes = { obb.XAxisNormalized, obb.YAxisNormalized, obb.ZAxisNormalized };
            Vertex3f halfExtents = obb.HalfExtents;

            for (int i = 0; i < 3; i++)
            {
                float extent = i == 0 ? halfExtents.x : (i == 1 ? halfExtents.y : halfExtents.z);
                float distance = localPoint.Dot(axes[i]);
                distance = MathFast.Clamp(distance, -extent, extent);
                result += axes[i] * distance;
            }

            return result;
        }

        #endregion

        #region 렌더링 및 디버그

        /// <summary>
        /// OBB를 렌더링하기 위한 모델 행렬 생성
        /// </summary>
        public static Matrix4x4f GetModelMatrix(in OBB3f obb, float tightScale = TIGHT_SCALED_SIZE)
        {
            Vertex3f center = obb.Center;
            Vertex3f size = obb.Size * tightScale;

            // 회전 행렬 추출
            Matrix4x4f rotation = Matrix4x4f.Identity;
            Vertex3f xAxis = obb.XAxisNormalized;
            Vertex3f yAxis = obb.YAxisNormalized;
            Vertex3f zAxis = obb.ZAxisNormalized;

            // 행렬에 축 설정
            rotation[0, 0] = xAxis.x;
            rotation[0, 1] = xAxis.y;
            rotation[0, 2] = xAxis.z;
            rotation[1, 0] = yAxis.x;
            rotation[1, 1] = yAxis.y;
            rotation[1, 2] = yAxis.z;
            rotation[2, 0] = zAxis.x;
            rotation[2, 1] = zAxis.y;
            rotation[2, 2] = zAxis.z;

            Matrix4x4f scale = Matrix4x4f.Scaled(size.x, size.y, size.z);
            Matrix4x4f translation = Matrix4x4f.Translated(center.x, center.y, center.z);

            return translation * rotation * scale; // T * R * S
        }

        #endregion

        #region 검증

        /// <summary>
        /// OBB의 축들이 직교하는지 검사
        /// </summary>
        public static bool IsOrthogonal(in OBB3f obb, float tolerance = 0.01f)
        {
            Vertex3f x = obb.XAxisNormalized;
            Vertex3f y = obb.YAxisNormalized;
            Vertex3f z = obb.ZAxisNormalized;

            float dotXY = MathFast.Abs(x.Dot(y));
            float dotYZ = MathFast.Abs(y.Dot(z));
            float dotZX = MathFast.Abs(z.Dot(x));

            return dotXY < tolerance && dotYZ < tolerance && dotZX < tolerance;
        }

        #endregion
    }
}