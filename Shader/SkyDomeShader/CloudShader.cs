using System;
using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 구름 생성을 위한 컴퓨트 셰이더
    /// </summary>
    public class CloudShader : ShaderProgramBase
    {
        // 컴퓨트 셰이더 파일 경로
        private const string COMPUTE_FILE = @"\Shader\SkyDomeShader\cloudGenerator.comp";

        // 텍스처 크기
        private readonly int _texWidth;
        private readonly int _texHeight;

        // 유니폼 위치 캐싱
        private int loc_sunPosition;          // 태양 위치 (정규화된 방향 벡터)
        private int loc_sunColor;             // 태양 색상
        private int loc_cloudCoverage;        // 구름 커버리지 (0.0 - 1.0)
        private int loc_cloudBaseAltitude;    // 구름 바닥 고도 (0.0 - 1.0, 0 = 지평선, 1 = 천정)
        private int loc_cloudTopAltitude;     // 구름 최상층 고도 (0.0 - 1.0)
        private int loc_cloudFeatheringDistance; // 구름 경계면 페더링 거리 (0.0 - 0.2)
        private int loc_cloudDensity;         // 구름 밀도 (0.0 - 2.0)
        private int loc_cloudDetail;          // 구름 디테일 수준 (0.0 - 2.0)
        private int loc_cloudOffset;          // 구름 오프셋 (구름 위치 조정)
        private int loc_time;                 // 시간 변수 (애니메이션용)
        private int loc_randomSeed;           // 랜덤 시드

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        public CloudShader(string projectPath, int width, int height) : base()
        {
            _name = this.GetType().Name;
            _texWidth = width;
            _texHeight = height;

            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 attribute 바인딩 불필요
        }

        protected override void GetAllUniformLocations()
        {
            // 태양 관련
            loc_sunPosition = GetUniformLocation("sunPosition");
            loc_sunColor = GetUniformLocation("sunColor");

            // 구름 형상 관련
            loc_cloudCoverage = GetUniformLocation("cloudCoverage");
            loc_cloudBaseAltitude = GetUniformLocation("cloudBaseAltitude");
            loc_cloudTopAltitude = GetUniformLocation("cloudTopAltitude");
            loc_cloudFeatheringDistance = GetUniformLocation("cloudFeatheringDistance");

            // 구름 품질 관련
            loc_cloudDensity = GetUniformLocation("cloudDensity");
            loc_cloudDetail = GetUniformLocation("cloudDetail");

            // 구름 애니메이션 관련
            loc_cloudOffset = GetUniformLocation("cloudOffset");
            loc_time = GetUniformLocation("time");
            loc_randomSeed = GetUniformLocation("randomSeed");
        }

        /// <summary>
        /// 기본 유니폼 값 설정
        /// </summary>
        private void SetDefaultUniforms()
        {
            Bind();

            // 태양 관련 기본값
            LoadSunColor(new Vertex3f(1.0f, 0.95f, 0.8f));

            // 구름 형상 관련 기본값
            LoadCloudCoverage(0.5f);
            LoadCloudBaseAltitude(0.1f);
            LoadCloudTopAltitude(0.3f);
            LoadCloudFeatheringDistance(0.03f);

            // 구름 품질 관련 기본값
            LoadCloudDensity(0.5f);
            LoadCloudDetail(1.0f);

            // 구름 애니메이션 관련 기본값
            LoadCloudOffset(new Vertex3f(0.0f, 0.0f, 0.0f));
            LoadTime(0.0f);
            LoadRandomSeed(new Vertex4f(0.123f, 0.456f, 0.789f, 0.0f));

            Unbind();
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 태양 위치 설정
        /// </summary>
        public void LoadSunPosition(Vertex3f position)
        {
            Gl.Uniform3(loc_sunPosition, position.x, position.y, position.z);
        }

        /// <summary>
        /// 태양 색상 설정
        /// </summary>
        public void LoadSunColor(Vertex3f color)
        {
            Gl.Uniform3(loc_sunColor, color.x, color.y, color.z);
        }

        /// <summary>
        /// 구름 커버리지 설정 (0.0 - 1.0)
        /// </summary>
        public void LoadCloudCoverage(float coverage)
        {
            Gl.Uniform1(loc_cloudCoverage, coverage);
        }

        /// <summary>
        /// 구름 바닥 고도 설정 (0.0 - 1.0)
        /// </summary>
        public void LoadCloudBaseAltitude(float altitude)
        {
            Gl.Uniform1(loc_cloudBaseAltitude, altitude);
        }

        /// <summary>
        /// 구름 상단 고도 설정 (0.0 - 1.0)
        /// </summary>
        public void LoadCloudTopAltitude(float altitude)
        {
            Gl.Uniform1(loc_cloudTopAltitude, altitude);
        }

        /// <summary>
        /// 구름 경계면 페더링 거리 설정 (0.0 - 0.2)
        /// </summary>
        public void LoadCloudFeatheringDistance(float distance)
        {
            Gl.Uniform1(loc_cloudFeatheringDistance, distance);
        }

        /// <summary>
        /// 구름 밀도 설정 (0.0 - 2.0)
        /// </summary>
        public void LoadCloudDensity(float density)
        {
            Gl.Uniform1(loc_cloudDensity, density);
        }

        /// <summary>
        /// 구름 디테일 수준 설정 (0.0 - 2.0)
        /// </summary>
        public void LoadCloudDetail(float detail)
        {
            Gl.Uniform1(loc_cloudDetail, detail);
        }

        /// <summary>
        /// 구름 오프셋 설정
        /// </summary>
        public void LoadCloudOffset(Vertex3f offset)
        {
            Gl.Uniform3(loc_cloudOffset, offset.x, offset.y, offset.z);
        }

        /// <summary>
        /// 시간 변수 설정 (애니메이션용)
        /// </summary>
        public void LoadTime(float time)
        {
            Gl.Uniform1(loc_time, time);
        }

        /// <summary>
        /// 랜덤 시드 설정
        /// </summary>
        public void LoadRandomSeed(Vertex4f seed)
        {
            Gl.Uniform4(loc_randomSeed, seed.x, seed.y, seed.z, seed.w);
        }

        /// <summary>
        /// 구름 텍스처 렌더링
        /// </summary>
        /// <param name="skyTextureId">입력 하늘색 텍스처 ID</param>
        /// <param name="finalTextureId">출력 최종 텍스처 ID</param>
        /// <param name="sunPosition">태양 위치</param>
        /// <param name="cloudCoverage">구름 커버리지 (0.0-1.0)</param>
        /// <param name="cloudBaseAltitude">구름 바닥 고도 (0.0-1.0)</param>
        /// <param name="cloudTopAltitude">구름 상단 고도 (0.0-1.0)</param>
        /// <param name="cloudOffset">구름 오프셋</param>
        public void RenderCloudTexture(
            uint skyTextureId,
            uint finalTextureId,
            Vertex3f sunPosition,
            float cloudCoverage = 0.5f,
            float cloudBaseAltitude = 0.1f,
            float cloudTopAltitude = 0.3f,
            Vertex3f? cloudOffset = null)
        {
            if (cloudOffset == null)
                cloudOffset = new Vertex3f(0.0f, 0.0f, 0.0f);

            Bind();

            // 태양 위치 설정
            LoadSunPosition(sunPosition);

            // 구름 형상 관련 유니폼 설정
            LoadCloudCoverage(cloudCoverage);
            LoadCloudBaseAltitude(cloudBaseAltitude);
            LoadCloudTopAltitude(cloudTopAltitude);

            // 구름 오프셋 설정
            LoadCloudOffset((Vertex3f)cloudOffset);

            // 이미지 바인딩
            Gl.BindImageTexture(0, skyTextureId, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
            Gl.BindImageTexture(1, finalTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute((uint)(_texWidth / 16) + 1, (uint)(_texHeight / 16) + 1, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}