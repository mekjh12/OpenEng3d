using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Diagnostics;
using ZetaExt;

namespace GPUDriven
{
    public unsafe class GPUCullingRenderer : IDisposable
    {
        private const int MAX_INSTANCES = 90000;
        private const float LOD_DISTANCE = 100f;

        private string _projPath;
        private string _modelName;

        // 빌보드 크기 (고정값으로 간단하게!)
        private float BILLBOARD_WIDTH = 8f;
        private float BILLBOARD_HEIGHT = 12f;
        private const int FRAME_COUNT_DEBUG = 600;
        private Vertex2f BILLBOARD_SIZE;

        // 나무 메시
        private TexturedModel[] _treeModel;
        private uint[] _vao;
        private uint[] _vertexCount;

        // SSBO들 (BillboardSize 제거!)
        private uint _transformSSBO;
        private uint _aabbSSBO;
        private uint _visibleIndicesSSBO;
        private uint _counterSSBO;
        private uint[] _indirectBuffers;        // LOD0 파트별
        private bool _isVisibleLod0 = true;     // LOD0 렌더링 여부

        // LOD1용 SSBO
        private uint _visibleIndicesSSBO_LOD1;
        private uint _counterSSBO_LOD1;
        private uint _indirectBuffer_LOD1;

        // 데이터
        private Matrix4x4f[] _transforms;
        private AABB[] _aabbs;

        // 셰이더 (BillboardSizeInit 제거!)
        private FrustumCullingComputeShader _cullingCompute;
        private GPUInstancedShader _instancedShader;
        private GPUBillboardShader _billboardShader;
        private UnlitShader _unlitShader;

        // 임포스터 관련
        private ImpostorInstancedShader _impostorInstancedShader;
        private ImpostorShader _impostorShader;     // 임포스터 렌더링용 쉐이더
        private ImpostorLODSystem _impostor;        // LOD 기반 임포스터 시스템
        public BaseModel3d _point = Loader3d.LoadPoint(0, 0, 0);
        private AABB _modelAABB;

        // 성능 모니터링
        private int _frameCount = 0;
        private uint _lastVisibleCount = 0;
        private uint _lastVisibleLOD1 = 0;
        private Stopwatch _computeTimer;

        public GPUCullingRenderer(string projPath)
        {
            _transforms = new Matrix4x4f[MAX_INSTANCES];
            _aabbs = new AABB[MAX_INSTANCES];
            _projPath = projPath;
            _computeTimer = new Stopwatch();
        }

        public void Initialize(string modelName, TexturedModel[] treeModel)
        {
            _modelName = modelName;
            _treeModel = treeModel;

            SetupMeshVAO();

            CalculateAABB(_treeModel, ref _modelAABB);
            BILLBOARD_SIZE = new Vertex2f(_modelAABB.SizeX, _modelAABB.SizeZ);

            GenerateInstancePositions();
            CreateSSBOs();  // BillboardSize SSBO 제거!
            LoadShaders(_projPath);
            UploadToGPU();

            // 임포스터 시스템 초기화
            _impostor = new ImpostorLODSystem(LOD_DISTANCE);
            _impostor.CreateImpostorModel(modelName, ImpostorSettings.CreateSettings(256, 16, 8), _unlitShader, _treeModel);

            Console.WriteLine("=== GPU Culling Renderer Initialized (Simplified) ===");
            Console.WriteLine($"Instances: {MAX_INSTANCES}");
            Console.WriteLine($"Billboard Size: {BILLBOARD_WIDTH}x{BILLBOARD_HEIGHT} (Fixed)");
            Console.WriteLine($"Vertices per instance: {_vertexCount}");
        }

        private void CalculateAABB(TexturedModel[] models, ref AABB aabb)
        {
            // 메시들의 합집합 AABB 계산(메시를 셋업한 후 실행해야 함)
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

        private void LoadShaders(string projPath)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _instancedShader = new GPUInstancedShader(projPath);
            _billboardShader = new GPUBillboardShader(projPath);
            _impostorShader = new ImpostorShader(projPath);
            _unlitShader = new UnlitShader(projPath);
            _impostorInstancedShader = new ImpostorInstancedShader(projPath);  // 새로 추가!
        }

        private void SetupMeshVAO()
        {
            _vao = new uint[_treeModel.Length];
            _vertexCount = new uint[_treeModel.Length];
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _vao[i] = _treeModel[i].VAO;
                _vertexCount[i] = (uint)_treeModel[i].VertexCount;
                Console.WriteLine($"Tree mesh loaded: {i}={_vertexCount} vertices");
            }
        }

        private void GenerateInstancePositions()
        {
            int gridSize = 300; // 300x300 = 90000
            float spacing = 20f;
            Random rand = new Random(42);

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;

                // 위치
                float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);
                float posZ = 0;
                float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);

                // Y축 회전
                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);

                // 스케일
                float scale = 0.99f + (float)(rand.NextDouble() * 0.01f);

                // 변환 행렬
                _transforms[i] = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);

                // 월드 공간 AABB
                _aabbs[i] = TransformAABB(_modelAABB, _transforms[i]);
            }

            Console.WriteLine($"Generated {MAX_INSTANCES} tree instances");
        }

        public void Update(Camera camera, Polyhedron viewFrustum)
        {
            camera.NEAR = 0.1f;

            // 1. Counter 초기화
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            // 2. Compute Shader 바인딩
            _cullingCompute.Bind();

            // 3. SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _counterSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _visibleIndicesSSBO_LOD1);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _counterSSBO_LOD1);

            // 4. 유니폼 설정
            _cullingCompute.LoadFrustumPlanes(viewFrustum.Planes);
            _cullingCompute.LoadCameraPosition(camera.Position);
            _cullingCompute.LoadLODDistance(LOD_DISTANCE);

            // 5. Dispatch
            int numWorkGroups = (MAX_INSTANCES + 255) / 256;
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            // 6. 메모리 배리어 강화
            Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

            // 7. Indirect Buffers 업데이트

            // 8. Update
            for (int i = 0; i < _treeModel.Length; i++)
            {
                UpdateIndirectBuffer(_indirectBuffers[i], _counterSSBO, _vertexCount[i]);
            }
            
            UpdateIndirectBuffer(_indirectBuffer_LOD1, _counterSSBO_LOD1, 6);

            _frameCount++;
        }

        public void DebugVisibleIndices()
        {
            int[] indices = new int[100];  // 처음 100개만 확인

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO_LOD1);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 400, indices);

            uint[] count = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count);

            Console.WriteLine($"\n=== LOD1 Visible Indices (Count: {count[0]}) ===");
            for (int i = 0; i < Math.Min(100, count[0]); i++)
            {
                Console.Write($"{indices[i]}, ");

                // 범위 체크
                if (indices[i] < 0 || indices[i] >= MAX_INSTANCES)
                {
                    Console.WriteLine($"\n⚠️ [{i}] = {indices[i]} ← 잘못된 인덱스!");
                }
            }
            Console.WriteLine();
        }

        private void UpdateIndirectBuffer(uint indirectBuffer, uint counterBuffer, uint vertexCount)
        {
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);

            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
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

        public uint GetVisibleCountDebug()
        {
            if (_frameCount % FRAME_COUNT_DEBUG == 0)
            {
                uint[] count0 = new uint[1];
                uint[] count1 = new uint[1];

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count0);

                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO_LOD1);
                Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count1);

                _lastVisibleCount = count0[0];
                _lastVisibleLOD1 = count1[0];

                Console.WriteLine($"[Frame {_frameCount}] LOD0: {_lastVisibleCount}, LOD1: {_lastVisibleLOD1}, Total: {_lastVisibleCount + _lastVisibleLOD1}");
            }
            return _lastVisibleCount;
        }

        public void Render(Camera camera)
        {
            // ===== 명시적으로 상태 초기화 =====
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(true);
            Gl.Enable(EnableCap.CullFace);

            // ===== 중요: Compute Shader 완료 대기 =====
            Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

            // ===== LOD0: Tree Mesh =====
            if (_isVisibleLod0)
            {
                _instancedShader.Bind();
                _instancedShader.LoadVPMatrix(camera.VPMatrix);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);

                for (int i = 0; i < _treeModel.Length; i++)
                {
                    if (_treeModel[i].Texture!=null)
                    {
                        _instancedShader.LoadTexture(TextureUnit.Texture0, _treeModel[i].Texture.TextureID);
                    }
                    Gl.BindVertexArray(_vao[i]);
                    Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                    Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
                }
                _instancedShader.Unbind();
            }

            // ===== LOD1: Impostor (Instanced) =====
            Gl.Disable(EnableCap.Blend);
            _impostorInstancedShader.Bind();
            _impostorInstancedShader.LoadEnableEdgeLine(false);
            _impostorInstancedShader.LoadVPMatrix(camera.VPMatrix);
            _impostorInstancedShader.LoadCameraPosition(camera.Position);

            ImpostorSettings settings = _impostor.GetImpostorSettings(_modelName);
            uint textureId = _impostor.AtlasTexture(_modelName);

            // ===== 여기가 핵심 수정 부분! =====
            // Atlas offset 계산 제거 - Geometry Shader에서 자동 계산됨
            // Vertex2f atlasOffset = _impostor.GetAtlasOffset(settings, camera.Position, Matrix4x4f.Identity);

            _impostorInstancedShader.LoadImpostorAtlas(TextureUnit.Texture0, textureId);
            _impostorInstancedShader.LoadAtlasSize(settings.AtlasSize);
            _impostorInstancedShader.LoadIndividualSize(settings.IndividualSize);

            // 프레임 수 전달 (수정됨: HorizontalAngles, VerticalAngles 사용)
            _impostorInstancedShader.LoadHorizontalFrames(settings.HorizontalAngles);
            _impostorInstancedShader.LoadVerticalFrames(settings.VerticalAngles);

            _impostorInstancedShader.LoadAABBSizeModel(_modelAABB.SphereRadius);
            _impostorInstancedShader.LoadAABBCenterEntity(_modelAABB.Center);

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

            Gl.BindVertexArray(_point.VAO);
            Gl.EnableVertexAttribArray(0);

            // Indirect Drawing으로 인스턴싱
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1);
            Gl.DrawArraysIndirect(PrimitiveType.Points, IntPtr.Zero);

            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _impostorInstancedShader.Unbind();

            Gl.Enable(EnableCap.Blend);

            // ===== 상태 복원 =====
            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);
            Gl.BindVertexArray(0);
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

            // LOD0 버퍼들
            _visibleIndicesSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            _counterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4,
                IntPtr.Zero, BufferUsage.DynamicDraw);

            // CreateSSBOs
            _indirectBuffers = new uint[_treeModel.Length];
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _indirectBuffers[i] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16, IntPtr.Zero, BufferUsage.DynamicDraw);
            }

            // LOD1 버퍼들
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

            Console.WriteLine("SSBOs created (Simplified - No BillboardSize SSBO)");
        }

        private void UploadToGPU()
        {
            // Transform 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = _transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 64), (IntPtr)ptr);
            }

            // AABB 업로드
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
            Gl.DeleteBuffers(_visibleIndicesSSBO);
            Gl.DeleteBuffers(_counterSSBO);
            Gl.DeleteBuffers(_indirectBuffers);
            Gl.DeleteBuffers(_visibleIndicesSSBO_LOD1);
            Gl.DeleteBuffers(_counterSSBO_LOD1);
            Gl.DeleteBuffers(_indirectBuffer_LOD1);

            Console.WriteLine("GPU Culling Renderer disposed");
        }
    }
}