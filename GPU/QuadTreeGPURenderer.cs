using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Shader;
using System;
using Ui3d;
using ZetaExt;

namespace GPUDriven
{
    public unsafe class QuadTreeGPURenderer : IDisposable
    {
        // 클래스 상단에 구조체 추가
        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        private struct GPUParams
        {
            public uint VisibleLeafCount;   // 0-3
            public float LODDistance;       // 4-7
            public Vertex4f CameraPosition; // 8-23 (x,y,z,w)
            public Vertex4f Padding;        // 24-39 (추가 여유)
        }

        // 클래스 멤버 변경
        private GPUParams _paramsData;  // float[] 대신

        private const int MAX_INSTANCES = 90000;
        private const float LOD0_DISTANCE = 100f;

        private string _projPath;
        private string _modelName;

        // 모델 정보
        private TexturedModel[] _treeModel;
        private uint[] _vao;
        private uint[] _vertexCount;
        private AABB _modelAABB;

        // QuadTree
        private QuadTreeEx _quadTree;
        private QuadTreeGPUBuffers _quadTreeBuffers;

        // GPU 버퍼들
        private uint _transformSSBO;
        private uint _aabbSSBO;
        private uint _visibleIndicesSSBO;
        private uint _counterSSBO;
        private uint[] _indirectBuffers;
        private uint _paramsSSBO;

        // 인스턴스 데이터
        private Matrix4x4f[] _transforms;
        private AABB3f[] _aabbs;

        // 셰이더
        private QuadTreeCullingComputeShader _cullingCompute;
        private GPUInstancedShader _instancedShader;

        // 가시 리프 노드 ID
        private uint[] _visibleLeafIDs;
        private int _visibleLeafCount = 0;

        // 통계
        private int _frameCount = 0;

        public QuadTreeGPURenderer(string projPath)
        {
            _projPath = projPath;
            _transforms = new Matrix4x4f[MAX_INSTANCES];
            _aabbs = new AABB3f[MAX_INSTANCES];
        }

        public void Initialize(string modelName, TexturedModel[] treeModel)
        {
            _modelName = modelName;
            _treeModel = treeModel;

            Console.WriteLine("=== QuadTree GPU Renderer 초기화 시작 ===");

            SetupMeshVAO();
            CalculateModelAABB();
            GenerateInstancePositions();
            InitializeQuadTree();
            CreateSSBOs();
            LoadShaders();
            UploadToGPU();

            Console.WriteLine("=== QuadTree GPU Renderer 초기화 완료 ===");
            Console.WriteLine($"총 인스턴스: {MAX_INSTANCES:N0}");
            Console.WriteLine($"메시 파트: {_treeModel.Length}개");
        }

        private void SetupMeshVAO()
        {
            _vao = new uint[_treeModel.Length];
            _vertexCount = new uint[_treeModel.Length];

            for (int i = 0; i < _treeModel.Length; i++)
            {
                _vao[i] = _treeModel[i].VAO;
                _vertexCount[i] = (uint)_treeModel[i].VertexCount;
            }
        }

        private void CalculateModelAABB()
        {
            _modelAABB = new AABB(
                new Vertex3f(float.MaxValue),
                new Vertex3f(float.MinValue)
            );

            for (int i = 0; i < _treeModel.Length; i++)
            {
                AABB partAABB = CalculateAABB(_treeModel[i]);
                _modelAABB.Min = Vertex3f.Min(_modelAABB.Min, partAABB.Min);
                _modelAABB.Max = Vertex3f.Max(_modelAABB.Max, partAABB.Max);
            }
        }

        private AABB CalculateAABB(TexturedModel model)
        {
            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                min = Vertex3f.Min(min, model.Vertices[i]);
                max = Vertex3f.Max(max, model.Vertices[i]);
            }

            return new AABB(min, max);
        }

        private void GenerateInstancePositions()
        {
            int gridSize = 100;  // 100×100 = 10,000개
            float spacing = 5f;
            Random rand = new Random(42);

            Console.WriteLine($"  인스턴스 생성 범위:");
            float minPos = -(gridSize / 2) * spacing;
            float maxPos = (gridSize / 2) * spacing;
            Console.WriteLine($"    X: {minPos:F1} ~ {maxPos:F1}");
            Console.WriteLine($"    Y: {minPos:F1} ~ {maxPos:F1}");
            Console.WriteLine($"    중심에서 가장 먼 거리: 약 {Math.Sqrt(2) * (gridSize / 2) * spacing:F1}m");

            int actualCount = 0;
            for (int i = 0; i < MAX_INSTANCES && actualCount < gridSize * gridSize; i++)
            {
                int x = actualCount % gridSize;
                int y = actualCount / gridSize;

                float posX = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * 0.5 - 0.25);
                float posY = (y - gridSize / 2) * spacing + (float)(rand.NextDouble() * 0.5 - 0.25);
                float posZ = 0;
                float rotZ = (float)(rand.NextDouble() * Math.PI * 2);
                float scale = 0.99f + (float)(rand.NextDouble() * 0.02f);

                _transforms[i] = Matrix4x4f.Scaled(scale, scale, scale) *
                                Matrix4x4f.RotatedZ(rotZ.ToDegree()) *
                                Matrix4x4f.Translated(posX, posY, posZ);

                _aabbs[i] = TransformAABB(_modelAABB, _transforms[i]);

                actualCount++;
            }

            // 나머지는 원점에 배치 (렌더링 안 됨)
            for (int i = actualCount; i < MAX_INSTANCES; i++)
            {
                _transforms[i] = Matrix4x4f.Identity;
                _aabbs[i] = TransformAABB(_modelAABB, _transforms[i]);
            }

            Console.WriteLine($"  실제 생성: {actualCount:N0}개, 더미: {MAX_INSTANCES - actualCount:N0}개");
        }

        private void InitializeQuadTree()
        {
            AABB3f worldBounds = CalculateWorldBounds();
            float maxObjectSize = Math.Max(_modelAABB.SizeX, _modelAABB.SizeY) * 0.5f;

            _quadTree = new QuadTreeEx(worldBounds, maxObjectSize, maxDepth: 8, maxObjectsPerNode: 64);

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                _quadTree.Insert(_aabbs[i], i);
            }

            _quadTree.PrintStatistics();
            _quadTree.FinalizeForGPU();

            _quadTreeBuffers = new QuadTreeGPUBuffers();
            _quadTreeBuffers.Initialize(_quadTree);
        }

        private void UploadToGPU()
        {
            // Transform
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            fixed (Matrix4x4f* ptr = _transforms)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 64), (IntPtr)ptr);
            }

            // AABB
            GPUInstanceAABB[] gpuAABBs = new GPUInstanceAABB[MAX_INSTANCES];
            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                gpuAABBs[i] = new GPUInstanceAABB(_aabbs[i]);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            fixed (GPUInstanceAABB* ptr = gpuAABBs)
            {
                Gl.BufferSubData(BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero, (uint)(MAX_INSTANCES * 32), (IntPtr)ptr);
            }
        }

        private AABB3f CalculateWorldBounds()
        {
            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                min = Vertex3f.Min(min, _aabbs[i].Min);
                max = Vertex3f.Max(max, _aabbs[i].Max);
            }

            return new AABB3f(min, max);
        }

        private AABB3f TransformAABB(AABB local, Matrix4x4f transform)
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

            return new AABB3f(min, max);
        }

        private void CreateSSBOs()
        {
            _transformSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _transformSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 64), IntPtr.Zero, BufferUsage.StaticDraw);

            _aabbSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 32), IntPtr.Zero, BufferUsage.StaticDraw);

            _visibleIndicesSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleIndicesSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(MAX_INSTANCES * 4), IntPtr.Zero, BufferUsage.DynamicDraw);

            _counterSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 4,
                IntPtr.Zero, BufferUsage.DynamicDraw);

            _paramsSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _paramsSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, 64,
                IntPtr.Zero, BufferUsage.DynamicDraw);

            _indirectBuffers = new uint[_treeModel.Length];
            for (int i = 0; i < _treeModel.Length; i++)
            {
                _indirectBuffers[i] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                Gl.BufferData(BufferTarget.DrawIndirectBuffer, 16,
                    IntPtr.Zero, BufferUsage.DynamicDraw);
            }
        }

        private void LoadShaders()
        {
            _cullingCompute = new QuadTreeCullingComputeShader(_projPath);
            _instancedShader = new GPUInstancedShader(_projPath);
            _lastVisibleLeafIDs = new uint[MAX_INSTANCES];  // 추가
        }

        // QuadTreeGPURenderer.cs에 추가
        private uint[] _lastVisibleLeafIDs;
        private int _lastVisibleLeafCount = 0;

        public void Update(Camera camera, Polyhedron viewFrustum, Text2d text2d)
        {
            // 1. CPU Frustum Culling
            _visibleLeafIDs = _quadTree.CullByFrustum(viewFrustum.Planes, ref _visibleLeafCount);

            // 2. 변경 감지: 이전 프레임과 다를 때만 업로드!
            bool needsUpdate = false;

            if (_visibleLeafCount != _lastVisibleLeafCount)
            {
                needsUpdate = true;
            }
            else
            {
                // 개수가 같아도 내용이 다를 수 있음
                for (int i = 0; i < _visibleLeafCount; i++)
                {
                    if (_visibleLeafIDs[i] != _lastVisibleLeafIDs[i])
                    {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            // 3. 변경된 경우에만 업로드
            if (needsUpdate && _visibleLeafCount > 0)
            {
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _quadTreeBuffers.VisibleLeafIDsSSBO);
                unsafe
                {
                    fixed (uint* ptr = _visibleLeafIDs)
                    {
                        Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                            (uint)(_visibleLeafCount * sizeof(uint)), new IntPtr(ptr));
                    }
                }

                // 복사
                Array.Copy(_visibleLeafIDs, _lastVisibleLeafIDs, _visibleLeafCount);
                _lastVisibleLeafCount = _visibleLeafCount;
            }
            // 3. GPU Compute Shader
            RunComputeShader(camera, _visibleLeafCount);

            // 4. Indirect Buffers 업데이트
            UpdateIndirectBuffers();

            // 5. 통계 (60프레임마다만!)
            if (_frameCount % 60 == 0)
            {
                PrintRenderStatistics(text2d);
            }

            _frameCount++;
        }

        private void RunComputeShader(Camera camera, int visibleLeafCount)
        {
            if (visibleLeafCount == 0) return;

            // Counter 초기화
            uint zero = 0;
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, zero);

            // Params 업데이트
            _paramsData.VisibleLeafCount = (uint)visibleLeafCount;
            _paramsData.LODDistance = 30f;
            _paramsData.CameraPosition = new Vertex4f(
                camera.Position.x,
                camera.Position.y,
                camera.Position.z,
                0f
            );

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _paramsSSBO);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf<GPUParams>(), _paramsData);

            _cullingCompute.Bind();

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _aabbSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _visibleIndicesSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _counterSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _quadTreeBuffers.LeafObjectIDsSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _quadTreeBuffers.LeafInfoStartIndexSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _quadTreeBuffers.LeafInfoCountSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _quadTreeBuffers.VisibleLeafIDsSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _paramsSSBO);

            int workGroups = (visibleLeafCount + 255) / 256;
            Gl.DispatchCompute((uint)workGroups, 1, 1);

            // ✅ 필요한 배리어만 사용
            Gl.MemoryBarrier(
                MemoryBarrierMask.ShaderStorageBarrierBit |
                MemoryBarrierMask.CommandBarrierBit
            );


        }

        private void UpdateIndirectBuffers()
        {
            for (int i = 0; i < _treeModel.Length; i++)
            {
                UpdateIndirectBuffer(_indirectBuffers[i], _counterSSBO, _vertexCount[i]);
            }
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

        public void Render(Camera camera)
        {
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);
            Gl.Enable(EnableCap.CullFace);

            Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

            _instancedShader.Bind();
            _instancedShader.LoadVPMatrix(camera.VPMatrix);

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _transformSSBO);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _visibleIndicesSSBO);

            for (int i = 0; i < _treeModel.Length; i++)
            {
                if (_treeModel[i].Texture != null)
                {
                    //_instancedShader.LoadTexture(TextureUnit.Texture0, _treeModel[i].Texture.TextureID);
                }

                Gl.BindVertexArray(_vao[i]);
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectBuffers[i]);
                Gl.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);
            }

            _instancedShader.Unbind();
            Gl.BindVertexArray(0);
        }

        private void PrintRenderStatistics(Text2d text2d)
        {
            uint[] count = new uint[1];
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _counterSSBO);
            Gl.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 4, count);

            text2d.Text = $"[Frame {_frameCount}] 렌더링: {count[0]:N0} | " +
                         $"리프: {_quadTree.VisibleLeafCount:N0}/{_quadTree.TotalNodes:N0}";
        }

        public void Dispose()
        {
            Gl.DeleteBuffers(_transformSSBO);
            Gl.DeleteBuffers(_aabbSSBO);
            Gl.DeleteBuffers(_visibleIndicesSSBO);
            Gl.DeleteBuffers(_counterSSBO);
            Gl.DeleteBuffers(_paramsSSBO);
            Gl.DeleteBuffers(_indirectBuffers);

            _quadTreeBuffers?.Dispose();
        }
    }

    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct GPUInstanceAABB
    {
        public Vertex4f AABBMin;
        public Vertex4f AABBMax;

        public GPUInstanceAABB(AABB3f aabb)
        {
            AABBMin = new Vertex4f(aabb.Min.x, aabb.Min.y, aabb.Min.z, 0);
            AABBMax = new Vertex4f(aabb.Max.x, aabb.Max.y, aabb.Max.z, 0);
        }
    }
}