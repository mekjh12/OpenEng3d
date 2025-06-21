using OpenGL;
using System;
using ZetaExt;

namespace Geometry
{
    /// <summary>
    /// 2D 공간의 선분을 표현하는 구조체
    /// </summary>
    public struct Line2d
    {
        private Vertex2f _start;
        private Vertex2f _end;
        private Vertex2f _direction;  // 선분의 방향 벡터
        private float _length;        // 선분의 길이

        public Vertex2f Start => _start;
        public Vertex2f End => _end;
        public Vertex2f Direction => _direction;
        public float Length => _length;

        /// <summary>
        /// AABB 영역 안에 점이 포함되어 있는지 검사한다
        /// </summary>
        public static bool Contains(Vertex2f lower, Vertex2f upper, Vertex2f point)
        {
            return point.x >= lower.x && point.x <= upper.x &&
                   point.y >= lower.y && point.y <= upper.y;
        }

        /// <summary>
        /// 좌표값으로 선분을 생성한다
        /// </summary>
        public Line2d(float x1, float y1, float x2, float y2)
        {
            _start = new Vertex2f(x1, y1);
            _end = new Vertex2f(x2, y2);
            _direction = (_end - _start).Normalized;
            _length = (_end - _start).Norm();
        }

        /// <summary>
        /// 두 점으로 선분을 생성한다
        /// </summary>
        public Line2d(Vertex2f start, Vertex2f end)
        {
            _start = start;
            _end = end;
            _direction = (_end - _start).Normalized;
            _length = (_end - _start).Norm();
        }

        /// <summary>
        /// 선분 위의 특정 지점을 반환한다. t는 0~1 사이의 값이다
        /// </summary>
        public Vertex2f PointAt(float t)
        {
            t = t.Clamp(0f, 1f);
            return _start + (_direction * _length * t);
        }

        /// <summary>
        /// 점이 선분으로부터 얼마나 떨어져 있는지 계산한다
        /// </summary>
        public float DistanceTo(Vertex2f point)
        {
            Vertex2f p = point - _start;
            float t = p.Dot(_direction);
            // 시작점 이전
            if (t < 0)
                return (point - _start).Norm();
            // 끝점 이후
            if (t > _length)
                return (point - _end).Norm();
            // 선분 위 가장 가까운 점까지의 거리
            return (p - _direction * t).Norm();
        }

        /// <summary>
        /// 점이 선분 위에 있는지 확인한다
        /// </summary>
        public bool Contains(Vertex2f point, float epsilon = 0.0001f)
        {
            return DistanceTo(point) < epsilon;
        }

        public override string ToString()
        {
            return $"Line2D: ({_start.x}, {_start.y}) -> ({_end.x}, {_end.y})";
        }

        /// <summary>
        /// 두 선분의 교차를 검사한다
        /// </summary>
        public static bool LineIntersect(Line2d line1, Line2d line2)
        {
            float x1 = line1.Start.x;
            float y1 = line1.Start.y;
            float x2 = line1.End.x;
            float y2 = line1.End.y;

            float x3 = line2.Start.x;
            float y3 = line2.Start.y;
            float x4 = line2.End.x;
            float y4 = line2.End.y;

            // 선분 교차 판별식
            float denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

            // 두 선분이 평행한 경우
            if (Math.Abs(denominator) < float.Epsilon)
                return false;

            float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;
            float s = ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) / denominator;

            // 교차점이 두 선분 위에 있는 경우
            return t >= 0 && t <= 1 && s >= 0 && s <= 1;
        }
    }
}