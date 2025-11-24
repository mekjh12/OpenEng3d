using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace GPUDriven
{
    public unsafe class GPUCullingRenderer : IDisposable
    {
        private const int MAX_INSTANCES = 90000;

        // 나무 메시
        private TexturedModel _treeModel;
        private uint _vao;
        private uint _vertexCount;

        // SSBO들
        private uint _transformSSBO;      // 90000개 변환 행렬
        private uint _aabbSSBO;           // 90000개 AABB
        private uint _visibleIndicesSSBO; // 가시 인덱스 출력
        private uint _counterSSBO;        // Atomic 카운터
        private uint _indirectBuffer;     // Indirect draw 파라미터

        // 데이터
        private Matrix4x4f[] _transforms;
        private AABB[] _aabbs;

        // 셰이더
        private FrustumCullingComputeShader _cullingCompute;
        private GPUInstancedShader _instancedShader;

        public GPUCullingRenderer()
        {
            _transforms = new Matrix4x4f[MAX_INSTANCES];
            _aabbs = new AABB[MAX_INSTANCES];
        }

        public void Initialize(TexturedModel treeModel, string projPath)
        {
            _treeModel = treeModel;

            // 1. 메시 VAO 준비
            SetupMeshVAO();

            // 2. 90000개 위치 생성
            GenerateInstancePositions();

            // 3. SSBO 생성
            CreateSSBOs();

            // 4. 셰이더 로드
            LoadShaders(projPath);

            // 5. GPU에 데이터 업로드
            UploadToGPU();
        }

        private void LoadShaders(string projPath)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _instancedShader = new GPUInstancedShader(projPath);
        }


        /// <summary>
        /// SetupMeshVAO 수정 버전
        /// </summary>
        private void SetupMeshVAO()
        {
            // 나무 메시의 VAO 가져오기
            _vao = _treeModel.VAO;
            _vertexCount = (uint)_treeModel.VertexCount;

            Console.WriteLine($"Tree mesh loaded: {_vertexCount} vertices");

            // VAO 설정 상태 출력
            VaoDebugger.PrintConfiguration(_vao, "Tree Model VAO");
        }

        private void GenerateInstancePositions()
        {
            // 300x300 그리드로 배치 (간격 10미터)
            int gridSize = 300;
            float spacing = 10f;

            Random rand = new Random(42);

            // 나무 메시의 AABB (로컬 공간)
            AABB localAABB = CalculateLocalAABB(_treeModel);

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;

                // 위치 (약간의 랜덤 오프셋)
                float posx = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);
                float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);
                float posZ = 0;

                // 랜덤 회전 (Y축만)
                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);

                // 랜덤 스케일 (0.8 ~ 1.2)
                float scale = 0.8f + (float)(rand.NextDouble() * 0.4);

                // 변환 행렬
                _transforms[i] = Matrix4x4f.Translated(posx, posY, posZ) *
                               Matrix4x4f.RotatedY(rotZ) *
                               Matrix4x4f.Scaled(scale, scale, scale);

                // 월드 공간 AABB 계산
                _aabbs[i] = TransformAABB(localAABB, _transforms[i]);
                //Console.WriteLine($"{_aabbs[i].Min}-{_aabbs[i].Max}");
            }

            Console.WriteLine($"Generated {MAX_INSTANCES} tree instances");
        }

        public void Update(Camera camera, Polyhedron viewFrustum)
        {
            // 1. Counter 초기화 (0으로)
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                IntPtr.Zero, 4, zero);

            // 2. Frustum Plane 계산
            Plane[] frustumPlanes = viewFrustum.Planes;

            // 3. Compute Shader 실행
            _cullingCompute.Bind();
            _cullingCompute.LoadFrustumPlanes(frustumPlanes);

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _counterSSBO);

            // Dispatch (90000 / 256 = 352 work groups)
            Gl.DispatchCompute(352, 1, 1);

            // 메모리 배리어 (Compute 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit |
                            MemoryBarrierMask.CommandBarrierBit);

            // 4. Indirect Buffer 업데이트 - 순서 변경!
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer);

            // 먼저 기본 값들을 설정
            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
            {
                VertexCount = _vertexCount,  // 18621
                InstanceCount = 0,  // 임시로 0, 곧 덮어씀
                First = 0,
                BaseInstance = 0
            };

            Gl.BufferSubData(BufferTarget.DrawIndirectBuffer,
                IntPtr.Zero, 16, cmd);

            // 그 다음 Counter 값을 instanceCount 위치(offset 4)에 복사
            Gl.BindBuffer(BufferTarget.CopyReadBuffer, _counterSSBO);
            Gl.BindBuffer(BufferTarget.CopyWriteBuffer, _indirectBuffer);
            Gl.CopyBufferSubData(BufferTarget.CopyReadBuffer,
                BufferTarget.CopyWriteBuffer,
                IntPtr.Zero,      // counter에서 0번째 바이트
                (IntPtr)4,        // indirect buffer의 4번째 바이트 (instanceCount)
                4);               // 4바이트 복사


            // Update() 끝에 추가
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            uint visibleCount = 0;
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer,
                IntPtr.Zero, 4, visibleCount);
            Console.WriteLine($"Frame: Visible instances: {visibleCount} / {MAX_INSTANCES}");
        }

        public void Render(Camera camera)
        {
            _instancedShader.Bind();
            _instancedShader.LoadProjectionMatrix(camera.ProjectiveMatrix);
            _instancedShader.LoadViewMatrix(camera.ViewMatrix);

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);

            // 텍스처 바인딩
            //_instancedShader.LoadTexture(_instancedShader.loc_texture0, 0);
            //_treeModel.BindTexture();

            // VAO 바인딩
            Gl.BindVertexArray(_vao);

            // Indirect Draw
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer);
            Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);

            // 언바인드
            Gl.BindVertexArray(0);
        }

        private AABB CalculateLocalAABB(TexturedModel model)
        {
            // 메시의 로컬 AABB 계산
            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            Vertex3f[] vertices = model.Vertices; // position만

            for (int i = 0; i < vertices.Length; i += 3)
            {
                Vertex3f pos = vertices[i];
                min = Vertex3f.Min(min, pos);
                max = Vertex3f.Max(max, pos);
            }

            return new AABB(min, max);
        }

        private AABB TransformAABB(AABB local, Matrix4x4f transform)
        {
            // AABB 8개 코너 변환 후 재계산
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
            // 1. Transform SSBO (64 bytes * 90000 = 5.76MB)
            _transformSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 64),
                IntPtr.Zero,
                BufferUsage.StaticDraw);

            // 2. AABB SSBO (32 bytes * 90000 = 2.88MB)
            _aabbSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 32),
                IntPtr.Zero,
                BufferUsage.StaticDraw);

            // 3. Visible Indices SSBO (4 bytes * 90000 = 360KB)
            _visibleIndicesSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4),
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            // 4. Counter SSBO (4 bytes)
            _counterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4,
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            // 5. Indirect Buffer (16 bytes)
            _indirectBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer);
            Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16,
                IntPtr.Zero,
                BufferUsage.DynamicDraw);

            Console.WriteLine("SSBOs created");
        }

        private void UploadToGPU()
        {
            // Transform 행렬 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = _transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero,
                    (uint)(MAX_INSTANCES * 64),
                    (IntPtr)ptr);
            }

            // AABB 업로드
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            fixed (AABB* ptr = _aabbs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero,
                    (uint)(MAX_INSTANCES * 32),
                    (IntPtr)ptr);
            }

            Console.WriteLine("Data uploaded to GPU");
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
