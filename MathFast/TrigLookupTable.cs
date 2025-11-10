using System;

namespace FastMath
{
    /// <summary>
    /// 삼각함수 룩업 테이블 (확장 버전)
    /// Sin, Cos, Tan, Atan2 지원
    /// </summary>
    internal class TrigLookupTable
    {
        private readonly float[] _sinTable;
        private readonly float[] _cosTable;
        private readonly float[] _tanTable;
        private readonly float[] _atanTable;  // atan(-1 ~ 1) 범위
        private readonly float[] _asinTable;  // asin(-1 ~ 1) 범위
        private readonly float[] _acosTable;  // acos(-1 ~ 1) 범위

        private readonly int _tableSize;
        private readonly float _angleStep;
        private const float TWO_PI = (float)(Math.PI * 2.0);
        private const float PI = (float)Math.PI;
        private const float PI_HALF = (float)(Math.PI * 0.5);

        public TrigLookupTable(int tableSize = 36000, int atanTableSize = 10000)
        {
            _tableSize = tableSize;
            _angleStep = TWO_PI / tableSize;

            // Sin, Cos, Tan 테이블
            _sinTable = new float[tableSize];
            _cosTable = new float[tableSize];
            _tanTable = new float[tableSize];

            for (int i = 0; i < tableSize; i++)
            {
                float angle = i * _angleStep;
                _sinTable[i] = (float)Math.Sin(angle);
                _cosTable[i] = (float)Math.Cos(angle);

                // Tan은 cos가 0에 가까울 때 발산하므로 클램핑
                float cosVal = _cosTable[i];
                if (Math.Abs(cosVal) < 0.0001f)
                    _tanTable[i] = cosVal > 0 ? 100000f : -100000f;  // 충분히 큰 값
                else
                    _tanTable[i] = _sinTable[i] / cosVal;
            }

            // ✅ Atan 테이블 초기화 (기존)
            _atanTable = new float[atanTableSize];
            float atanStep = 2.0f / atanTableSize;

            for (int i = 0; i < atanTableSize; i++)
            {
                float x = -1.0f + i * atanStep;
                _atanTable[i] = (float)Math.Atan(x);
            }

            // ✅ Asin/Acos 테이블 초기화 (추가)
            _asinTable = new float[atanTableSize];
            _acosTable = new float[atanTableSize];

            for (int i = 0; i < atanTableSize; i++)
            {
                float x = -1.0f + i * atanStep;

                // 범위 클램핑 (부동소수점 오차 방지)
                if (x < -1f) x = -1f;
                if (x > 1f) x = 1f;

                _asinTable[i] = (float)Math.Asin(x);
                _acosTable[i] = (float)Math.Acos(x);
            }

            // 메모리 계산 업데이트
            int memoryBytes = (tableSize * 3 + atanTableSize * 3) * sizeof(float);  // ← 3개 추가
            Console.WriteLine($"[TrigLookupTable] 메모리: {memoryBytes / 1024f:F2}KB");
            Console.WriteLine($"  Sin/Cos/Tan 크기: {tableSize}");
            Console.WriteLine($"  Atan/Asin/Acos 크기: {atanTableSize}");
        }

        public float Sin(float radians)
        {
            radians = NormalizeAngle(radians);

            float exactIndex = radians / _angleStep;
            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            int nextIndex = (index + 1) % _tableSize;

            return _sinTable[index] + (_sinTable[nextIndex] - _sinTable[index]) * fraction;
        }

        public float Cos(float radians)
        {
            radians = NormalizeAngle(radians);

            float exactIndex = radians / _angleStep;
            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            int nextIndex = (index + 1) % _tableSize;

            return _cosTable[index] + (_cosTable[nextIndex] - _cosTable[index]) * fraction;
        }

        /// <summary>
        /// Tan 계산 (라디안)
        /// </summary>
        public float Tan(float radians)
        {
            radians = NormalizeAngle(radians);

            float exactIndex = radians / _angleStep;
            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            int nextIndex = (index + 1) % _tableSize;

            return _tanTable[index] + (_tanTable[nextIndex] - _tanTable[index]) * fraction;
        }

        /// <summary>
        /// Atan2 계산 - 두 점 사이의 각도 계산 (라디안 반환)
        /// 결과: -π ~ π
        /// </summary>
        public float Atan2(float y, float x)
        {
            // ✅ 특수 케이스 처리
            if (x == 0f && y == 0f) return 0f;

            // ✅ 사분면 판단 및 계산
            float absX = Math.Abs(x);
            float absY = Math.Abs(y);

            // 더 큰 값으로 나눠서 -1 ~ 1 범위로 정규화
            float ratio;
            float baseAngle;

            if (absX > absY)
            {
                ratio = y / x;
                baseAngle = AtanLookup(ratio);

                if (x < 0f)
                {
                    // 2, 3사분면
                    baseAngle = (y >= 0f) ? (PI - baseAngle) : (-PI + baseAngle);
                }
            }
            else
            {
                ratio = x / y;
                baseAngle = AtanLookup(ratio);

                baseAngle = PI_HALF - baseAngle;

                if (y < 0f)
                {
                    // 3, 4사분면
                    baseAngle = -baseAngle;
                }
            }

            return baseAngle;
        }

        /// <summary>
        /// Asin 계산 (역사인)
        /// 입력: -1 ~ 1, 출력: -π/2 ~ π/2
        /// </summary>
        public float Asin(float x)
        {
            // 범위 체크
            if (x <= -1f) return -PI_HALF;
            if (x >= 1f) return PI_HALF;

            return AsinAcosLookup(_asinTable, x);
        }

        /// <summary>
        /// Acos 계산 (역코사인)
        /// 입력: -1 ~ 1, 출력: 0 ~ π
        /// </summary>
        public float Acos(float x)
        {
            // 범위 체크
            if (x <= -1f) return PI;
            if (x >= 1f) return 0f;

            return AsinAcosLookup(_acosTable, x);
        }

        /// <summary>
        /// Asin/Acos 테이블 조회 공통 함수
        /// </summary>
        private float AsinAcosLookup(float[] table, float x)
        {
            // -1 ~ 1을 0 ~ tableSize로 변환
            float normalized = (x + 1f) * 0.5f;  // 0 ~ 1
            float exactIndex = normalized * (table.Length - 1);

            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            if (index >= table.Length - 1)
                return table[table.Length - 1];

            return table[index] + (table[index + 1] - table[index]) * fraction;
        }

        /// <summary>
        /// Atan 테이블 조회 (-1 ~ 1 범위)
        /// </summary>
        private float AtanLookup(float x)
        {
            // 범위 체크
            if (x <= -1f) return _atanTable[0];
            if (x >= 1f) return _atanTable[_atanTable.Length - 1];

            // -1 ~ 1을 0 ~ tableSize로 변환
            float normalized = (x + 1f) * 0.5f;  // 0 ~ 1
            float exactIndex = normalized * (_atanTable.Length - 1);

            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            if (index >= _atanTable.Length - 1)
                return _atanTable[_atanTable.Length - 1];

            return _atanTable[index] + (_atanTable[index + 1] - _atanTable[index]) * fraction;
        }

        public void SinCos(float radians, out float sin, out float cos)
        {
            radians = NormalizeAngle(radians);

            float exactIndex = radians / _angleStep;
            int index = (int)exactIndex;
            float fraction = exactIndex - index;

            int nextIndex = (index + 1) % _tableSize;

            sin = _sinTable[index] + (_sinTable[nextIndex] - _sinTable[index]) * fraction;
            cos = _cosTable[index] + (_cosTable[nextIndex] - _cosTable[index]) * fraction;
        }

        private float NormalizeAngle(float radians)
        {
            radians = radians % TWO_PI;
            if (radians < 0f) radians += TWO_PI;
            return radians;
        }
    }
}
