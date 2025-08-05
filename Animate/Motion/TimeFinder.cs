using System;

namespace Animate
{
    /// <summary>
    /// 정렬된 시간 배열에서 이진 탐색으로 가장 가까운 시간을 찾는 클래스
    /// GC 없이 최적화된 성능 제공
    /// <code>
    /// 1. Motion의 KeyFrame 시간들로 초기화
    ///     var timeFinder = new TimeFinder();
    ///     float[] keyframeTimes = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f };
    ///     timeFinder.SetTimes(keyframeTimes);
    /// 
    /// 2. 가장 가까운 시간 찾기
    ///     int nearestIndex = timeFinder.FindNearestIndex(0.6f);
    ///     → index 2 (0.5초가 가장 가까움)
    /// 
    /// 3. 보간용 두 인덱스 찾기  
    ///     bool success = timeFinder.FindInterpolationIndices(0.6f, out int lower, out int upper, out float blend);
    ///     → lower=2 (0.5초), upper=3 (0.75초), blend=0.4
    /// </code>
    /// </summary>
    public class TimeFinder
    {
        private float[] _times;      // 정렬된 시간 배열
        private int _count;          // 실제 사용 중인 시간 개수

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="capacity">최대 시간 개수</param>
        public TimeFinder(int capacity = 128)
        {
            _times = new float[capacity];
            _count = 0;
        }

        /// <summary>
        /// 시간 배열 설정 (정렬된 상태로 전달되어야 함)
        /// </summary>
        /// <param name="sortedTimes">정렬된 시간 배열</param>
        public void SetTimes(float[] sortedTimes)
        {
            _count = Math.Min(sortedTimes.Length, _times.Length);
            Array.Copy(sortedTimes, _times, _count);
        }

        /// <summary>
        /// 가장 가까운 시간의 인덱스를 찾습니다 (GC 없음)
        /// </summary>
        /// <param name="targetTime">찾을 시간</param>
        /// <returns>가장 가까운 시간의 인덱스</returns>
        public int FindNearestIndex(float targetTime)
        {
            if (_count == 0) return -1;
            if (_count == 1) return 0;

            // 경계값 처리
            if (targetTime <= _times[0]) return 0;
            if (targetTime >= _times[_count - 1]) return _count - 1;

            // 이진 탐색
            int left = 0;
            int right = _count - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                float midTime = _times[mid];

                if (midTime == targetTime)
                {
                    return mid; // 정확히 일치
                }
                else if (midTime < targetTime)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // left와 right 사이에서 더 가까운 것 선택
            if (right < 0) return 0;
            if (left >= _count) return _count - 1;

            float leftDiff = Math.Abs(targetTime - _times[right]);
            float rightDiff = Math.Abs(targetTime - _times[left]);

            return leftDiff <= rightDiff ? right : left;
        }

        /// <summary>
        /// 가장 가까운 시간 값을 반환합니다
        /// </summary>
        /// <param name="targetTime">찾을 시간</param>
        /// <returns>가장 가까운 시간</returns>
        public float FindNearestTime(float targetTime)
        {
            int index = FindNearestIndex(targetTime);
            return index >= 0 ? _times[index] : 0f;
        }

        /// <summary>
        /// 보간을 위한 두 인접한 인덱스를 찾습니다
        /// </summary>
        /// <param name="targetTime">찾을 시간</param>
        /// <param name="lowerIndex">작은 시간의 인덱스</param>
        /// <param name="upperIndex">큰 시간의 인덱스</param>
        /// <param name="blendFactor">보간 계수 (0~1)</param>
        /// <returns>성공 여부</returns>
        public bool FindInterpolationIndices(float targetTime, out int lowerIndex, out int upperIndex, out float blendFactor)
        {
            lowerIndex = -1;
            upperIndex = -1;
            blendFactor = 0f;

            if (_count == 0) return false;
            if (_count == 1)
            {
                lowerIndex = upperIndex = 0;
                return true;
            }

            // 경계값 처리
            if (targetTime <= _times[0])
            {
                lowerIndex = upperIndex = 0;
                return true;
            }
            if (targetTime >= _times[_count - 1])
            {
                lowerIndex = upperIndex = _count - 1;
                return true;
            }

            // 이진 탐색으로 삽입 위치 찾기
            int left = 0;
            int right = _count - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;

                if (_times[mid] == targetTime)
                {
                    lowerIndex = upperIndex = mid;
                    return true;
                }
                else if (_times[mid] < targetTime)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // right < targetTime < left
            lowerIndex = right;
            upperIndex = left;

            // 보간 계수 계산
            float lowerTime = _times[lowerIndex];
            float upperTime = _times[upperIndex];
            float range = upperTime - lowerTime;

            if (range > 0f)
            {
                blendFactor = (targetTime - lowerTime) / range;
            }

            return true;
        }

        /// <summary>
        /// 현재 저장된 시간 개수
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 지정된 인덱스의 시간 값
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <returns>시간 값</returns>
        public float GetTime(int index)
        {
            return (index >= 0 && index < _count) ? _times[index] : 0f;
        }

        /// <summary>
        /// 배열 용량 확장 (필요시에만)
        /// </summary>
        /// <param name="newCapacity">새로운 용량</param>
        public void EnsureCapacity(int newCapacity)
        {
            if (newCapacity > _times.Length)
            {
                Array.Resize(ref _times, newCapacity);
            }
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public override string ToString()
        {
            return $"TimeFinder(Count: {_count}, Range: {(_count > 0 ? $"{_times[0]:F3}~{_times[_count - 1]:F3}" : "Empty")})";
        }
    }
}