using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 타일링 가능한 하이트맵 생성
    /// </summary>
    public class HeightmapGeneratorComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\Terrain\heightmap_gen.comp";

        // Uniform 위치 캐싱
        private int loc_scale;
        private int loc_octaves;
        private int loc_seed;

        public HeightmapGeneratorComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_scale = GetUniformLocation("scale");
            loc_octaves = GetUniformLocation("octaves");
            loc_seed = GetUniformLocation("seed");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 노이즈 스케일 설정
        /// </summary>
        public void SetScale(float scale)
        {
            Gl.Uniform1(loc_scale, scale);
        }

        /// <summary>
        /// 옥타브 수 설정
        /// </summary>
        public void SetOctaves(int octaves)
        {
            Gl.Uniform1(loc_octaves, octaves);
        }

        /// <summary>
        /// 랜덤 시드 설정
        /// </summary>
        public void SetSeed(int seed)
        {
            Gl.Uniform1(loc_seed, seed);
        }

        /// <summary>
        /// 모든 유니폼 한번에 설정
        /// </summary>
        public void LoadUniforms(float scale, int octaves, int seed)
        {
            SetScale(scale);
            SetOctaves(octaves);
            SetSeed(seed);
        }

        /// <summary>
        /// 이미지 버퍼 바인딩
        /// </summary>
        public void BindBuffers(uint heightmapTexture)
        {
            // binding = 0: heightmap (write only)
            Gl.BindImageTexture(0, heightmapTexture, 0, false, 0,
                               BufferAccess.WriteOnly, InternalFormat.Rgba32f);
        }

        /// <summary>
        /// Compute Shader Dispatch
        /// </summary>
        public void Dispatch(int width, int height)
        {
            // 16x16 워크그룹 기준  ← 주석 수정
            int groupsX = (width + 15) / 16;   // ← 15로 변경
            int groupsY = (height + 15) / 16;  // ← 15로 변경
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}