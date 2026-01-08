using Common;
using OpenGL;

namespace Shader
{
    public class GrassCullingFinalizeComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\grass_culling_finalize.comp";

        private int loc_lodIndex;
        private int loc_grassPerTile;

        public GrassCullingFinalizeComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_lodIndex = GetUniformLocation("u_LODIndex");
            loc_grassPerTile = GetUniformLocation("u_GrassPerTile");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// LOD 인덱스 로드 (0, 1, 2)
        /// </summary>
        public void LoadLODIndex(int lodIndex)
        {
            Gl.Uniform1(loc_lodIndex, lodIndex);
        }

        /// <summary>
        /// 해당 LOD의 Grass Per Tile 로드
        /// </summary>
        public void LoadGrassPerTile(int count)
        {
            Gl.Uniform1(loc_grassPerTile, count);
        }

        public void Dispatch()
        {
            Gl.DispatchCompute(1, 1, 1);
        }
    }
}