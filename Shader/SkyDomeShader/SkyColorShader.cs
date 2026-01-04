using OpenGL;
using System;
using Common;

namespace Shader
{
    /// <summary>
    /// 스카이 돔 색상 생성 컴퓨트 셰이더
    /// - 반구형 텍스처에 하늘색 그라데이션 생성
    /// - 태양 위치에 따른 일출/일몰 효과
    /// </summary>
    public class SkyColorShader : ShaderProgramBase
    {
        // 셰이더 파일 경로 상수 정의
        private const string COMPUTE_FILE = @"\Shader\SkyDomeShader\skyColorGenerator.comp";

        // 텍스처 크기
        private readonly int _texWidth;
        private readonly int _texHeight;

        // 유니폼 위치 캐싱
        private int loc_sunPosition;          // 태양 위치 (정규화된 방향 벡터)
        private int loc_sunColor;             // 태양 색상
        private int loc_sunSize;              // 태양 크기
        private int loc_sunGlowSize;          // 태양 주변 글로우 크기
        private int loc_sunGlowStrength;      // 태양 주변 글로우 강도

        private int loc_zenithColor;          // 천정 색상
        private int loc_horizonColor;         // 지평선 색상
        private int loc_skyGradientExponent;  // 하늘 그라데이션 지수

        private int loc_sunriseColor;         // 일출/일몰 색상
        private int loc_sunriseIntensity;     // 일출/일몰 효과 강도

        private int loc_hazeFactor;           // 연무 효과
        private int loc_atmosphericMie;       // 미 산란 강도

        public SkyColorShader(string projectPath, int width, int height) : base()
        {
            _name = this.GetType().Name;
            _texWidth = width;
            _texHeight = height;

            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
        }

        protected override void GetAllUniformLocations()
        {
            // 태양 관련
            loc_sunPosition = GetUniformLocation("sunPosition");
            loc_sunColor = GetUniformLocation("sunColor");
            loc_sunSize = GetUniformLocation("sunSize");
            loc_sunGlowSize = GetUniformLocation("sunGlowSize");
            loc_sunGlowStrength = GetUniformLocation("sunGlowStrength");

            // 하늘색 관련
            loc_zenithColor = GetUniformLocation("zenithColor");
            loc_horizonColor = GetUniformLocation("horizonColor");
            loc_skyGradientExponent = GetUniformLocation("skyGradientExponent");

            // 일출/일몰 효과
            loc_sunriseColor = GetUniformLocation("sunriseColor");
            loc_sunriseIntensity = GetUniformLocation("sunriseIntensity");

            // 대기 효과
            loc_hazeFactor = GetUniformLocation("hazeFactor");
            loc_atmosphericMie = GetUniformLocation("atmosphericMie");
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 attribute 바인딩 불필요
        }

        /// <summary>
        /// 기본 유니폼 값 설정
        /// </summary>
        private void SetDefaultUniforms()
        {
            Bind();

            // 태양 관련 기본값
            LoadSunColor(new Vertex3f(1.0f, 0.95f, 0.8f));
            LoadSunSize(0.05f);
            LoadSunGlowSize(0.2f);
            LoadSunGlowStrength(0.5f);

            // 하늘색 관련 기본값
            LoadZenithColor(new Vertex3f(0.3f, 0.5f, 0.9f));
            LoadHorizonColor(new Vertex3f(0.7f, 0.85f, 1.0f));
            LoadSkyGradientExponent(1.0f);

            // 일출/일몰 관련 기본값
            LoadSunriseColor(new Vertex3f(1.0f, 0.6f, 0.4f));
            LoadSunriseIntensity(1.0f);

            // 대기 효과 관련 기본값
            LoadHazeFactor(0.0f);
            LoadAtmosphericMie(0.2f);

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
        /// 태양 크기 설정
        /// </summary>
        public void LoadSunSize(float size)
        {
            Gl.Uniform1(loc_sunSize, size);
        }

        /// <summary>
        /// 태양 글로우 크기 설정
        /// </summary>
        public void LoadSunGlowSize(float size)
        {
            Gl.Uniform1(loc_sunGlowSize, size);
        }

        /// <summary>
        /// 태양 글로우 강도 설정
        /// </summary>
        public void LoadSunGlowStrength(float strength)
        {
            Gl.Uniform1(loc_sunGlowStrength, strength);
        }

        /// <summary>
        /// 천정 색상 설정
        /// </summary>
        public void LoadZenithColor(Vertex3f color)
        {
            Gl.Uniform3(loc_zenithColor, color.x, color.y, color.z);
        }

        /// <summary>
        /// 지평선 색상 설정
        /// </summary>
        public void LoadHorizonColor(Vertex3f color)
        {
            Gl.Uniform3(loc_horizonColor, color.x, color.y, color.z);
        }

        /// <summary>
        /// 하늘 그라데이션 지수 설정
        /// </summary>
        public void LoadSkyGradientExponent(float exponent)
        {
            Gl.Uniform1(loc_skyGradientExponent, exponent);
        }

        /// <summary>
        /// 일출/일몰 색상 설정
        /// </summary>
        public void LoadSunriseColor(Vertex3f color)
        {
            Gl.Uniform3(loc_sunriseColor, color.x, color.y, color.z);
        }

        /// <summary>
        /// 일출/일몰 효과 강도 설정
        /// </summary>
        public void LoadSunriseIntensity(float intensity)
        {
            Gl.Uniform1(loc_sunriseIntensity, intensity);
        }

        /// <summary>
        /// 연무 효과 설정
        /// </summary>
        public void LoadHazeFactor(float factor)
        {
            Gl.Uniform1(loc_hazeFactor, factor);
        }

        /// <summary>
        /// 미 산란 강도 설정
        /// </summary>
        public void LoadAtmosphericMie(float mie)
        {
            Gl.Uniform1(loc_atmosphericMie, mie);
        }

        /// <summary>
        /// 스카이 텍스처 생성 (컴퓨트 셰이더 실행)
        /// </summary>
        /// <param name="outputTextureId">출력 텍스처 ID</param>
        /// <param name="sunDirection">태양 위치</param>
        public void GenerateSkyTexture(uint outputTextureId, Vertex3f sunDirection)
        {
            Bind();

            // 태양 위치 설정
            LoadSunPosition(sunDirection);

            // 출력 이미지 바인딩
            Gl.BindImageTexture(0, outputTextureId, 0, false, 0,
                BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 컴퓨트 셰이더 실행
            uint groupsX = (uint)((_texWidth + 15) / 16);
            uint groupsY = (uint)((_texHeight + 15) / 16);
            Gl.DispatchCompute(groupsX, groupsY, 1);

            // 메모리 배리어 (쓰기 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}