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

        // ===== 추가: Batch 관련 Uniform =====
        private int loc_batchLODs;
        private int loc_batchStarts;
        private int loc_batchCounts;

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

            // ===== 추가: Batch 관련 Uniform =====
            loc_batchLODs = GetUniformLocation("batchLODs");
            loc_batchStarts = GetUniformLocation("batchStarts");
            loc_batchCounts = GetUniformLocation("batchCounts");

            System.Console.WriteLine($"[{_name}] Uniform Locations:");
            System.Console.WriteLine($"  batchLODs: {loc_batchLODs}");
            System.Console.WriteLine($"  batchStarts: {loc_batchStarts}");
            System.Console.WriteLine($"  batchCounts: {loc_batchCounts}");
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

        // ===== 추가: Batch 관련 메서드 =====

        /// <summary>
        /// Batch별 LOD 거리 배열 로드
        /// </summary>
        public void LoadBatchLODs(float[] batchLODs)
        {
            if (batchLODs == null || batchLODs.Length == 0)
            {
                System.Console.WriteLine("Warning: Empty batchLODs array");
                return;
            }

            if (batchLODs.Length > 64)
            {
                throw new System.ArgumentException(
                    $"Max 64 batches supported, got {batchLODs.Length}");
            }

            if (loc_batchLODs < 0)
            {
                System.Console.WriteLine("Warning: batchLODs uniform not found");
                return;
            }

            Gl.Uniform1(loc_batchLODs, batchLODs);
        }

        /// <summary>
        /// Batch별 시작 인덱스 배열 로드
        /// </summary>
        public void LoadBatchStarts(uint[] batchStarts)
        {
            if (batchStarts == null || batchStarts.Length == 0)
            {
                System.Console.WriteLine("Warning: Empty batchStarts array");
                return;
            }

            if (batchStarts.Length > 64)
            {
                throw new System.ArgumentException(
                    $"Max 64 batches supported, got {batchStarts.Length}");
            }

            if (loc_batchStarts < 0)
            {
                System.Console.WriteLine("Warning: batchStarts uniform not found");
                return;
            }

            Gl.Uniform1(loc_batchStarts, batchStarts);
        }

        /// <summary>
        /// Batch별 인스턴스 개수 배열 로드
        /// </summary>
        public void LoadBatchCounts(uint[] batchCounts)
        {
            if (batchCounts == null || batchCounts.Length == 0)
            {
                System.Console.WriteLine("Warning: Empty batchCounts array");
                return;
            }

            if (batchCounts.Length > 64)
            {
                throw new System.ArgumentException(
                    $"Max 64 batches supported, got {batchCounts.Length}");
            }

            if (loc_batchCounts < 0)
            {
                System.Console.WriteLine("Warning: batchCounts uniform not found");
                return;
            }

            Gl.Uniform1(loc_batchCounts, batchCounts);
        }

    }
}