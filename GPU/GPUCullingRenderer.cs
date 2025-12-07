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

namespace GPUDriven
{
    public unsafe class GPUCullingRenderer : IDisposable
    {
        private const int MAX_INSTANCES = 90000;
        private const float LOD_DISTANCE = 200f;

        private string _projPath;
        private string _modelName;

        // 빌보드 크기
        private float BILLBOARD_WIDTH = 8f;
        private float BILLBOARD_HEIGHT = 12f;
        private const int FRAME_COUNT_DEBUG = 600;
        private Vertex2f BILLBOARD_SIZE;

        // 나무 메시
        private TexturedModel[] _treeModel;
        private uint[] _vao;
        private uint[] _vertexCount;

        // ===== SSBO들 (HiZ 추가) =====
        private uint _transformSSBO;
        private uint _aabbSSBO;

        // Frustum 중간 결과 (LOD0 전용)
        private uint _frustumPassedSSBO;
        private uint _frustumCounterSSBO;

        // 최종 가시 결과 (LOD0)
        private uint _visibleIndicesSSBO;
        private uint _counterSSBO;
        private uint[] _indirectBuffers;
        private bool _isVisibleLod0 = true;

        // LOD1용 SSBO (빌보드는 HiZ 불필요)
        private uint _visibleIndicesSSBO_LOD1;
        private uint _counterSSBO_LOD1;
        private uint _indirectBuffer_LOD1;

        private uint _debugDepthSSBO;  // ✅ 디버그 깊이 버퍼
        private DepthDebugInstancedShader _depthDebugShader;
        private bool _debugDepthMode = false;
        public bool DebugDepthMode { get => _debugDepthMode; set => _debugDepthMode = value; }

        // 데이터
        private Matrix4x4f[] _transforms;
        private AABB[] _aabbs;

        // ===== 셰이더 =====
        private FrustumCullingComputeShader _cullingCompute;
        private HiZOcclusionComputeShader _hizCullingCompute;
        private GPUInstancedShader _instancedShader;
        private GPUBillboardShader _billboardShader;
        private UnlitShader _unlitShader;

        // 임포스터 관련
        private ImpostorInstancedShader _impostorInstancedShader;
        private ImpostorShader _impostorShader;
        private ImpostorLODSystem _impostor;
        public BaseModel3d _point = Loader3d.LoadPoint(0, 0, 0);
        private AABB _modelAABB;

        // HiZ 컨트롤
        private bool _enableHiZCulling = true;

        // 드로우 커맨드
        float[] debugInit = new float[MAX_INSTANCES];
        uint[] count = new uint[1];
        DrawArraysIndirectCommand cmd;

        // 성능 모니터링
        private int _frameCount = 0;
        private uint _lastVisibleCount = 0;
        private uint _lastVisibleLOD1 = 0;
        private uint _lastFrustumPassed = 0;
        private Stopwatch _computeTimer;

        public GPUCullingRenderer(string projPath)
        {
            _transforms = new Matrix4x4f[MAX_INSTANCES];
            _aabbs = new AABB[MAX_INSTANCES];
            _projPath = projPath;
            _computeTimer = new Stopwatch();
        }

        public void Initialize(string modelName, TexturedModel[] treeModel, int maxMipLevels = 0, TerrainRegion terrainRegion = null)
        {
            _modelName = modelName;
            _treeModel = treeModel;

            if (maxMipLevels > 10)
            {
                throw new ArgumentException("Max mip levels exceed limit (10)");
            }

            SetupMeshVAO();
            CalculateAABB(_treeModel, ref _modelAABB);
            BILLBOARD_SIZE = new Vertex2f(_modelAABB.SizeX, _modelAABB.SizeZ);

            GenerateInstancePositions(terrainRegion);
            CreateSSBOs();
            LoadShaders(_projPath, maxMipLevels);
            UploadToGPU();

            // 임포스터 시스템 초기화
            _impostor = new ImpostorLODSystem(LOD_DISTANCE);
            _impostor.CreateImpostorModel(modelName, ImpostorSettings.CreateSettings(256, 16, 8),
                _unlitShader, _treeModel);

            Console.WriteLine("=== GPU Culling Renderer Initialized with HiZ ===");
            Console.WriteLine($"Instances: {MAX_INSTANCES}");
            Console.WriteLine($"Model AABB: Min{_modelAABB.Min}, Max{_modelAABB.Max}");
            Console.WriteLine($"Billboard Size: {BILLBOARD_WIDTH}x{BILLBOARD_HEIGHT}");
            Console.WriteLine($"HiZ Culling: {(_enableHiZCulling ? "Enabled" : "Disabled")}");
        }

        private void CalculateAABB(TexturedModel[] models, ref AABB aabb)
        {
            aabb = new AABB(new Vertex3f(float.MaxValue), new Vertex3f(float.MinValue));
            for (int i = 0; i < _treeModel.Length; i++)
            {
                AABB b = CalculateAABB(_treeModel[i]);
                aabb.Min = Vertex3f.Min(aabb.Min, b.Min);
                aabb.Max = Vertex3f.Max(aabb.Max, b.Max);
            }
        }

        private AABB CalculateAABB(TexturedModel model)
        {
            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);
            Vertex3f[] vertices = model.Vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex3f pos = vertices[i];
                min = Vertex3f.Min(min, pos);
                max = Vertex3f.Max(max, pos);
            }

            return new AABB(min, max);
        }

        private void LoadShaders(string projPath, int maxMipLevels)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _hizCullingCompute = new HiZOcclusionComputeShader(projPath, maxMipLevels);
            _instancedShader = new GPUInstancedShader(projPath);
            _billboardShader = new GPUBillboardShader(projPath);
            _impostorShader = new ImpostorShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _impostorInstancedShader = new ImpostorInstancedShader(projPath);
            _depthDebugShader = new DepthDebugInstancedShader(projPath);
        }

        public void SetDebugDepthMode(bool enabled)
        {
            _debugDepthMode = enabled;
            Console.WriteLine($"Debug Depth Mode: {(_debugDepthMode ? "Enabled" : "Disabled")}");
        }

        private void SetupMeshVAO()
        {
            _vao = new uint[_treeModel.Length];
            _vertexCount = new uint[_treeModel.Length];
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _vao[i] = _treeModel[i].VAO;
                _vertexCount[i] = (uint)_treeModel[i].VertexCount;
                Console.WriteLine($"Tree mesh [{i}]: {_vertexCount[i]} vertices");
            }
        }

        private void GenerateInstancePositions(TerrainRegion terrainRegion)
        {
            int gridSize = 300;
            float spacing = 15f;
            float halfSpacing = spacing / 2f;
            float quaterSpacing = spacing / 4f;
            Random rand = new Random(42);
            Vertex3f position = Vertex3f.Zero;

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;

                float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * halfSpacing - quaterSpacing);
                position.x = posX;
                position.y = posY;

                float posZ = terrainRegion.TerrainData.GetTerrainHeight(ref position,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE);

                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                float scale = 0.5f + (float)(rand.NextDouble() * 1.0f);

                _transforms[i] = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);

                _aabbs[i] = TransformAABB(_modelAABB, _transforms[i]);
            }

            Console.WriteLine($"Generated {MAX_INSTANCES} tree instances");
        }

        // ===== 2단계 컬링 파이프라인 =====
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchyZBuffer hzBuffer = null)
        {
            // ===== 1단계: Frustum Culling =====
            PerformFrustumCulling(camera, viewFrustum);

            // ===== 2단계: HiZ Occlusion Culling (선택적) =====
            if (_enableHiZCulling && hzBuffer != null)//&& hzBuffer.IsValid)
            {
                PerformHiZCulling(camera, hzBuffer);
            }
            else
            {
                // HiZ 비활성화 시: Frustum 결과를 최종 결과로 복사
                CopyFrustumResultToFinal();
            }

            // ===== 3단계: Indirect Buffer 업데이트 =====
            for (int i = 0; i < _treeModel.Length; i++)
            {
                UpdateIndirectBuffer(_indirectBuffers[i], _counterSSBO, _vertexCount[i]);
            }

            UpdateIndirectBuffer(_indirectBuffer_LOD1, _counterSSBO_LOD1, 6);

            _frameCount++;
        }

        private void PerformFrustumCulling(Camera camera, Polyhedron viewFrustum)
        {
            // Counter 초기화 (Frustum 중간 버퍼만)
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            // Compute Shader 실행
            _cullingCompute.Bind();

            // SSBO 바인딩 (단순화)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);

            // 유니폼 설정 (LOD 관련 제거)
            _cullingCompute.LoadFrustumPlanes(viewFrustum.Planes);

            // Dispatch
            int numWorkGroups = (MAX_INSTANCES + 255) / 256;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            _cullingCompute.Unbind();
        }


        private void PerformHiZCulling(Camera camera, HierarchyZBuffer hzBuffer)
        {
            // 최종 카운터 초기화
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            if (_debugDepthMode)
            {
                // 디버그 버퍼 초기화 (음수로 초기화하여 미처리 인스턴스 구별)
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
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);         // transforms[]
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _frustumPassedSSBO);     // frustumPassedIndices[]
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _frustumCounterSSBO);    // frustumPassedCount
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _aabbSSBO);              // worldAABBs[] (이미 월드 공간)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _visibleIndicesSSBO);    // visibleIndices_LOD0[]
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _counterSSBO);           // visibleCount_LOD0
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _visibleIndicesSSBO_LOD1); // visibleIndices_LOD1[]
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _counterSSBO_LOD1);      // visibleCount_LOD1

            if (_debugDepthMode)
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _debugDepthSSBO);        // 디버그 버퍼

            // 유니폼 설정
            _hizCullingCompute.LoadHiZTextures(hzBuffer.HiZTexture);
            _hizCullingCompute.LoadMaxMipLevel(hzBuffer.Levels - 1);
            _hizCullingCompute.LoadVPMatrix(camera.VPMatrix);
            _hizCullingCompute.LoadCameraPosition(camera.Position);
            _hizCullingCompute.LoadLODDistance(LOD_DISTANCE);
            _hizCullingCompute.LoadScreenSize(hzBuffer.Width, hzBuffer.Height);
            _hizCullingCompute.LoadMaxInstanceCount(MAX_INSTANCES);
            _hizCullingCompute.LoadCameraNearFar(camera.NEAR, camera.FAR);
            _hizCullingCompute.LoadViewMatrix(camera.ViewMatrix);
            _hizCullingCompute.LoadIsDebugMode(_debugDepthMode);

            // Frustum 통과한 개수만큼만 처리
            uint[] frustumCount = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumCount);

            if (frustumCount[0] > 0)
            {
                int numWorkGroups = ((int)frustumCount[0] + 63) / 64;
                Gl.DispatchCompute((uint)numWorkGroups, 1, 1);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
            }

            _hizCullingCompute.Unbind();
        }

        private void CopyFrustumResultToFinal()
        {
            // HiZ 비활성화 시: Frustum 통과한 모든 객체를 LOD0로 처리
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count);

            if (count[0] > 0)
            {
                // LOD1 카운터는 0으로
                uint zero = 0;
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

                // 모든 객체를 LOD0로 복사
                Gl.BindBuffer(BufferTarget.CopyReadBuffer, _frustumCounterSSBO);
                Gl.BindBuffer(BufferTarget.CopyWriteBuffer, _counterSSBO);
                Gl.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer,
                    IntPtr.Zero, IntPtr.Zero, 4);

                Gl.BindBuffer(BufferTarget.CopyReadBuffer, _frustumPassedSSBO);
                Gl.BindBuffer(BufferTarget.CopyWriteBuffer, _visibleIndicesSSBO);
                Gl.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer,
                    IntPtr.Zero, IntPtr.Zero, count[0] * 4);
            }
        }

        public void SetHiZCullingEnabled(bool enabled)
        {
            _enableHiZCulling = enabled;
            Console.WriteLine($"HiZ Occlusion Culling: {(_enableHiZCulling ? "Enabled" : "Disabled")}");
        }

        public bool IsHiZEnabled => _enableHiZCulling;

        public uint GetVisibleCountDebug()
        {
            if (_frameCount % FRAME_COUNT_DEBUG == 0)
            {
                uint[] count0 = new uint[1];
                uint[] count1 = new uint[1];
                uint[] frustumPassed = new uint[1];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count0);

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count1);

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, frustumPassed);

                _lastVisibleCount = count0[0];
                _lastVisibleLOD1 = count1[0];
                _lastFrustumPassed = frustumPassed[0];

            }

            return _lastVisibleCount + _lastVisibleLOD1;
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

            // ===== LOD0: Tree Mesh =====
            if (_isVisibleLod0)
            {
                if (_debugDepthMode)
                {
                    // ✅ 디버그 모드: 깊이 시각화
                    _depthDebugShader.Bind();
                    _depthDebugShader.LoadVPMatrix(camera.VPMatrix);
                    _depthDebugShader.LoadCameraNearFar(camera.NEAR, camera.FAR);  // ✅ 추가

                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _debugDepthSSBO);

                    for (int i = 0; i < _treeModel.Length; i++)
                    {
                        Gl.BindVertexArray(_vao[i]);
                        Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                        Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
                    }
                    _depthDebugShader.Unbind();
                }
                else
                {
                    // 기존 일반 렌더링
                    _instancedShader.Bind();
                    _instancedShader.LoadVPMatrix(camera.VPMatrix);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                    Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);

                    for (int i = 0; i < _treeModel.Length; i++)
                    {
                        if (_treeModel[i].Texture != null)
                        {
                            _instancedShader.LoadTexture(TextureUnit.Texture0,
                                _treeModel[i].Texture.TextureID);
                        }
                        Gl.BindVertexArray(_vao[i]);
                        Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                        Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
                    }
                    _instancedShader.Unbind();
                }
            }

            // ===== LOD1: Impostor (디버그 모드에서는 렌더링 안 함) =====
            if (!_debugDepthMode)
            {
                Gl.Disable(EnableCap.Blend);
                _impostorInstancedShader.Bind();
                _impostorInstancedShader.LoadEnableEdgeLine(false);
                _impostorInstancedShader.LoadVPMatrix(camera.VPMatrix);
                _impostorInstancedShader.LoadCameraPosition(camera.Position);

                ImpostorSettings settings = _impostor.GetImpostorSettings(_modelName);
                uint textureId = _impostor.AtlasTexture(_modelName);

                _impostorInstancedShader.LoadImpostorAtlas(TextureUnit.Texture0, textureId);
                _impostorInstancedShader.LoadAtlasSize(settings.AtlasSize);
                _impostorInstancedShader.LoadIndividualSize(settings.IndividualSize);
                _impostorInstancedShader.LoadHorizontalFrames(settings.HorizontalAngles);
                _impostorInstancedShader.LoadVerticalFrames(settings.VerticalAngles);
                _impostorInstancedShader.LoadAABBSizeModel(_modelAABB.SphereRadius);
                _impostorInstancedShader.LoadAABBCenterEntity(_modelAABB.Center);

                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

                Gl.BindVertexArray(_point.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1);
                Gl.DrawArraysIndirect(PrimitiveType.Points, IntPtr.Zero);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
                _impostorInstancedShader.Unbind();

                Gl.Enable(EnableCap.Blend);
            }

            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);
            Gl.BindVertexArray(0);
        }

        private void UpdateIndirectBuffer(uint indirectBuffer, uint counterBuffer, uint vertexCount)
        {
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);

            cmd = new DrawArraysIndirectCommand
            {
                VertexCount = vertexCount,
                InstanceCount = 0,
                First = 0,
                BaseInstance = 0
            };

            Gl.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, 16, cmd);

            Gl.BindBuffer(BufferTarget.CopyReadBuffer, counterBuffer);
            Gl.BindBuffer(BufferTarget.CopyWriteBuffer, indirectBuffer);
            Gl.CopyBufferSubData(BufferTarget.CopyReadBuffer,
                BufferTarget.CopyWriteBuffer, IntPtr.Zero, (IntPtr)4, 4);
        }

        private AABB TransformAABB(AABB local, Matrix4x4f transform)
        {
            Vertex3f[] corners = new Vertex3f[8];
            corners[0] = new Vertex3f(local.Min.x, local.Min.y, local.Min.z);
            corners[1] = new Vertex3f(local.Max.x, local.Min.y, local.Min.z);
            corners[2] = new Vertex3f(local.Min.x, local.Max.y, local.Min.z);
            corners[3] = new Vertex3f(local.Max.x, local.Max.y, local.Min.z);
            corners[4] = new Vertex3f(local.Min.x, local.Min.y, local.Max.z);
            corners[5] = new Vertex3f(local.Max.x, local.Min.y, local.Max.z);
            corners[6] = new Vertex3f(local.Min.x, local.Max.y, local.Max.z);
            corners[7] = new Vertex3f(local.Max.x, local.Max.y, local.Max.z);

            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            for (int i = 0; i < 8; i++)
            {
                Vertex3f transformed = transform.Transform(corners[i]);
                min = Vertex3f.Min(min, transformed);
                max = Vertex3f.Max(max, transformed);
            }

            return new AABB(min, max);
        }

        private void CreateSSBOs()
        {
            // Transform
            _transformSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 64), IntPtr.Zero, BufferUsage.StaticDraw);

            // AABB
            _aabbSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 32), IntPtr.Zero, BufferUsage.StaticDraw);

            // ===== Frustum 중간 버퍼 (LOD0) =====
            _frustumPassedSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumPassedSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            _frustumCounterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _frustumCounterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4, IntPtr.Zero, BufferUsage.DynamicDraw);

            // ===== LOD0 최종 버퍼 =====
            _visibleIndicesSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            _counterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4,
                IntPtr.Zero, BufferUsage.DynamicDraw);

            _indirectBuffers = new uint[_treeModel.Length];
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _indirectBuffers[i] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16, IntPtr.Zero, BufferUsage.DynamicDraw);
            }

            // ===== LOD1 버퍼 =====
            _visibleIndicesSSBO_LOD1 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD1);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            _counterSSBO_LOD1 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4, IntPtr.Zero, BufferUsage.DynamicDraw);

            _indirectBuffer_LOD1 = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16, IntPtr.Zero, BufferUsage.DynamicDraw);

            Console.WriteLine("SSBOs created with HiZ support");

            // ✅ 디버그 깊이 버퍼 생성
            _debugDepthSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _debugDepthSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            Console.WriteLine("SSBOs created with HiZ support and debug depth buffer");
        }

        private void UploadToGPU()
        {
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = _transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 64), (IntPtr)ptr);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            fixed (AABB* ptr = _aabbs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 32), (IntPtr)ptr);
            }

            Console.WriteLine("Data uploaded to GPU successfully");
        }

        public void Dispose()
        {
            Gl.DeleteBuffers(_transformSSBO);
            Gl.DeleteBuffers(_aabbSSBO);
            Gl.DeleteBuffers(_frustumPassedSSBO);
            Gl.DeleteBuffers(_frustumCounterSSBO);
            Gl.DeleteBuffers(_visibleIndicesSSBO);
            Gl.DeleteBuffers(_counterSSBO);
            Gl.DeleteBuffers(_indirectBuffers);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD1);
            Gl.DeleteBuffers(_counterSSBO_LOD1);
            Gl.DeleteBuffers(_indirectBuffer_LOD1);
            Gl.DeleteBuffers(_debugDepthSSBO);  // ✅

            Console.WriteLine("GPU Culling Renderer disposed");
        }
    }
}