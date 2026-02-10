using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 타일링 가능한 하이트맵 생성 컴퓨트 셰이더
    /// </summary>
    public class HeightmapGeneratorComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\Terrain\heightmap_gen.comp";

        // Uniform 위치 캐싱
        private int loc_scale;
        private int loc_octaves;
        private int loc_seed;
        private int loc_baseHeightmap;
        private int loc_mode;
        private int loc_blurPass;
        private int loc_gaussianSigma;
        private int loc_blurRadius;

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
            loc_baseHeightmap = GetUniformLocation("baseHeightmap");
            loc_mode = GetUniformLocation("mode");
            loc_blurPass = GetUniformLocation("blurPass");
            loc_gaussianSigma = GetUniformLocation("gaussianSigma");
            loc_blurRadius = GetUniformLocation("blurRadius");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 블러 방향 설정 (0=수평, 1=수직)
        /// </summary>
        public void SetBlurPass(int pass)
        {
            Gl.Uniform1(loc_blurPass, pass);
        }

        /// <summary>
        /// 처리 모드 설정
        /// 0: Bilinear 업스케일
        /// 1: Bicubic 보간
        /// 2: Gaussian 블러
        /// </summary>
        public void SetMode(int mode)
        {
            Gl.Uniform1(loc_mode, mode);
        }

        /// <summary>
        /// Gaussian 블러 시그마 설정 (기본값: 3.0)
        /// </summary>
        public void SetGaussianSigma(float sigma)
        {
            Gl.Uniform1(loc_gaussianSigma, sigma);
        }

        /// <summary>
        /// 블러 반경 설정 (기본값: 8)
        /// </summary>
        public void SetBlurRadius(int radius)
        {
            Gl.Uniform1(loc_blurRadius, radius);
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
        /// 기본 높이맵 텍스처 바인딩 (binding = 2)
        /// </summary>
        public void LoadBaseHeightmap(uint texture)
        {
            Gl.Uniform1(loc_baseHeightmap, 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>
        /// 이미지 버퍼 바인딩
        /// binding 0: readBuffer (ReadOnly)
        /// binding 1: writeBuffer (WriteOnly)
        /// </summary>
        public void BindBuffers(uint readBuffer, uint writeBuffer)
        {
            // ✅ ReadOnly로 수정
            Gl.BindImageTexture(0, readBuffer, 0, false, 0,
                               BufferAccess.ReadOnly, InternalFormat.Rgba32f);

            Gl.BindImageTexture(1, writeBuffer, 0, false, 0,
                               BufferAccess.WriteOnly, InternalFormat.Rgba32f);
        }

        /// <summary>
        /// Compute Shader Dispatch
        /// 16x16 워크그룹 기준으로 그룹 수 계산
        /// </summary>
        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}