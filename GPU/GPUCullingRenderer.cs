using Common;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUDriven
{
    public unsafe class GPUCullingRenderer : IDisposable
    {
        private const int MAx_INSTANCES = 90000;

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
        private ShaderProgramBase _cullingCompute;
        private ShaderProgramBase _renderShader;

        public GPUCullingRenderer()
        {
            _transforms = new Matrix4x4f[MAx_INSTANCES];
            _aabbs = new AABB[MAx_INSTANCES];
        }

        public void Initialize(TexturedModel treeModel)
        {
            _treeModel = treeModel;

            // 1. 메시 VAO 준비
            SetupMeshVAO();

            // 2. 90000개 위치 생성
            GenerateInstancePositions();

            // 3. SSBO 생성
            CreateSSBOs();

            // 4. 셰이더 로드
            LoadShaders();

            // 5. GPU에 데이터 업로드
            UploadToGPU();
        }

        private void SetupMeshVAO()
        {
            // 나무 메시의 VAO 가져오기
            _vao = _treeModel
            _vertexCount = (uint)_treeModel.VertexCount;

            Console.WriteLine($"Tree mesh loaded: {_vertexCount} vertices");
        }

        private void GenerateInstancePositions()
        {
            // 300x300 그리드로 배치 (간격 10미터)
            int gridSize = 300;
            float spacing = 10f;

            Random rand = new Random(42);

            // 나무 메시의 AABB (로컬 공간)
            AABB localAABB = CalculateLocalAABB(_treeModel);

            for (int i = 0; i < MAx_INSTANCES; i++)
            {
                int x = i % gridSize;
                int z = i / gridSize;

                // 위치 (약간의 랜덤 오프셋)
                float posx = (x - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);
                float posY = 0;
                float posZ = (z - gridSize / 2) * spacing + (float)(rand.NextDouble() * 2 - 1);

                // 랜덤 회전 (Y축만)
                float rotY = (float)(rand.NextDouble() * Math.PI * 2);

                // 랜덤 스케일 (0.8 ~ 1.2)
                float scale = 0.8f + (float)(rand.NextDouble() * 0.4);

                // 변환 행렬
                _transforms[i] = Matrix4x4f.Scaled(scale, scale, scale) *
                               Matrix4x4f.RotatedY(rotY) *
                               Matrix4x4f.Translated(posx, posY, posZ);

                // 월드 공간 AABB 계산
                _aabbs[i] = TransformAABB(localAABB, _transforms[i]);
            }

            Console.WriteLine($"Generated {MAx_INSTANCES} tree instances");
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
            corners[0] = new Vertex3f(local.Min.x, local.Min.Y, local.Min.Z);
            corners[1] = new Vertex3f(local.Max.x, local.Min.Y, local.Min.Z);
            corners[2] = new Vertex3f(local.Min.x, local.Max.Y, local.Min.Z);
            corners[3] = new Vertex3f(local.Max.x, local.Max.Y, local.Min.Z);
            corners[4] = new Vertex3f(local.Min.x, local.Min.Y, local.Max.Z);
            corners[5] = new Vertex3f(local.Max.x, local.Min.Y, local.Max.Z);
            corners[6] = new Vertex3f(local.Min.x, local.Max.Y, local.Max.Z);
            corners[7] = new Vertex3f(local.Max.x, local.Max.Y, local.Max.Z);

            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            for (int i = 0; i < 8; i++)
            {
                Vertex3f transformed = Vertex3f.Transform(corners[i], transform);
                min = Vertex3f.Min(min, transformed);
                max = Vertex3f.Max(max, transformed);
            }

            return new AABB(min, max);
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
