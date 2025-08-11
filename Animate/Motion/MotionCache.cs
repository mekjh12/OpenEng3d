using System;

namespace Animate
{
    /// <summary>
    /// 모션과 모션 사이의 전환을 위한 모션들의 캐시
    /// </summary>
    public class MotionCache
    {
        // 블렌딩 모션 캐시
        private string _name; // 캐시 이름
        private const int MAX_BLENDMOTION_CACHE_COUNT = 64;
        private Motionable[] _blendingMotionCache;
        private int _blendMotionCacheIndex = 0; // 블렌딩 모션 캐시 인덱스

        public MotionCache(string name)
        {
            _name = name;
            _blendingMotionCache = new Motionable[MAX_BLENDMOTION_CACHE_COUNT];
        }

        public void AddMotionToCache(Motionable blendMotion)
        {
            if (blendMotion == null || string.IsNullOrEmpty(blendMotion.Name))
            {
                return; // 유효하지 않은 모션은 캐시에 추가하지 않음
            }

            // 캐시가 가득 찬 경우, 가장 오래된 모션을 제거
            if (_blendMotionCacheIndex >= MAX_BLENDMOTION_CACHE_COUNT)
            {
                _blendMotionCacheIndex = 0; // 인덱스를 초기화
            }

            // 새로운 모션을 캐시에 추가
            _blendingMotionCache[_blendMotionCacheIndex] = blendMotion;
            _blendMotionCacheIndex++;
        }

        public Motionable GetMotionFromCache(string blendMotionName)
        {
            for (int i = 0; i < MAX_BLENDMOTION_CACHE_COUNT; i++)
            {
                if (_blendingMotionCache[i] != null && _blendingMotionCache[i].Name == blendMotionName)
                {
                    // 캐시에서 찾은 경우
                    return _blendingMotionCache[i];
                }
            }
            return null;
        }

        public void Print()
        {
            Console.WriteLine($"----------------------{_name}-----------------------");
            Console.WriteLine($"_blendMotionCacheIndex = {_blendMotionCacheIndex}");
            for (int i = 0; i < MAX_BLENDMOTION_CACHE_COUNT; i++)
            {
                if (_blendingMotionCache[i] != null)
                {
                    Console.WriteLine($"_blendingMotionCache[{i}] = {_blendingMotionCache[i].Name} (Length: {_blendingMotionCache[i].PeriodTime})");
                }
            }
        }
    }


}
