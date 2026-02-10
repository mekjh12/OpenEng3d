using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZetaExt;

namespace Terrain
{
    /// <summary>
    /// 지형의 청크를 효율적으로 생성하고 관리하는 클래스
    /// </summary>
    public class ChunkCreator
    {
        private RegionCoord _currentCoord;        // 현재 처리 중인 리전 좌표 (월드 공간에서 생성 중인 지형 영역)
        private int _n;                           // 리전의 반경 (청크 개수는 2n x 2n, 값이 클수록 더 넓은 영역 생성)
        private int _chunkSize;                   // 각 청크의 크기 (청크의 물리적 크기를 결정)
        private TerrainData _terrainData;         // 지형 데이터 참조 (높이맵, 텍스처 등 지형 생성에 필요한 정보)
        private List<AABB> _entities;             // 생성된 청크(AABB) 목록 (충돌 감지 및 컬링에 사용)
        private bool _isComplete;                 // 생성 완료 여부 (모든 청크 생성이 완료되었는지 표시)
        private bool _isProcessing;               // 현재 생성 작업 중인지 여부 (중복 작업 방지용)
        private object _syncLock = new object();  // 스레드 동기화를 위한 락 객체 (멀티스레딩 환경에서 리소스 접근 동기화)
        private Task _backgroundTask;             // 백그라운드 처리 작업 (청크 생성을 비동기적으로 처리)
        private float _progress;                  // 생성 작업 진행률 (0.0-1.0, UI 표시 등에 활용)
        private Action _onComplete;                // 청크 생성 완료 시 호출될 콜백 액션

        private const float MIN_HEIGHT_AABB = 50.0f;

        /// <summary>
        /// 청크 생성이 완료되었을 때 호출될 콜백 액션을 가져오거나 설정합니다.
        /// </summary>
        public Action Completed { get => _onComplete; set => _onComplete = value; }

        /// <summary>
        /// 청크 생성 진행률을 0.0에서 1.0 사이의 값으로 반환합니다.
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// 청크 생성 작업이 완료되었는지 여부를 반환합니다.
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// 현재 청크 생성 작업이 진행 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// ChunkCreator 클래스의 생성자입니다.
        /// </summary>
        public ChunkCreator()
        {

        }

        /// <summary>
        /// 새로운 청크 생성 작업을 시작합니다.
        /// </summary>
        /// <param name="coord">생성할 리전의 좌표</param>
        /// <param name="n">리전의 반경 (총 청크 수는 2n x 2n)</param>
        /// <param name="chunkSize">각 청크의 크기</param>
        /// <param name="terrainData">지형 데이터</param>
        public void StartCreatingChunks(RegionCoord coord, 
            int n, 
            int chunkSize, 
            TerrainData terrainData,
            Action completed)
        {
            _onComplete = completed;

            // 이미 작업 중이면 새 작업 시작 전 취소/완료 처리
            if (_isProcessing)
            {
                // 이전 작업 완료 대기(필요한 경우)
                if (_backgroundTask != null && !_backgroundTask.IsCompleted)
                {
                    _backgroundTask.Wait();
                }
            }

            lock (_syncLock)
            {
                _currentCoord = coord;
                _n = n;
                _chunkSize = chunkSize;
                _terrainData = terrainData;

                // 미리 용량 할당하여 재할당 방지
                int totalChunks = 4 * n * n;
                _entities = new List<AABB>(totalChunks);

                _isComplete = false;
                _isProcessing = true;
                _progress = 0f;
            }

            // 백그라운드에서 모든 청크 처리
            _backgroundTask = Task.Run(() => ProcessAllChunksBackground());
        }

        /// <summary>
        /// 백그라운드 스레드에서 모든 청크를 병렬로 생성합니다.
        /// </summary>
        private void ProcessAllChunksBackground()
        {
            try
            {
                int totalWidth = 2 * _n;                  // 총 가로 청크 수
                float halfSize = _n * _chunkSize;         // 리전 반경 크기
                float regionWidth = halfSize * 2;         // 리전 전체 너비
                float basex = _currentCoord.X * regionWidth;  // 리전 시작 x 좌표
                float baseY = _currentCoord.Y * regionWidth;  // 리전 시작 Y 좌표
                float verticalScale = TerrainConstants.DEFAULT_VERTICAL_SCALE;  // 수직 스케일

                // 미리 결과 공간 할당
                List<AABB> result = new List<AABB>(4 * _n * _n);

                // 청크 생성을 병렬로 처리
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount  // CPU 코어 수에 맞춰 병렬 처리
                };

                int totalChunks = 2 * _n;  // 진행률 계산용 총 처리할 행 수
                int processedChunks = 0;    // 진행률 계산용 처리 완료된 행 수

                Parallel.For(-_n, _n, options, x =>
                {
                    // 각 스레드별 재사용 객체
                    Vertex3f lower = new Vertex3f();      // AABB 하단 좌표
                    Vertex3f upper = new Vertex3f();      // AABB 상단 좌표
                    Vertex3f index = new Vertex3f();      // 청크 인덱스
                    List<AABB> threadChunks = new List<AABB>(totalWidth);  // 스레드별 청크 결과

                    for (int y = -_n; y < _n; y++)
                    {
                        int i = y + _n;  // 높이맵 배열 인덱스 Y
                        int j = x + _n;  // 높이맵 배열 인덱스 x

                        // 높이 데이터 미리 가져오기
                        Vertex2f h = _terrainData.GetHeightBound(j, i, _n);

                        // 객체 재사용하여 AABB 좌표 계산
                        lower.x = _chunkSize * x + basex;
                        lower.y = _chunkSize * y + baseY;
                        lower.z = verticalScale * h.x;    // 최소 높이

                        upper.x = lower.x + _chunkSize;
                        upper.y = lower.y + _chunkSize;
                        upper.z = verticalScale * h.y;    // 최대 높이

                        index.x = x;
                        index.y = y;
                        index.z = 0;

                        // 너무 작지 않은 AABB의 높이를 가지기 위해서
                        if (Math.Abs(lower.z - upper.z) < MIN_HEIGHT_AABB)
                        {
                            upper.z += MIN_HEIGHT_AABB;
                        }

                        // 새 AABB 생성
                        AABB newAABB = new AABB(
                            new Vertex3f(lower.x, lower.y, lower.z),
                            new Vertex3f(upper.x, upper.y, upper.z)
                        );
                        newAABB.Index = new Vertex3f(index.x, index.y, index.z);

                        // 스레드 로컬 리스트에 추가
                        threadChunks.Add(newAABB);
                    }

                    // 스레드 결과를 메인 리스트에 병합
                    lock (_syncLock)
                    {
                        result.AddRange(threadChunks);
                        
                        // 진행률 업데이트
                        processedChunks++;
                        _progress = (float)processedChunks / totalChunks;
                    }
                });

                // 결과 저장 및 완료 표시
                lock (_syncLock)
                {
                    _entities = result;
                    _progress = 1.0f;
                    _isComplete = true;
                    _isProcessing = false;

                    // 완료시 액션
                    _onComplete();
                }
            }
            catch (Exception ex)
            {
                // 예외 처리 - 실제 구현에서는 로깅 등의 처리 필요
                Console.WriteLine($"Error in background processing: {ex.Message}");

                lock (_syncLock)
                {
                    _isProcessing = false;
                }
            }
        }

        /// <summary>
        /// 청크 생성이 완료된 경우 결과를 반환합니다. 완료되지 않은 경우 null을 반환합니다.
        /// </summary>
        /// <returns>생성된 청크(AABB) 목록 또는 null</returns>
        public List<AABB> GetResult()
        {
            lock (_syncLock)
            {
                return _isComplete ? _entities : null;
            }
        }

        /// <summary>
        /// 작업 완료 여부와 관계없이 현재까지 생성된 청크 목록의 복사본을 반환합니다.
        /// </summary>
        /// <returns>현재까지 생성된 청크(AABB) 목록의 복사본</returns>
        public List<AABB> GetCurrentResult()
        {
            lock (_syncLock)
            {
                return new List<AABB>(_entities);
            }
        }

        /// <summary>
        /// 백그라운드 작업 상태를 확인하고 필요시 상태를 업데이트합니다.
        /// 메인 스레드에서 주기적으로 호출해야 합니다.
        /// </summary>
        public void Update()
        {
            try
            {
                // 백그라운드 작업이 완료되었는지 확인
                if (_isProcessing && _backgroundTask != null)
                {
                    // 태스크 상태 직접 확인
                    bool isTaskCompleted = _backgroundTask.IsCompleted;

                    if (isTaskCompleted)
                    {
                        lock (_syncLock)
                        {
                            // 청크 생성 완료 상태로 설정
                            _isProcessing = false;
                            _isComplete = true;
                            _progress = 1.0f;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 진행 중인 청크 생성 작업을 취소합니다.
        /// </summary>
        public void Cancel()
        {
            // 실제 Task 취소는 CancellationToken 사용이 필요하나
            // 여기서는 간단히 상태만 변경
            lock (_syncLock)
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 청크 생성기의 상태를 초기화하고 모든 리소스를 해제합니다.
        /// </summary>
        public void Reset()
        {
            Cancel();

            lock (_syncLock)
            {
                _entities?.Clear();
                _terrainData = null;
                _isComplete = false;
                _isProcessing = false;
                _progress = 0f;
                _backgroundTask = null;
            }
        }

        /// <summary>
        /// 리소스를 정리하고 작업을 종료합니다.
        /// </summary>
        public void Dispose()
        {
            Cancel();

            if (_backgroundTask != null && !_backgroundTask.IsCompleted)
            {
                // 작업 완료 대기 (선택적)
                // _backgroundTask.Wait();
            }

            lock (_syncLock)
            {
                _entities?.Clear();
                _terrainData = null;
            }
        }
    }
}