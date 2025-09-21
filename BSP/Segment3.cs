using OpenGL;
using ZetaExt;

namespace BSP
{
    public class Segment3
    {
        Vertex3f _start;
        Vertex3f _end;
        Vertex3f _line;

        public string EquationToString => $"{_line.x}x+{_line.y}y+{_line.z}=0";

        /// <summary>
        /// 선분의 직선의 방정식 (법선벡터는 a부터 b로의 이동에서 왼쪽 방향이다.)
        /// </summary>
        public Vertex3f LineEquation => _line;

        /// <summary>
        /// 선분의 시작점
        /// </summary>
        public Vertex3f Start
        {
            get => _start;
            set
            {
                _start = value;
                Vertex2f u = _end.xy() - _start.xy();
                Vertex2f n = new Vertex2f(-u.y, u.x).Normalized;
                float c = n.Dot(_start.xy());
                _line = new Vertex3f(n.x, n.y, -c);
            }
        }

        /// <summary>
        /// 선분의 종점
        /// </summary>
        public Vertex3f End
        {
            get => _end;
            set
            {
                _end = value;
                Vertex2f u = _end.xy() - _start.xy();
                Vertex2f n = new Vertex2f(-u.y, u.x).Normalized;
                float c = n.Dot(_start.xy());
                _line = new Vertex3f(n.x, n.y, -c);
            }
        }

        /// <summary>
        /// 선분의 길이
        /// </summary>
        public float Length => (_end - _start).Length();

        /// <summary>
        /// 영역의 넓이
        /// </summary>
        public float Area => Length * (_start.z + _end.z) * 0.5f;

        /// <summary>
        /// 생성자, 법선벡터는 a부터 b로의 이동에서 왼쪽 방향이다.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public Segment3(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            _start = new Vertex3f(x1, y1, z1);
            _end = new Vertex3f(x2, y2, z2);
            Vertex2f u = _end.xy() - _start.xy();
            Vertex2f n = new Vertex2f(-u.y, u.x).Normalized;
            float c = n.Dot(_start.xy());
            _line = new Vertex3f(n.x, n.y, -c);
        }

        /// <summary>
        /// 생성자, 법선벡터는 a부터 b로의 이동에서 왼쪽 방향이다.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public Segment3(Vertex3f left, Vertex3f right)
        {
            _start = left;
            _end = right;
            Vertex2f u = _end.xy() - _start.xy();
            Vertex2f n = new Vertex2f(-u.y, u.x).Normalized;
            float c = n.Dot(_start.xy());
            _line = new Vertex3f(n.x, n.y, -c);
        }

        /// <summary>
        /// 현재 선분이 다른 선분을 나눌 수 있는지 여부를 반환한다.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public bool IsSplitNeed(Segment3 segment)
        {
            return _line.Dot(segment._start.xy()) * _line.Dot(segment._end.xy()) < -0.1f ;
        }

        public bool IsFront(Segment3 segment)
        {
            return _line.Dot(segment._start.xy()) >= 0 && _line.Dot(segment._end.xy()) >= 0;
        }

        public bool IsBack(Segment3 segment)
        {
            return _line.Dot(segment._start.xy()) < 0 && _line.Dot(segment._end.xy()) < 0;
        }

        /// <summary>
        /// 직선에 대한 선분의 교차점을 가져온다.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public Vertex3f[] CrossPoint(Segment3 segment)
        {
            float dot1 = _line.Dot(segment._start.xy());
            float dot2 = _line.Dot(segment._end.xy());
            float d1 = MathF.Abs(dot1);
            float d2 = MathF.Abs(dot2);
            float t1 = 0.99999f * d2 / (d1 + d2);
            float t2 = 1.00001f * d2 / (d1 + d2);

            Vertex3f cp1 = segment._end + (segment._start - segment._end) * t1;
            Vertex3f cp2 = segment._end + (segment._start - segment._end) * t2;           
            
            return new Vertex3f[] { cp1, cp2 };
        }


    }
}
