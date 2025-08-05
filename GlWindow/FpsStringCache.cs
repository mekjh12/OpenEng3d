using System;

namespace GlWindow
{
    /// <summary>
    /// 완전 GC-Free FPS 텍스트 캐시
    /// 0~maxFps까지 모든 문자열을 미리 생성하여 런타임 GC 제거
    /// </summary>
    public class FpsStringCache
    {
        private readonly string[] _fpsTexts;
        private readonly uint _maxFps;
        private uint _prevFps = 0;

        /// <summary>
        /// FPS 문자열 캐시 초기화
        /// </summary>
        /// <param name="maxFps">지원할 최대 FPS (기본값: 120)</param>
        /// <param name="prefix">FPS 앞에 붙일 텍스트 (기본값: "FPS: ")</param>
        public FpsStringCache(uint maxFps = 120, string prefix = "FPS: ")
        {
            _maxFps = maxFps;
            _fpsTexts = new string[maxFps + 1]; // 0~maxFps

            // 모든 FPS 문자열 미리 생성 (초기화 시 한 번만)
            for (int i = 0; i <= maxFps; i++)
            {
                _fpsTexts[i] = $"{prefix}{i}";
            }
        }

        /// <summary>
        /// FPS 값에 해당하는 캐시된 문자열 반환 (GC 없음)
        /// </summary>
        /// <param name="fps">현재 FPS 값</param>
        /// <returns>캐시된 FPS 문자열</returns>
        public string GetFpsText(uint fps)
        {
            // 범위 체크
            if (fps < 0) fps = 0;
            if (fps > _maxFps) fps = _maxFps;

            return _fpsTexts[fps]; // 배열 접근만, GC 없음!
        }

        /// <summary>
        /// FPS가 변경되었을 때만 문자열 반환
        /// 변경되지 않았으면 null 반환
        /// </summary>
        /// <param name="currentFps">현재 FPS</param>
        /// <returns>변경된 경우 캐시된 텍스트, 아니면 null</returns>
        public string GetFpsTextIfChanged(uint currentFps)
        {
            if (_prevFps != currentFps)
            {
                _prevFps = currentFps;
                return GetFpsText(currentFps); // 캐시된 문자열, GC 없음!
            }

            return null;
        }

        /// <summary>
        /// 이전 FPS 값 리셋 (강제 업데이트용)
        /// </summary>
        public void ResetPreviousFps()
        {
            _prevFps = 0;
        }

        /// <summary>
        /// 지원하는 최대 FPS 값
        /// </summary>
        public uint MaxFps => _maxFps;

        /// <summary>
        /// 사용된 메모리 추정치 (바이트)
        /// </summary>
        public long EstimatedMemoryUsage
        {
            get
            {
                long totalSize = 0;
                for (int i = 0; i <= _maxFps; i++)
                {
                    totalSize += _fpsTexts[i].Length * sizeof(char); // 문자 데이터
                    totalSize += IntPtr.Size; // 참조 크기
                }
                return totalSize;
            }
        }
    }

}
