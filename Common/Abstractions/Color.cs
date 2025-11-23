using OpenGL;

namespace Common.Abstractions
{
    public struct Color4
    {
        public float r, g, b, a;

        // 0-1 사이 값으로 생성자
        public Color4(float r, float g, float b, float a = 1.0f)
        {
            this.r = Mathf.Clamp01(r);
            this.g = Mathf.Clamp01(g);
            this.b = Mathf.Clamp01(b);
            this.a = Mathf.Clamp01(a);
        }

        // 0-255 사이 값으로 생성자 
        public static Color4 FromRGB(byte r, byte g, byte b, byte a = 255)
        {
            return new Color4(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        // 자주 사용하는 색상 정의
        public static Color4 white => new Color4(1, 1, 1);
        public static Color4 black => new Color4(0, 0, 0);
        public static Color4 red => new Color4(1, 0, 0);
        public static Color4 green => new Color4(0, 1, 0);
        public static Color4 blue => new Color4(0, 0, 1);
        public static Color4 yellow => new Color4(1, 1, 0);

        // 색상 연산
        public static Color4 operator *(Color4 c, float f)
        {
            return new Color4(c.r * f, c.g * f, c.b * f, c.a);
        }

        public static Color4 operator +(Color4 a, Color4 b)
        {
            return new Color4(
                a.r + b.r,
                a.g + b.g,
                a.b + b.b,
                a.a + b.a
            );
        }
        // Vector3f로 암시적 변환 (RGB만)
        public static implicit operator Vertex3f(Color4 color)
        {
            return new Vertex3f(color.r, color.g, color.b);
        }

        // Vector4f로 암시적 변환 (RGBA)
        public static implicit operator Vertex4f(Color4 color)
        {
            return new Vertex4f(color.r, color.g, color.b, color.a);
        }

        // Vector3f에서 Color로 명시적 변환
        public static explicit operator Color4(Vertex3f vector)
        {
            return new Color4(vector.x, vector.y, vector.z, 1.0f);
        }

        // Vector4f에서 Color로 명시적 변환
        public static explicit operator Color4(Vertex4f vector)
        {
            return new Color4(vector.x, vector.y, vector.z, vector.w);
        }

        // 메서드 형태로도 제공
        public Vertex3f ToVector3f()
        {
            return new Vertex3f(r, g, b);
        }

        public Vertex4f ToVector4f()
        {
            return new Vertex4f(r, g, b, a);
        }


        // 색상을 HTML/HEx 형식으로 변환
        public string ToHexString()
        {
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}",
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255),
                (byte)(a * 255));
        }

        // 선형 보간
        public static Color4 Lerp(Color4 a, Color4 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color4(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t
            );
        }

        public override string ToString()
        {
            return $"RGBA({r:F2}, {g:F2}, {b:F2}, {a:F2})";
        }
    }

    // Mathf 클래스의 필요한 부분
    public static class Mathf
    {
        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
