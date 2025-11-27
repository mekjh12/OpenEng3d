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
        private uint[] _indirectBuffers;  // LOD0 파트별

        // LOD1용 SSBO
        private uint _visibleIndicesSSBO_LOD1;
        private uint _counterSSBO_LOD1;
        private uint _indirectBuffer_LOD1;

        // 빌보드 메시
        private uint _billboardVAO;
        private uint _billboardVBO;
        private uint _billboardVertexCount;
        private uint _billboardTextureID;
        private bool _isBillboard = true;

        // 데이터
        private Matrix4x4f[] _transforms;
        private AABB[] _aabbs;

        // 셰이더 (BillboardSizeInit 제거!)
        private FrustumCullingComputeShader _cullingCompute;
        private GPUInstancedShader _instancedShader;
        private GPUBillboardShader _billboardShader;
        private UnlitShader _unlitShader;

        // 임포스터 관련
        private ImpostorShader _impostorShader;             // 임포스터 렌더링용 쉐이더
        ImpostorLODSystem _impostor;                        // LOD 기반 임포스터 시스템

        // 성능 모니터링
        private int _frameCount = 0;
        private uint _lastVisibleCount = 0;
        private uint _lastVisibleLOD1 = 0;

        private Stopwatch _computeTimer = new Stopwatch();

        AABB _modelUnionAABB;


        public GPUCullingRenderer()
        {
            _transforms = new Matrix4x4f[MAX_INSTANCES];
            _aabbs = new AABB[MAX_INSTANCES];
        }

        public void Initialize(TexturedModel[] treeModel, string projPath)
        {
            _treeModel = treeModel;
            SetupMeshVAO();
            CreateBillboardMesh();

            // 메시들의 합집합 AABB 계산(메시를 셋업한 후 실행해야 함)
            _modelUnionAABB = new AABB(new Vertex3f(float.MaxValue), new Vertex3f(float.MinValue));
            for (int i = 0; i < _treeModel.Length; i++)
            {
                AABB b = CalculateLocalAABB(_treeModel[i]);
                _modelUnionAABB.Min = Vertex3f.Min(_modelUnionAABB.Min, b.Min);
                _modelUnionAABB.Max = Vertex3f.Max(_modelUnionAABB.Max, b.Max);
            }

            BILLBOARD_SIZE = new Vertex2f(_modelUnionAABB.SizeX, _modelUnionAABB.SizeZ);


            GenerateInstancePositions();
            CreateSSBOs();  // BillboardSize SSBO 제거!
            LoadShaders(projPath);
            UploadToGPU();

            // 빌보드 텍스처
            Texture texture = new Texture(projPath + @"\Res\T_beech_tree_billboard.png");
            _billboardTextureID = texture.TextureID;

            // 임포스터 시스템 초기화
            _impostor = new ImpostorLODSystem(200);
            _impostor.CreateImpostorModel("Palm4", ImpostorSettings.CreateSettings(256, 16, 8), _unlitShader, _treeModel);

            Console.WriteLine("=== GPU Culling Renderer Initialized (Simplified) ===");
            Console.WriteLine($"Instances: {MAX_INSTANCES}");
            Console.WriteLine($"Billboard Size: {BILLBOARD_WIDTH}x{BILLBOARD_HEIGHT} (Fixed)");
            Console.WriteLine($"Vertices per instance: {_vertexCount}");
        }

        private void CreateBillboardMesh()
        {
            // 단순한 쿼드 메시 (2개 삼각형)
            float[] vertices = new float[]
            {
                // Position (3) + TexCoord (2)
                -0.5f, -0.0f, 0.0f,   0.0f, 1.0f,  // 좌하
                 0.5f, -0.0f, 0.0f,   1.0f, 1.0f,  // 우하
                 0.5f,  1.0f, 0.0f,   1.0f, 0.0f,  // 우상
                -0.5f, -0.0f, 0.0f,   0.0f, 1.0f,  // 좌하
                 0.5f,  1.0f, 0.0f,   1.0f, 0.0f,  // 우상
                -0.5f,  1.0f, 0.0f,   0.0f, 0.0f,  // 좌상
            };

            _billboardVertexCount = 6;
            _billboardVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_billboardVAO);

            _billboardVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _billboardVBO);
            fixed (float* ptr = vertices)
            {
                Gl.BufferData(BufferTarget.ArrayBuffer,
                    (uint)(vertices.Length * sizeof(float)),
                    (IntPtr)ptr,
                    BufferUsage.StaticDraw);
            }

            // Position
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 5 * sizeof(float), (IntPtr)0);

            // TexCoord
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 5 * sizeof(float), (IntPtr)(3 * sizeof(float)));

            Gl.BindVertexArray(0);
        }

        private void LoadShaders(string projPath)
        {
            _cullingCompute = new FrustumCullingComputeShader(projPath);
            _instancedShader = new GPUInstancedShader(projPath);
            _billboardShader = new GPUBillboardShader(projPath);
            _impostorShader = new ImpostorShader(projPath);
            _unlitShader = new UnlitShader(projPath);
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
            float spacing = 15f;
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
                float scale = 0.8f + (float)(rand.NextDouble() * 0.4);

                // 변환 행렬
                _transforms[i] = Matrix4x4f.Translated(posX, posY, posZ) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Scaled(scale, scale, scale);

                // 월드 공간 AABB
                _aabbs[i] = TransformAABB(_modelUnionAABB, _transforms[i]);
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

            // Update
            for (int i = 0; i < _treeModel.Length; i++)
            {
                UpdateIndirectBuffer(_indirectBuffers[i], _counterSSBO, _vertexCount[i]);
            }
            UpdateIndirectBuffer(_indirectBuffer_LOD1, _counterSSBO_LOD1, _billboardVertexCount);

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
                if (indices[i] < 0 || indices[i] >= 90000)
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
            Gl.Disable(EnableCap.Blend);  // 블렌딩 끄고 시작!
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.DepthMask(true);
            Gl.Enable(EnableCap.CullFace);  // 컬링도 켜기

            // ===== 중요: Compute Shader 완료 대기 =====
            Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

            // ===== LOD0: Tree Mesh =====
            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);
            
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);

            // Render
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _instancedShader.LoadTexture(TextureUnit.Texture0, _treeModel[i].Texture.TextureID);
                Gl.BindVertexArray(_vao[i]);
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
            }

            if (_isBillboard)
            {
                // ===== LOD1: Billboard =====
                Gl.DepthMask(true);  // Depth 쓰기 비활성화

                _billboardShader.Bind();
                _billboardShader.LoadVPMatrix(camera.VPMatrix);
                _billboardShader.LoadBillboardSize(BILLBOARD_SIZE);
                _billboardShader.LoadCameraVectors(camera.Position, camera.Right, camera.Up);
                _billboardShader.LoadTexture(TextureUnit.Texture0, _billboardTextureID);

                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO_LOD1);

                Gl.BindVertexArray(_billboardVAO);
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffer_LOD1);
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);

                // ===== 해제 =====
                Gl.Disable(EnableCap.PolygonOffsetFill);
                Gl.DepthFunc(DepthFunction.Less);  // 원래대로
                Gl.DepthMask(true);
                Gl.Disable(EnableCap.Blend);
            }

            // ===== 상태 복원 =====
            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);
            Gl.BindVertexArray(0);

        }

        private AABB CalculateLocalAABB(TexturedModel model)
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
            Gl.DeleteBuffers(_billboardVBO);
            Gl.DeleteVertexArrays(_billboardVAO);

            Console.WriteLine("GPU Culling Renderer disposed");
        }
    }
}