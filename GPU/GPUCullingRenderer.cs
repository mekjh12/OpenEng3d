using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Diagnostics;
using Terrain;
using ZetaExt;
using Occlusion;
using System.Runtime.InteropServices;

namespace GPUDriven
{
    public unsafe class GPUCullingRenderer : IDisposable
    {
        private const int MAX_INSTANCES = 100000;
        private const int MAX_BATCHES = 64;
        private const int FRAME_COUNT_DEBUG = 500;

        private string _projPath;

        // ===== ModelBatchManager 통합 =====
        private ModelBatchManager _batchManager;

        // ===== SSBO들 =====
        private uint _transformSSBO;
        private uint _aabbSSBO;
        private uint _batchIDSSBO;

        // Frustum 중간 결과
        private uint _frustumPassedSSBO;
        private uint _frustumCounterSSBO;

        // ===== LOD별 통합 버퍼 (Offset 기반) =====
        // LOD0
        private uint _visibleIndicesSSBO_LOD0;      // [100,000] 통합 버퍼
        private uint _visibleCountsSSBO_LOD0;       // [64] Batch별 카운터 배열

        // LOD1
        private uint _visibleIndicesSSBO_LOD1;      // [100,000] 통합 버퍼
        private uint _visibleCountsSSBO_LOD1;       // [64] Batch별 카운터 배열

        // Indirect Buffers (Batch별)
        private uint[][] _indirectBuffers_LOD0;     // [MAX_BATCHES][meshCount]
        private uint[] _indirectBuffer_LOD1;        // [MAX_BATCHES]

        // 디버그
        private bool _isDebugPrint = true;
        private uint _debugDepthSSBO;
        private DepthDebugInstancedShader _depthDebugShader;
        private bool _debugDepthMode = true;
        private bool _enableEdgeLine = true;
        public bool DebugDepthMode { get => _debugDepthMode; set => _debugDepthMode = value; }
        public bool EnableEdgeLine { get => _enableEdgeLine; set => _enableEdgeLine = value; }

        // ===== 셰이더 =====
        private FrustumCullingComputeShader _cullingCompute;
        private HiZOcclusionComputeShader _hizCullingCompute;
        private GPUInstancedShader _instancedShader;
        private ImpostorInstancedShader _impostorInstancedShader;
        private UnlitShader _unlitShader;

        // 임포스터 관련
        private ImpostorLODSystem _impostor;
        public BaseModel3d _point = Loader3d.LoadPoint(0, 0, 0);

        // HiZ 컨트롤
        private bool _enableHiZCulling = true;

        // 성능 모니터링
        private int _frameCount = 0;
        private uint[] _lastVisibleCount_LOD0;
        private uint[] _lastVisibleCount_LOD1;
        private uint _lastFrustumPassed = 0;
        private Stopwatch _computeTimer;

        public GPUCullingRenderer(string projPath)
        {
            _projPath = projPath;
            _computeTimer = new Stopwatch();
        }

        /// <summary>
        /// ModelBatchManager로 초기화
        /// </summary>
        public void Initialize(ModelBatchManager batchManager, int maxMipLevels = 0)
        {
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

            // 모니터링 배열 초기화
            _lastVisibleCount_LOD0 = new uint[MAX_BATCHES];
            _lastVisibleCount_LOD1 = new uint[MAX_BATCHES];

            CreateSSBOs();
            LoadShaders(_projPath, maxMipLevels);
            UploadToGPU();
            InitializeImpostors();

            Console.WriteLine("=== GPU Culling Renderer Initialized ===");
            Console.WriteLine($"Max Instances: {MAX_INSTANCES}");
            Console.WriteLine($"HiZ Culling: {(_enableHiZCulling ? "Enabled" : "Disabled")}");
        }

        private void LoadShaders(string projPath, int maxMipLevels)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _hizCullingCompute = new HiZOcclusionComputeShader(projPath, maxMipLevels);
            _instancedShader = new GPUInstancedShader(projPath);
            _impostorInstancedShader = new ImpostorInstancedShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _depthDebugShader = new DepthDebugInstancedShader(projPath);
        }

        private void InitializeImpostors()
        {
            _impostor = new ImpostorLODSystem(0);

            for (uint i = 0; i < _batchManager.ActualBatchCount; i++)
            {
                var batch = _batchManager.GetBatch(i);

                _impostor.CreateImpostorModel(
                    batch.ModelName,
                    ImpostorSettings.CreateSettings(256, 16, 8),
                    _unlitShader,
                    batch.Models);

                Console.WriteLine($"Created impostor for: {batch.ModelName}");
            }
        }

        private void CreateSSBOs()
        {
            // ===== 공통 인스턴스 데이터 버퍼 =====
            _transformSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 64), IntPtr.Zero, BufferUsage.StaticDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);

            _aabbSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 32), IntPtr.Zero, BufferUsage.StaticDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _aabbSSBO);

            _batchIDSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _batchIDSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.StaticDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _batchIDSSBO);

            // ===== Frustum 중간 버퍼 =====
            _frustumPassedSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumPassedSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);

            _frustumCounterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            // ===== LOD0 통합 버퍼 (Offset 기반) =====
            _visibleIndicesSSBO_LOD0 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD0);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _visibleIndicesSSBO_LOD0);

            _visibleCountsSSBO_LOD0 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_BATCHES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _visibleCountsSSBO_LOD0);

            // ===== LOD1 통합 버퍼 (Offset 기반) =====
            _visibleIndicesSSBO_LOD1 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD1);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _visibleIndicesSSBO_LOD1);

            _visibleCountsSSBO_LOD1 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_BATCHES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _visibleCountsSSBO_LOD1);

            // 디버그 버퍼
            _debugDepthSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _debugDepthSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _debugDepthSSBO);

            // ===== Indirect Buffers (Batch별) =====
            _indirectBuffers_LOD0 = new uint[MAX_BATCHES][];
            _indirectBuffer_LOD1 = new uint[MAX_BATCHES];

            for (int b = 0; b < MAX_BATCHES; b++)
            {
                // Batch가 실제로 사용되는 경우만 생성
                if (b < _batchManager.ActualBatchCount)
                {
                    var batch = _batchManager.GetBatch((uint)b);

                    // LOD0: 메시별 Indirect Buffer
                    _indirectBuffers_LOD0[b] = new uint[batch.Models.Length];
                    for (int m = 0; m < batch.Models.Length; m++)
                    {
                        _indirectBuffers_LOD0[b][m] = Gl.GenBuffer();
                        Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers_LOD0[b][m]);
                        Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16, IntPtr.Zero, BufferUsage.DynamicDraw);
                    }

                    // LOD1: Batch별 하나
                    _indirectBuffer_LOD1[b] = Gl.GenBuffer();
                    Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1[b]);
                    Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16, IntPtr.Zero, BufferUsage.DynamicDraw);
                }
            }
        }

        private void UploadToGPU()
        {
            var transforms = _batchManager.GetTransforms();
            var aabbs = _batchManager.GetAABBs();
            var batchIDs = _batchManager.GetBatchIDs();

            // Transform 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 64), (IntPtr)ptr);
            }

            // AABB 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            fixed (AABB* ptr = aabbs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 32), (IntPtr)ptr);
            }

            // BatchID 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _batchIDSSBO);
            fixed (uint* ptr = batchIDs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 4), (IntPtr)ptr);
            }

            Console.WriteLine("Data uploaded to GPU successfully");
        }

        public void Update(Camera camera, Polyhedron viewFrustum, HierarchyZBuffer hzBuffer = null)
        {
            // ===== 1단계: Frustum Culling =====
            PerformFrustumCulling(camera, viewFrustum);

            // ===== 2단계: HiZ Occlusion + LOD =====
            if (_enableHiZCulling && hzBuffer != null)
            {
                PerformHiZCulling(camera, hzBuffer);
            }
            else
            {
                CopyFrustumResultToFinal();
            }

            // ===== 3단계: Indirect Buffer 업데이트 =====
            UpdateIndirectBuffers();

            DebugPrintGPUState(camera);

            _frameCount++;
        }

        private void PerformFrustumCulling(Camera camera, Polyhedron viewFrustum)
        {
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            _cullingCompute.Bind();

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            _cullingCompute.LoadFrustumPlanes(viewFrustum.Planes);

            int numWorkGroups = (MAX_INSTANCES + 255) / 256;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _cullingCompute.Unbind();
        }

        private void PerformHiZCulling(Camera camera, HierarchyZBuffer hzBuffer)
        {
            // Batch별 카운터 초기화
            uint[] zeros = new uint[MAX_BATCHES];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
            fixed (uint* ptr = zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
            fixed (uint* ptr = zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }

            if (_debugDepthMode)
            {
                float[] debugInit = new float[MAX_INSTANCES];
                for (int i = 0; i < MAX_INSTANCES; i++)
                    debugInit[i] = -1.0f;

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _debugDepthSSBO);
                fixed (float* ptr = debugInit)
                {
                    Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero, (uint)(MAX_INSTANCES * 4), (IntPtr)ptr);
                }
            }

            _hizCullingCompute.Bind();

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _visibleIndicesSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _visibleCountsSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _batchIDSSBO);
            if (_debugDepthMode)
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _debugDepthSSBO);

            // Uniform 설정
            _hizCullingCompute.LoadHiZTextures(hzBuffer.HiZTexture);
            _hizCullingCompute.LoadMaxMipLevel(hzBuffer.Levels - 1);
            _hizCullingCompute.LoadVPMatrix(camera.VPMatrix);
            _hizCullingCompute.LoadCameraPosition(camera.Position);
            _hizCullingCompute.LoadScreenSize(hzBuffer.Width, hzBuffer.Height);
            _hizCullingCompute.LoadMaxInstanceCount(MAX_INSTANCES);
            _hizCullingCompute.LoadCameraNearFar(camera.NEAR, camera.FAR);
            _hizCullingCompute.LoadViewMatrix(camera.ViewMatrix);
            _hizCullingCompute.LoadIsDebugMode(_debugDepthMode);

            // ===== Batch 메타데이터 전달 =====
            float[] batchLODs = _batchManager.GetBatchLODs();
            uint[] batchStarts = _batchManager.GetBatchStarts();
            uint[] batchCounts = _batchManager.GetBatchCounts();

            if (_isDebugPrint)
            {
                Console.WriteLine($"\n======================== Frustum Result =========================");
                Console.WriteLine("Batch Metadata:");
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    Console.WriteLine($"  Batch {b}: Start={batchStarts[b]}, Count={batchCounts[b]}, LOD={batchLODs[b]}");
                }
            }

            // Uniform 전달
            _hizCullingCompute.LoadBatchLODs(batchLODs);
            _hizCullingCompute.LoadBatchStarts(batchStarts);
            _hizCullingCompute.LoadBatchCounts(batchCounts);

            uint[] frustumCount = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumCount);

            int numWorkGroups = ((int)frustumCount[0] + 63) / 64;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _hizCullingCompute.Unbind();

            // PerformHiZCulling 마지막에
            if (_debugDepthMode && _frameCount == 0)
            {
                float[] debugData = new float[40];  // 10개 * 4
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _debugDepthSSBO);
                fixed (float* ptr = debugData)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 160, (IntPtr)ptr);
                }

                Console.WriteLine("\n=== Shader Debug Data (first 10 threads) ===");
                for (int i = 0; i < 10; i++)
                {
                    int instanceIdx = (int)debugData[i * 4 + 0];
                    int batchStart = (int)debugData[i * 4 + 1];
                    int relativeSlot = (int)debugData[i * 4 + 2];
                    int absoluteSlot = (int)debugData[i * 4 + 3];

                    Console.WriteLine($"Thread {i}: instanceIdx={instanceIdx}, " +
                        $"batchStart={batchStart}, relativeSlot={relativeSlot}, absoluteSlot={absoluteSlot}");
                }
            }

            // Frustum 통과 개수
            if (_isDebugPrint)
            {
                if (frustumCount[0] > 0)
                {
                    Console.WriteLine($"\nDispatching: {numWorkGroups} work groups");
                    Console.WriteLine($"Passed: {frustumCount[0]} / {MAX_INSTANCES}");

                    // ===== 몇 개 샘플 확인 =====
                    int[] sampleIndices = new int[Math.Min(10, (int)frustumCount[0])];
                    Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumPassedSSBO);
                    fixed (int* ptr = sampleIndices)
                    {
                        Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                            (uint)(sampleIndices.Length * 4), (IntPtr)ptr);
                    }

                    Console.WriteLine("\nFrustum Passed Samples (first 10):");
                    for (int i = 0; i < sampleIndices.Length; i++)
                    {
                        Console.WriteLine($"  [{i}] = {sampleIndices[i]}");
                    }

                    // ===== BatchID 확인 =====
                    var batchIDs = _batchManager.GetBatchIDs();
                    Console.WriteLine("\nBatchID for these samples:");
                    for (int i = 0; i < sampleIndices.Length; i++)
                    {
                        int idx = sampleIndices[i];
                        if (idx >= 0 && idx < batchIDs.Length)
                        {
                            Console.WriteLine($"  Instance {idx} → Batch {batchIDs[idx]}");
                        }
                    }

                    // ===== Transform 위치 확인 =====
                    var transforms = _batchManager.GetTransforms();
                    Console.WriteLine("\nWorld positions for these samples:");
                    for (int i = 0; i < Math.Min(3, sampleIndices.Length); i++)
                    {
                        int idx = sampleIndices[i];
                        if (idx >= 0 && idx < transforms.Length)
                        {
                            var pos = new Vertex3f(transforms[idx][3, 0], transforms[idx][3, 1], transforms[idx][3, 2]);
                            float dist = (pos - camera.Position).Length();
                            Console.WriteLine($"  Instance {idx}: Pos={pos}, Distance={dist:F2}");
                        }
                    }

                }

                uint[] countsLOD0 = new uint[MAX_BATCHES];
                uint[] countsLOD1 = new uint[MAX_BATCHES];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
                fixed (uint* ptr = countsLOD0)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
                fixed (uint* ptr = countsLOD1)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Console.WriteLine($"[Frame {_frameCount}] Visible Counts:");
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    Console.WriteLine($"  Batch {b}: LOD0={countsLOD0[b]}, LOD1={countsLOD1[b]}");
                }
            }
        }

        private void CopyFrustumResultToFinal()
        {
            // HiZ 비활성화 시: 모든 객체를 LOD0로 (간단 구현)
            uint[] frustumCount = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumCount);

            if (frustumCount[0] == 0) return;

            // TODO: Batch별 분류 필요 (현재는 간단히 첫 번째 Batch로)
            // 실제로는 frustumPassedIndices를 읽어서 batchID별로 분류해야 함

            uint[] zeros = new uint[MAX_BATCHES];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
            fixed (uint* ptr = zeros)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }
        }

        private void UpdateIndirectBuffers()
        {
            // 전역 카운터 읽기
            uint[] globalCountLOD0 = new uint[1];
            uint[] globalCountLOD1 = new uint[1];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, globalCountLOD0);

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, globalCountLOD1);

            if (_frameCount % 100 == 0)
            {
                Console.WriteLine($"\n[UpdateIndirectBuffers] Global Counts:");
                Console.WriteLine($"  LOD0[0] = {globalCountLOD0[0]}");
                Console.WriteLine($"  LOD1[0] = {globalCountLOD1[0]}");
            }

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                var batch = _batchManager.GetBatch(b);

                // LOD0 Indirect Buffers (모든 배치가 같은 버퍼 공유)
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers_LOD0[b][m]);

                    DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
                    {
                        VertexCount = batch.VertexCounts[m],
                        InstanceCount = globalCountLOD0[0],  // 전역 카운트
                        First = 0,
                        BaseInstance = 0
                    };

                    Gl.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmd);

                    if (_frameCount % 100 == 0 && m == 0)
                    {
                        Console.WriteLine($"  Batch {b} Mesh[0]: VertexCount={cmd.VertexCount}, InstanceCount={cmd.InstanceCount}");
                    }
                }

                // LOD1 Indirect Buffer
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1[b]);

                DrawArraysIndirectCommand cmdLOD1 = new DrawArraysIndirectCommand
                {
                    VertexCount = 6,
                    InstanceCount = globalCountLOD1[0],  // 전역 카운트
                    First = 0,
                    BaseInstance = 0
                };

                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmdLOD1);
            }
        }

        private void UpdateIndirectBuffer(uint indirectBuffer, uint counterBuffer, uint batchIndex, uint vertexCount)
        {
            // 1. 먼저 전체 구조체 초기화
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);

            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
            {
                VertexCount = vertexCount,
                InstanceCount = 0,  // 일단 0으로
                First = 0,
                BaseInstance = 0
            };

            Gl.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmd);

            // 2. InstanceCount만 GPU 카운터에서 복사
            uint sourceOffset = batchIndex * 4;  // visibleCounts[batchIndex]
            uint destOffset = 4;                 // cmd.InstanceCount 위치

            Gl.BindBuffer(BufferTarget.CopyReadBuffer, counterBuffer);
            Gl.BindBuffer(BufferTarget.CopyWriteBuffer, indirectBuffer);
            Gl.CopyBufferSubData(
                BufferTarget.CopyReadBuffer,
                BufferTarget.CopyWriteBuffer,
                (IntPtr)sourceOffset,
                (IntPtr)destOffset,
                4);
        }

        // GPUCullingRenderer 클래스에 추가

        /// <summary>
        /// GPU 상태를 읽어서 상세하게 출력하는 디버그 함수
        /// </summary>
        public void DebugPrintGPUState(Camera camera)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("GPU CULLING STATE DEBUG");
            Console.WriteLine(new string('=', 80));

            // ===== 1. Frustum Culling 결과 =====
            uint[] frustumCount = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumCount);

            Console.WriteLine($"\n[1] Frustum Culling:");
            Console.WriteLine($"  Passed: {frustumCount[0]} / {MAX_INSTANCES}");

            // ===== 2. Frustum 통과 인덱스 샘플 =====
            if (frustumCount[0] > 0)
            {
                int sampleCount = Math.Min(10, (int)frustumCount[0]);
                int[] frustumSamples = new int[sampleCount];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumPassedSSBO);
                fixed (int* ptr = frustumSamples)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(sampleCount * 4), (IntPtr)ptr);
                }

                Console.WriteLine($"\n[2] Frustum Passed Samples (first {sampleCount}):");
                var batchIDs = _batchManager.GetBatchIDs();
                var transforms = _batchManager.GetTransforms();

                for (int i = 0; i < sampleCount; i++)
                {
                    int idx = frustumSamples[i];
                    if (idx >= 0 && idx < batchIDs.Length)
                    {
                        uint batchID = batchIDs[idx];
                        var pos = new Vertex3f(
                            transforms[idx][3, 0],
                            transforms[idx][3, 1],
                            transforms[idx][3, 2]);
                        float dist = (pos - camera.Position).Length();

                        Console.WriteLine($"  [{i}] Instance {idx:D5} → Batch {batchID}, " +
                            $"Pos={pos.x:F1},{pos.y:F1},{pos.z:F1}, Dist={dist:F1}m");
                    }
                }
            }

            // ===== 3. Batch별 LOD 카운트 =====
            uint[] countsLOD0 = new uint[MAX_BATCHES];
            uint[] countsLOD1 = new uint[MAX_BATCHES];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
            fixed (uint* ptr = countsLOD0)
            {
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
            fixed (uint* ptr = countsLOD1)
            {
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
            }

            Console.WriteLine($"\n[3] Visible Counts per Batch:");
            Console.WriteLine($"  {"Batch",-8} {"Model",-20} {"LOD Dist",-10} {"LOD0",-10} {"LOD1",-10} {"Total",-10}");
            Console.WriteLine($"  {new string('-', 78)}");

            uint totalLOD0 = 0;
            uint totalLOD1 = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                var batch = _batchManager.GetBatch(b);
                totalLOD0 += countsLOD0[b];
                totalLOD1 += countsLOD1[b];

                Console.WriteLine($"  {b,-8} {batch.ModelName,-20} {batch.LODDistance,-10:F1} " +
                    $"{countsLOD0[b],-10} {countsLOD1[b],-10} {countsLOD0[b] + countsLOD1[b],-10}");
            }

            Console.WriteLine($"  {new string('-', 78)}");
            Console.WriteLine($"  {"TOTAL",-39} {totalLOD0,-10} {totalLOD1,-10} {totalLOD0 + totalLOD1,-10}");

            // ===== 4. LOD0 인덱스 샘플 =====
            if (totalLOD0 > 0)
            {
                int sampleCount = Math.Min(5, (int)totalLOD0);
                int[] lod0Samples = new int[sampleCount];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD0);
                fixed (int* ptr = lod0Samples)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(sampleCount * 4), (IntPtr)ptr);
                }

                Console.WriteLine($"\n[4] LOD0 Visible Samples (first {sampleCount}):");
                var batchIDs = _batchManager.GetBatchIDs();
                var transforms = _batchManager.GetTransforms();
                var aabbs = _batchManager.GetAABBs();

                for (int i = 0; i < sampleCount; i++)
                {
                    int idx = lod0Samples[i];
                    if (idx >= 0 && idx < batchIDs.Length)
                    {
                        uint batchID = batchIDs[idx];
                        var batch = _batchManager.GetBatch(batchID);
                        var pos = new Vertex3f(
                            transforms[idx][3, 0],
                            transforms[idx][3, 1],
                            transforms[idx][3, 2]);
                        float dist = (pos - camera.Position).Length();
                        var center = (aabbs[idx].Min + aabbs[idx].Max) * 0.5f;

                        Console.WriteLine($"  [{i}] Instance {idx:D5} → Batch {batchID} ({batch.ModelName})");
                        Console.WriteLine($"      Pos={pos.x:F1},{pos.y:F1},{pos.z:F1}, " +
                            $"Dist={dist:F1}m (LOD threshold: {batch.LODDistance:F1}m)");
                        Console.WriteLine($"      AABB Center={center.x:F1},{center.y:F1},{center.z:F1}");
                    }
                }
            }

            // ===== 5. LOD1 인덱스 샘플 =====
            if (totalLOD1 > 0)
            {
                int sampleCount = Math.Min(5, (int)totalLOD1);
                int[] lod1Samples = new int[sampleCount];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD1);
                fixed (int* ptr = lod1Samples)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(sampleCount * 4), (IntPtr)ptr);
                }

                Console.WriteLine($"\n[5] LOD1 Visible Samples (first {sampleCount}):");
                var batchIDs = _batchManager.GetBatchIDs();
                var transforms = _batchManager.GetTransforms();
                var aabbs = _batchManager.GetAABBs();

                for (int i = 0; i < sampleCount; i++)
                {
                    int idx = lod1Samples[i];
                    if (idx >= 0 && idx < batchIDs.Length)
                    {
                        uint batchID = batchIDs[idx];
                        var batch = _batchManager.GetBatch(batchID);
                        var pos = new Vertex3f(
                            transforms[idx][3, 0],
                            transforms[idx][3, 1],
                            transforms[idx][3, 2]);
                        float dist = (pos - camera.Position).Length();
                        var center = (aabbs[idx].Min + aabbs[idx].Max) * 0.5f;

                        Console.WriteLine($"  [{i}] Instance {idx:D5} → Batch {batchID} ({batch.ModelName})");
                        Console.WriteLine($"      Pos={pos.x:F1},{pos.y:F1},{pos.z:F1}, " +
                            $"Dist={dist:F1}m (LOD threshold: {batch.LODDistance:F1}m)");
                        Console.WriteLine($"      AABB Center={center.x:F1},{center.y:F1},{center.z:F1}");
                    }
                }
            }

            // ===== 6. Indirect Buffer 상태 =====
            Console.WriteLine($"\n[6] Indirect Buffer Status:");

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                var batch = _batchManager.GetBatch(b);
                Console.WriteLine($"  Batch {b} ({batch.ModelName}):");

                // LOD0 (메시별)
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand();
                    Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers_LOD0[b][m]);
                    Gl.GetBufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmd);

                    Console.WriteLine($"    LOD0 Mesh[{m}]: VertexCount={cmd.VertexCount}, " +
                        $"InstanceCount={cmd.InstanceCount}, First={cmd.First}, BaseInstance={cmd.BaseInstance}");
                }

                // LOD1
                DrawArraysIndirectCommand cmdLOD1 = new DrawArraysIndirectCommand();
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1[b]);
                Gl.GetBufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmdLOD1);

                Console.WriteLine($"    LOD1 Impostor: VertexCount={cmdLOD1.VertexCount}, " +
                    $"InstanceCount={cmdLOD1.InstanceCount}, First={cmdLOD1.First}, BaseInstance={cmdLOD1.BaseInstance}");
            }

            // ===== 7. 카메라 정보 =====
            Console.WriteLine($"\n[7] Camera State:");
            Console.WriteLine($"  Position: {camera.Position.x:F1}, {camera.Position.y:F1}, {camera.Position.z:F1}");
            Console.WriteLine($"  Near/Far: {camera.NEAR:F1} / {camera.FAR:F1}");
            Console.WriteLine($"  Direction: {camera.Forward.x:F2}, {camera.Forward.y:F2}, {camera.Forward.z:F2}");

            Console.WriteLine(new string('=', 80) + "\n");
        }

        // DrawArraysIndirectCommand 구조체도 필요하면 추가
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DrawArraysIndirectCommand
        {
            public uint VertexCount;
            public uint InstanceCount;
            public uint First;
            public uint BaseInstance;
        }

        public void Render(Camera camera)
        {
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(true);
            Gl.Enable(EnableCap.CullFace);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit |
                             MemoryBarrierMask.CommandBarrierBit);

            // 각 Batch 렌더링
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                RenderBatch(b, camera);
            }

            Gl.BindVertexArray(0);
        }

        private void RenderBatch(uint batchID, Camera camera)
        {
            BatchDescriptor batch = _batchManager.GetBatch(batchID);

            // ===== LOD0: Mesh 렌더링 =====
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(0);  // 전역 버퍼 사용

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);

            for (int m = 0; m < batch.Models.Length; m++)
            {
                if (batch.Models[m].Texture != null)
                {
                    _instancedShader.LoadTexture(TextureUnit.Texture0,
                        batch.Models[m].Texture.TextureID);
                }

                Gl.BindVertexArray(batch.VAOs[m]);
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers_LOD0[batchID][m]);
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
            }

            _instancedShader.Unbind();

            // ===== LOD1: Impostor 렌더링 =====
            _impostorInstancedShader.Bind();
            _impostorInstancedShader.LoadVPMatrix(camera.VPMatrix);
            _impostorInstancedShader.LoadCameraPosition(camera.Position);
            _impostorInstancedShader.LoadBatchStartOffset(0);  // 전역 버퍼 사용

            ImpostorSettings settings = _impostor.GetImpostorSettings(batch.ModelName);
            uint textureId = _impostor.AtlasTexture(batch.ModelName);

            _impostorInstancedShader.LoadImpostorAtlas(TextureUnit.Texture0, textureId);
            _impostorInstancedShader.LoadAtlasSize(settings.AtlasSize);
            _impostorInstancedShader.LoadIndividualSize(settings.IndividualSize);
            _impostorInstancedShader.LoadHorizontalFrames(settings.HorizontalAngles);
            _impostorInstancedShader.LoadVerticalFrames(settings.VerticalAngles);
            _impostorInstancedShader.LoadAABBSizeModel(batch.ReferenceAABB.SphereRadius);
            _impostorInstancedShader.LoadAABBCenterEntity(batch.ReferenceAABB.Center);
            _impostorInstancedShader.LoadEnableEdgeLine(_enableEdgeLine);

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

            Gl.BindVertexArray(_point.VAO);
            Gl.EnableVertexAttribArray(0);
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1[batchID]);
            Gl.DrawArraysIndirect(PrimitiveType.Points, IntPtr.Zero);
            Gl.DisableVertexAttribArray(0);

            _impostorInstancedShader.Unbind();
        }

        public void SetHiZCullingEnabled(bool enabled)
        {
            _enableHiZCulling = enabled;
            Console.WriteLine($"HiZ Occlusion Culling: {(_enableHiZCulling ? "Enabled" : "Disabled")}");
        }

        public void GetVisibleCountDebug(ref uint visibleCount, ref uint frustumPassCount)
        {
            if (_frameCount % FRAME_COUNT_DEBUG == 0)
            {
                uint[] frustumPassed = new uint[1];
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumPassed);

                _lastFrustumPassed = frustumPassed[0];

                // Batch별 가시 개수 읽기
                uint[] countsLOD0 = new uint[MAX_BATCHES];
                uint[] countsLOD1 = new uint[MAX_BATCHES];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD0);
                fixed (uint* ptr = countsLOD0)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleCountsSSBO_LOD1);
                fixed (uint* ptr = countsLOD1)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_BATCHES * 4), (IntPtr)ptr);
                }

                // 합산
                uint totalVisible = 0;
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    _lastVisibleCount_LOD0[b] = countsLOD0[b];
                    _lastVisibleCount_LOD1[b] = countsLOD1[b];
                    totalVisible += countsLOD0[b] + countsLOD1[b];
                }

                visibleCount = totalVisible;
                frustumPassCount = _lastFrustumPassed;
            }
            else
            {
                uint totalVisible = 0;
                for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
                {
                    totalVisible += _lastVisibleCount_LOD0[b] + _lastVisibleCount_LOD1[b];
                }
                visibleCount = totalVisible;
                frustumPassCount = _lastFrustumPassed;
            }
        }

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

            for (int b = 0; b < MAX_BATCHES; b++)
            {
                if (_indirectBuffers_LOD0[b] != null)
                {
                    foreach (var buf in _indirectBuffers_LOD0[b])
                    {
                        Gl.DeleteBuffers(buf);
                    }
                }
                Gl.DeleteBuffers(_indirectBuffer_LOD1[b]);
            }

            Gl.DeleteBuffers(_debugDepthSSBO);

            Console.WriteLine("GPU Culling Renderer disposed");
        }
    }
}