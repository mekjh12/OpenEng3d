using Common;
using OpenGL;
using System;

namespace Shader
{
    public class FrustumCullingComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\frustum_culling.comp";

        private int[] loc_frustumPlanes = new int[6];

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

        public void LoadFrustumPlanes(Plane[] planes)
        {
            if (planes.Length != 6)
                throw new ArgumentException("Frustum planes must be 6");

            for (int i = 0; i < 6; i++)
            {
                Gl.Uniform4f(loc_frustumPlanes[i], 1, planes[i].Vertex4f);
            }
        }
    }
}