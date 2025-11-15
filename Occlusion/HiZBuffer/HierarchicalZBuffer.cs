using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Occlusion
{
    public class HierarchicalZBuffer : HierarchicalAbstracZBuffer
    {
        // ===================================================================
        // 멀티스레딩 필드
        // ===================================================================

        private int _maxThreads;
        private ManualResetEvent[] _levelDoneEvents;
        private int[] _levelCompletedCounts;
        private Thread[] _workerThreads;
        private ConcurrentQueue<MipMapThreadState>[] _workQueues;
        private ManualResetEvent[] _workerSignals;
        private volatile bool _workerRunning = true;


        // ===================================================================
        // 생성자 및 초기화
        // ===================================================================

        public HierarchicalZBuffer(int width, int height, string projectPath) : base(width, height, projectPath)
        {
            // 셰이더 초기화
            if (_terrainDepthShader == null) _terrainDepthShader = new TerrainDepthShader(projectPath);

            // 멀티스레딩 초기화
            InitializeWorkerThreads();
        }

        /// <summary>
        /// 워커 스레드와 관련 리소스를 초기화합니다.
        /// </summary>
        private void InitializeWorkerThreads()
        {
            _maxThreads = Environment.ProcessorCount;

            // 레벨별 동기화 이벤트 초기화
            _levelDoneEvents = new ManualResetEvent[_levels];
            for (int i = 0; i < _levels; i++)
            {
                _levelDoneEvents[i] = new ManualResetEvent(false);
            }
            _levelCompletedCounts = new int[_levels];

            // 워커 스레드 큐 및 시그널 초기화
            _workerThreads = new Thread[_maxThreads];
            _workQueues = new ConcurrentQueue<MipMapThreadState>[_maxThreads];
            _workerSignals = new ManualResetEvent[_maxThreads];

            for (int i = 0; i < _maxThreads; i++)
            {
                _workQueues[i] = new ConcurrentQueue<MipMapThreadState>();
                _workerSignals[i] = new ManualResetEvent(false);

                int threadIndex = i;
                _workerThreads[i] = new Thread(() => WorkerThreadLoop(threadIndex))
                {
                    IsBackground = true,
                    Name = $"MipMapWorker-{i}"
                };
                _workerThreads[i].Start();
            }
        }


        // ===================================================================
        // 워커 스레드 관리
        // ===================================================================

        /// <summary>
        /// 워커 스레드의 메인 루프입니다.
        /// 작업 큐에서 밉맵 생성 작업을 가져와 실행합니다.
        /// </summary>
        /// <param name="threadId">워커 스레드 ID</param>
        private void WorkerThreadLoop(int threadId)
        {
            while (_workerRunning)
            {
                if (_workQueues[threadId].TryDequeue(out MipMapThreadState state))
                {
                    state.WorkItem.Execute();

                    int completed = Interlocked.Increment(
                        ref state.CompletedCountArray[state.LevelIndex]);

                    if (completed >= state.ActiveThreads)
                    {
                        state.DoneEvent.Set();
                    }
                }
                else
                {
                    _workerSignals[threadId].WaitOne(10);
                    _workerSignals[threadId].Reset();
                }
            }
        }


        // ===================================================================
        // Z-버퍼 생성
        // ===================================================================

        /// <summary>
        /// GPU에서 계층적 Z 버퍼를 생성하고 CPU로 전송한 후,
        /// CPU에서 나머지 밉맵 레벨을 생성합니다.
        /// 
        /// 처리 순서:
        /// 1. GPU에서 레벨 0의 Z 버퍼 생성
        /// 2. 레벨 0 버퍼를 GPU에서 CPU로 전송
        /// 3. CPU에서 레벨 1부터 최고 레벨까지의 밉맵 생성
        /// </summary>
        /// <remarks>
        /// 성능 이점:
        /// - GPU 작업 최소화: 레벨 0만 GPU에서 생성
        /// - 데이터 전송 최소화: 레벨 0만 GPU→CPU 전송
        /// - 멀티스레딩 CPU 활용: 병렬 처리로 효율적인 밉맵 생성
        /// - 오버헤드 감소: 컴퓨트 셰이더 디스패치와 메모리 배리어 없음
        /// </remarks>
        [Obsolete("이 메서드는 향후 제거될 예정입니다. HierarchicalGpuZBuffer 클래스를 사용하십시오.")]
        public void GenerateZBuffer()
        {
            GenerateHierachyZBufferOnGPU(maxDepth: 0);
            TransferDepthDataToCPU(maxDepth: 0);
            GenerateMipmapLevelsOnCPU(fromLevel: 0, toLevel: -1);
        }

        /// <summary>
        /// CPU에서 멀티스레딩을 이용하여 밉맵 레벨을 생성합니다.
        /// </summary>
        /// <param name="fromLevel">시작 레벨 (이 레벨의 데이터로부터 다음 레벨을 생성)</param>
        /// <param name="toLevel">종료 레벨 (-1이면 최고 레벨까지)</param>
        private void GenerateMipmapLevelsOnCPU(int fromLevel = 0, int toLevel = -1)
        {
            if (toLevel == -1) toLevel = _levels - 1;

            for (int i = fromLevel; i < toLevel; i++)
            {
                int parentWidth = _width >> i;
                int parentHeight = _height >> i;
                int currWidth = _width >> (i + 1);
                int currHeight = _height >> (i + 1);

                float[] parentBuffer = _zbuffer[i];
                float[] currentBuffer = _zbuffer[i + 1];

                int rowsPerThread = (currHeight + _maxThreads - 1) / _maxThreads;
                int activeThreads = (currHeight + rowsPerThread - 1) / rowsPerThread;

                ManualResetEvent doneEvent = _levelDoneEvents[i];
                doneEvent.Reset();
                _levelCompletedCounts[i] = 0;

                // 각 스레드에 작업 할당
                for (int t = 0; t < activeThreads; t++)
                {
                    int startY = t * rowsPerThread;
                    int endY = Math.Min(startY + rowsPerThread, currHeight);

                    MipMapThreadState state = new MipMapThreadState
                    {
                        WorkItem = new MipMapWorkItem
                        {
                            StartY = startY,
                            EndY = endY,
                            ParentWidth = parentWidth,
                            ParentHeight = parentHeight,
                            CurrWidth = currWidth,
                            CurrHeight = currHeight,
                            ParentBuffer = parentBuffer,
                            CurrentBuffer = currentBuffer
                        },
                        LevelIndex = i,
                        ActiveThreads = activeThreads,
                        DoneEvent = doneEvent,
                        CompletedCountArray = _levelCompletedCounts
                    };

                    _workQueues[t].Enqueue(state);
                    _workerSignals[t].Set();
                }

                doneEvent.WaitOne();
            }
        }


        // ===================================================================
        // 렌더링 메서드
        // ===================================================================

        /// <summary>
        /// 폐색체들을 간단하게 렌더링합니다.
        /// </summary>
        /// <param name="shader">사용할 간단한 셰이더</param>
        /// <param name="entities">렌더링할 폐색체 목록</param>
        /// <param name="view">뷰 행렬</param>
        /// <param name="proj">투영 행렬</param>
        /// <exception cref="ArgumentNullException">entity나 model이 null인 경우</exception>
        public void RenderSimpleEntity(SimpleDepthShader shader, List<PhysicalRenderEntity> entities, Matrix4x4f view, Matrix4x4f proj)
        {
            Gl.Disable(EnableCap.Blend);

            shader.Bind();
            shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.proj, proj);
            shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.view, view);

            foreach (PhysicalRenderEntity entity in entities)
            {
                if (entity?.Model == null || entity.Model.Length == 0)
                    throw new ArgumentNullException(nameof(entity));

                shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.model, entity.ModelMatrix);

                foreach (RawModel3d rawModel in entity.Model)
                {
                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }

            shader.Unbind();
        }

        // ===================================================================
        // 유틸리티 메서드
        // ===================================================================

        /// <summary>
        /// 지정된 레벨에서의 너비와 높이를 계산하여 반환합니다.
        /// 각 레벨마다 이전 레벨의 1/2 크기로 축소됩니다.
        /// </summary>
        /// <param name="level">계산할 레벨 (0: 원본 크기)</param>
        /// <returns>해당 레벨에서의 너비와 높이</returns>
        public Vertex2i GetLevelResolution(int level)
        {
            int cw = _width >> level;
            int ch = _height >> level;
            return new Vertex2i(cw, ch);
        }


        // ===================================================================
        // 내부 구조체
        // ===================================================================

        /// <summary>
        /// 워커 스레드에 전달되는 상태 정보 (구조체로 Zero-Allocation 구현).
        /// </summary>
        private struct MipMapThreadState
        {
            public MipMapWorkItem WorkItem;
            public int LevelIndex;
            public int ActiveThreads;
            public ManualResetEvent DoneEvent;
            public int[] CompletedCountArray;
        }

        /// <summary>
        /// 밉맵 생성 작업을 나타내는 구조체.
        /// </summary>
        private struct MipMapWorkItem
        {
            public int StartY;
            public int EndY;
            public int ParentWidth;
            public int ParentHeight;
            public int CurrWidth;
            public int CurrHeight;
            public float[] ParentBuffer;
            public float[] CurrentBuffer;

            /// <summary>
            /// 할당된 행 범위에 대해 밉맵을 생성합니다.
            /// 부모 레벨의 2x2 픽셀 중 최대 깊이값을 선택하여 현재 레벨의 픽셀을 생성합니다.
            /// </summary>
            public void Execute()
            {
                for (int y = StartY; y < EndY; y++)
                {
                    for (int x = 0; x < CurrWidth; x++)
                    {
                        int m = 2 * x;
                        int n = 2 * y;

                        // 2x2 영역의 최대 깊이값 계산
                        float topLeft = ParentBuffer[(n + 0) * ParentWidth + (m + 0)];
                        float topRight = ParentBuffer[(n + 0) * ParentWidth + (m + 1)];
                        float bottomLeft = ParentBuffer[(n + 1) * ParentWidth + (m + 0)];
                        float bottomRight = ParentBuffer[(n + 1) * ParentWidth + (m + 1)];

                        float maxDepth = Math.Max(Math.Max(topLeft, bottomLeft),
                                                   Math.Max(topRight, bottomRight));

                        // 홀수 너비 처리: 마지막 열에서 추가 샘플 고려
                        if ((ParentWidth & 1) == 1 && m + 2 < ParentWidth)
                        {
                            if (x == CurrWidth - 1)
                            {
                                float extraTopRight = ParentBuffer[(n + 0) * ParentWidth + (m + 2)];
                                float extraBottomRight = ParentBuffer[(n + 1) * ParentWidth + (m + 2)];
                                maxDepth = Math.Max(maxDepth, Math.Max(extraTopRight, extraBottomRight));
                            }
                        }

                        // 홀수 높이 처리: 마지막 행에서 추가 샘플 고려
                        if ((ParentHeight & 1) == 1 && n + 2 < ParentHeight)
                        {
                            if (y == CurrHeight - 1)
                            {
                                float extraBottomLeft = ParentBuffer[(n + 2) * ParentWidth + (m + 0)];
                                float extraBottomRight = ParentBuffer[(n + 2) * ParentWidth + (m + 1)];
                                maxDepth = Math.Max(maxDepth, Math.Max(extraBottomLeft, extraBottomRight));
                            }
                        }

                        CurrentBuffer[y * CurrWidth + x] = maxDepth;
                    }
                }
            }
        }
    }
}