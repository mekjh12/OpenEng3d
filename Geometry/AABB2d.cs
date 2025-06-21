using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    /// <summary>
    /// 2D 축 정렬 경계 상자(Axis-Aligned Bounding Box)를 나타내는 구조체
    /// </summary>
    public struct AABB2d
    {
        private Vertex2f _min;    // 좌하단 점
        private Vertex2f _max;    // 우상단 점
        private Vertex2f _center; // 중심점
        private Vertex2f _size;   // 크기

        public Vertex2f Min => _min;
        public Vertex2f Max => _max;
        public Vertex2f Center => _center;
        public Vertex2f Size => _size;

        /// <summary>
        /// 2D 좌표를 사용하여 AABB를 생성합니다
        /// </summary>
        public AABB2d(Vertex2f min, Vertex2f max)
        {
            _min = min;
            _max = max;
            _center = (min + max) * 0.5f;
            _size = max - min;
        }

        /// <summary>
        /// 3D 좌표의 XY 평면 투영을 사용하여 AABB를 생성합니다
        /// </summary>
        public AABB2d(Vertex3f min, Vertex3f max)
        {
            _min = min.xy();
            _max = max.xy();
            _center = (_min + _max) * 0.5f;
            _size = _max - _min;
        }

        /// <summary>
        /// 주어진 점이 AABB 영역 안에 포함되어 있는지 검사합니다
        /// </summary>
        public bool Contains(Vertex2f point)
        {
            return point.x >= _min.x && point.x <= _max.x &&
                   point.y >= _min.y && point.y <= _max.y;
        }

        /// <summary>
        /// 다른 AABB와 겹치는 영역이 있는지 검사합니다
        /// </summary>
        public bool Intersects(AABB2d other)
        {
            return _min.x <= other._max.x && _max.x >= other._min.x &&
                   _min.y <= other._max.y && _max.y >= other._min.y;
        }

        /// <summary>
        /// AABB의 경계를 확장하여 주어진 점을 포함하도록 합니다
        /// </summary>
        public void Expand(Vertex2f point)
        {
            _min.x = Math.Min(_min.x, point.x);
            _min.y = Math.Min(_min.y, point.y);
            _max.x = Math.Max(_max.x, point.x);
            _max.y = Math.Max(_max.y, point.y);

            _center = (_min + _max) * 0.5f;
            _size = _max - _min;
        }

        /// <summary>
        /// 두 AABB의 겹치는 영역(교집합)을 새로운 AABB로 반환합니다
        /// </summary>
        public static AABB2d Intersection(AABB2d a, AABB2d b)
        {
            Vertex2f min = new Vertex2f(
                Math.Max(a._min.x, b._min.x),
                Math.Max(a._min.y, b._min.y)
            );

            Vertex2f max = new Vertex2f(
                Math.Min(a._max.x, b._max.x),
                Math.Min(a._max.y, b._max.y)
            );

            return new AABB2d(min, max);
        }

        /// <summary>
        /// 두 AABB를 모두 포함하는 최소 크기의 새로운 AABB를 반환합니다
        /// </summary>
        public static AABB2d Union(AABB2d a, AABB2d b)
        {
            Vertex2f min = new Vertex2f(
                Math.Min(a._min.x, b._min.x),
                Math.Min(a._min.y, b._min.y)
            );

            Vertex2f max = new Vertex2f(
                Math.Max(a._max.x, b._max.x),
                Math.Max(a._max.y, b._max.y)
            );

            return new AABB2d(min, max);
        }

        public override string ToString()
        {
            return $"AABB2D: Min({_min.x}, {_min.y}), Max({_max.x}, {_max.y})";
        }

        /// <summary>
        /// AABB와 카메라 절두체(프러스텀)의 2D 투영이 서로 겹치는지 검사합니다
        /// </summary>
        public static bool IsIntersectWithFrustum2D(AABB2d aabb, Camera camera)
        {
            // 카메라 절두체의 near plane 2D 투영을 위한 4개의 점을 생성합니다
            var camPos = camera.Position.xy();
            var camDir = (camera.PivotPosition - camera.Position).xy().Normalized;

            // 카메라 좌표계의 오른쪽 방향 벡터를 계산합니다
            var right = new Vertex2f(camDir.y, -camDir.x).Normalized;
            float fov2 = camera.FOV.ToRadian() * 0.5f;

            // near plane에서의 절두체 너비의 절반을 계산합니다
            float nearHalfWidth = camera.NEAR * (float)Math.Tan(fov2);

            // 절두체의 near plane 모서리 점들을 계산합니다
            Vertex2f nearCenter = camPos + camDir * camera.NEAR;
            Vertex2f nearRight = nearCenter + right * nearHalfWidth;
            Vertex2f nearLeft = nearCenter - right * nearHalfWidth;

            // far plane에서의 절두체 너비의 절반을 계산합니다
            float farHalfWidth = camera.FAR * (float)Math.Tan(fov2);

            // 절두체의 far plane 모서리 점들을 계산합니다
            Vertex2f farCenter = camPos + camDir * camera.FAR;
            Vertex2f farRight = farCenter + right * farHalfWidth;
            Vertex2f farLeft = farCenter - right * farHalfWidth;

            // 절두체를 구성하는 3개의 경계선을 정의합니다
            Line2d[] frustumLines = new Line2d[]
            {
               new Line2d(farLeft, farRight),  // far plane의 선
               new Line2d(camPos, farLeft),    // 왼쪽 경계선
               new Line2d(camPos, farRight)    // 오른쪽 경계선
            };

            // AABB의 네 꼭지점 중 하나라도 절두체 내부에 있는지 검사합니다
            if (IsPointInFrustum(aabb.Min, camPos, frustumLines) ||
                IsPointInFrustum(aabb.Max, camPos, frustumLines) ||
                IsPointInFrustum(new Vertex2f(aabb.Min.x, aabb.Max.y), camPos, frustumLines) ||
                IsPointInFrustum(new Vertex2f(aabb.Max.x, aabb.Min.y), camPos, frustumLines))
                return true;

            // AABB의 네 변이 절두체의 경계선과 교차하는지 검사합니다
            Line2d[] aabbLines = new Line2d[]
            {
               new Line2d(aabb.Min.x, aabb.Min.y, aabb.Max.x, aabb.Min.y), // 아래쪽 변
               new Line2d(aabb.Max.x, aabb.Min.y, aabb.Max.x, aabb.Max.y), // 오른쪽 변
               new Line2d(aabb.Max.x, aabb.Max.y, aabb.Min.x, aabb.Max.y), // 위쪽 변
               new Line2d(aabb.Min.x, aabb.Max.y, aabb.Min.x, aabb.Min.y)  // 왼쪽 변
            };

            foreach (var aabbLine in aabbLines)
            {
                foreach (var frustumLine in frustumLines)
                {
                    if (LineIntersect(aabbLine, frustumLine))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 주어진 점이 절두체 내부에 있는지 검사합니다
        /// </summary>
        private static bool IsPointInFrustum(Vertex2f point, Vertex2f camPos, Line2d[] frustumLines)
        {
            // 모든 절두체 경계선에 대해 점이 왼쪽에 있는지 검사합니다
            foreach (var line in frustumLines)
            {
                if (!IsPointLeftOfLine(point, line.Start, line.End))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 점이 주어진 선분의 왼쪽에 있는지 검사합니다 (외적 사용)
        /// </summary>
        private static bool IsPointLeftOfLine(Vertex2f point, Vertex2f lineStart, Vertex2f lineEnd)
        {
            return ((lineEnd.x - lineStart.x) * (point.y - lineStart.y) -
                    (lineEnd.y - lineStart.y) * (point.x - lineStart.x)) > 0;
        }

        /// <summary>
        /// 두 선분이 서로 교차하는지 검사합니다
        /// </summary>
        private static bool LineIntersect(Line2d line1, Line2d line2)
        {
            float x1 = line1.Start.x;
            float y1 = line1.Start.y;
            float x2 = line1.End.x;
            float y2 = line1.End.y;

            float x3 = line2.Start.x;
            float y3 = line2.Start.y;
            float x4 = line2.End.x;
            float y4 = line2.End.y;

            float denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

            if (Math.Abs(denominator) < float.Epsilon)
                return false;

            float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;
            float s = ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) / denominator;

            return t >= 0 && t <= 1 && s >= 0 && s <= 1;
        }
    }
}