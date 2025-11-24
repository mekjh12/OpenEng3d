using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// GPU에서 View Frustum Culling을 수행하는 Compute Shader
    /// 90000개 인스턴스를 병렬로 처리하여 가시 객체만 필터링합니다.
    /// </summary>
    public class FrustumCullingComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\frustum_culling.comp";

        // SSBO 바인딩 포인트
        private const int AABB_BUFFER_BINDING = 0;
        private const int VISIBLE_INDICES_BINDING = 1;
        private const int COUNTER_BINDING = 2;

        // 유니폼 위치
        private int[] loc_frustumPlanes = new int[6];

        // Compute Shader 작업 그룹 크기
        private const int WORK_GROUP_SIZE = 256;

        public FrustumCullingComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < 6; i++)
            {
                loc_frustumPlanes[i] = GetUniformLocation($"uFrustumPlanes[{i}]");
            }
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// Frustum Plane을 설정합니다.
        /// </summary>
        public void LoadFrustumPlanes(Vertex4f[] planes)
        {
            if (planes.Length != 6)
                throw new ArgumentException("Frustum planes must be 6");

            for (int i = 0; i < 6; i++)
            {
                Gl.Uniform4f(loc_frustumPlanes[i], 1, planes[i]);
            }
        }

        public void LoadFrustumPlanes(Plane[] planes)
        {
            if (planes.Length != 6)
                throw new ArgumentException("Frustum planes must be 6");

            for (int i = 0; i < 6; i++)
            {
                Gl.Uniform4f(loc_frustumPlanes[i], 1, planes[i].Vertex4f);
            }
        }

        /// <summary>
        /// Compute Shader를 실행합니다.
        /// </summary>
        /// <param name="instanceCount">처리할 인스턴스 개수</param>
        public void Dispatch(int instanceCount)
        {
            // 작업 그룹 개수 계산 (올림)
            int numWorkGroups = (instanceCount + WORK_GROUP_SIZE - 1) / WORK_GROUP_SIZE;

            Bind();
            Gl.DispatchCompute((uint)numWorkGroups, 1, 1);

            // 메모리 배리어 - Compute 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit |
                            MemoryBarrierMask.CommandBarrierBit);
            Unbind();
        }
    }
}