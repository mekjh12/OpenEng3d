using FastMath;
using OpenGL;
using System;

namespace Animate
{
    /// <summary>
    /// 3D 공간상의 점을 나타내는 구조체 (위치, 색상, 크기 정보 포함)<br/>
    /// - 3D 애니메이션에서 디버깅이나 시각화 목적으로 사용<br/>
    /// - 본 위치 표시, IK 솔버 결과 시각화 등에 활용
    /// </summary>
    public struct ColorPoint
    {
        Vertex3f _pos;   // 3D 공간상의 위치 (x, y, z 좌표)
        Vertex3f _color; // 점의 색상 (RGB 값, 0.0~1.0 범위)
        float _size;     // 점의 렌더링 크기 (단위: 월드 좌표계)

        public float Size
        {
            get => _size;
            set => _size = value;
        }

        public Vertex3f Color3
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>RGBA 색상 (알파값 1.0으로 고정)</summary>
        public Vertex4f Color4 => new Vertex4f(_color.x, _color.y, _color.z, 1.0f);

        public Vertex3f Position
        {
            get => _pos;
            set => _pos = value;
        }

        public ColorPoint(float x, float y, float z, float r, float g, float b, float size)
        {
            _pos = new Vertex3f(x, y, z);
            _color = new Vertex3f(r, g, b);
            _size = size;
        }

        public ColorPoint(float x, float y, float z, float r, float g, float b)
        {
            _pos = new Vertex3f(x, y, z);
            _color = new Vertex3f(r, g, b);
            _size = 0.02f;
        }

        /// <summary>위치만 지정 (기본: 빨간색, 크기 0.02f)</summary>
        public ColorPoint(float x, float y, float z)
        {
            _pos = new Vertex3f(x, y, z);
            _color = new Vertex3f(1, 0, 0);
            _size = 0.02f;
        }

        public ColorPoint(Vertex3f pos, Vertex3f color)
        {
            _pos = pos;
            _color = color;
            _size = 0.02f;
        }

        public ColorPoint(Vertex3f pos, float r, float g, float b)
        {
            _pos = pos;
            _color = new Vertex3f(r, g, b);
            _size = 0.02f;
        }

        public ColorPoint(Vertex3f pos, float r, float g, float b, float size)
        {
            _pos = pos;
            _color = new Vertex3f(r, g, b);
            _size = size;
        }

        public override string ToString()
        {
            return $"ColorPoint(Pos: {_pos}, Color: {_color}, Size: {_size:F3})";
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorPoint other)
            {
                return _pos.Equals(other._pos) &&
                       _color.Equals(other._color) &&
                       MathFast.Abs(_size - other._size) < 0.0001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _pos.GetHashCode() ^ _color.GetHashCode() ^ _size.GetHashCode();
        }

        /// <summary>빨간색 점 생성</summary>
        public static ColorPoint CreateRed(Vertex3f position, float size = 0.02f)
        {
            return new ColorPoint(position, 1.0f, 0.0f, 0.0f, size);
        }

        /// <summary>초록색 점 생성</summary>
        public static ColorPoint CreateGreen(Vertex3f position, float size = 0.02f)
        {
            return new ColorPoint(position, 0.0f, 1.0f, 0.0f, size);
        }

        /// <summary>파란색 점 생성</summary>
        public static ColorPoint CreateBlue(Vertex3f position, float size = 0.02f)
        {
            return new ColorPoint(position, 0.0f, 0.0f, 1.0f, size);
        }

        /// <summary>흰색 점 생성</summary>
        public static ColorPoint CreateWhite(Vertex3f position, float size = 0.02f)
        {
            return new ColorPoint(position, 1.0f, 1.0f, 1.0f, size);
        }
    }
}