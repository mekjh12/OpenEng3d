using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GPUDriven
{
    public unsafe abstract class RenderPassPipeLine
    {
        // ------------------------------------------------------------
        // 상수
        // ------------------------------------------------------------

        protected const int COMMAND_SIZE = 16;
        //protected const int COMMAND_SIZE_ELEMENTS = 20;
        protected const int BYTES_PER_BATCH = 64;
        protected const int FRAME_COUNT_DEBUG = 5;

        // 
        private readonly uint MAX_BATCHES_COUNT = 64;       //  
        private readonly uint MAX_INSTANCES = 1_000_000;    // 최대 수용 가능 인스턴스의 개수
        private readonly int _maxMipLevels = 10;        // HiZ 최대 밉맵 레벨

        // LOD별 커맨드 오프셋 (바이트)
        private static readonly int[] LOD_OFFSETS = new int[] { 0, 16, 32, 48 };

        // ------------------------------------------------------------
        // SSBO 바인딩 포인트 상수 (고정 할당)
        // ------------------------------------------------------------
        protected const int BINDING_TRANSFORM = 0;
        protected const int BINDING_FRUSTUM_PASSED = 1;
        protected const int BINDING_FRUSTUM_COUNTER = 2;
        protected const int BINDING_AABB = 3;
        protected const int BINDING_BATCH_ID = 4;
        protected const int BINDING_VISIBLE_INDICES_LOD0 = 5;
        protected const int BINDING_VISIBLE_COUNTS_LOD0 = 6;
        protected const int BINDING_VISIBLE_INDICES_LOD1 = 7;
        protected const int BINDING_VISIBLE_COUNTS_LOD1 = 8;
        protected const int BINDING_VISIBLE_INDICES_LOD2 = 9;
        protected const int BINDING_VISIBLE_COUNTS_LOD2 = 10;
        protected const int BINDING_VISIBLE_INDICES_LOD3 = 11;
        protected const int BINDING_VISIBLE_COUNTS_LOD3 = 12;
        protected const int BINDING_INDIRECT_COMMAND = 13;
        protected const int BINDING_BATCH_INFO = 14;
        protected const int BINDING_VISIBILITY = 15;

        /* SSBO 바인딩 포인트 맵 (참조용)
         * ================================================
         *   Binding 0  : Transform Buffer (Mat4x4 변환 행렬)
         *   Binding 1  : Frustum Passed Indices (프러스텀 통과 인스턴스)
         *   Binding 2  : Frustum Counter (프러스텀 통과 개수, Atomic)
         *   Binding 3  : AABB Buffer (바운딩 박스)
         *   Binding 4  : Batch ID Buffer (인스턴스별 배치 ID)
         *   Binding 5  : Visible Indices LOD0
         *   Binding 6  : Visible Counts LOD0
         *   Binding 7  : Visible Indices LOD1
         *   Binding 8  : Visible Counts LOD1
         *   Binding 9  : Visible Indices LOD2
         *   Binding 10 : Visible Counts LOD2
         *   Binding 11 : Visible Indices LOD3
         *   Binding 12 : Visible Counts LOD3
         *   Binding 13 : Indirect Command Buffer
         *   Binding 14 : Batch Info Buffer
         *   Binding 15 : Visibility Buffer (Temporal Dithering)
         * ================================================
         */

        // ------------------------------------------------------------
        // 멤버 변수
        // ------------------------------------------------------------

        private readonly string _projPath;              // 프로젝트 경로
        private readonly string _name;                  // 렌더 패스 이름
        protected ModelBatchManager _batchManager;      // 배치 매니저

        // 설정 변수
        protected uint _batchedModelCount = 0;            // 배치된 모델의 개수

        // GPU 버퍼 (SSBO)
        protected uint _transformSSBO;           // 모든 인스턴스의 Transform 행렬
        protected uint _aabbSSBO;                // 모든 인스턴스의 AABB
        protected uint _batchIDSSBO;             // 각 인스턴스가 속한 배치 ID
        protected uint _frustumPassedSSBO;       // 프러스텀 통과한 인스턴스 인덱스
        protected uint _frustumCounterSSBO;      // 프러스텀 통과 개수 (Atomic Counter)
        protected int _indirectCommandBuffer = -1;   // DrawIndirect 커맨드 버퍼
        private uint _batchInfoSSBO;            // 배치 정보 버퍼

        // LOD별 가시 인스턴스 버퍼
        protected uint _visibleIndicesSSBO_LOD0;  // LOD0 가시 인스턴스 인덱스 배열
        protected uint _visibleIndicesSSBO_LOD1;  // LOD1 가시 인스턴스 인덱스 배열
        protected uint _visibleIndicesSSBO_LOD2;  // LOD2 가시 인스턴스 인덱스 배열
        protected uint _visibleIndicesSSBO_LOD3;  // LOD3 가시 인스턴스 인덱스 배열

        protected uint _visibleCountsSSBO_LOD0;  // 배치별 LOD0 가시 개수
        protected uint _visibleCountsSSBO_LOD1;  // 배치별 LOD1 가시 개수
        protected uint _visibleCountsSSBO_LOD2;  // 배치별 LOD2 가시 개수
        protected uint _visibleCountsSSBO_LOD3;  // 배치별 LOD3 가시 개수

        // Visibility 버퍼 추가 
        protected uint _visibilitySSBO;  // 인스턴스별 visibility 값 (0.0~1.0) (binding 16)

        // 셰이더
        private FrustumCullingComputeShader _cullingCompute;    // 프러스텀 컬링 컴퓨트 셰이더
        private HiZOcclusionComputeShader _hiZOcclusionCompute; // Hi-Z 오클루전 컬링 컴퓨트 셰이더
        private GPUInstancedDepthShader _depthShader;               // 깊이 렌더링 셰이더
        protected GPUInstancedShadowMapShader _gpuInstancedShadowMapShader;
        private UpdateIndirectCommandsComputeShader _updateCommandsCompute; 
                                                                // Indirect 커맨드 업데이트 컴퓨트 셰이더

        // DrawIndirect 관련
        protected Dictionary<uint, int> _batchCommandStartIndices; // 배치별 커맨드 시작 인덱스 (바이트 오프셋)
        private int _totalDrawCommands;                          // 전체 DrawIndirect 커맨드 개수

        // 작업 버퍼
        private uint[] _startIndices;  // Compute Shader에 전달할 시작 인덱스 (예) [batchid,index] [0,0] [1,1000] ...
        private uint[] _frustumCount;  // 프러스텀 통과 개수 읽기용
        private uint[] _zeros;         // 제로 초기화 배열
        private uint[] _countsLOD0;    // LOD0 가시 개수 읽기용
        private uint[] _countsLOD1;    // LOD1 가시 개수 읽기용
        private uint[] _countsLOD2;    // LOD2 가시 개수 읽기용
        private uint[] _countsLOD3;    // LOD3 가시 개수 읽기용
        private uint[] _frustumPassed; // 프러스텀 통과 개수 읽기용

        // 배치 정보 캐시
        private float[] _batchLODs;    // 배치별 LOD 거리 임계값
        private uint[] _batchStarts;   // 배치별 시작 인덱스
        private uint[] _batchCounts;   // 배치별 인스턴스 개수

        // LOD 거리 임계값 캐시
        private float _distance0;
        private float _distance1;
        private float _distance2;

        // 렌더링 임시 변수
        private BatchDescriptor _batch;     // 현재 렌더링 중인 배치
        protected UnifiedTexturedModel _unifiedTexturedModel; // 통합 텍스처 모델
        public BaseModel3d _point;          // 단일 포인트 모델 (인스턴싱용)

        // 디버그 정보 캐시
        protected uint[] _lastVisibleCount_LOD0;  // 마지막 LOD0 가시 개수
        protected uint[] _lastVisibleCount_LOD1;  // 마지막 LOD1 가시 개수
        protected uint[] _lastVisibleCount_LOD2;  // 마지막 LOD2 가시 개수
        protected uint[] _lastVisibleCount_LOD3;  // 마지막 LOD3 가시 개수
        protected uint _lastFrustumPassed = 0;    // 마지막 프러스텀 통과 개수
        protected int _frameCount = 0;            // 프레임 카운터

        // 디버그 옵션
        protected bool _isDebugLOD1 = false;                          // LOD1 디버깅 여부 
        protected Vertex4f COLOR_RED4 = new Vertex4f(1f, 0f, 0f, 1f);  // 빨간색
        protected Vertex4f COLOR_GREEN4 = new Vertex4f(0f, 1f, 0f, 1f); // 초록색
        protected Vertex4f COLOR_BLUE4 = new Vertex4f(0f, 0f, 1f, 1f);  // 파란색

        // 최적화
        uint[] _batchStartIndices;

        // ------------------------------------------------------------
        // 상속 변수 및 추상 메서드
        // ------------------------------------------------------------
        private bool _isInitialized = false;
        public abstract void RenderBatchLod0(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        public abstract void RenderBatchLod1(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        public abstract void RenderBatchLod2(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);
        public abstract void RenderBatchLod3(uint batchID, BatchDescriptor batch, string batchName, int cmdStartIndex, Camera camera);

        // ------------------------------------------------------------
        // 속성
        // ------------------------------------------------------------

        public bool IsDebugLOD1 { get => _isDebugLOD1; set => _isDebugLOD1 = value; }

        // ------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------

        /// <summary>
        /// HiZRenderPass 생성자
        /// </summary>
        /// <param name="name">렌더패스 이름</param>
        /// <param name="projPath">프로젝트 경로</param>
        /// <param name="maxInstances">최대 수용 가능 인스턴스의 개수</param>
        /// <param name="maxBatches">최대 배치모델의 개수(나무1, 나무2, 바위1, ... )</param>
        /// <param name="maxMipLevels">HiZ 최대 밉맵 레벨 (0이면 비활성화)</param>
        public RenderPassPipeLine(string name, string projPath, uint maxInstances = 100_000, uint maxBatches = 64, int maxMipLevels = 7)
        {
            // 이름 설정
            _name = name;

            // 최대 수용 인스턴스 및 배치 수 설정
            MAX_INSTANCES = maxInstances;
            MAX_BATCHES_COUNT = maxBatches;
            _maxMipLevels = maxMipLevels;
            
            // 작업 버퍼 초기화
            _projPath = projPath;
            _zeros = new uint[MAX_BATCHES_COUNT];
            _countsLOD0 = new uint[MAX_BATCHES_COUNT];
            _countsLOD1 = new uint[MAX_BATCHES_COUNT];
            _countsLOD2 = new uint[MAX_BATCHES_COUNT];
            _countsLOD3 = new uint[MAX_BATCHES_COUNT];
            _frustumPassed = new uint[1];

            // 단일 포인트 모델 로드
            _point = Loader3d.LoadPoint(0, 0, 0);
        }

        // ------------------------------------------------------------
        // 초기화
        // ------------------------------------------------------------

        #region 초기화 관련 및 버퍼 생성

        /// <summary>
        /// 렌더러 초기화
        /// </summary>
        public virtual void Initialize(Camera camera, ModelBatchManager batchManager, 
            float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f)
        {
            // 배치 매니저 설정
            if (!batchManager.IsFinalized)
            {
                throw new InvalidOperationException(
                    "BatchManager must be finalized before initializing renderer");
            }

            // LOD 거리 임계값 설정
            _distance0 = distance0;
            _distance1 = distance1;
            _distance2 = distance2;

            // 배치 매니저 참조 저장
            _batchManager = batchManager;
            _batchedModelCount = _batchManager.ActualBatchCount;

            // 리소스 초기화
            _lastVisibleCount_LOD0 = new uint[MAX_BATCHES_COUNT];
            _lastVisibleCount_LOD1 = new uint[MAX_BATCHES_COUNT];
            _lastVisibleCount_LOD2 = new uint[MAX_BATCHES_COUNT];
            _lastVisibleCount_LOD3 = new uint[MAX_BATCHES_COUNT];

            // 배치 정보 캐시
            _startIndices = new uint[_batchedModelCount];      // 배치별 시작 인덱스(실제 배치된 인스턴스 기준)
            _frustumCount = new uint[1];

            // GPU 리소스 생성
            CreateSSBOs();

            // CPU -> GPU 데이터 업로드
            UploadToGPU();

            // 셰이더 로드
            if (_maxMipLevels > 11)
            {
                throw new ArgumentException("Max mip levels exceed limit (11)");
            }
            LoadShaders(_maxMipLevels);

            _isInitialized = true;

            //Console.WriteLine("\n========== Visibility Buffer Test ==========");
            //Console.WriteLine($"Buffer ID: {_visibilitySSBO}");
            //Console.WriteLine($"Max Instances: {_maxInstances}");
            //Console.WriteLine($"Buffer Size: {_maxInstances * 4 / 1024.0:F1} KB");

            // 처음 5개 값 확인
            //DebugPrintVisibility(5);
            //Console.WriteLine("===========================================\n");
        }

        // ------------------------------------------------------------
        // 셰이더 로드
        // ------------------------------------------------------------

        /// <summary>
        /// 모든 셰이더 로드
        /// </summary>
        /// <param name="maxMipLevels">Hi-Z 최대 밉맵 레벨</param>
        private void LoadShaders(int maxMipLevels)
        {
            if (!ShaderManager.Instance.HasShader("FrustumCullingComputeShader"))
                ShaderManager.Instance.AddShader(new FrustumCullingComputeShader(StrRes.PROJECT_PATH));

            if (!ShaderManager.Instance.HasShader("HiZOcclusionComputeShader"))
                ShaderManager.Instance.AddShader(new HiZOcclusionComputeShader(StrRes.PROJECT_PATH, maxMipLevels));

            if (!ShaderManager.Instance.HasShader("UpdateIndirectCommandsComputeShader"))
                ShaderManager.Instance.AddShader(new UpdateIndirectCommandsComputeShader(StrRes.PROJECT_PATH));

            if (!ShaderManager.Instance.HasShader("GPUInstancedDepthShader"))
                ShaderManager.Instance.AddShader(new GPUInstancedDepthShader(StrRes.PROJECT_PATH));

            if (!ShaderManager.Instance.HasShader("GPUInstancedShadowMapShader"))
                ShaderManager.Instance.AddShader(new GPUInstancedShadowMapShader(StrRes.PROJECT_PATH));

            _cullingCompute = ShaderManager.Instance.GetShader<FrustumCullingComputeShader>();
            _hiZOcclusionCompute = ShaderManager.Instance.GetShader<HiZOcclusionComputeShader>();
            _updateCommandsCompute = ShaderManager.Instance.GetShader<UpdateIndirectCommandsComputeShader>();
            _depthShader = ShaderManager.Instance.GetShader<GPUInstancedDepthShader>();
            _gpuInstancedShadowMapShader = ShaderManager.Instance.GetShader<GPUInstancedShadowMapShader>();
        }

        // ------------------------------------------------------------
        // GPU 버퍼 생성
        // ------------------------------------------------------------

        /// <summary>
        /// SSBO 버퍼 생성 및 바인딩
        /// </summary>
        /// <param name="bufferId">버퍼 ID (out)</param>
        /// <param name="bufferIndex">바인딩 포인트 인덱스</param>
        /// <param name="size">버퍼 크기 (바이트)</param>
        /// <param name="bufferUsage">버퍼 사용 타입</param>
        private void CreateSSBOBuffer(ref uint bufferId, uint bufferIndex, uint size, BufferUsage bufferUsage = BufferUsage.DynamicDraw)
        {
            bufferId = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, bufferId);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, size, IntPtr.Zero, bufferUsage);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferId);
        }

        /// <summary>
        /// 모든 SSBO 및 Indirect Command Buffer 생성
        /// </summary>
        private void CreateSSBOs()
        {
            // 배치별 커맨드 시작 인덱스 계산
            CalculateCommandIndices();

            // 구조체 크기 정확하게 계산
            uint instanceDataSize = (uint)Marshal.SizeOf<InstanceModelMatrixData>();  // 128
            uint aabbSize = (uint)Marshal.SizeOf<AABB>();  // 32
            uint batchIDSize = sizeof(uint);  // 4
            uint batchInfoSize = (uint)((MAX_BATCHES_COUNT+1) * Marshal.SizeOf<BatchInfoGPU>());

            // 인스턴스 데이터 버퍼
            CreateSSBOBuffer(ref _transformSSBO, BINDING_TRANSFORM, MAX_INSTANCES * instanceDataSize, BufferUsage.StaticDraw);
            CreateSSBOBuffer(ref _aabbSSBO, BINDING_AABB, MAX_INSTANCES * aabbSize, BufferUsage.StaticDraw);
            CreateSSBOBuffer(ref _batchIDSSBO, BINDING_BATCH_ID, MAX_INSTANCES * batchIDSize, BufferUsage.StaticDraw);

            // 배치 정보 SSBO 생성
            CreateSSBOBuffer(ref _batchInfoSSBO, BINDING_BATCH_INFO, batchInfoSize, BufferUsage.StaticDraw);

            // 프러스텀 컬링 중간 버퍼
            CreateSSBOBuffer(ref _frustumPassedSSBO, BINDING_FRUSTUM_PASSED, MAX_INSTANCES * sizeof(int), BufferUsage.DynamicDraw);
            CreateSSBOBuffer(ref _frustumCounterSSBO, BINDING_FRUSTUM_COUNTER, sizeof(uint), BufferUsage.DynamicDraw);

            // HiZ컬링 후 LOD별 가시 인스턴스 버퍼
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD0, BINDING_VISIBLE_INDICES_LOD0, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD0, BINDING_VISIBLE_COUNTS_LOD0, (uint)(MAX_BATCHES_COUNT * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD1, BINDING_VISIBLE_INDICES_LOD1, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD1, BINDING_VISIBLE_COUNTS_LOD1, (uint)(MAX_BATCHES_COUNT * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD2, BINDING_VISIBLE_INDICES_LOD2, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD2, BINDING_VISIBLE_COUNTS_LOD2, (uint)(MAX_BATCHES_COUNT * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD3, BINDING_VISIBLE_INDICES_LOD3, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD3, BINDING_VISIBLE_COUNTS_LOD3, (uint)(MAX_BATCHES_COUNT * 4));

            // Visibility 버퍼 생성(인스턴스당 float 1개 = 4 bytes)
            CreateSSBOBuffer(ref _visibilitySSBO, BINDING_VISIBILITY, MAX_INSTANCES * sizeof(float), BufferUsage.DynamicDraw);

            // Visibility 버퍼 0.0으로 초기화
            InitializeVisibilityBuffer();

            // DrawIndirect 커맨드 버퍼 생성
            CreateUnifiedIndirectBuffer();
        }

        /// <summary>
        /// Visibility 버퍼를 0.0으로 초기화
        /// </summary>
        private void InitializeVisibilityBuffer()
        {
            float[] initialVisibility = new float[MAX_INSTANCES];

            // 모두 0.0으로 초기화 (처음엔 안 보임)
            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                initialVisibility[i] = 0.0f;
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibilitySSBO);

            unsafe
            {
                fixed (float* ptr = initialVisibility)
                {
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)(MAX_INSTANCES * sizeof(float)),
                        (IntPtr)ptr
                    );
                }
            }

            Console.WriteLine($"[Visibility] Buffer initialized: {MAX_INSTANCES} instances ({MAX_INSTANCES * 4 / 1024.0:F1} KB)");
        }

        /// <summary>
        /// Visibility 버퍼 리셋 (씬 변경 시)
        /// </summary>
        public void ResetVisibility()
        {
            if (_visibilitySSBO == 0) return;

            float[] resetData = new float[MAX_INSTANCES];

            // 모두 0.0으로 리셋
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibilitySSBO);

            unsafe
            {
                fixed (float* ptr = resetData)
                {
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)(MAX_INSTANCES * sizeof(float)),
                        (IntPtr)ptr
                    );
                }
            }

            // Console.WriteLine("[Visibility] Buffer reset to 0.0");
        }

        /// <summary>
        /// 배치별 DrawIndirect 커맨드 시작 인덱스 계산
        /// </summary>
        private void CalculateCommandIndices()
        {
            _batchCommandStartIndices = new Dictionary<uint, int>();
            _totalDrawCommands = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                _batchCommandStartIndices[b] = (int)(b * BYTES_PER_BATCH);
                _totalDrawCommands += 4; // 배치당 4개의 LOD 커맨드
            }
        }

        /// <summary>
        /// 통합 Indirect Command Buffer 생성 및 초기화
        /// LOD0, LOD1, LOD2, LOD3 커맨드를 순차적으로 배치
        /// </summary>
        public abstract void CreateUnifiedIndirectBuffer();

        /// <summary>
        /// 배치 메타데이터를 GPU로 업로드
        /// </summary>
        private void UploadBatchInfoToGPU()
        {
            BatchInfoGPU[] batchInfos = new BatchInfoGPU[MAX_BATCHES_COUNT + 1];

            // 0번 인덱스는 사용 안 함
            batchInfos[0] = new BatchInfoGPU
            {
                LODDistance = 0,
                StartIndex = MAX_INSTANCES,
                Count = _batchedModelCount,
                Padding = 0
            };

            // 1 ~ MAX_BATCHES_COUNT까지는 배치정보
            for (uint i = 0; i < _batchedModelCount; i++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(i);

                batchInfos[i + 1] = new BatchInfoGPU
                {
                    LODDistance = batch.LODDistance,
                    StartIndex = batch.StartIndex,
                    Count = batch.Count,
                    Padding = 0
                };
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _batchInfoSSBO);

            unsafe
            {
                fixed (BatchInfoGPU* ptr = batchInfos)
                {
                    uint sizeInBytes = (uint)((MAX_BATCHES_COUNT + 1) * Marshal.SizeOf<BatchInfoGPU>());
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        sizeInBytes,
                        (IntPtr)ptr
                    );

                    int error = (int)Gl.GetError();
                    if (error != 0)
                    {
                        Console.WriteLine($"  ❌ BatchInfo Upload Error: 0x{error:X}");
                    }
                    else
                    {
                        Console.WriteLine($"\t[OK] BatchInfo SSBO uploaded: {sizeInBytes / 1024.0:F1} KB");
                    }
                }
            }
        }

        /// <summary>
        /// CPU에서 준비한 인스턴스 데이터를 GPU로 업로드
        /// </summary>
        private void UploadToGPU()
        {
            // (1) 배치 정보 업로드
            UploadBatchInfoToGPU();

            // (2) 인스턴스 데이터 업로드
            const int GL_BUFFER_SIZE = 0x8764;  // BufferParameterName.BufferSize

            if (!_batchManager.IsFinalized)
            {
                throw new InvalidOperationException("BatchManager must be finalized!");
            }

            InstanceModelMatrixData[] instanceData = _batchManager.GetInstanceData();

            AABB[] aabbs = _batchManager.GetAABBs();
            uint[] batchIDs = _batchManager.GetBatchIDs();

            uint totalInstances = _batchManager.TotalInstances;

            int instanceDataSize = Marshal.SizeOf<InstanceModelMatrixData>();
            int aabbSize = Marshal.SizeOf<AABB>();
            int batchIDSize = sizeof(uint);

            Console.WriteLine($"----------------------------------------------------------------------");
            Console.WriteLine($"          GPU 업로드(GPU-DRIVEN **{_name}** 모델) 정보");
            Console.WriteLine($"----------------------------------------------------------------------");
            Console.WriteLine($"[GPU Upload] Starting...");
            Console.WriteLine($"  Total Instances: {totalInstances}");
            Console.WriteLine($"  InstanceModelMatrixData size: {instanceDataSize} bytes");
            Console.WriteLine($"  AABB size: {aabbSize} bytes");

            // ------------------------------
            // Transform SSBO 업로드
            // ------------------------------
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);

            // 버퍼 크기 확인
            int bufferSize = 0;
            Gl.GetBufferParameter(BufferTarget.ShaderStorageBuffer, GL_BUFFER_SIZE, out bufferSize);
            Console.WriteLine($"Transform SSBO buffer size: {bufferSize} bytes");

            uint requiredSize = totalInstances * (uint)instanceDataSize;

            if (bufferSize < requiredSize)
            {
                Console.WriteLine($"  ❌ ERROR: Buffer too small! Allocating new buffer...");
                Gl.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    requiredSize,
                    IntPtr.Zero,
                    BufferUsage.DynamicDraw
                );
            }

            unsafe
            {
                fixed (InstanceModelMatrixData* ptr = instanceData)
                {
                    uint sizeInBytes = totalInstances * (uint)instanceDataSize;

                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        sizeInBytes,
                        (IntPtr)ptr
                    );

                    // ✅ OpenGL 에러 확인
                    int error = (int)Gl.GetError();
                    if (error != 0)
                    {
                        Console.WriteLine($"  ❌ OpenGL Error after BufferSubData: 0x{error:X} ({(ErrorCode)error})");
                    }
                    else
                    {
                        Console.WriteLine($"\t[OK] Transform SSBO uploaded: {sizeInBytes / 1024.0:F1} KB");
                    }
                }
            }

            // ------------------------------
            // AABB 업로드
            // ------------------------------
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);

            Gl.GetBufferParameter(BufferTarget.ShaderStorageBuffer, GL_BUFFER_SIZE, out bufferSize);
            requiredSize = totalInstances * (uint)aabbSize;

            Console.WriteLine($"AABB SSBO buffer size: {bufferSize} bytes");

            if (bufferSize < requiredSize)
            {
                Console.WriteLine($"  ❌ AABB Buffer too small! Allocating...");
                Gl.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    requiredSize,
                    IntPtr.Zero,
                    BufferUsage.DynamicDraw
                );
            }

            unsafe
            {
                fixed (AABB* ptr = aabbs)
                {
                    uint sizeInBytes = totalInstances * (uint)aabbSize;
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        sizeInBytes,
                        (IntPtr)ptr
                    );

                    int error = (int)Gl.GetError();
                    if (error != 0)
                    {
                        Console.WriteLine($"  ❌ OpenGL Error: 0x{error:X}");
                    }
                    else
                    {
                        Console.WriteLine($"\t[OK] AABB SSBO uploaded: {sizeInBytes / 1024.0:F1} KB");
                    }
                }
            }

            // ------------------------------
            // Batch ID 업로드
            // ------------------------------
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _batchIDSSBO);

            Gl.GetBufferParameter(BufferTarget.ShaderStorageBuffer, GL_BUFFER_SIZE, out bufferSize);
            requiredSize = totalInstances * (uint)batchIDSize;

            Console.WriteLine($"BatchID SSBO buffer size: {bufferSize} bytes");

            if (bufferSize < requiredSize)
            {
                Console.WriteLine($"  ❌ BatchID Buffer too small! Allocating...");
                Gl.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    requiredSize,
                    IntPtr.Zero,
                    BufferUsage.DynamicDraw
                );
            }

            unsafe
            {
                fixed (uint* ptr = batchIDs)
                {
                    uint sizeInBytes = totalInstances * (uint)batchIDSize;
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        sizeInBytes,
                        (IntPtr)ptr
                    );

                    int error = (int)Gl.GetError();
                    if (error != 0)
                    {
                        Console.WriteLine($"  ❌ OpenGL Error: 0x{error:X}");
                    }
                    else
                    {
                        Console.WriteLine($"\t[OK] BatchID SSBO uploaded: {sizeInBytes / 1024.0:F1} KB");
                    }
                }
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            Console.WriteLine($"[GPU Upload] Complete! Total: {(totalInstances * (instanceDataSize + aabbSize + batchIDSize)) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine("\r\n");
        }

        #endregion


        // ------------------------------------------------------------
        // 프레임 업데이트
        // ------------------------------------------------------------

        #region 업데이트 관련

        /// <summary>
        /// 매 프레임 컬링 및 LOD 업데이트
        /// 1. Frustum Culling (Compute Shader)
        /// 2. Hi-Z Occlusion Culling & LOD Selection (Compute Shader)
        /// 3. Indirect Command Update (Compute Shader)
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="viewFrustum">뷰 프러스텀</param>
        /// <param name="hizBuffer">Hi-Z 버퍼</param>
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchyZBuffer hizBuffer)
        {
            if (!_isInitialized)
            {
                throw new Exception("해당 클래스를 상속하여 사용하기 위해서는 반드시 자식클래스에서 먼저 초기화를 하세요.");
            }

            if (viewFrustum == null) return;

            // 1. 프러스텀 컬링
            PerformFrustumCulling(camera, viewFrustum);

            // 2. Hi-Z 오클루전 컬링 및 LOD 선택
            PerformHiZCulling(camera, hizBuffer);

            // 3. Indirect 커맨드 버퍼 업데이트
            UpdateIndirectCommandsGPU();

            _frameCount++;

            // 60프레임마다 디버그 출력
            /*
            if (_frameCount % 60 == 0)
            {
                Console.WriteLine($"\n========== Frame {_frameCount} ==========");
                DebugPrintVisibility(10);

                // 더 자세한 통계는 300프레임마다
                if (_frameCount % 300 == 0)
                {
                    DebugVisibilityStatistics();
                }
            }
            */
        }

        /// <summary>
        /// 1단계: 프러스텀 컬링 Compute Shader 실행
        /// </summary>
        private void PerformFrustumCulling(Camera camera, Polyhedron viewFrustum)
        {
            if (_cullingCompute == null) return;

            // 카운터 초기화
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            // 메모리 배리어 추가
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _cullingCompute.Bind();

            // SSBO 바인딩 (고정 바인딩 포인트 사용)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            _cullingCompute.LoadFrustumPlanes(viewFrustum.Planes);

            uint totalInstances = _batchManager.TotalInstances;  // 9,073
            _cullingCompute.LoadMaxInstanceCount((int)totalInstances);

            int numWorkGroups = (int)((totalInstances + 255) / 256);
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
            _cullingCompute.Unbind();
        }

        /// <summary>
        /// 배치별 LOD 카운터 초기화 (최적화 버전)
        /// </summary>
        private void InitializeBatchLODCount(uint buffer)
        {
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);

            // 실제 사용하는 배치 개수만 초기화
            uint actualSize = _batchedModelCount * sizeof(uint);  // 4 * 4 = 16 바이트

            fixed (uint* ptr = _zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    actualSize, (IntPtr)ptr);
            }
        }

        /// <summary>
        /// 2단계: Hi-Z Occlusion Culling 및 LOD 선택 Compute Shader 실행
        /// </summary>
        private void PerformHiZCulling(Camera camera, HierarchyZBuffer hizBuffer)
        {
            // 배치별 LOD 카운터 초기화
            InitializeBatchLODCount(_visibleCountsSSBO_LOD0);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD1);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD2);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD3);

            // 메모리 배리어 추가!
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _hiZOcclusionCompute.Bind();

            // SSBO 바인딩 (고정 바인딩 포인트 사용)
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
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 14, _batchInfoSSBO);    // 13: Indirect Command Buffer
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 15, _visibilitySSBO);

            // Uniform 전달
            _hiZOcclusionCompute.LoadHiZTextures(hizBuffer.HiZTexture);
            _hiZOcclusionCompute.LoadMaxMipLevel(hizBuffer.Levels - 1);
            _hiZOcclusionCompute.LoadCameraPosition(camera.Position);
            _hiZOcclusionCompute.LoadScreenSize(hizBuffer.Width, hizBuffer.Height);
            _hiZOcclusionCompute.LoadActualBatchCount((int)_batchManager.ActualBatchCount);

            // LOD 거리 임계값 전달
            _hiZOcclusionCompute.LoadDistanceThresholds(_distance0, _distance1, _distance2);

            uint totalInstances = _batchManager.TotalInstances;
            _hiZOcclusionCompute.LoadMaxInstanceCount((int)totalInstances);
            _hiZOcclusionCompute.LoadCameraNearFar(camera.NEAR, camera.FAR);

            // 프러스텀 통과 개수 읽기
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, _frustumCount);
            int numWorkGroups = ((int)_frustumCount[0] + 63) / 64;

            // 디스패치
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _hiZOcclusionCompute.Unbind();
        }


        protected void BufferSubDataDrawArraysIndirectCommand(uint vertexCount, int commandOffset)
        {
            DrawArraysIndirectCommand cmdLOD0 = new DrawArraysIndirectCommand
            {
                VertexCount = vertexCount,
                InstanceCount = 0,
                First = 0,
                BaseInstance = 0
            };
            Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                           (IntPtr)commandOffset, COMMAND_SIZE, cmdLOD0);
            commandOffset += COMMAND_SIZE;
        }

        /// <summary>
        /// 3단계: DrawIndirect 커맨드의 InstanceCount 필드 업데이트
        /// </summary>
        private void UpdateIndirectCommandsGPU()
        {
            if (_updateCommandsCompute == null) return;

            _updateCommandsCompute.Bind();

            // SSBO 바인딩 (고정 바인딩 포인트 사용)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 13, (uint)_indirectCommandBuffer);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _visibleCountsSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _visibleCountsSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD3);

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                _startIndices[b] = (uint)_batchCommandStartIndices[b];
            }

            _updateCommandsCompute.LoadNumBatches(_batchManager.ActualBatchCount);
            _updateCommandsCompute.LoadBatchCommandStartIndices(_startIndices);

            Gl.DispatchCompute(_batchManager.ActualBatchCount, 1, 1);

            Gl.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit |
                             MemoryBarrierMask.ShaderStorageBarrierBit);

            _updateCommandsCompute.Unbind();
        }

        #endregion

        // ------------------------------------------------------------
        // 렌더링
        // ------------------------------------------------------------

        #region 렌더링 관련

        /// <summary>
        /// 모든 배치 렌더링
        /// DrawIndirect를 사용하여 GPU에서 결정된 개수만큼 인스턴싱
        /// </summary>
        /// <param name="camera">카메라</param>
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
        /// 단일 배치 렌더링 (4단계 LOD)
        /// LOD0: 풀 메시
        /// LOD1: 인덱스 메시 (단순화)
        /// LOD2: 크로스 빌보드
        /// LOD3: 임포스터
        /// </summary>
        /// <param name="batchID">배치 ID</param>
        /// <param name="camera">카메라</param>
        private void RenderBatch(uint batchID, Camera camera)
        {
            _batch = _batchManager.GetBatch(batchID);
            string batchName = _batch.ModelName;
            int cmdStartIndex = _batchCommandStartIndices[batchID];

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, (uint)_indirectCommandBuffer);

            _batch = _batchManager.GetBatch(batchID);

            // LOD0: 풀 메시 렌더링
            RenderBatchLod0(batchID, _batch, batchName, cmdStartIndex, camera);

            // LOD1: 인덱스 메시 렌더링 (단순화된 메시)
            RenderBatchLod1(batchID, _batch, batchName, cmdStartIndex, camera);

            // LOD2: 크로스 빌보드 렌더링
            RenderBatchLod2(batchID, _batch, batchName, cmdStartIndex, camera);

            // LOD3: 임포스터 렌더링
            RenderBatchLod3(batchID, _batch, batchName, cmdStartIndex, camera);
        }


        /// <summary>
        /// 이전 프레임의 LOD0, LOD1 깊이만 렌더링
        /// </summary>
        public void RenderDepthPrePassFromPrevFrame(Camera camera)
        {
            Gl.Disable(EnableCap.CullFace);

            _depthShader.Bind();
            {
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, (uint)_indirectCommandBuffer);

                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    BatchDescriptor batch = _batchManager.GetBatch(b);
                    int cmdStartIndex = _batchCommandStartIndices[b];

                    _depthShader.LoadTextureArray(batch.Model.TextureIDArray);
                    _depthShader.LoadBatchStartOffset(batch.StartIndex);

                    // 고정 바인딩 포인트 사용
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);

                    // LOD0 렌더링
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);
                    Gl.BindVertexArray(batch.VAO);
                    Gl.DrawArraysIndirect(PrimitiveType.Triangles, (IntPtr)(cmdStartIndex + LOD_OFFSETS[0]));

                    // LOD1 렌더링
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);
                    Gl.BindVertexArray(batch.Model_LOD1.VaoID);
                    Gl.DrawArraysIndirect(PrimitiveType.Triangles, (IntPtr)(cmdStartIndex + LOD_OFFSETS[1]));
                }
            }
            _depthShader.Unbind();
        }

        /// <summary>
        /// DrawArraysIndirect 호출
        /// </summary>
        protected void DrawArraysIndirect(
            uint vao,
            int cmdStartIndex,
            uint lodIndex,
            uint ssboIndex,
            PrimitiveType primitiveType = PrimitiveType.Points)
        {
            Gl.BindVertexArray(vao);
            int byteOffset = cmdStartIndex + LOD_OFFSETS[lodIndex];
            Gl.DrawArraysIndirect(primitiveType, (IntPtr)byteOffset);
        }

        #endregion


        // ------------------------------------------------------------
        // 리소스 정리
        // ------------------------------------------------------------

        #region 리소스 정리 관련

        /// <summary>
        /// GPU 리소스 해제
        /// </summary>
        public void Dispose()
        {
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
            Gl.DeleteBuffers((uint)_indirectCommandBuffer);
            Gl.DeleteBuffers(_batchInfoSSBO);
            Gl.DeleteBuffers(_visibilitySSBO);
        }

        #endregion


        // ------------------------------------------------------------
        // 디버그 정보 조회
        // ------------------------------------------------------------

        #region 디버그 정보 관련

        // 멤버 변수에 추가
        const int NUM_INDICES = 30;
        private uint[] _visibleIndicesSample_LOD0 = new uint[NUM_INDICES];  // LOD0 샘플 인덱스
        private uint[] _visibleIndicesSample_LOD1 = new uint[NUM_INDICES];  // LOD1 샘플 인덱스
        private uint[] _visibleIndicesSample_LOD2 = new uint[NUM_INDICES];  // LOD2 샘플 인덱스
        private uint[] _visibleIndicesSample_LOD3 = new uint[NUM_INDICES];  // LOD3 샘플 인덱스


        /// <summary>
        /// 디버그용 가시 객체 개수 및 샘플 인덱스 조회
        /// 성능을 위해 FRAME_COUNT_DEBUG 프레임마다만 GPU에서 읽음
        /// </summary>
        public void GetVisibleCountDebug(
            ref uint visibleCount,
            ref uint visibleCountLod0,
            ref uint visibleCountLod1,
            ref uint visibleCountLod2,
            ref uint visibleCountLod3,
            ref uint frustumPassCount,
            ref string report, bool isHookIndices = false)
        {
            if (_frameCount % FRAME_COUNT_DEBUG == 0)
            {
                report = "";

                // 프러스텀 통과 개수
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, _frustumPassed);
                _lastFrustumPassed = _frustumPassed[0];

                report += $"프러스텀: {_lastFrustumPassed}개\n";
                report += "\n";

                // 배치별 LOD 개수 읽기
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
                fixed (uint* ptr = _countsLOD0)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES_COUNT * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
                fixed (uint* ptr = _countsLOD1)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES_COUNT * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD2);
                fixed (uint* ptr = _countsLOD2)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES_COUNT * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD3);
                fixed (uint* ptr = _countsLOD3)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES_COUNT * 4), (IntPtr)ptr);
                }

                // 캐시 저장 및 합산
                uint totalVisible = 0;
                uint lod0 = 0;
                uint lod1 = 0;
                uint lod2 = 0;
                uint lod3 = 0;

                for (uint b = 0; b < _batchedModelCount; b++)
                {
                    _lastVisibleCount_LOD0[b] = _countsLOD0[b];
                    _lastVisibleCount_LOD1[b] = _countsLOD1[b];
                    _lastVisibleCount_LOD2[b] = _countsLOD2[b];
                    _lastVisibleCount_LOD3[b] = _countsLOD3[b];
                    totalVisible += _countsLOD0[b] + _countsLOD1[b] + _countsLOD2[b] + _countsLOD3[b];

                    report += $"[ID={b}]{_countsLOD0[b]}/{_countsLOD1[b]}/{_countsLOD2[b]}/{_countsLOD3[b]} \n";

                    if (b == 0) lod0 += _countsLOD0[b];
                    if (b == 1) lod1 += _countsLOD1[b];
                    if (b == 2) lod2 += _countsLOD2[b];
                    if (b == 3) lod3 += _countsLOD3[b];

                    // 샘플 인덱스 읽기
                    if (isHookIndices)
                    {
                        _batchStartIndices = _batchManager.GetBatchStarts();
                        uint offsetInBytes = _batchStartIndices[b] * 4;

                        // 각 LOD별 실제 개수만큼만 읽기
                        uint readCountLOD0 = Math.Min(_countsLOD0[b], NUM_INDICES);
                        uint readCountLOD1 = Math.Min(_countsLOD1[b], NUM_INDICES);
                        uint readCountLOD2 = Math.Min(_countsLOD2[b], NUM_INDICES);
                        uint readCountLOD3 = Math.Min(_countsLOD3[b], NUM_INDICES);

                        if (readCountLOD0 > 0)
                        {
                            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD0);
                            fixed (uint* ptr = _visibleIndicesSample_LOD0)
                            {
                                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offsetInBytes, 4 * readCountLOD0, (IntPtr)ptr);
                            }
                        }

                        if (readCountLOD1 > 0)
                        {
                            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD1);
                            fixed (uint* ptr = _visibleIndicesSample_LOD1)
                            {
                                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offsetInBytes, 4 * readCountLOD1, (IntPtr)ptr);
                            }
                        }

                        if (readCountLOD2 > 0)
                        {
                            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD2);
                            fixed (uint* ptr = _visibleIndicesSample_LOD2)
                            {
                                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offsetInBytes, 4 * readCountLOD2, (IntPtr)ptr);
                            }
                        }

                        if (readCountLOD3 > 0)
                        {
                            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD3);
                            fixed (uint* ptr = _visibleIndicesSample_LOD3)
                            {
                                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offsetInBytes, 4 * readCountLOD3, (IntPtr)ptr);
                            }
                        }

                        // 샘플 인덱스 리포트 추가
                        report += "[LOD0]";
                        for (int i = 0; i < readCountLOD0; i++) report += $"{_visibleIndicesSample_LOD0[i]} ";
                        report += "\n[LOD1]";
                        for (int i = 0; i < readCountLOD1; i++) report += $"{_visibleIndicesSample_LOD1[i]} ";
                        report += "\n[LOD2]";
                        for (int i = 0; i < readCountLOD2; i++) report += $"{_visibleIndicesSample_LOD2[i]} ";
                        report += "\n[LOD3]";
                        for (int i = 0; i < readCountLOD3; i++) report += $"{_visibleIndicesSample_LOD3[i]} ";
                        report += "\n";
                    }
                }

                report += "\n";
                report += $"총 가시 객체: {totalVisible}개\n";

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

        /// <summary>
        /// Visibility 값 디버그 출력
        /// </summary>
        /// <param name="count">출력할 인스턴스 개수</param>
        public void DebugPrintVisibility(int count = 20)
        {
            if (_visibilitySSBO == 0)
            {
                Console.WriteLine("[Visibility Debug] Buffer not initialized!");
                return;
            }

            float[] visData = new float[count];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibilitySSBO);

            unsafe
            {
                fixed (float* ptr = visData)
                {
                    Gl.GetBufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)(count * sizeof(float)),
                        (IntPtr)ptr
                    );
                }
            }

            Console.WriteLine("========== Visibility Debug ==========");
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine($"Instance[{i}]: visibility = {visData[i]:F3}");
            }
            Console.WriteLine("======================================");
        }

        /// <summary>
        /// Visibility 통계 출력
        /// </summary>
        public void DebugVisibilityStatistics()
        {
            if (_visibilitySSBO == 0) return;

            float[] visData = new float[MAX_INSTANCES];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibilitySSBO);

            unsafe
            {
                fixed (float* ptr = visData)
                {
                    Gl.GetBufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)(MAX_INSTANCES * sizeof(float)),
                        (IntPtr)ptr
                    );
                }
            }

            // 통계 계산
            int countZero = 0;
            int countLow = 0;    // 0.0 < v <= 0.3
            int countMid = 0;    // 0.3 < v <= 0.7
            int countHigh = 0;   // 0.7 < v < 1.0
            int countFull = 0;   // v == 1.0

            for (int i = 0; i < _batchManager.TotalInstances; i++)
            {
                float v = visData[i];

                if (v == 0.0f) countZero++;
                else if (v <= 0.3f) countLow++;
                else if (v <= 0.7f) countMid++;
                else if (v < 1.0f) countHigh++;
                else if (v == 1.0f) countFull++;
            }

            Console.WriteLine("========== Visibility Statistics ==========");
            Console.WriteLine($"Total Instances: {_batchManager.TotalInstances}");
            Console.WriteLine($"  Zero (0.0):        {countZero,6} ({100.0f * countZero / _batchManager.TotalInstances:F1}%)");
            Console.WriteLine($"  Low (0.0~0.3):     {countLow,6} ({100.0f * countLow / _batchManager.TotalInstances:F1}%)");
            Console.WriteLine($"  Mid (0.3~0.7):     {countMid,6} ({100.0f * countMid / _batchManager.TotalInstances:F1}%)");
            Console.WriteLine($"  High (0.7~1.0):    {countHigh,6} ({100.0f * countHigh / _batchManager.TotalInstances:F1}%)");
            Console.WriteLine($"  Full (1.0):        {countFull,6} ({100.0f * countFull / _batchManager.TotalInstances:F1}%)");
            Console.WriteLine($"  Rendering (>0.05): {countLow + countMid + countHigh + countFull,6}");
            Console.WriteLine("===========================================");
        }

        #endregion

    }

    /// <summary>
    /// GPU에 전달할 배치 메타데이터
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchInfoGPU
    {
        public float LODDistance;    // 4 bytes
        public uint StartIndex;      // 4 bytes
        public uint Count;           // 4 bytes
        public uint Padding;         // 4 bytes (16바이트 정렬)

        // Total: 16 bytes per batch
    }
}
