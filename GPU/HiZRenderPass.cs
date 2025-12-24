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

namespace GPUDriven
{
    /// <summary>
    /// GPU 드리븐 렌더링 파이프라인
    /// - Frustum Culling (Compute Shader)
    /// - Hi-Z Occlusion Culling (Compute Shader)
    /// - LOD 선택 (4단계)
    /// - Multi-Draw Indirect
    /// </summary>
    public unsafe class HiZRenderPass
    {
        // ------------------------------------------------------------
        // 상수
        // ------------------------------------------------------------

        private const int MAX_INSTANCES = 100000;  // 최대 인스턴스 개수
        private const int MAX_BATCHES = 64;        // 최대 배치 개수
        private const int COMMAND_SIZE = 16;       // DrawArraysIndirectCommand 크기 (4 uint * 4 bytes)
        private const int COMMAND_SIZE_ELEMENTS = 20; // DrawElementsIndirectCommand 크기
        private const int BYTES_PER_BATCH = 68;    // 배치당 커맨드 버퍼 크기 (16 + 20 + 16 + 16)
        private const int FRAME_COUNT_DEBUG = 60;  // 디버그 정보 갱신 주기

        // LOD별 커맨드 오프셋 (바이트)
        private static readonly int[] LOD_OFFSETS = new int[] { 0, 16, 36, 52 };

        // ------------------------------------------------------------
        // SSBO 바인딩 포인트 문서
        // ------------------------------------------------------------
        /* 
         * 0  - Transform Buffer (Mat4x4)
         * 1  - Frustum Passed Indices
         * 2  - Frustum Counter
         * 3  - AABB Buffer
         * 4  - Visible Indices LOD0
         * 5  - Visible Counts LOD0
         * 6  - Visible Indices LOD1
         * 7  - Visible Counts LOD1
         * 8  - Visible Indices LOD2
         * 9  - Batch ID Buffer / Visible Counts LOD2
         * 10 - Visible Indices LOD3 / Visible Counts LOD3
         * 11 - Indirect Command Buffer
         * 12 - Visible Counts LOD1 (Command용)
         * 13 - Visible Counts LOD2 (Command용)
         * 14 - Visible Counts LOD3 (Command용)
         */

        // ------------------------------------------------------------
        // 멤버 변수
        // ------------------------------------------------------------

        private readonly string _projPath; // 프로젝트 경로
        private ModelBatchManager _batchManager; // 배치 매니저

        // GPU 버퍼 (SSBO)
        private uint _transformSSBO;           // 모든 인스턴스의 Transform 행렬
        private uint _aabbSSBO;                // 모든 인스턴스의 AABB
        private uint _batchIDSSBO;             // 각 인스턴스가 속한 배치 ID
        private uint _frustumPassedSSBO;       // 프러스텀 통과한 인스턴스 인덱스
        private uint _frustumCounterSSBO;      // 프러스텀 통과 개수 (Atomic Counter)
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

        // 셰이더
        private FrustumCullingComputeShader _cullingCompute;        // 프러스텀 컬링 컴퓨트 셰이더
        private HiZOcclusionComputeShader _hiZOcclusionCompute;     // Hi-Z 오클루전 컬링 컴퓨트 셰이더
        private UpdateIndirectCommandsComputeShader _updateCommandsCompute; // Indirect 커맨드 업데이트 컴퓨트 셰이더
        private GPUInstancedShader _instancedShader;                // 메시 렌더링 셰이더
        private ImpostorInstancedShader _impostorInstancedShader;   // 임포스터 렌더링 셰이더
        private UnlitShader _unlitShader;                           // 임포스터 생성용 셰이더
        private GPUDrivenImpostorShader _gpuDrivenImpostorShader;   // GPU 드리븐 임포스터 렌더링 셰이더
        private CrossBillboardInstanceShader _crossBillboardInstanceShader; // 크로스 빌보드 렌더링 셰이더

        // 임포스터 관련
        private ImpostorAssets _impostor;   // 임포스터 에셋 관리자
        public BaseModel3d _point;          // 단일 포인트 모델 (인스턴싱용)

        // 크로스 빌보드 관련
        private CrossBillboardAtlasGenerator _generator;  // 크로스 빌보드 아틀라스 생성기
        private CrossBillboardData[] _billboardData;      // 배치별 크로스 빌보드 데이터

        // DrawIndirect 관련
        private Dictionary<uint, int> _batchCommandStartIndices; // 배치별 커맨드 시작 인덱스 (바이트 오프셋)
        private int _totalDrawCommands;                          // 전체 DrawIndirect 커맨드 개수

        // 작업 버퍼
        private uint[] _startIndices;  // Compute Shader에 전달할 시작 인덱스
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

        // 렌더링 임시 변수
        private BatchDescriptor _batch;               // 현재 렌더링 중인 배치
        private ImpostorRenderData _renderData;       // 임포스터 렌더링 데이터
        private UnifiedTexturedModel _unifiedTexturedModel; // 통합 텍스처 모델

        // 디버그 정보 캐시
        private uint[] _lastVisibleCount_LOD0;  // 마지막 LOD0 가시 개수
        private uint[] _lastVisibleCount_LOD1;  // 마지막 LOD1 가시 개수
        private uint[] _lastVisibleCount_LOD2;  // 마지막 LOD2 가시 개수
        private uint[] _lastVisibleCount_LOD3;  // 마지막 LOD3 가시 개수
        private uint _lastFrustumPassed = 0;    // 마지막 프러스텀 통과 개수
        private int _frameCount = 0;            // 프레임 카운터

        // ------------------------------------------------------------
        // 생성자
        // ------------------------------------------------------------

        /// <summary>
        /// HiZRenderPass 생성자
        /// </summary>
        /// <param name="projPath">프로젝트 경로</param>
        public HiZRenderPass(string projPath)
        {
            _projPath = projPath;
            _zeros = new uint[MAX_BATCHES];
            _countsLOD0 = new uint[MAX_BATCHES];
            _countsLOD1 = new uint[MAX_BATCHES];
            _countsLOD2 = new uint[MAX_BATCHES];
            _countsLOD3 = new uint[MAX_BATCHES];
            _frustumPassed = new uint[1];

            _point = Loader3d.LoadPoint(0, 0, 0);
        }

        // ------------------------------------------------------------
        // 초기화
        // ------------------------------------------------------------

        /// <summary>
        /// 렌더러 초기화
        /// </summary>
        /// <param name="batchManager">Finalize된 배치 매니저</param>
        /// <param name="camera">카메라 (임포스터 생성용)</param>
        /// <param name="maxMipLevels">HiZ 최대 밉맵 레벨 (0이면 비활성화)</param>
        public void Initialize(ModelBatchManager batchManager, Camera camera, int maxMipLevels = 0)
        {
            if (!batchManager.IsFinalized)
            {
                throw new InvalidOperationException(
                    "BatchManager must be finalized before initializing renderer");
            }

            if (maxMipLevels > 10)
            {
                throw new ArgumentException("Max mip levels exceed limit (10)");
            }

            _batchManager = batchManager;

            // 리소스 초기화
            _lastVisibleCount_LOD0 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD1 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD2 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD3 = new uint[MAX_BATCHES];

            _startIndices = new uint[_batchManager.ActualBatchCount];
            _frustumCount = new uint[1];

            // GPU 리소스 생성
            CreateSSBOs();

            // 셰이더 로드
            LoadShaders(_projPath, maxMipLevels);

            // 인스턴스 데이터 업로드
            UploadToGPU();

            // 임포스터 아틀라스 생성
            InitializeImpostors(camera);

            // 크로스 빌보드 아틀라스 생성
            _generator = new CrossBillboardAtlasGenerator();
            _billboardData = new CrossBillboardData[_batchManager.ActualBatchCount];
            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);
                _billboardData[i] = _generator.GenerateAtlas(_unlitShader, batch.Model);
            }
        }

        /// <summary>
        /// 모든 셰이더 로드
        /// </summary>
        /// <param name="projPath">프로젝트 경로</param>
        /// <param name="maxMipLevels">Hi-Z 최대 밉맵 레벨</param>
        private void LoadShaders(string projPath, int maxMipLevels)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _hiZOcclusionCompute = new HiZOcclusionComputeShader(projPath, maxMipLevels);
            _instancedShader = new GPUInstancedShader(projPath);
            _impostorInstancedShader = new ImpostorInstancedShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _updateCommandsCompute = new UpdateIndirectCommandsComputeShader(projPath);
            _gpuDrivenImpostorShader = new GPUDrivenImpostorShader(projPath);
            _crossBillboardInstanceShader = new CrossBillboardInstanceShader(projPath);
        }

        /// <summary>
        /// 배치별 임포스터 생성
        /// </summary>
        /// <param name="camera">카메라</param>
        private void InitializeImpostors(Camera camera)
        {
            _impostor = new ImpostorAssets(_unlitShader, camera);

            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);
                _impostor.CreateImpostorModel(
                    ImpostorSettings.CreateSettings(batch.ModelName, 256, 16, 8),
                    batch.Model);
            }
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
            // DrawIndirect 커맨드 인덱스 계산
            CalculateCommandIndices();

            // 인스턴스 데이터 버퍼
            CreateSSBOBuffer(ref _transformSSBO, 0, MAX_INSTANCES * 64, BufferUsage.StaticDraw);  // Transform (Mat4x4)
            CreateSSBOBuffer(ref _aabbSSBO, 3, MAX_INSTANCES * 32, BufferUsage.StaticDraw);       // AABB (Min + Max = 8 floats)
            CreateSSBOBuffer(ref _batchIDSSBO, 9, MAX_INSTANCES * 4, BufferUsage.StaticDraw);     // Batch ID (uint)

            // 프러스텀 컬링 중간 버퍼
            CreateSSBOBuffer(ref _frustumPassedSSBO, 1, MAX_INSTANCES * 4, BufferUsage.DynamicDraw);  // 프러스텀 통과 인스턴스 인덱스
            CreateSSBOBuffer(ref _frustumCounterSSBO, 2, 4, BufferUsage.DynamicDraw);                 // 프러스텀 통과 개수 (Atomic Counter)

            // LOD별 가시 인스턴스 버퍼
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD0, 4, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD0, 5, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD1, 6, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD1, 7, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD2, 8, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD2, 9, (uint)(MAX_BATCHES * 4));
            CreateSSBOBuffer(ref _visibleIndicesSSBO_LOD3, 10, (uint)(MAX_INSTANCES * 4));
            CreateSSBOBuffer(ref _visibleCountsSSBO_LOD3, 11, (uint)(MAX_BATCHES * 4));

            // DrawIndirect 커맨드 버퍼 생성
            CreateUnifiedIndirectBuffer();
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
        private void CreateUnifiedIndirectBuffer()
        {
            int bufferSize = (int)(_batchManager.ActualBatchCount * BYTES_PER_BATCH);

            _indirectCommandBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, (uint)bufferSize,
                          IntPtr.Zero, BufferUsage.DynamicDraw);

            int commandOffset = 0;
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                // LOD0 커맨드 (DrawArraysIndirect)
                DrawArraysIndirectCommand cmdLOD0 = new DrawArraysIndirectCommand
                {
                    VertexCount = batch.VertexCount,
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };
                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)commandOffset, COMMAND_SIZE, cmdLOD0);
                commandOffset += COMMAND_SIZE;

                // LOD1 커맨드 (DrawElementsIndirect)
                DrawElementsIndirectCommand cmdLod1 = new DrawElementsIndirectCommand
                {
                    IndexCount = (uint)batch.Model.IndexCount,
                    InstanceCount = 0,
                    FirstIndex = 0,
                    BaseVertex = 0,
                    BaseInstance = 0
                };
                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)commandOffset, COMMAND_SIZE_ELEMENTS, cmdLod1);
                commandOffset += COMMAND_SIZE_ELEMENTS;

                // LOD2 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                DrawArraysIndirectCommand cmdLOD2 = new DrawArraysIndirectCommand
                {
                    VertexCount = 1,
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };
                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)commandOffset, COMMAND_SIZE, cmdLOD2);
                commandOffset += COMMAND_SIZE;

                // LOD3 커맨드 (DrawArraysIndirect - 포인트 인스턴싱)
                DrawArraysIndirectCommand cmdLOD3 = new DrawArraysIndirectCommand
                {
                    VertexCount = 1,
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };
                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)commandOffset, COMMAND_SIZE, cmdLOD3);
                commandOffset += COMMAND_SIZE;
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
        }

        // ------------------------------------------------------------
        // 프레임 업데이트
        // ------------------------------------------------------------

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
            PerformFrustumCulling(camera, viewFrustum);
            PerformHiZCulling(camera, hizBuffer);
            UpdateIndirectCommandsGPU();

            _frameCount++;
        }

        /// <summary>
        /// 1단계: 프러스텀 컬링 Compute Shader 실행
        /// 입력: 모든 인스턴스 AABB
        /// 출력: 프러스텀 통과 인스턴스 인덱스 배열
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="viewFrustum">뷰 프러스텀</param>
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

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _cullingCompute.Unbind();
        }

        /// <summary>
        /// 배치별 LOD 카운터 초기화
        /// </summary>
        /// <param name="buffer">초기화할 버퍼 ID</param>
        private void InitializeBatchLODCount(uint buffer)
        {
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
            fixed (uint* ptr = _zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }
        }

        /// <summary>
        /// 2단계: Hi-Z Occlusion Culling 및 LOD 선택 Compute Shader 실행
        /// 입력: 프러스텀 통과 인스턴스, 카메라 위치, Hi-Z 버퍼
        /// 출력: LOD0/LOD1/LOD2/LOD3별 가시 인스턴스 인덱스 및 배치별 개수
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="hizBuffer">Hi-Z 버퍼</param>
        private void PerformHiZCulling(Camera camera, HierarchyZBuffer hizBuffer)
        {
            // 배치별 LOD 카운터 초기화
            InitializeBatchLODCount(_visibleCountsSSBO_LOD0);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD1);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD2);
            InitializeBatchLODCount(_visibleCountsSSBO_LOD3);

            _hiZOcclusionCompute.Bind();

            // SSBO 바인딩
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

            // Uniform 전달
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

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _indirectCommandBuffer);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 11, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 13, _visibleCountsSSBO_LOD2);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 14, _visibleCountsSSBO_LOD3);

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

        // ------------------------------------------------------------
        // 렌더링
        // ------------------------------------------------------------

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
            _renderData = _impostor.GetImpostorRenderData(batchName);
            _unifiedTexturedModel = _impostor.UnifiedTexturedModel(batchName);
            CrossBillboardData crossBillboardData = _billboardData[batchID];

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // LOD0: 풀 메시 렌더링
            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(_batch.StartIndex);
            _instancedShader.LoadViewMatrix(camera.ViewMatrix);
            _instancedShader.LoadTextureArray(_batch.Model.TextureIDArray);
            DrawArraysIndirect(_batch.VAO, cmdStartIndex, 0, _visibleIndicesSSBO_LOD0, PrimitiveType.Triangles);
            _instancedShader.Unbind();

            // LOD1: 인덱스 메시 렌더링 (단순화된 메시)
            Gl.Disable(EnableCap.CullFace);
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(_batch.StartIndex);
            _instancedShader.LoadViewMatrix(camera.ViewMatrix);
            _instancedShader.LoadTextureArray(_batch.Model.TextureIDArray);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);
            Gl.BindVertexArray(_batch.Model_LOD1.VaoID);
            int byteOffset = cmdStartIndex + LOD_OFFSETS[1];
            Gl.DrawElementsIndirect(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, (IntPtr)byteOffset);
            _instancedShader.Unbind();

            // LOD2: 크로스 빌보드 렌더링
            _crossBillboardInstanceShader.Bind();
            _crossBillboardInstanceShader.LoadVPMatrix(camera.VPMatrix);
            _crossBillboardInstanceShader.LoadCurrentBatchID(batchID);
            _crossBillboardInstanceShader.LoadBatchStartOffset(_batch.StartIndex);
            _crossBillboardInstanceShader.LoadAtlasTexture(crossBillboardData.AtlasTexture.TextureID);
            _crossBillboardInstanceShader.UseTexture(true);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _aabbSSBO);
            DrawArraysIndirect(_point.VAO, cmdStartIndex, 2, _visibleIndicesSSBO_LOD2);
            _crossBillboardInstanceShader.Unbind();

            // LOD3: 임포스터 렌더링
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
            DrawArraysIndirect(_point.VAO, cmdStartIndex, 3, _visibleIndicesSSBO_LOD3);
            _gpuDrivenImpostorShader.Unbind();
        }

        /// <summary>
        /// DrawArraysIndirect 호출
        /// </summary>
        /// <param name="vao">VAO ID</param>
        /// <param name="cmdStartIndex">커맨드 시작 인덱스 (바이트 오프셋)</param>
        /// <param name="lodIndex">LOD 인덱스 (0~3)</param>
        /// <param name="ssboIndex">가시 인스턴스 SSBO 인덱스</param>
        /// <param name="primitiveType">프리미티브 타입</param>
        private void DrawArraysIndirect(
            uint vao,
            int cmdStartIndex,
            uint lodIndex,
            uint ssboIndex,
            PrimitiveType primitiveType = PrimitiveType.Points)
        {
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, ssboIndex);
            Gl.BindVertexArray(vao);

            int byteOffset = cmdStartIndex + LOD_OFFSETS[lodIndex];
            Gl.DrawArraysIndirect(primitiveType, (IntPtr)byteOffset);
        }

        // ------------------------------------------------------------
        // 디버그 정보 조회
        // ------------------------------------------------------------

        /// <summary>
        /// 디버그용 가시 객체 개수 조회
        /// 성능을 위해 FRAME_COUNT_DEBUG 프레임마다만 GPU에서 읽음
        /// </summary>
        /// <param name="visibleCount">전체 가시 객체 개수</param>
        /// <param name="visibleCountLod0">LOD0 가시 객체 개수</param>
        /// <param name="visibleCountLod1">LOD1 가시 객체 개수</param>
        /// <param name="visibleCountLod2">LOD2 가시 객체 개수</param>
        /// <param name="visibleCountLod3">LOD3 가시 객체 개수</param>
        /// <param name="frustumPassCount">프러스텀 통과 개수</param>
        /// <param name="report">배치별 상세 리포트</param>
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

                // 배치별 LOD 개수 읽기
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

        // ------------------------------------------------------------
        // 리소스 정리
        // ------------------------------------------------------------

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
            Gl.DeleteBuffers(_indirectCommandBuffer);
        }
    }
}