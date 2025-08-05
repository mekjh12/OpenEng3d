using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlWindow
{
    /// <summary>
    /// 확장 버전: 여러 정보를 조합한 문자열 캐시
    /// 자주 사용되는 조합을 미리 생성
    /// </summary>
    public class GameInfoStringCache
    {
        private readonly FpsStringCache _fpsCache;
        private readonly Dictionary<string, string> _combinationCache;
        private readonly int _maxCacheSize;

        // 이전 값들
        private int _prevFps = -1;
        private int _prevTick = -1;
        private string _prevCameraDir = "";

        public GameInfoStringCache(uint maxFps = 120, int maxCacheSize = 1000)
        {
            _fpsCache = new FpsStringCache(maxFps);
            _combinationCache = new Dictionary<string, string>(maxCacheSize);
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// 게임 정보 텍스트 반환 (변경시에만)
        /// </summary>
        public string GetGameInfoIfChanged(int fps, int tick, string cameraDir)
        {
            // 변경 확인
            if (_prevFps == fps && _prevTick == tick && _prevCameraDir == cameraDir)
            {
                return null; // 변경 없음
            }

            // 캐시 키 생성
            string cacheKey = $"{fps}|{tick}|{cameraDir}";

            // 캐시에서 찾기
            if (_combinationCache.TryGetValue(cacheKey, out string cachedText))
            {
                UpdatePrevValues(fps, tick, cameraDir);
                return cachedText; // 캐시 히트, GC 없음!
            }

            // 새로 생성 (캐시 미스시에만 GC 발생)
            string newText = $"FPS: {fps} | Tick: {tick} | Camera: {cameraDir}";

            // 캐시 크기 관리
            if (_combinationCache.Count >= _maxCacheSize)
            {
                _combinationCache.Clear(); // 오래된 캐시 정리
            }

            _combinationCache[cacheKey] = newText;
            UpdatePrevValues(fps, tick, cameraDir);

            return newText;
        }

        /// <summary>
        /// FPS만 단순 표시 (완전 GC-Free)
        /// </summary>
        public string GetFpsOnlyIfChanged(uint fps)
        {
            return _fpsCache.GetFpsTextIfChanged(fps);
        }

        private void UpdatePrevValues(int fps, int tick, string cameraDir)
        {
            _prevFps = fps;
            _prevTick = tick;
            _prevCameraDir = cameraDir;
        }

        /// <summary>
        /// 캐시 상태 리셋
        /// </summary>
        public void ResetCache()
        {
            _prevFps = -1;
            _prevTick = -1;
            _prevCameraDir = "";
            _fpsCache.ResetPreviousFps();
        }

        /// <summary>
        /// 캐시 통계
        /// </summary>
        public (long fpsMemory, int combinationCount) GetCacheStats()
        {
            return (_fpsCache.EstimatedMemoryUsage, _combinationCache.Count);
        }
    }
}
