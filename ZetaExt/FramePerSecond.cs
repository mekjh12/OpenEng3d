using System;

namespace ZetaExt
{
    /// <summary>
    /// 완전 GC-Free FramePerSecond 클래스
    /// uint FPS 프로퍼티에서 boxing/unboxing 제거
    /// </summary>
    public class FramePerSecond
    {
        // 마지막 프레임과 현재 프레임 사이의 시간
        static int deltaTime = 0;
        static int globalTick = 0;
        // 마지막 프레임의 시간
        static int lastFrame = 0;
        static float fps = 0.0f;
        static int prevDeltaTime;
        static float weightedValueTime = 0.005f;

        // GC-Free를 위한 캐시된 uint 값들
        static uint _cachedFpsUint = 60;
        static int _lastFpsUpdateTick = 0;
        static readonly uint[] _precomputedFps = new uint[201]; // 0~200 FPS 미리 계산

        // 정밀도 제어
        static readonly int FPS_UPDATE_INTERVAL = 1; // 3프레임마다 FPS 업데이트

        // 정적 생성자 - 프로그램 시작 시 한 번만 실행
        static FramePerSecond()
        {
            InitializePrecomputedValues();
        }

        /// <summary>
        /// 0~1000까지의 FPS uint 값을 미리 계산 (초기화 시 한 번만)
        /// </summary>
        static void InitializePrecomputedValues()
        {
            for (int i = 0; i <= 200; i++)
            {
                _precomputedFps[i] = (uint)i;
            }
        }

        public static int DeltaTime => deltaTime;
        public static int GlobalTick => globalTick;

        /// <summary>
        /// 완전 GC-Free uint FPS 반환
        /// 캐시된 값을 반환하므로 boxing 없음
        /// </summary>
        public static uint FPS => _cachedFpsUint;

        /// <summary>
        /// 더 정확한 float FPS (내부 계산용)
        /// </summary>
        public static float FPSFloat => fps;

        /// <summary>
        /// 프레임 업데이트 (매 프레임 호출)
        /// </summary>
        public static void Update()
        {
            int currentTick = System.Environment.TickCount;
            deltaTime = currentTick - lastFrame;
            if (deltaTime <= 0) deltaTime = 1;

            prevDeltaTime = deltaTime;
            float deltaTimeFloat = (float)deltaTime;  // 한 번만 변환
            float deltaAverage = 1000.0f / deltaTimeFloat;
            float wd = weightedValueTime * deltaTimeFloat;

            // Math 함수 대신 조건문
            if (wd < 0f) wd = 0f;
            if (wd > 1f) wd = 1f;

            fps = (1f - wd) * fps + wd * deltaAverage;
            lastFrame = currentTick;
            globalTick++;

            if (globalTick - _lastFpsUpdateTick >= FPS_UPDATE_INTERVAL)
            {
                UpdateCachedFpsUint();
                _lastFpsUpdateTick = globalTick;
            }
        }

        /// <summary>
        /// 캐시된 uint FPS 업데이트 - GC 없는 방식
        /// </summary>
        static void UpdateCachedFpsUint()
        {
            // float fps를 안전하게 int로 변환
            int fpsInt = (int)(fps + 0.5f); // 반올림

            // 범위 체크
            if (fpsInt < 0) fpsInt = 0;
            if (fpsInt > 200) fpsInt = 200;

            // 미리 계산된 배열에서 가져오기 (GC 없음)
            _cachedFpsUint = _precomputedFps[fpsInt];
        }

        /// <summary>
        /// 강제로 FPS 캐시 업데이트
        /// </summary>
        public static void ForceUpdateFpsCache()
        {
            UpdateCachedFpsUint();
        }

        /// <summary>
        /// FPS 통계 정보
        /// </summary>
        public static (uint current, float precise, int updateInterval) GetFpsStats()
        {
            return (_cachedFpsUint, fps, FPS_UPDATE_INTERVAL);
        }

        /// <summary>
        /// 디버깅용 - 현재 FPS 상태 출력
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"FPS: {_cachedFpsUint} (precise: {fps:F2}, tick: {globalTick}, delta: {deltaTime}ms)";
        }
    }
}
