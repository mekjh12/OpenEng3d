using System;

namespace FastMath
{
    internal class TripleSqrtTable
    {
        private readonly float[] _tinyTable;   // 0.0001 ~ 0.25
        private readonly float[] _smallTable;  // 0.25 ~ 10.0
        private readonly float[] _largeTable;  // 10.0 ~ max

        private readonly float _tinyMin = 0.0001f;
        private readonly float _tinyMax = 0.25f;
        private readonly float _smallMax = 10.0f;


        private readonly float _tinyStep;
        private readonly float _smallStep;
        private readonly float _largeStep;
        private readonly float _maxValue;

        public TripleSqrtTable(float maxValue = 10000000f)
        {
            _maxValue = maxValue;

            // ✅ 매우 작은 값 (0.0001 ~ 0.25): 최고 밀도
            // 기울기 1.0 ~ 5.0 구간
            int tinySize = 20000;  // 80KB
            _tinyStep = (_tinyMax - _tinyMin) / tinySize;
            _tinyTable = new float[tinySize + 1];

            for (int i = 0; i <= tinySize; i++)
            {
                float value = _tinyMin + i * _tinyStep;
                _tinyTable[i] = (float)Math.Sqrt(value);
            }

            // ✅ 작은 값 (0.25 ~ 10.0): 중간 밀도
            // 기울기 0.16 ~ 1.0 구간
            int smallSize = 30000;  // 120KB
            _smallStep = (_smallMax - _tinyMax) / smallSize;
            _smallTable = new float[smallSize + 1];

            for (int i = 0; i <= smallSize; i++)
            {
                float value = _tinyMax + i * _smallStep;
                _smallTable[i] = (float)Math.Sqrt(value);
            }

            // ✅ 큰 값 (10.0 ~ max): 성긴 밀도
            // 기울기 < 0.16 구간
            int largeSize = 50000;  // 200KB
            _largeStep = (maxValue - _smallMax) / largeSize;
            _largeTable = new float[largeSize + 1];

            for (int i = 0; i <= largeSize; i++)
            {
                float value = _smallMax + i * _largeStep;
                _largeTable[i] = (float)Math.Sqrt(value);
            }

            int totalMemory = (tinySize + smallSize + largeSize + 3) * sizeof(float);
            Console.WriteLine($"[TripleSqrtTable] 메모리: {totalMemory / 1024f:F2}KB");
            Console.WriteLine($"  매우작은값 (0.0001~0.25): 간격 {_tinyStep:F6}");
            Console.WriteLine($"  작은값 (0.25~10): 간격 {_smallStep:F6}");
            Console.WriteLine($"  큰값 (10~max): 간격 {_largeStep:F2}");
        }

        public float Sqrt(float value)
        {
            if (value <= 0f) return 0f;

            // 매우 작은 값
            if (value < _tinyMax)
            {
                if (value < _tinyMin) return (float)Math.Sqrt(value);

                float exactIndex = (value - _tinyMin) / _tinyStep;
                int index = (int)exactIndex;

                if (index >= _tinyTable.Length - 1)
                    return _tinyTable[_tinyTable.Length - 1];

                float fraction = exactIndex - index;
                return _tinyTable[index] + (_tinyTable[index + 1] - _tinyTable[index]) * fraction;
            }

            // 작은 값
            if (value < _smallMax)
            {
                float exactIndex = (value - _tinyMax) / _smallStep;
                int index = (int)exactIndex;

                if (index >= _smallTable.Length - 1)
                    return _smallTable[_smallTable.Length - 1];

                float fraction = exactIndex - index;
                return _smallTable[index] + (_smallTable[index + 1] - _smallTable[index]) * fraction;
            }

            // 큰 값
            if (value < _maxValue)
            {
                float exactIndex = (value - _smallMax) / _largeStep;
                int index = (int)exactIndex;

                if (index >= _largeTable.Length - 1)
                    return _largeTable[_largeTable.Length - 1];

                float fraction = exactIndex - index;
                return _largeTable[index] + (_largeTable[index + 1] - _largeTable[index]) * fraction;
            }

            return (float)Math.Sqrt(value);
        }
    }
}
