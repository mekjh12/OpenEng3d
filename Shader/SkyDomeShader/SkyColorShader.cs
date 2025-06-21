using System;
using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 하늘색 생성을 위한 컴퓨트 셰이더
    /// </summary>
    public class SkyColorShader : ShaderProgram<SkyColorShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 유니폼 변수 열거형
        /// </summary>
        public enum UNIFORM_NAME
        {
            // 태양 관련 유니폼
            sunPosition,          // 태양 위치 (정규화된 방향 벡터)
            sunColor,             // 태양 색상
            sunSize,              // 태양 크기
            sunGlowSize,          // 태양 주변 글로우 크기
            sunGlowStrength,      // 태양 주변 글로우 강도

            // 하늘색 관련 유니폼
            zenithColor,          // 천정 색상
            horizonColor,         // 지평선 색상
            skyGradientExponent,  // 하늘 그라데이션 지수

            // 일출/일몰 효과 관련 유니폼
            sunriseColor,         // 일출/일몰 색상
            sunriseIntensity,     // 일출/일몰 효과 강도

            // 대기 효과 관련 유니폼
            hazeFactor,           // 연무 효과
            atmosphericMie,       // 미 산란 강도

            // 총 유니폼 개수
            Count
        }

        // 컴퓨트 셰이더 파일 경로
        const string COMPUTE_FILE = @"\Shader\SkyDomeShader\skyColorGenerator.comp";

        // 텍스처 속성
        private uint _skyTextureId;
        private readonly int _texWidth;
        private readonly int _texHeight;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        public SkyColorShader(string projectPath, int width, int height) : base()
        {
            _name = this.GetType().Name;

            _texWidth = width;
            _texHeight = height;

            // 컴퓨트 셰이더 파일 경로 설정
            _compFilename = projectPath + COMPUTE_FILE;

            // 셰이더 초기화
            InitCompileShader();

            // 텍스처 초기화
            InitializeTexture();

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 BindAttributes가 필요 없음
        }

        protected override void GetAllUniformLocations()
        {
            // 유니폼 변수 이름을 이용하여 위치 찾기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        /// <summary>
        /// 텍스처 초기화 메서드
        /// </summary>
        private void InitializeTexture()
        {
            // 텍스처 생성
            _skyTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _skyTextureId);

            // 텍스처 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            // 텍스처 데이터 할당 (초기 값은 빈 상태)
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f,
                         _texWidth, _texHeight, 0,
                         OpenGL.PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        /// <summary>
        /// 기본 유니폼 값 설정
        /// </summary>
        private void SetDefaultUniforms()
        {
            Bind();

            // 태양 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunColor, new Vertex3f(1.0f, 0.95f, 0.8f));
            LoadUniform(UNIFORM_NAME.sunSize, 0.05f);
            LoadUniform(UNIFORM_NAME.sunGlowSize, 0.2f);
            LoadUniform(UNIFORM_NAME.sunGlowStrength, 0.5f);

            // 하늘색 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.zenithColor, new Vertex3f(0.3f, 0.5f, 0.9f));
            LoadUniform(UNIFORM_NAME.horizonColor, new Vertex3f(0.7f, 0.85f, 1.0f));
            LoadUniform(UNIFORM_NAME.skyGradientExponent, 1.0f);

            // 일출/일몰 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunriseColor, new Vertex3f(1.0f, 0.6f, 0.4f));
            LoadUniform(UNIFORM_NAME.sunriseIntensity, 1.0f);

            // 대기 효과 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.hazeFactor, 0.0f);
            LoadUniform(UNIFORM_NAME.atmosphericMie, 0.2f);

            Unbind();
        }

        /// <summary>
        /// 상반구 하늘 텍스처 렌더링
        /// </summary>
        /// <param name="sunPosition">태양 위치</param>
        public void RenderSkyTexture(uint outputTextureId, Vertex3f sunPosition)
        {
            Bind();

            // 태양 위치 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunPosition, sunPosition);

            // 이미지 바인딩
            Gl.BindImageTexture(0, outputTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute((uint)(_texWidth / 16) + 1, (uint)(_texHeight / 16) + 1, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}