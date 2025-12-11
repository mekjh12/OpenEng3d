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
using System.Collections.Generic;

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

        // 디버그
        private bool _isDebugPrint = true;
        private uint _debugDepthSSBO;
        private DepthDebugInstancedShader _depthDebugShader;
        private bool _debugDepthMode = false;
        private bool _enableEdgeLine = true;
        public bool DebugDepthMode { get => _debugDepthMode; set => _debugDepthMode = value; }
        public bool EnableEdgeLine { get => _enableEdgeLine; set => _enableEdgeLine = value; }

        // ===== 셰이더 =====
        private FrustumCullingComputeShader _cullingCompute;
        private HiZOcclusionComputeShader _hizCullingCompute;
        private GPUInstancedShader _instancedShader;
        private ImpostorInstancedShader _impostorInstancedShader;
        private UnlitShader _unlitShader;
        private UpdateIndirectCommandsComputeShader _updateCommandsCompute;


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

        // ===== 클래스 필드에 추가 =====
        private Dictionary<uint, int> _batchCommandStartIndices;
        private const int COMMAND_SIZE = 16;  // DrawArraysIndirectCommand
        private int _totalDrawCommands;
        private uint _indirectCommandBuffer;  // 새로운 통합 버퍼


        // DrawArraysIndirectCommand 구조체도 필요하면 추가
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DrawArraysIndirectCommand
        {
            public uint VertexCount;
            public uint InstanceCount;
            public uint First;
            public uint BaseInstance;
        }


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
            _updateCommandsCompute = new UpdateIndirectCommandsComputeShader(projPath);
        }

        // ===== 새 함수: GPU에서 Indirect Command 업데이트 =====
        private void UpdateIndirectCommandsGPU()
        {
            _updateCommandsCompute.Bind();

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _indirectCommandBuffer);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 11, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD1);

            // Uniform 데이터 준비
            uint[] startIndices = new uint[_batchManager.ActualBatchCount];
            uint[] modelCounts = new uint[_batchManager.ActualBatchCount];

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                startIndices[b] = (uint)_batchCommandStartIndices[b];
                modelCounts[b] = (uint)_batchManager.GetBatch(b).Models.Length;
            }

            // Uniform 전달
            _updateCommandsCompute.LoadNumBatches(_batchManager.ActualBatchCount);
            _updateCommandsCompute.LoadBatchCommandStartIndices(startIndices);
            _updateCommandsCompute.LoadNumModelsPerBatch(modelCounts);

            // Dispatch (배치 개수만큼)
            Gl.DispatchCompute(_batchManager.ActualBatchCount, 1, 1);

            // Barrier
            Gl.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit |
                             MemoryBarrierMask.ShaderStorageBarrierBit);

            _updateCommandsCompute.Unbind();
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

        private void CalculateCommandIndices()
        {
            _batchCommandStartIndices = new Dictionary<uint, int>();
            _totalDrawCommands = 0;

            Console.WriteLine("\n===== Command Index Mapping =====");

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                // 이 배치의 시작 인덱스 저장
                _batchCommandStartIndices[b] = _totalDrawCommands;

                Console.WriteLine($"Batch {b}: Command Start Index = {_totalDrawCommands}");

                // LOD0 models
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    Console.WriteLine($"  Model {m} LOD0 → Command Index {_totalDrawCommands}");
                    _totalDrawCommands++;
                }

                // LOD1 impostor
                Console.WriteLine($"  Impostor LOD1 → Command Index {_totalDrawCommands}");
                _totalDrawCommands++;
            }

            Console.WriteLine($"Total Commands: {_totalDrawCommands}");
            Console.WriteLine("=================================\n");
        }

        private void CreateSSBOs()
        {
            // ===== 1️⃣ Command 인덱스 계산 (새로 추가) =====
            CalculateCommandIndices();

            // ===== 공통 인스턴스 데이터 버퍼 ===== (동일)
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

            // ===== Frustum 중간 버퍼 ===== (동일)
            _frustumPassedSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumPassedSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);

            _frustumCounterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            // ===== LOD 통합 버퍼 ===== (동일)
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

            _debugDepthSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _debugDepthSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _debugDepthSSBO);

            // ===== 새로운 통합 Indirect Command Buffer 생성 =====
            CreateUnifiedIndirectBuffer();
        }

        // ===== 새 함수: 통합 버퍼 생성 =====
        private void CreateUnifiedIndirectBuffer()
        {
            Console.WriteLine("\n===== Creating Unified Indirect Command Buffer =====");

            int bufferSize = _totalDrawCommands * COMMAND_SIZE;
            _indirectCommandBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, (uint)bufferSize,
                          IntPtr.Zero, BufferUsage.DynamicDraw);

            Console.WriteLine($"Buffer Created: {_totalDrawCommands} commands × {COMMAND_SIZE} bytes = {bufferSize} bytes");

            // ===== 초기 Command 데이터 채우기 =====
            InitializeUnifiedCommands();

            Console.WriteLine("Unified Indirect Buffer Initialized");
            Console.WriteLine("====================================================\n");
        }

        // ===== 새 함수: Command 구조체 초기화 =====
        private void InitializeUnifiedCommands()
        {
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            int commandIndex = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                Console.WriteLine($"Initializing Batch {b} commands (start: {_batchCommandStartIndices[b]})");

                // ===== LOD0 Commands =====
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
                    {
                        VertexCount = batch.VertexCounts[m],  // 모델의 정점 개수
                        InstanceCount = 0,                     // GPU가 채울 필드
                        First = 0,
                        BaseInstance = 0
                    };

                    int offset = commandIndex * COMMAND_SIZE;
                    Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                                   (IntPtr)offset, COMMAND_SIZE, cmd);

                    Console.WriteLine($"  Command {commandIndex}: Model {m} LOD0, VertexCount={cmd.VertexCount}");
                    commandIndex++;
                }

                // ===== LOD1 Command (Impostor) =====
                DrawArraysIndirectCommand impostorCmd = new DrawArraysIndirectCommand
                {
                    VertexCount = 6,       // Impostor quad = 2 triangles = 6 vertices
                    InstanceCount = 0,     // GPU가 채울 필드
                    First = 0,
                    BaseInstance = 0
                };

                int impostorOffset = commandIndex * COMMAND_SIZE;
                Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                               (IntPtr)impostorOffset, COMMAND_SIZE, impostorCmd);

                Console.WriteLine($"  Command {commandIndex}: Impostor LOD1, VertexCount={impostorCmd.VertexCount}");
                commandIndex++;
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
            UpdateIndirectCommandsGPU();

            VerifyBatchIDArray();

            _frameCount++;
        }

        // ===== 새 함수: BatchID 배열 검증 =====
        private void VerifyBatchIDArray()
        {
            var batchIDs = _batchManager.GetBatchIDs();

            Console.WriteLine("\n===== BatchID Array Verification =====");

            // Batch 0 샘플
            Console.WriteLine("Batch 0 sample (indices 0-4):");
            for (uint i = 0; i < 5; i++)
            {
                Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
            }

            // Batch 1 샘플
            Console.WriteLine("Batch 1 sample (indices 33334-33338):");
            for (uint i = 33334; i < 33339; i++)
            {
                Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
            }

            // Batch 2 샘플
            Console.WriteLine("Batch 2 sample (indices 66667-66671):");
            for (uint i = 66667; i < 66672; i++)
            {
                Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
            }

            // ⭐ Visible indices의 실제 BatchID 확인
            Console.WriteLine("\n===== Visible Instances BatchID Check =====");

            uint[] indices = new uint[MAX_INSTANCES];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD0);
            fixed (uint* ptr = indices)
            {
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                                    (uint)(MAX_INSTANCES * 4), (IntPtr)ptr);
            }

            // Batch 0의 첫 3개
            Console.WriteLine("Batch 0 visible instances:");
            for (int i = 0; i < 3; i++)
            {
                uint instanceID = indices[i];
                uint instanceBatchID = batchIDs[instanceID];
                Console.WriteLine($"  visibleIndices[{i}] = {instanceID}, BatchID = {instanceBatchID}");
            }

            // Batch 1의 첫 3개
            Console.WriteLine("Batch 1 visible instances:");
            for (int i = 0; i < 3; i++)
            {
                uint instanceID = indices[33334 + i];
                uint instanceBatchID = batchIDs[instanceID];
                Console.WriteLine($"  visibleIndices[{33334 + i}] = {instanceID}, BatchID = {instanceBatchID}");
            }

            // Batch 2의 첫 3개
            Console.WriteLine("Batch 2 visible instances:");
            for (int i = 0; i < 3; i++)
            {
                uint instanceID = indices[66667 + i];
                uint instanceBatchID = batchIDs[instanceID];
                Console.WriteLine($"  visibleIndices[{66667 + i}] = {instanceID}, BatchID = {instanceBatchID}");
            }

            Console.WriteLine("==========================================\n");
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

            DebugPrint(camera);

            _hizCullingCompute.Unbind();

        }

        private void DebugPrint(Camera camera)
        {
            uint[] frustumCount = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumCount);

            int numWorkGroups = ((int)frustumCount[0] + 63) / 64;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _hizCullingCompute.Unbind();

            // ⭐ BatchID 배열 가져오기 (여기로 이동!)
            var batchIDs = _batchManager.GetBatchIDs();

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

                    Console.WriteLine("\n[1] Frustum Passed Samples (first 10):");
                    for (int i = 0; i < sampleIndices.Length; i++)
                    {
                        Console.WriteLine($"  [{i}] = {sampleIndices[i]}");
                    }

                    // ===== BatchID 확인 =====
                    Console.WriteLine("\n[2] BatchID for these samples:");
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
                    Console.WriteLine("\n[3] World positions for these samples:");
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

                Console.WriteLine($"[6] visible indices lod0");
                var batchStarts = _batchManager.GetBatchStarts();

                uint[] indices = new uint[MAX_INSTANCES];
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD0);
                fixed (uint* ptr = indices)
                {
                    Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                        (uint)(MAX_INSTANCES * 4), (IntPtr)ptr);
                }

                for (int b = 0; b < 3; b++)
                {
                    int start = (int)batchStarts[b];
                    for (int i = 0; i < countsLOD0[b]; i++)
                    {
                        int index = start + i;
                        uint instanceID = indices[index];
                        uint instanceBatchID = batchIDs[instanceID];  // ⭐ 이제 작동!

                        // ⭐ BatchID 불일치 경고
                        if (instanceBatchID != b)
                        {
                            Console.WriteLine($"batch={b}, [{index}] {instanceID} ⚠️ BatchID={instanceBatchID} (Expected {b})");
                        }
                        else
                        {
                            Console.WriteLine($"batch={b}, [{index}] {instanceID} ✓");
                        }
                    }
                    Console.WriteLine("-------------");
                }

                // ⭐ 추가: BatchID 배열 샘플 확인
                //if (_frameCount == 1)
                {
                    Console.WriteLine("\n===== BatchID Array Verification =====");
                    Console.WriteLine("Sample from each batch:");

                    // Batch 0 샘플
                    Console.WriteLine("Batch 0 (indices 0-4):");
                    for (uint i = 0; i < 5; i++)
                    {
                        Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
                    }

                    // Batch 1 샘플
                    Console.WriteLine("Batch 1 (indices 33334-33338):");
                    for (uint i = 33334; i < 33339; i++)
                    {
                        Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
                    }

                    // Batch 2 샘플
                    Console.WriteLine("Batch 2 (indices 66667-66671):");
                    for (uint i = 66667; i < 66672; i++)
                    {
                        Console.WriteLine($"  batchIDs[{i}] = {batchIDs[i]}");
                    }
                    Console.WriteLine("======================================\n");
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


        // 이 함수를 추가하고 한 번 실행해보세요
        private void AnalyzeCurrentStructure()
        {
            Console.WriteLine("===== Current Indirect Buffer Structure =====");
            Console.WriteLine($"Total Batches: {_batchManager.ActualBatchCount}");

            int totalCommands = 0;
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);
                int batchCommands = batch.Models.Length + 1;  // LOD0 models + LOD1 impostor
                totalCommands += batchCommands;

                Console.WriteLine($"Batch {b}:");
                Console.WriteLine($"  Models: {batch.Models.Length}");
                Console.WriteLine($"  Start Index: {batch.StartIndex}");
                Console.WriteLine($"  Commands: {batchCommands} (LOD0: {batch.Models.Length}, LOD1: 1)");
            }

            Console.WriteLine($"Total Commands Needed: {totalCommands}");
            Console.WriteLine($"Buffer Size: {totalCommands * 16} bytes");
            Console.WriteLine("=============================================");
        }

        private void RenderBatch(uint batchID, Camera camera)
        {
            BatchDescriptor batch = _batchManager.GetBatch(batchID);

            int cmdStartIndex = _batchCommandStartIndices[batchID];
            int currentCmdIndex = cmdStartIndex;

            // ===== 디버그 로그 (첫 프레임만) =====
            if (_frameCount == 1)
            {
                Console.WriteLine($"\n===== Rendering Batch {batchID} ({batch.ModelName}) =====");
                Console.WriteLine($"  Start Index: {batch.StartIndex}");
                Console.WriteLine($"  Command Start: {cmdStartIndex}");
            }

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // ===== LOD0: Mesh 렌더링 =====
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(batch.StartIndex);
            _instancedShader.LoadCurrentBatchID(batchID);

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _batchIDSSBO);

            for (int m = 0; m < batch.Models.Length; m++)
            {
                if (_frameCount == 1)
                {
                    Console.WriteLine($"  LOD0 Model {m}: Cmd={currentCmdIndex}, Offset={currentCmdIndex * COMMAND_SIZE}");
                }

                if (batch.Models[m].Texture != null)
                {
                    _instancedShader.LoadTexture(TextureUnit.Texture0,
                        batch.Models[m].Texture.TextureID);
                }

                Gl.BindVertexArray(batch.VAOs[m]);

                int cmdOffset = currentCmdIndex * COMMAND_SIZE;
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, (IntPtr)cmdOffset);

                currentCmdIndex++;
            }

            _instancedShader.Unbind();

            /*
            // ===== LOD1: Impostor 렌더링 =====
            _impostorInstancedShader.Bind();
            _impostorInstancedShader.LoadVPMatrix(camera.VPMatrix);
            _impostorInstancedShader.LoadCameraPosition(camera.Position);
            _impostorInstancedShader.LoadBatchStartOffset(batch.StartIndex);

            // Impostor 텍스처 설정 (기존 코드 유지)
            var impostorModel = _impostor.GetImpostorModel(batch.ModelName);
            if (impostorModel != null)
            {
                _impostorInstancedShader.LoadImpostorTexture(
                    TextureUnit.Texture0,
                    impostorModel.ImpostorTexture);
                _impostorInstancedShader.LoadGridSize(impostorModel.GridSize);
            }

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _batchIDSSBO);

            Gl.BindVertexArray(_point.VAO);
            Gl.EnableVertexAttribArray(0);

            // ✅ Impostor Command 오프셋
            int impostorCmdOffset = currentCmdIndex * COMMAND_SIZE;

            // ✅ DrawArraysIndirect 호출
            Gl.DrawArraysIndirect(PrimitiveType.Points, (IntPtr)impostorCmdOffset);

            Gl.DisableVertexAttribArray(0);
            _impostorInstancedShader.Unbind();
            */
        }

        // ===== 새 함수: InstanceCount 검증 =====
        private void VerifyInstanceCounts()
        {
            Console.WriteLine($"\n===== Frame {_frameCount}: Verifying InstanceCounts =====");

            // 통합 버퍼 읽기
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);
            uint[] commandData = new uint[_totalDrawCommands * 4];
            Gl.GetBufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero,
                                (uint)(_totalDrawCommands * COMMAND_SIZE), commandData);

            // Visible Counts 읽기 (비교용)
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

            int cmdIdx = 0;
            bool allCorrect = true;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                Console.WriteLine($"\nBatch {b}:");
                Console.WriteLine($"  Expected LOD0={countsLOD0[b]}, LOD1={countsLOD1[b]}");

                // LOD0 models
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    uint instanceCount = commandData[cmdIdx * 4 + 1];
                    Console.WriteLine($"  Model {m} LOD0: instanceCount={instanceCount}");

                    if (instanceCount != countsLOD0[b])
                    {
                        Console.WriteLine($"    ❌ ERROR: Expected {countsLOD0[b]}");
                        allCorrect = false;
                    }
                    else
                    {
                        Console.WriteLine($"    ✅ Correct");
                    }

                    cmdIdx++;
                }

                // LOD1 impostor
                uint impostorInstanceCount = commandData[cmdIdx * 4 + 1];
                Console.WriteLine($"  Impostor LOD1: instanceCount={impostorInstanceCount}");

                if (impostorInstanceCount != countsLOD1[b])
                {
                    Console.WriteLine($"    ❌ ERROR: Expected {countsLOD1[b]}");
                    allCorrect = false;
                }
                else
                {
                    Console.WriteLine($"    ✅ Correct");
                }

                cmdIdx++;
            }

            if (allCorrect)
            {
                Console.WriteLine("\n✅ All InstanceCounts are correct!");
            }
            else
            {
                Console.WriteLine("\n❌ Some InstanceCounts are incorrect!");
            }

            Console.WriteLine("==============================================\n");
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

            // ===== 검증 (처음 1회만) =====
            if (_frameCount == 1)
            {
                Console.WriteLine("\n===== First Frame: Verifying Unified Buffer =====");
                VerifyUnifiedBuffer();
                Console.WriteLine("\n===== First Frame: Verifying InstanceCounts =====");
                VerifyInstanceCounts();
            }

            // 각 Batch 렌더링
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                RenderBatch(b, camera);
            }

            Gl.BindVertexArray(0);
        }

        private void VerifyUnifiedBuffer()
        {
            Console.WriteLine("\n===== Verifying Unified Indirect Buffer =====");

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // 전체 버퍼 읽기 (4 uints per command)
            uint[] commandData = new uint[_totalDrawCommands * 4];
            Gl.GetBufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero,
                                (uint)(_totalDrawCommands * COMMAND_SIZE), commandData);

            int cmdIdx = 0;
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                Console.WriteLine($"\nBatch {b} (Start Index: {_batchCommandStartIndices[b]}):");

                // LOD0 models
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    uint vertexCount = commandData[cmdIdx * 4 + 0];
                    uint instanceCount = commandData[cmdIdx * 4 + 1];
                    uint first = commandData[cmdIdx * 4 + 2];
                    uint baseInstance = commandData[cmdIdx * 4 + 3];

                    Console.WriteLine($"  Command {cmdIdx} (Model {m} LOD0):");
                    Console.WriteLine($"    VertexCount={vertexCount}, InstanceCount={instanceCount}");
                    Console.WriteLine($"    First={first}, BaseInstance={baseInstance}");

                    // 검증: VertexCount가 올바른지
                    if (vertexCount != batch.VertexCounts[m])
                    {
                        Console.WriteLine($"    ⚠️ WARNING: Expected VertexCount={batch.VertexCounts[m]}");
                    }

                    cmdIdx++;
                }

                // LOD1 impostor
                uint impostorVertexCount = commandData[cmdIdx * 4 + 0];
                uint impostorInstanceCount = commandData[cmdIdx * 4 + 1];

                Console.WriteLine($"  Command {cmdIdx} (Impostor LOD1):");
                Console.WriteLine($"    VertexCount={impostorVertexCount}, InstanceCount={impostorInstanceCount}");

                if (impostorVertexCount != 6)
                {
                    Console.WriteLine($"    ⚠️ WARNING: Expected VertexCount=6 for impostor");
                }

                cmdIdx++;
            }

            Console.WriteLine("\n==============================================\n");
        }

        // ===== 새 함수: 통합 렌더링 =====
        private void RenderAllBatchesUnified(Camera camera)
        {
            // Indirect Command Buffer 바인딩 (모든 배치에 공통)
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // 각 배치 렌더링
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                RenderBatch(b, camera);
            }
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
            Gl.DeleteBuffers(_debugDepthSSBO);

            // ===== ✅ 통합 버퍼만 삭제 =====
            Gl.DeleteBuffers(_indirectCommandBuffer);

            Console.WriteLine("GPU Culling Renderer disposed");
        }
    }
}