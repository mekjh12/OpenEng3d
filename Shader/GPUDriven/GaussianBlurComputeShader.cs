using Common;
using OpenGL;
using System;

namespace Shader
{
    public class GaussianBlurComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\GPUDriven\comp\gaussian_blur.comp";

        private int loc_width;
        private int loc_height;
        private int loc_blurStrength;  // ⭐ 추가

        public GaussianBlurComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_width = GetUniformLocation("u_Width");
            loc_height = GetUniformLocation("u_Height");
            loc_blurStrength = GetUniformLocation("u_BlurStrength");  // ⭐ 추가
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        /// <summary>
        /// 파라미터 로드
        /// </summary>
        public void LoadParams(int width, int height, float blurStrength = 0.5f)
        {
            LoadUniform1i(loc_width, width);
            LoadUniform1i(loc_height, height);
            LoadUniform1f(loc_blurStrength, blurStrength);  // ⭐ 추가
        }

        public void Dispatch(int width, int height)
        {
            int groupsX = (width + 15) / 16;
            int groupsY = (height + 15) / 16;
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        }
    }
}