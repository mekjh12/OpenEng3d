using BillBoard;
using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GPUDriven
{
    public unsafe class HiZRenderPass
    {
        private const int MAX_INSTANCES = 100000;  // 최대 인스턴스 개수
        private const int MAX_BATCHES = 64;        // 최대 배치 개수
        private const int COMMAND_SIZE = 16;       // DrawArraysIndirectCommand 크기 (4 uint * 4 bytes)
        private const int FRAME_COUNT_DEBUG = 60;   // 디버그 정보 갱신 주기

        #region SSBO 바인딩 포인트 문서화

        /* 아래는 예시이고 실제로 바인딩할 때는 다른 바인딩 포인트를 사용할 수 있음.
         * SSBO 바인딩 포인트:
         * 0  - Transform Buffer
         * 1  - Frustum Passed Indices
         * 2  - Frustum Counter
         * 3  - AABB Buffer
         * 4  - Batch ID Buffer
         * 5  - Visible Indices LOD0  ← 업데이트
         * 6  - Visible Counts LOD0   ← 업데이트
         * 7  - Visible Indices LOD1  ← 업데이트
         * 8  - Visible Counts LOD1   ← 업데이트
         * 9  - Visible Indices LOD2  ← 신규
         * 10 - Visible Counts LOD2   ← 신규
         * 11 - Visible Indices LOD3  ← 신규
         * 12 - Visible Counts LOD3   ← 신규
         * 13 - Indirect Command Buffer (UpdateCommandsCompute용) ← 10에서 변경
         * 14 - Visible Counts LOD0 (Command용) ← 11에서 변경
         * 15 - Visible Counts LOD1 (Command용) ← 12에서 변경
         * 16 - Visible Counts LOD2 (Command용) ← 신규
         * 17 - Visible Counts LOD3 (Command용) ← 신규
         */

        #endregion

        private readonly string _projPath;
        private ModelBatchManager _batchManager;

        // ===== GPU 버퍼 (SSBO) =====
        private uint _transformSSBO;           // 모든 인스턴스의 Transform 행렬
        private uint _aabbSSBO;                // 모든 인스턴스의 AABB
        private uint _batchIDSSBO;             // 각 인스턴스가 속한 배치 ID
        private uint _frustumPassedSSBO;       // 프러스텀 통과한 인스턴스 인덱스
        private uint _frustumCounterSSBO;      // 프러스텀 통과 개수
        private uint _indirectCommandBuffer;   // DrawIndirect 커맨드 버퍼

        // LOD별 가시 인스턴스 버퍼
        private uint _visibleIndicesSSBO_LOD0;  // LOD0 가시 인스턴스 인덱스 배열
        private uint _visibleIndicesSSBO_LOD1;  // LOD1 가시 인스턴스 인덱스 배열
        private uint _visibleIndicesSSBO_LOD2;  // LOD2 가시 인스턴스 인덱스 배열
        private uint _visibleIndicesSSBO_LOD3;  // LOD3 가시 인스턴스 인덱스 배열

        private uint _visibleCountsSSBO_LOD0;  // 배치별 LOD0 가시 개수
        private uint _visibleCountsSSBO_LOD1;  // 배치별 LOD1 가시 개수
        private uint _visibleCountsSSBO_LOD2;  // 배치별 LOD2 가시 개수
        private uint _visibleCountsSSBO_LOD3;  // 배치별 LOD3 가시 개수

        // ===== 셰이더 =====
        private FrustumCullingComputeShader _cullingCompute;        // 프러스텀 컬링
        private HiZOcclusionComputeShader _hiZOcclusionCompute;
        private UpdateIndirectCommandsComputeShader _updateCommandsCompute; // Indirect 커맨드 업데이트
        private GPUInstancedShader _instancedShader;                // 메시 렌더링
        private ImpostorInstancedShader _impostorInstancedShader;   // 임포스터 렌더링
        private UnlitShader _unlitShader;                           // 임포스터 생성용
        private SimpleQuadTestShader _simpleQuadTestShader;         // 디버그용 쿼드 렌더링
        private GPUDrivenImpostorShader _gpuDrivenImpostorShader;   // GPU 드리븐 임포스터 렌더링

        private AABBInstanceShader _aabbInstanceShader;             // AABB 인스턴스 렌더링 (디버그용)

        // ===== 임포스터 관련 =====
        private ImpostorAssets _impostor;
        public BaseModel3d _point;

        // ===== DrawIndirect 관련 =====
        private Dictionary<uint, int> _batchCommandStartIndices; // 배치별 커맨드 시작 인덱스
        private int _totalDrawCommands;                          // 전체 DrawIndirect 커맨드 개수

        // ===== 작업 버퍼 =====
        private uint[] _startIndices;  // Compute Shader에 전달할 시작 인덱스
        private uint[] _frustumCount;  // 프러스텀 통과 개수 읽기용

        // ===== 최적화 ======
        private BatchDescriptor _batch;             // 현재 렌더링 중인 배치
        private uint[] _zeros;                              // 제로 초기화 배열
        private uint[] _countsLOD0;
        private uint[] _countsLOD1;
        private uint[] _countsLOD2;
        private uint[] _countsLOD3;
        uint[] _frustumPassed = new uint[1];

        float[] _batchLODs;
        uint[] _batchStarts;
        uint[] _batchCounts;

        ImpostorRenderData _renderData;
        UnifiedTexturedModel _unifiedTexturedModel;

        // ===== 디버그 ======
        private bool _isSimpleQuadDraw = true;      // 디버그용 쿼드 렌더링 활성화 여부
        private int _frameCount = 0;

        private uint[] _lastVisibleCount_LOD0;
        private uint[] _lastVisibleCount_LOD1;
        private uint[] _lastVisibleCount_LOD2;
        private uint[] _lastVisibleCount_LOD3;

        private uint _lastFrustumPassed = 0;
        private Stopwatch _computeTimer;

        // --------------------------------------------------------
        // 속성
        // --------------------------------------------------------
        public bool IsSimpleQuadDraw { get => _isSimpleQuadDraw; set => _isSimpleQuadDraw = value; }

        public HiZRenderPass(string projPath)
        {
            _projPath = projPath;
            _computeTimer = new Stopwatch();
            _zeros = new uint[MAX_BATCHES];
            _countsLOD0 = new uint[MAX_BATCHES];
            _countsLOD1 = new uint[MAX_BATCHES];
            _countsLOD2 = new uint[MAX_BATCHES];
            _countsLOD3 = new uint[MAX_BATCHES];

            _point = Loader3d.LoadPoint(0, 0, 0);
        }

        /// <summary>
        /// 렌더러 초기화
        /// </summary>
        /// <param name="batchManager">Finalize된 배치 매니저</param>
        /// <param name="camera">카메라 (임포스터 생성용)</param>
        /// <param name="maxMipLevels">HiZ 최대 밉맵 레벨 (0이면 비활성화)</param>
        public void Initialize(ModelBatchManager batchManager, Camera camera, int maxMipLevels = 0)
        {
            // 1. 배치 매니저 검증
            if (!batchManager.IsFinalized)
            {
                throw new InvalidOperationException(
                    "BatchManager must be finalized before initializing renderer");
            }

            _batchManager = batchManager;

            if (maxMipLevels > 10)
            {
                throw new ArgumentException("Max mip levels exceed limit (10)");
            }

            Console.WriteLine("\n=== Initializing GPU Culling Renderer ===");
            Console.WriteLine($"Total Batches: {_batchManager.ActualBatchCount}");
            Console.WriteLine($"Total Instances: {_batchManager.TotalInstances}");

            // 2. 리소스 초기화
            _lastVisibleCount_LOD0 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD1 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD2 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD3 = new uint[MAX_BATCHES];
           
            // 배치별 커맨드 시작 인덱스 배열
            _startIndices = new uint[_batchManager.ActualBatchCount];
            _frustumCount = new uint[1];

            // 3. GPU 리소스 생성 (SSBO, Indirect Buffer 등)
            CreateSSBOs();

            // 4. 셰이더 로드 및 컴파일
            LoadShaders(_projPath, maxMipLevels);

            // 5. 인스턴스 데이터 업로드
            UploadToGPU();

            // 6. 임포스터 아틀라스 생성
            InitializeImpostors(camera);

            Console.WriteLine("=== GPU Culling Renderer Initialized ===");
            Console.WriteLine($"Max Instances: {MAX_INSTANCES}");
        }

        /// <summary>
        /// 모든 셰이더 로드
        /// </summary>
        private void LoadShaders(string projPath, int maxMipLevels)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _hiZOcclusionCompute = new HiZOcclusionComputeShader(projPath, maxMipLevels);
            _instancedShader = new GPUInstancedShader(projPath);
            _impostorInstancedShader = new ImpostorInstancedShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _updateCommandsCompute = new UpdateIndirectCommandsComputeShader(projPath);
            _simpleQuadTestShader = new SimpleQuadTestShader(projPath);
            _aabbInstanceShader = new AABBInstanceShader(projPath);
            _gpuDrivenImpostorShader = new GPUDrivenImpostorShader(projPath);
        }

        /// <summary>
        /// 배치별 임포스터 생성
        /// </summary>
        private void InitializeImpostors(Camera camera)
        {
            _impostor = new ImpostorAssets(_unlitShader, camera);

            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);
                _impostor.CreateImpostorModel(
                    ImpostorSettings.CreateSettings(batch.ModelName, 256, 16, 8),
                    batch.Model);

                Console.WriteLine($"Created impostor for: {batch.ModelName}");
            }
        }

        #region GPU 버퍼 생성


        private void CreateSSBOBuffer(ref uint bufferId, uint bufferIndex, uint size, BufferUsage bufferUsage = BufferUsage.DynamicDraw)
        {
            bufferId = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, bufferId);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, size, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferId);
        }


        /// <summary>
        /// 모든 SSBO 및 Indirect Command Buffer 생성
        /// </summary>
        private void CreateSSBOs()
        {
            // ===== 1단계: DrawIndirect 커맨드 인덱스 계산 =====
            CalculateCommandIndices();

            // ===== 2단계: 인스턴스 데이터 버퍼 생성 =====
            // Transform (Mat4x4)
            CreateSSBOBuffer(ref _transformSSBO, 0, MAX_INSTANCES * 64, BufferUsage.StaticDraw);
            // AABB (Min + Max = 8 floats)
            CreateSSBOBuffer(ref _aabbSSBO, 3, MAX_INSTANCES * 32, BufferUsage.StaticDraw);
            // Batch ID (uint)
            CreateSSBOBuffer(ref _batchIDSSBO, 9, MAX_INSTANCES * 4, BufferUsage.StaticDraw);

            // ===== 3단계: 프러스텀 컬링 중간 버퍼 =====
            // 프러스텀 통과 인스턴스 인덱스
            CreateSSBOBuffer(ref _frustumPassedSSBO, 1, MAX_INSTANCES * 4, BufferUsage.DynamicDraw);
            // 프러스텀 통과 개수 (Atomic Counter)
            CreateSSBOBuffer(ref _frustumCounterSSBO, 2, 4, BufferUsage.DynamicDraw);

            // ===== 4단계: LOD별 가시 인스턴스 버퍼 =====
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD0, 4, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD0, 5, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD1, 6, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD1, 7, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD2, 8, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD2, 9, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD3, 10, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD3, 11, (uint)(MAX_BATCHES * 4));

            // ===== 5단계: DrawIndirect 커맨드 버퍼 생성 =====
            CreateUnifiedIndirectBuffer();
        }

        /// <summary>
        /// 배치별 DrawIndirect 커맨드 시작 인덱스 계산
        /// 현재: 배치당 1개의 커맨드 (LOD0 메시만)
        /// </summary>
        private void CalculateCommandIndices()
        {
            _batchCommandStartIndices = new Dictionary<uint, int>();
            _totalDrawCommands = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                // 이 배치의 커맨드 시작 위치 저장
                _batchCommandStartIndices[b] = _totalDrawCommands;

                // 배치당 2개 커맨드 (LOD0, LOD1, LOD2, LOD3)
                _totalDrawCommands += 4;
            }

            Console.WriteLine($"Total Draw Commands: {_totalDrawCommands}");
        }

        private void CreateUnifiedIndirectBuffer()
        {
            int bufferSize = _totalDrawCommands * COMMAND_SIZE;

            _indirectCommandBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, (uint)bufferSize,
                          IntPtr.Zero, BufferUsage.DynamicDraw);

            int commandIndex = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                // LOD0 커맨드
                DrawArraysIndirectCommand cmdLOD0 = new DrawArraysIndirectCommand
                {
                    VertexCount = batch.VertexCount,
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };

                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)(commandIndex * COMMAND_SIZE), COMMAND_SIZE, cmdLOD0);
                commandIndex++;

                // ✅ LOD1 커맨드 (Point 1개 → Geometry Shader가 Quad로 확장)
                DrawArraysIndirectCommand cmdLOD1 = new DrawArraysIndirectCommand
                {
                    VertexCount = 1,  // Point 1개만
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };

                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)(commandIndex * COMMAND_SIZE), COMMAND_SIZE, cmdLOD1);
                commandIndex++;

                // ✅ LOD2 커맨드 (Point 1개 → Geometry Shader가 Quad로 확장)
                DrawArraysIndirectCommand cmdLOD2 = new DrawArraysIndirectCommand
                {
                    VertexCount = 1,  // Point 1개만
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };

                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)(commandIndex * COMMAND_SIZE), COMMAND_SIZE, cmdLOD2);
                commandIndex++;

                // ✅ LOD3 커맨드 (Point 1개 → Geometry Shader가 Quad로 확장)
                DrawArraysIndirectCommand cmdLOD3 = new DrawArraysIndirectCommand
                {
                    VertexCount = 1,  // Point 1개만
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };

                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)(commandIndex * COMMAND_SIZE), COMMAND_SIZE, cmdLOD3);
                commandIndex++;
            }
        }

        /// <summary>
        /// CPU에서 준비한 인스턴스 데이터를 GPU로 업로드
        /// </summary>
        private void UploadToGPU()
        {
            var transforms = _batchManager.GetTransforms();
            var aabbs = _batchManager.GetAABBs();
            var batchIDs = _batchManager.GetBatchIDs();

            // Transform 행렬 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(_batchManager.TotalInstances * 64), (IntPtr)ptr);
            }

            // AABB 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            fixed (AABB* ptr = aabbs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(_batchManager.TotalInstances * 32), (IntPtr)ptr);
            }

            // Batch ID 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _batchIDSSBO);
            fixed (uint* ptr = batchIDs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(_batchManager.TotalInstances * 4), (IntPtr)ptr);
            }

            Console.WriteLine($"Uploaded {_batchManager.TotalInstances} instances to GPU");
        }

        #endregion

        #region 프레임 업데이트

        /// <summary>
        /// 매 프레임 컬링 및 LOD 업데이트
        /// </summary>
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchyZBuffer hizBuffer)
        {
            // ===== 1단계: 프러스텀 컬링 =====
            // GPU에서 뷰 프러스텀 내부 객체만 필터링
            PerformFrustumCulling(camera, viewFrustum);

            // ===== 2단계: HiZ컬링과 LOD 그룹핑 =====
            PerformHiZCulling(camera, hizBuffer);

            // ===== 3단계: DrawIndirect 커맨드 업데이트 =====
            // GPU에서 각 커맨드의 InstanceCount 필드를 가시 개수로 설정
            UpdateIndirectCommandsGPU();

            _frameCount++;
        }

        /// <summary>
        /// 1단계: 프러스텀 컬링 Compute Shader 실행
        /// 입력: 모든 인스턴스 AABB
        /// 출력: 프러스텀 통과 인스턴스 인덱스 배열
        /// </summary>
        private void PerformFrustumCulling(Camera camera, Polyhedron viewFrustum)
        {
            if (_cullingCompute == null) return;

            // 카운터 초기화
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            _cullingCompute.Bind();

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            // 프러스텀 평면 전달
            _cullingCompute.LoadFrustumPlanes(viewFrustum.Planes);

            // Dispatch: 256 스레드씩 워크 그룹 실행
            int numWorkGroups = (MAX_INSTANCES + 255) / 256;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            // 다음 단계에서 결과 읽기 전 동기화
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _cullingCompute.Unbind();
        }

        private void InitializeBatchLODCount(uint buffer)
        {
            // 배치별 LOD 카운터 초기화
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
            fixed (uint* ptr = _zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }
        }

        /// <summary>
        /// 2단계: LOD 그룹핑 Compute Shader 실행
        /// 입력: 프러스텀 통과 인스턴스, 카메라 위치
        /// 출력: LOD0/LOD1별 가시 인스턴스 인덱스 및 배치별 개수
        /// </summary>
        private void PerformHiZCulling(Camera camera, HierarchyZBuffer hizBuffer)
        {
            InitializeBatchLODCount(_visibleCountsSSBO_LOD0);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD1);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD2);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD3);

            _hiZOcclusionCompute.Bind();

            // ===== SSBO 바인딩 =====
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _batchIDSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _visibleIndicesSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _visibleCountsSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _visibleIndicesSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _visibleCountsSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 11, _visibleIndicesSSBO_LOD3);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD3);

            // ===== Uniform 전달 =====
            _hiZOcclusionCompute.LoadHiZTextures(hizBuffer.HiZTexture);
            _hiZOcclusionCompute.LoadMaxMipLevel(hizBuffer.Levels - 1);
            _hiZOcclusionCompute.LoadVPMatrix(camera.VPMatrix);
            _hiZOcclusionCompute.LoadCameraPosition(camera.Position);
            _hiZOcclusionCompute.LoadScreenSize(hizBuffer.Width, hizBuffer.Height);
            _hiZOcclusionCompute.LoadMaxInstanceCount(MAX_INSTANCES);
            _hiZOcclusionCompute.LoadCameraNearFar(camera.NEAR, camera.FAR);
            _hiZOcclusionCompute.LoadViewMatrix(camera.ViewMatrix);

            // 배치별 LOD 거리 임계값 전달
            _batchLODs = _batchManager.GetBatchLODs();
            _batchStarts = _batchManager.GetBatchStarts();
            _batchCounts = _batchManager.GetBatchCounts();

            _hiZOcclusionCompute.LoadBatchLODs(_batchLODs);
            _hiZOcclusionCompute.LoadBatchStarts(_batchStarts);
            _hiZOcclusionCompute.LoadBatchCounts(_batchCounts);

            // 프러스텀 통과 개수 읽기
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, _frustumCount);

            // Dispatch: 64 스레드씩 워크 그룹 실행
            int numWorkGroups = ((int)_frustumCount[0] + 63) / 64;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
            _hiZOcclusionCompute.Unbind();
        }

        /// <summary>
        /// 3단계: DrawIndirect 커맨드의 InstanceCount 필드 업데이트
        /// GPU에서 각 배치의 가시 개수를 커맨드 버퍼에 기록
        /// </summary>
        private void UpdateIndirectCommandsGPU()
        {
            if (_updateCommandsCompute == null) return;

            _updateCommandsCompute.Bind();

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _indirectCommandBuffer);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 11, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 13, _visibleCountsSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 14, _visibleCountsSSBO_LOD3);

            // 배치별 커맨드 시작 인덱스 전달
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                _startIndices[b] = (uint)_batchCommandStartIndices[b];
            }

            _updateCommandsCompute.LoadNumBatches(_batchManager.ActualBatchCount);
            _updateCommandsCompute.LoadBatchCommandStartIndices(_startIndices);

            // Dispatch: 배치당 1 스레드
            Gl.DispatchCompute(_batchManager.ActualBatchCount, 1, 1);

            // 렌더링 전 커맨드 업데이트 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit |
                             MemoryBarrierMask.ShaderStorageBarrierBit);

            _updateCommandsCompute.Unbind();
        }

        #endregion

        #region 렌더링

        /// <summary>
        /// 모든 배치 렌더링
        /// </summary>
        public void Render(Camera camera)
        {
            // 렌더 스테이트 설정
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(true);
            Gl.Enable(EnableCap.CullFace);

            // Compute Shader 결과가 반영되도록 메모리 배리어
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit |
                             MemoryBarrierMask.CommandBarrierBit);

            if (_batchManager == null) return;

            // 각 배치별 렌더링
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                RenderBatch(b, camera);
            }
        }

        /// <summary>
        /// 단일 배치 렌더링 (DrawArraysIndirect 사용)
        /// </summary>
        private void RenderBatch(uint batchID, Camera camera)
        {
            // 배치 정보 가져오기
            _batch = _batchManager.GetBatch(batchID);
            string batchName = _batch.ModelName;
            int cmdStartIndex = _batchCommandStartIndices[batchID];
            _renderData = _impostor.GetImpostorRenderData(batchName);
            _unifiedTexturedModel = _impostor.UnifiedTexturedModel(batchName);

            // GPU 간접 명령 버퍼 바인딩
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // ===== LOD0: 메시 렌더링 =====
            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(_batch.StartIndex);
            _instancedShader.LoadViewMatrix(camera.ViewMatrix);
            _instancedShader.LoadTextureArray(_batch.Model.TextureIDArray);
            DrawArraysIndirect(_batch.VAO, cmdStartIndex, 0, COMMAND_SIZE, _visibleIndicesSSBO_LOD0, PrimitiveType.Triangles);
            _instancedShader.Unbind();

            // ===== LOD1: 테스트 사각형 렌더링 =====
            Gl.Disable(EnableCap.CullFace);  // Face culling 끄기
            int cmdOffset_LOD1 = (cmdStartIndex + 1) * COMMAND_SIZE;
            _aabbInstanceShader.Bind();
            _aabbInstanceShader.LoadVPMatrix(camera.VPMatrix);
            _aabbInstanceShader.LoadBatchStartOffset(_batch.StartIndex);
            _aabbInstanceShader.LoadCurrentBatchID(batchID);
            _aabbInstanceShader.LoadAlpha(0.3f);  // 반투명

            if (batchID == 0) _aabbInstanceShader.LoadBoxColor(1, 0, 0);
            else if (batchID == 1) _aabbInstanceShader.LoadBoxColor(0, 1, 0);
            else _aabbInstanceShader.LoadBoxColor(0, 0, 1);

            // ✅ 반투명 렌더링 설정
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // ✅ Depth 설정 (매우 중요!)
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(false);  // ✅ 반투명 객체는 depth write 비활성화!

            // ✅ Face culling (앞뒷면 모두 그리기)
            Gl.Disable(EnableCap.CullFace);

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _aabbSSBO);
            Gl.BindVertexArray(_point.VAO);

            // ✅ 디버깅: 몇 개 그려지는지 확인
            Gl.DrawArraysIndirect(PrimitiveType.Points, (IntPtr)cmdOffset_LOD1);

            // ✅ 상태 복원
            Gl.DepthMask(true);   // ✅ depth write 다시 활성화
            Gl.Enable(EnableCap.CullFace);
            Gl.Disable(EnableCap.Blend);

            _aabbInstanceShader.Unbind();

            // ===== LOD2: 테스트 사각형 렌더링 =====
            _simpleQuadTestShader.Bind();
            _simpleQuadTestShader.LoadVPMatrix(camera.VPMatrix);
            _simpleQuadTestShader.LoadCameraPosition(camera.Position);
            _simpleQuadTestShader.LoadBatchStartOffset(_batch.StartIndex);
            _simpleQuadTestShader.LoadCurrentBatchID(batchID);
            _simpleQuadTestShader.LoadQuadSize(0.25f);
            DrawArraysIndirect(_point.VAO,cmdStartIndex, 2, COMMAND_SIZE, _visibleIndicesSSBO_LOD2);

            // ===== LOD3: 테스트 사각형 렌더링 =====
            Gl.Disable(EnableCap.CullFace);
            _gpuDrivenImpostorShader.Bind();
            _gpuDrivenImpostorShader.LoadVPMatrix(camera.VPMatrix);
            _gpuDrivenImpostorShader.LoadCameraPosition(camera.Position);
            _gpuDrivenImpostorShader.LoadAABBSphereRadius(_unifiedTexturedModel.AABB.Radius);
            _gpuDrivenImpostorShader.LoadModelMatrix(_unifiedTexturedModel.AABB.ModelMatrix);
            _gpuDrivenImpostorShader.LoadImpostorAtlas(_renderData.AtlasTextureId);
            _gpuDrivenImpostorShader.LoadAtlasSize(_renderData.atlasSize);
            _gpuDrivenImpostorShader.LoadIndividualSize(_renderData.individualSize);
            _gpuDrivenImpostorShader.LoadFrameCounts(_renderData.horizontalFrames, _renderData.verticalFrames);
            _gpuDrivenImpostorShader.LoadEnableEdgeLine(false);
            _gpuDrivenImpostorShader.LoadBatchStartOffset(_batch.StartIndex);
            DrawArraysIndirect(_point.VAO, cmdStartIndex, 3, COMMAND_SIZE, _visibleIndicesSSBO_LOD3);
            _gpuDrivenImpostorShader.Unbind();

        }

        private void DrawArraysIndirect(uint vao, 
            int cmdStartIndex, 
            uint cmdOffset, 
            uint commandSize, 
            uint ssboIndex,
            PrimitiveType primitiveType = PrimitiveType.Points)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, ssboIndex);
            Gl.BindVertexArray(vao);
            uint cmdOffset_LOD3 = (uint)((cmdStartIndex + cmdOffset) * commandSize);
            Gl.DrawArraysIndirect(primitiveType, (IntPtr)cmdOffset_LOD3);
        }

        #endregion

        #region 설정 및 디버그

        /// <summary>
        /// 디버그용 가시 객체 개수 조회
        /// 성능을 위해 FRAME_COUNT_DEBUG 프레임마다만 GPU에서 읽음
        /// </summary>
        public void GetVisibleCountDebug(
            ref uint visibleCount,
            ref uint visibleCountLod0,
            ref uint visibleCountLod1,
            ref uint visibleCountLod2,
            ref uint visibleCountLod3,
            ref uint frustumPassCount,
            ref string report)
        {
            if (_frameCount % FRAME_COUNT_DEBUG == 0)
            {
                // 프러스텀 통과 개수
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, _frustumPassed);
                _lastFrustumPassed = _frustumPassed[0];

                // 배치별 LOD 개수
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
                fixed (uint* ptr = _countsLOD0)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
                fixed (uint* ptr = _countsLOD1)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD2);
                fixed (uint* ptr = _countsLOD2)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD3);
                fixed (uint* ptr = _countsLOD3)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                // 캐시 저장 및 합산
                uint totalVisible = 0;
                uint lod0 = 0;
                uint lod1 = 0;
                uint lod2 = 0;
                uint lod3 = 0;

                report = "";
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    _lastVisibleCount_LOD0[b] = _countsLOD0[b];
                    _lastVisibleCount_LOD1[b] = _countsLOD1[b];
                    _lastVisibleCount_LOD2[b] = _countsLOD2[b];
                    _lastVisibleCount_LOD3[b] = _countsLOD3[b];
                    totalVisible += _countsLOD0[b] + _countsLOD1[b] + _countsLOD2[b] + _countsLOD3[b];

                    report += $"({b}){_countsLOD0[b]}/{_countsLOD1[b]}/{_countsLOD2[b]}/{_countsLOD3[b]} \n";

                    // 배치별 개수 (디버그용)
                    if (b == 0) lod0 += _countsLOD0[b];
                    if (b == 1) lod1 += _countsLOD0[b];
                }

                visibleCountLod0 = lod0;
                visibleCountLod1 = lod1;
                visibleCountLod2 = lod2;
                visibleCountLod3 = lod3;

                visibleCount = totalVisible;
                frustumPassCount = _lastFrustumPassed;
            }
            else
            {
                // 캐시된 데이터 사용
                uint totalVisible = 0;
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    totalVisible += _lastVisibleCount_LOD0[b] + _lastVisibleCount_LOD1[b] + 
                                    _lastVisibleCount_LOD2[b] + _lastVisibleCount_LOD3[b];
                }
                visibleCount = totalVisible;
                frustumPassCount = _lastFrustumPassed;
            }
        }

        #endregion

        #region 리소스 정리

        public void Dispose()
        {
            // SSBO 삭제
            Gl.DeleteBuffers(_transformSSBO);
            Gl.DeleteBuffers(_aabbSSBO);
            Gl.DeleteBuffers(_batchIDSSBO);
            Gl.DeleteBuffers(_frustumPassedSSBO);
            Gl.DeleteBuffers(_frustumCounterSSBO);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD0);
            Gl.DeleteBuffers(_visibleCountsSSBO_LOD0);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD1);
            Gl.DeleteBuffers(_visibleCountsSSBO_LOD1);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD2);
            Gl.DeleteBuffers(_visibleCountsSSBO_LOD2);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD3);
            Gl.DeleteBuffers(_visibleCountsSSBO_LOD3);
            Gl.DeleteBuffers(_indirectCommandBuffer);

            Console.WriteLine("GPU Culling Renderer disposed");
        }

        #endregion
    }
}
