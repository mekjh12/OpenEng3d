using Common;
using Common.Abstractions;
using OpenGL;
using System;

namespace Shader
{
    public class HiZOcclusionComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\hiz_occlusion.comp";

        private int loc_hizTexture;
        private int loc_viewProjMatrix;
        private int loc_viewMatrix;  // ✅ 추가
        private int loc_maxMipLevel;
        private int loc_cameraPosition;
        private int loc_lodDistance;
        private int loc_screenSize;
        private int loc_maxInstanceCount;
        private int loc_cameraNear;      // ✅ 추가
        private int loc_cameraFar;       // ✅ 추가


        public HiZOcclusionComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_hizTexture = GetUniformLocation("u_hizTexture");
            loc_viewProjMatrix = GetUniformLocation("vp");
            loc_maxMipLevel = GetUniformLocation("u_maxMipLevel");
            loc_cameraPosition = GetUniformLocation("u_cameraPosition");
            loc_lodDistance = GetUniformLocation("u_lodDistance");
            loc_screenSize = GetUniformLocation("u_screenSize");
            loc_maxInstanceCount = GetUniformLocation("u_maxInstanceCount");
            loc_cameraNear = GetUniformLocation("u_cameraNear");        // ✅ 추가
            loc_cameraFar = GetUniformLocation("u_cameraFar");          // ✅ 추가
            loc_viewMatrix = GetUniformLocation("view");  // ✅
        }


        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        public void LoadViewMatrix(Matrix4x4f matrix)  // ✅
        {
            LoadUniformMatrix4(loc_viewMatrix, matrix);
        }

        public void LoadCameraNearFar(float near, float far)
        {
            Gl.Uniform1(loc_cameraNear, near);
            Gl.Uniform1(loc_cameraFar, far);
        }

        public void LoadHiZTexture(TextureUnit unit, uint textureId)
        {
            // HiZ 텍스처 바인딩
            Gl.Uniform1(loc_hizTexture, (uint)unit);
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        public void LoadMaxInstanceCount(int maxInstanceCount)
        {
            Gl.Uniform1(loc_maxInstanceCount, maxInstanceCount);
        }

        public void LoadVPMatrix(Matrix4x4f vpMatrix)
        {
            Gl.UniformMatrix4f(loc_viewProjMatrix, 1, false, vpMatrix);
        }

        public void LoadMaxMipLevel(int maxLevel)
        {
            Gl.Uniform1(loc_maxMipLevel, maxLevel);
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