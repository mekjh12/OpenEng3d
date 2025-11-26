using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// Hierarchical Z-Buffer (Hi-Z) 생성을 위한 Compute Shader
    /// 깊이 버퍼의 밉맵 레벨을 생성하여 occlusion culling에 사용합니다.
    /// </summary>
    public class HiZComputeShader : ShaderProgramBase
    {
        const string COMPUTE_FILE = @"\Shader\ComputeShader\hiz.comp";

        // 유니폼 위치 (캐싱)
        private int loc_inputDepth;
        private int loc_outputDepth;
        private int loc_currentLevel;

        // Compute Shader 작업 그룹 크기 (일반적으로 8x8 또는 16x16)
        private const int WORK_GROUP_SIZE = 8;

        public HiZComputeShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_inputDepth = GetUniformLocation("InputDepth");
            loc_outputDepth = GetUniformLocation("OutputDepth");
            loc_currentLevel = GetUniformLocation("CurrentLevel");
        }

        protected override void BindAttributes()
        {
            // Compute Shader는 애트리뷰트 불필요
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 입력 깊이 텍스처 설정 (읽기용)
        /// </summary>
        public void LoadInputDepth(int textureUnit)
        {
            Gl.Uniform1i(loc_inputDepth, 1, textureUnit);
        }

        /// <summary>
        /// 출력 깊이 텍스처 설정 (쓰기용, Image Load/Store)
        /// </summary>
        public void LoadOutputDepth(int imageUnit)
        {
            Gl.Uniform1i(loc_outputDepth, 1, imageUnit);
        }

        /// <summary>
        /// 현재 처리 중인 밉맵 레벨 설정
        /// </summary>
        public void LoadCurrentLevel(int level)
        {
            Gl.Uniform1i(loc_currentLevel, 1, level);
        }
    }
}