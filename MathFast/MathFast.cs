using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FastMath
{
    /// <summary>
    /// 빠른 수학 연산 라이브러리
    /// 사용 전 반드시 Initialize() 호출 필요
    /// </summary>
    public static class MathFast
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        private static TripleSqrtTable _tripleSqrtTable = null;
        private static TrigLookupTable _trigTable = null;

        /// <summary>
        /// MathFast 초기화
        /// </summary>
        /// <param name="sqrtMaxValue">제곱근 테이블 최대값</param>
        /// <param name="trigTableSize">삼각함수 테이블 크기 (기본: 36000 = 0.01도 간격)</param>
        public static void Initialize(float sqrtMaxValue = 100_000_000f, int trigTableSize = 36000)
        {
            if (_initialized)
            {
                Console.WriteLine("[MathFast] 이미 초기화되었습니다.");
                return;
            }

            lock (_lock)
            {
                if (_initialized) return;

                Console.WriteLine("[MathFast] 초기화 시작...");
                var sw = Stopwatch.StartNew();

                _tripleSqrtTable = new TripleSqrtTable(sqrtMaxValue);
                _trigTable = new TrigLookupTable(trigTableSize);

                _initialized = true;

                sw.Stop();
                Console.WriteLine($"[MathFast] 초기화 완료 ({sw.ElapsedMilliseconds}ms)");
            }
        }

        // ==================== 제곱근 ====================

        /// <summary>
        /// 제곱근 (Math.Sqrt보다 약 15배 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float value)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _tripleSqrtTable.Sqrt(value);
        }

        /// <summary>
        /// 빠른 역제곱근 (정규화용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float InvSqrt(float x)
        {
            float xhalf = 0.5f * x;
            int i = *(int*)&x;
            i = 0x5f3759df - (i >> 1);
            x = *(float*)&i;
            x = x * (1.5f - xhalf * x * x);
            return x;
        }

        // ==================== 삼각함수 ====================

        /// <summary>
        /// Sin 계산 (라디안, Math.Sin보다 약 10배 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float radians)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Sin(radians);
        }

        /// <summary>
        /// Cos 계산 (라디안, Math.Cos보다 약 10배 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float radians)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Cos(radians);
        }

        /// <summary>
        /// Sin/Cos 동시 계산 (회전 행렬용, 개별 호출보다 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SinCos(float radians, out float sin, out float cos)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            _trigTable.SinCos(radians, out sin, out cos);
        }

        /// <summary>
        /// Sin 계산 (도 단위)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SinDegrees(float degrees)
        {
            return Sin(degrees * Deg2Rad);
        }

        /// <summary>
        /// Cos 계산 (도 단위)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CosDegrees(float degrees)
        {
            return Cos(degrees * Deg2Rad);
        }

        /// <summary>
        /// Tan 계산 (라디안, Math.Tan보다 약 10배 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float radians)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Tan(radians);
        }

        /// <summary>
        /// Tan 계산 (도 단위)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TanDegrees(float degrees)
        {
            return Tan(degrees * Deg2Rad);
        }

        /// <summary>
        /// Atan2 - 두 점 사이의 각도 (라디안)
        /// 결과: -π ~ π
        /// Math.Atan2보다 약 8배 빠름
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2(float y, float x)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Atan2(y, x);
        }

        /// <summary>
        /// Atan2 (도 단위 반환)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2Degrees(float y, float x)
        {
            return Atan2(y, x) * Rad2Deg;
        }

        /// <summary>
        /// 두 점 사이의 방향 각도 계산 (2D)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DirectionAngle(float fromx, float fromZ, float tox, float toZ)
        {
            return Atan2(toZ - fromZ, tox - fromx);
        }

        // ==================== 역삼각함수 ====================

        /// <summary>
        /// Asin 계산 (역사인)
        /// 입력: -1 ~ 1, 출력: -π/2 ~ π/2 라디안
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Asin(float x)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Asin(x);
        }

        /// <summary>
        /// Asin 계산 (도 단위 반환)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AsinDegrees(float x)
        {
            return Asin(x) * Rad2Deg;
        }

        /// <summary>
        /// Acos 계산 (역코사인)
        /// 입력: -1 ~ 1, 출력: 0 ~ π 라디안
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Acos(float x)
        {
            if (!_initialized)
                throw new InvalidOperationException("MathFast.Initialize()를 먼저 호출하세요!");
            return _trigTable.Acos(x);
        }

        /// <summary>
        /// Acos 계산 (도 단위 반환)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AcosDegrees(float x)
        {
            return Acos(x) * Rad2Deg;
        }

        // ==================== Min/Max/Clamp ====================

        /// <summary>
        /// 두 값 중 작은 값 반환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// 두 값 중 큰 값 반환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// 세 값 중 작은 값 반환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b, float c)
        {
            return a < b ? (a < c ? a : c) : (b < c ? b : c);
        }

        /// <summary>
        /// 세 값 중 큰 값 반환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b, float c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        /// <summary>
        /// 값을 min과 max 사이로 제한
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 0 ~ 1 범위로 제한
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        /// <summary>
        /// int 버전 Min
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// int 버전 Max
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// int 버전 세 값 중 작은 값
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b, int c)
        {
            return a < b ? (a < c ? a : c) : (b < c ? b : c);
        }

        /// <summary>
        /// int 버전 세 값 중 큰 값
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int a, int b, int c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        /// <summary>
        /// int 버전 Clamp
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ==================== 유틸리티 ====================

        /// <summary>
        /// 제곱 거리 비교 (제곱근 불필요, 가장 빠름)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinRangeSquared(float x1, float y1, float z1,
                                                 float x2, float y2, float z2,
                                                 float thresholdSquared)
        {
            float dx = x1 - x2;
            float dy = y1 - y2;
            float dz = z1 - z2;
            float distSq = dx * dx + dy * dy + dz * dz;
            return distSq < thresholdSquared;
        }

        /// <summary>
        /// 2D 수평 거리 (Y축 무시)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalDistance(float x1, float z1, float x2, float z2)
        {
            float dx = x1 - x2;
            float dz = z1 - z2;
            float distSq = dx * dx + dz * dz;
            return Sqrt(distSq);
        }

        /// <summary>
        /// 절댓값
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }

        /// <summary>
        /// 절댓값 (int)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        /// <summary>
        /// 부호 반환 (-1, 0, 1)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(float value)
        {
            if (value > 0f) return 1f;
            if (value < 0f) return -1f;
            return 0f;
        }

        /// <summary>
        /// 선형 보간
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 선형 보간 (t를 0~1로 클램핑)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpClamped(float a, float b, float t)
        {
            t = Clamp01(t);
            return a + (b - a) * t;
        }

        /// <summary>
        /// 역 선형 보간 (값으로부터 t 계산)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InverseLerp(float a, float b, float value)
        {
            if (Abs(b - a) < 0.0001f) return 0f;
            return (value - a) / (b - a);
        }

        // ==================== 상수 ====================

        public const float PI = (float)Math.PI;
        public const float TWO_PI = (float)(Math.PI * 2.0);
        public const float PI_HALF = (float)(Math.PI * 0.5);
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);

        public static bool IsInitialized => _initialized;
    }
}