using Common;
using OpenGL;

namespace Shader
{
    public class HiZOcclusionComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\hiz_occlusion.comp";

        private int[] loc_hizTextures;  // 각 mip level별 텍스처 location
        private int loc_viewProjMatrix;
        private int loc_viewMatrix;
        private int loc_maxMipLevel;
        private int loc_cameraPosition;
        private int loc_lodDistance;
        private int loc_screenSize;
        private int loc_maxInstanceCount;
        private int loc_cameraNear;
        private int loc_cameraFar;
        private int loc_isDebugMode;
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
            // 각 mip level별 텍스처 uniform location 가져오기
            loc_hizTextures = new int[_maxMipLevels];
            for (int i = 0; i < _maxMipLevels; i++)
            {
                loc_hizTextures[i] = GetUniformLocation($"u_hizTexture{i}");
            }

            loc_viewProjMatrix = GetUniformLocation("vp");
            loc_maxMipLevel = GetUniformLocation("u_maxMipLevel");
            loc_cameraPosition = GetUniformLocation("u_cameraPosition");
            loc_lodDistance = GetUniformLocation("u_lodDistance");
            loc_screenSize = GetUniformLocation("u_screenSize");
            loc_maxInstanceCount = GetUniformLocation("u_maxInstanceCount");
            loc_cameraNear = GetUniformLocation("u_cameraNear");
            loc_cameraFar = GetUniformLocation("u_cameraFar");
            loc_viewMatrix = GetUniformLocation("view");
            loc_isDebugMode = GetUniformLocation("u_isDebugMode");
        }

        public void LoadMaxMipLevel(int maxMipLevel)
        {
            Gl.Uniform1(loc_maxMipLevel, maxMipLevel);
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        public void LoadIsDebugMode(bool isDebug)
        {
            Gl.Uniform1(loc_isDebugMode, isDebug ? 1 : 0);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_viewMatrix, matrix);
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

        public void LoadVPMatrix(Matrix4x4f vpMatrix)
        {
            Gl.UniformMatrix4f(loc_viewProjMatrix, 1, false, vpMatrix);
        }

        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
        }

        public void LoadLODDistance(float distance)
        {
            Gl.Uniform1(loc_lodDistance, distance);
        }

        public void LoadScreenSize(int width, int height)
        {
            Gl.Uniform2(loc_screenSize, new float[] { width, height });
        }
    }
}