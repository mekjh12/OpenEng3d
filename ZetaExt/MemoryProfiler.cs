using System;

namespace ZetaExt
{
    /// <summary>
    /// 렌더링 중 메모리 할당량과 GC 발생 횟수를 모니터링하는 정적 클래스
    /// </summary>
    public static class MemoryProfiler
    {
        private static long _memoryBefore;
        private static int _gen0Before;
        private static int _gen1Before;
        private static int _gen2Before;
        private static bool _isCheckpointing = false;

        // 프레임 모니터링용 변수들
        private static int _lastGen0Count = 0;
        private static int _lastGen1Count = 0;
        private static int _lastGen2Count = 0;
        private static long _lastMemory = 0;
        private static DateTime _lastGCTime = DateTime.MinValue;
        private static uint _frameCount = 0;
        private static bool _isFrameMonitoringEnabled = false;

        /// <summary>
        /// 메모리 체크포인트 시작 - 현재 메모리 상태를 기록
        /// </summary>
        public static void StartCheckpoint()
        {
            _memoryBefore = GC.GetTotalMemory(false);
            _gen0Before = GC.CollectionCount(0);
            _gen1Before = GC.CollectionCount(1);
            _gen2Before = GC.CollectionCount(2);
            _isCheckpointing = true;
        }

        /// <summary>
        /// 메모리 체크포인트 종료 - 메모리 변화량을 계산하고 출력
        /// </summary>
        /// <param name="checkpointName">체크포인트 이름 (선택사항)</param>
        /// <param name="memoryThreshold">메모리 할당량 임계값 (기본: 1KB)</param>
        public static void EndCheckpoint(string checkpointName = "Rendering", long memoryThreshold = 1024)
        {
            if (!_isCheckpointing)
            {
                Console.WriteLine("Warning: StartCheckpoint()를 먼저 호출해주세요.");
                return;
            }

            // 현재 메모리 상태 체크
            long memoryAfter = GC.GetTotalMemory(false);
            int gen0After = GC.CollectionCount(0);
            int gen1After = GC.CollectionCount(1);
            int gen2After = GC.CollectionCount(2);

            // 메모리 할당량 및 GC 발생 횟수 계산
            long memoryAllocated = memoryAfter - _memoryBefore;
            int gen0Diff = gen0After - _gen0Before;
            int gen1Diff = gen1After - _gen1Before;
            int gen2Diff = gen2After - _gen2Before;

            // 임계값을 넘는 할당이 있을 때만 로그 출력
            if (memoryAllocated > memoryThreshold || gen0Diff > 0 || gen1Diff > 0 || gen2Diff > 0)
            {
                Console.WriteLine($"[{checkpointName}] Memory: {memoryAllocated:N0} bytes, " +
                                $"Gen0: +{gen0Diff}, " +
                                $"Gen1: +{gen1Diff}, " +
                                $"Gen2: +{gen2Diff}");
            }

            _isCheckpointing = false;
        }

        /// <summary>
        /// 체크포인트 종료 후 강제 가비지 컬렉션 수행 (디버깅용)
        /// </summary>
        /// <param name="checkpointName">체크포인트 이름</param>
        /// <param name="memoryThreshold">메모리 할당량 임계값</param>
        public static void EndCheckpointWithGC(string checkpointName = "Rendering", long memoryThreshold = 1024)
        {
            EndCheckpoint(checkpointName, memoryThreshold);

            // 강제 GC 수행 (성능 테스트용 - 실제 운영에서는 권장하지 않음)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine($"[{checkpointName}] Forced GC completed.");
        }

        /// <summary>
        /// 현재 메모리 상태를 즉시 출력 (디버깅용)
        /// </summary>
        public static void PrintCurrentMemoryState()
        {
            long currentMemory = GC.GetTotalMemory(false);
            int gen0Count = GC.CollectionCount(0);
            int gen1Count = GC.CollectionCount(1);
            int gen2Count = GC.CollectionCount(2);

            Console.WriteLine($"[Current State] Memory: {currentMemory:N0} bytes, " +
                             $"Gen0: {gen0Count}, " +
                             $"Gen1: {gen1Count}, " +
                             $"Gen2: {gen2Count}");
        }

        /// <summary>
        /// 프레임별 GC 모니터링 시작
        /// </summary>
        public static void StartFrameMonitoring()
        {
            _isFrameMonitoringEnabled = true;
            _lastGen0Count = GC.CollectionCount(0);
            _lastGen1Count = GC.CollectionCount(1);
            _lastGen2Count = GC.CollectionCount(2);
            _lastMemory = GC.GetTotalMemory(false);
            _frameCount = 0;
            Console.WriteLine("[Frame Monitor] 시작됨");
        }

        /// <summary>
        /// 매 프레임마다 호출하여 GC 발생을 체크
        /// </summary>
        /// <param name="checkInterval">몇 프레임마다 체크할지 (기본: 60프레임)</param>
        public static void CheckFrameGC(uint checkInterval = 60)
        {
            if (!_isFrameMonitoringEnabled) return;

            _frameCount++;

            // 지정된 간격마다만 체크
            if (_frameCount % checkInterval == 0)
            {
                int currentGen0 = GC.CollectionCount(0);
                int currentGen1 = GC.CollectionCount(1);
                int currentGen2 = GC.CollectionCount(2);
                long currentMemory = GC.GetTotalMemory(false);

                // GC 발생 체크
                if (currentGen0 > _lastGen0Count || currentGen1 > _lastGen1Count || currentGen2 > _lastGen2Count)
                {
                    DateTime now = DateTime.Now;
                    double intervalSeconds = _lastGCTime != DateTime.MinValue ? (now - _lastGCTime).TotalSeconds : 0;

                    Console.WriteLine($"[Frame {_frameCount}] GC 발생! " +
                                    $"Gen0: +{currentGen0 - _lastGen0Count}, " +
                                    $"Gen1: +{currentGen1 - _lastGen1Count}, " +
                                    $"Gen2: +{currentGen2 - _lastGen2Count}, " +
                                    $"Memory: {currentMemory / 1024:F1}KB, " +
                                    $"Interval: {intervalSeconds:F1}s");

                    _lastGCTime = now;
                }

                // 값 업데이트
                _lastGen0Count = currentGen0;
                _lastGen1Count = currentGen1;
                _lastGen2Count = currentGen2;
                _lastMemory = currentMemory;
            }
        }

        /// <summary>
        /// 프레임별 GC 모니터링 중지
        /// </summary>
        public static void StopFrameMonitoring()
        {
            _isFrameMonitoringEnabled = false;
            Console.WriteLine("[Frame Monitor] 중지됨");
        }
    }
}