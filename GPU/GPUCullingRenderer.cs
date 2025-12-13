using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        private bool _isDebugPrint = false;
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
        private const int COMMAND_SIZE = 16;  // DrawArraysIndirectCommand 4x4
        private int _totalDrawCommands;
        private uint _indirectCommandBuffer;  // 새로운 통합 버퍼

        uint[] _startIndices;
        uint[] _modelCounts;
        uint[] _frustumCount;

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

            // 배치별 커맨드 인덱스 계산
            _startIndices = new uint[_batchManager.ActualBatchCount];
            _modelCounts = new uint[_batchManager.ActualBatchCount];
            _frustumCount = new uint[1];

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

        private void UpdateIndirectCommandsGPU()
        {
            _updateCommandsCompute.Bind();

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 10, _indirectCommandBuffer);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 11, _visibleCountsSSBO_LOD0);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 12, _visibleCountsSSBO_LOD1);

            // Uniform 데이터 준비
            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                _startIndices[b] = (uint)_batchCommandStartIndices[b];
                _modelCounts[b] = (uint)_batchManager.GetBatch(b).Models.Length;
            }

            // Uniform 전달
            _updateCommandsCompute.LoadNumBatches(_batchManager.ActualBatchCount);
            _updateCommandsCompute.LoadBatchCommandStartIndices(_startIndices);
            _updateCommandsCompute.LoadNumModelsPerBatch(_modelCounts);

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

                throw new Exception("수정할 곳");
                /*
                _impostor.CreateImpostorModel(
                    batch.ModelName,
                    ImpostorSettings.CreateSettings(256, 16, 8),
                    _unlitShader,
                    batch.Models);

                Console.WriteLine($"Created impostor for: {batch.ModelName}");
                */
            }
        }

        private void CalculateCommandIndices()
        {
            _batchCommandStartIndices = new Dictionary<uint, int>();
            _totalDrawCommands = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);

                // 이 배치의 시작 인덱스 저장
                _batchCommandStartIndices[b] = _totalDrawCommands;

                // LOD0 models
                for (int m = 0; m < batch.Models.Length; m++)
                {
                    _totalDrawCommands++;
                }

                // LOD1 impostor
                _totalDrawCommands++;
            }
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
            int bufferSize = _totalDrawCommands * COMMAND_SIZE;
            _indirectCommandBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, (uint)bufferSize,
                          IntPtr.Zero, BufferUsage.DynamicDraw);

            // ===== 초기 Command 데이터 채우기 =====
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            int commandIndex = 0;

            for (uint b = 0; b < _batchManager.ActualBatchCount; b++)
            {
                BatchDescriptor batch = _batchManager.GetBatch(b);
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

            // Frustum 통과 개수 가져오기
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, _frustumCount);

            // Work Group 계산 및 Dispatch
            int numWorkGroups = ((int)_frustumCount[0] + 63) / 64;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
            _hizCullingCompute.Unbind();
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
        }


        private void RenderBatch(uint batchID, Camera camera)
        {
            BatchDescriptor batch = _batchManager.GetBatch(batchID);

            int cmdStartIndex = _batchCommandStartIndices[batchID];
            int currentCmdIndex = cmdStartIndex;

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // ===== LOD0: Mesh 렌더링 =====
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            _instancedShader.LoadBatchStartOffset(batch.StartIndex);

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

                int cmdOffset = currentCmdIndex * COMMAND_SIZE;
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, (IntPtr)cmdOffset);

                currentCmdIndex++;
            }

            _instancedShader.Unbind();

            // ===== LOD1: Impostor 렌더링 =====
            /*
            _impostorInstancedShader.Bind();

            // 🔹 기본 변환 행렬
            _impostorInstancedShader.LoadVPMatrix(camera.VPMatrix);
            _impostorInstancedShader.LoadCameraPosition(camera.Position);
            _impostorInstancedShader.LoadBatchStartOffset(batch.StartIndex);
            _impostorInstancedShader.LoadCurrentBatchID(batchID);  // ← 추가

            // 🔹 Impostor 설정 가져오기
            ImpostorSettings impostorSettings = _impostor.GetImpostorSettings(batch.ModelName);
            uint atlasTexture = _impostor.AtlasTexture(batch.ModelName);

            if (atlasTexture != 0)
            {
                // ✅ 텍스처 아틀라스 바인딩
                _impostorInstancedShader.LoadImpostorAtlas(
                    TextureUnit.Texture0,
                    atlasTexture);

                // ✅ 아틀라스 프레임 구성 정보
                _impostorInstancedShader.LoadHorizontalFrames(impostorSettings.HorizontalAngles);
                _impostorInstancedShader.LoadVerticalFrames(impostorSettings.VerticalAngles);

                // ✅ 아틀라스 개별 프레임 크기 (0~1 정규화)
                float atlasFrameSize = impostorSettings.IndividualSize / impostorSettings.AtlasSize;
                _impostorInstancedShader.LoadAtlasSize(atlasFrameSize);

                // ✅ 월드 공간 크기 정보 (첫 번째 모델의 AABB에서 가져오기)
                if (batch.Models.Length > 0)
                {
                    TexturedModel firstModel = batch.Models[0];

                    // Billboard 크기 = AABB 최대 치수
                    float aabbMaxDim = Math.Max(
                        Math.Max(firstModel.AABB.Size.x, firstModel.AABB.Size.y),
                        firstModel.AABB.Size.z);

                    _impostorInstancedShader.LoadIndividualSize(aabbMaxDim);
                    _impostorInstancedShader.LoadAABBSizeModel(aabbMaxDim);
                    _impostorInstancedShader.LoadAABBCenterEntity(firstModel.AABB.Center);
                }

                // ✅ 렌더링 옵션
                _impostorInstancedShader.LoadEnableEdgeLine(true);
            }

            // 🔹 SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 9, _batchIDSSBO);

            // 🔹 Point VAO 바인딩 및 렌더링
            Gl.BindVertexArray(_point.VAO);
            Gl.EnableVertexAttribArray(0);

            int impostorCmdOffset = currentCmdIndex * COMMAND_SIZE;
            Gl.DrawArraysIndirect(PrimitiveType.Points, (IntPtr)impostorCmdOffset);

            Gl.DisableVertexAttribArray(0);
            _impostorInstancedShader.Unbind();
            */
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