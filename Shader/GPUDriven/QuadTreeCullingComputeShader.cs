using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// QuadTree 기반 GPU Culling을 수행하는 Compute Shader
    /// CPU가 선별한 가시 리프 노드의 인스턴스만 병렬 처리합니다.
    /// </summary>
    public class QuadTreeCullingComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\quadtree_culling.comp";

        public QuadTreeCullingComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // Params는 SSBO로 전달하므로 유니폼 불필요
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }
    }
}