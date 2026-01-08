using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// Grass Frustum Culling Compute Shader
    /// </summary>
    public class GrassCullingComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\grass_culling.comp";

        private int[] loc_frustumPlanes = new int[6];
        private int loc_candidateCount;
        private int loc_tileSize;
        private int loc_cameraPos;
        private int loc_lod0Distance;
        private int loc_lod1Distance;
        private int loc_lod2Distance;

        public GrassCullingComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // Frustum Planes (6개)
            for (int i = 0; i < 6; i++)
            {
                loc_frustumPlanes[i] = GetUniformLocation($"uFrustumPlanes[{i}]");
            }

            // 기타 Uniforms
            loc_candidateCount = GetUniformLocation("u_CandidateCount");
            loc_tileSize = GetUniformLocation("u_TileSize");
            loc_cameraPos = GetUniformLocation("u_CameraPos");
            loc_lod0Distance = GetUniformLocation("u_LOD0Distance");
            loc_lod1Distance = GetUniformLocation("u_LOD1Distance");
            loc_lod2Distance = GetUniformLocation("u_LOD2Distance");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 카메라 위치 로드 (LOD 계산용)
        /// </summary>
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3(loc_cameraPos, position.x, position.y, position.z);
        }

        /// <summary>
        /// LOD 거리 설정 로드
        /// </summary>
        public void LoadLODDistances(float lod0, float lod1, float lod2)
        {
            Gl.Uniform1(loc_lod0Distance, lod0);
            Gl.Uniform1(loc_lod1Distance, lod1);
            Gl.Uniform1(loc_lod2Distance, lod2);
        }

        /// <summary>
        /// Frustum Planes 로드
        /// </summary>
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
        /// Candidate Count 로드
        /// </summary>
        public void LoadCandidateCount(int count)
        {
            Gl.Uniform1(loc_candidateCount, count);
        }

        /// <summary>
        /// Tile Size 로드
        /// </summary>
        public void LoadTileSize(float size)
        {
            Gl.Uniform1(loc_tileSize, size);
        }

        /// <summary>
        /// Compute Shader Dispatch
        /// </summary>
        public void Dispatch(int candidateCount)
        {
            int numGroups = (candidateCount + 63) / 64;  // 64개씩 처리
            Gl.DispatchCompute((uint)numGroups, 1, 1);
        }
    }
}