using Common;
using OpenGL;

namespace Shader
{
    public class HiZOcclusionComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\hiz_occlusion.comp";

        // 기존 Uniform 위치
        private int[] loc_hizTextures;
        private int loc_maxMipLevel;
        private int loc_cameraPosition;
        private int loc_screenSize;
        private int loc_maxInstanceCount;
        private int loc_cameraNear;
        private int loc_cameraFar;
        private int loc_isDebugMode;

        private int loc_distance0;
        private int loc_distance1;
        private int loc_distance2;
        private int loc_actualBatchCount;

        // ===== 기타 멤버 =====
        private int _maxMipLevels;

        public HiZOcclusionComputeShader(string projectPath, int maxMipLevels) : base()
        {
            _name = this.GetType().Name;
            _maxMipLevels = maxMipLevels;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // 각 mip level별 텍스처 uniform location
            loc_hizTextures = new int[_maxMipLevels];
            for (int i = 0; i < _maxMipLevels; i++)
            {
                loc_hizTextures[i] = GetUniformLocation($"u_hizTexture{i}");
            }

            // 기존 Uniform
            loc_maxMipLevel = GetUniformLocation("u_maxMipLevel");
            loc_cameraPosition = GetUniformLocation("u_cameraPosition");
            loc_screenSize = GetUniformLocation("u_screenSize");
            loc_maxInstanceCount = GetUniformLocation("u_maxInstanceCount");
            loc_cameraNear = GetUniformLocation("u_cameraNear");
            loc_cameraFar = GetUniformLocation("u_cameraFar");
            loc_isDebugMode = GetUniformLocation("u_isDebugMode");
            loc_actualBatchCount = GetUniformLocation("u_actualBatchCount");

            // 추가: 거리 임계값 Uniform
            loc_distance0 = GetUniformLocation("u_distance0");
            loc_distance1 = GetUniformLocation("u_distance1");
            loc_distance2 = GetUniformLocation("u_distance2");

        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        // ===== 기존 메서드 =====

        public void LoadMaxMipLevel(int maxMipLevel)
        {
            Gl.Uniform1(loc_maxMipLevel, maxMipLevel);
        }

        public void LoadIsDebugMode(bool isDebug)
        {
            Gl.Uniform1(loc_isDebugMode, isDebug ? 1 : 0);
        }

        public void LoadDistanceThresholds(float distance0 = 50.0f, float distance1 = 150.0f, float distance2 = 450.0f)
        {
            Gl.Uniform1(loc_distance0, distance0);
            Gl.Uniform1(loc_distance1, distance1);
            Gl.Uniform1(loc_distance2, distance2);
        }

        public void LoadCameraNearFar(float near, float far)
        {
            Gl.Uniform1(loc_cameraNear, near);
            Gl.Uniform1(loc_cameraFar, far);
        }

        public void LoadHiZTextures(uint[] textureIds)
        {
            // 각 mip level 텍스처 바인딩
            for (int i = 0; i < textureIds.Length && i < loc_hizTextures.Length; i++)
            {
                TextureUnit unit = TextureUnit.Texture0 + i;
                Gl.Uniform1(loc_hizTextures[i], i);
                Gl.ActiveTexture(unit);
                Gl.BindTexture(TextureTarget.Texture2d, textureIds[i]);
            }
        }

        public void LoadMaxInstanceCount(int maxInstanceCount)
        {
            Gl.Uniform1(loc_maxInstanceCount, maxInstanceCount);
        }

        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
        }

        public void LoadScreenSize(int width, int height)
        {
            Gl.Uniform2(loc_screenSize, new float[] { width, height });
        }

        public void LoadActualBatchCount(int count)
        {
            Gl.Uniform1(loc_actualBatchCount, count);            
        }
    }
}