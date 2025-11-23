using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 대기 산란 효과를 렌더링하기 위한 버텍스+프래그먼트 셰이더
    /// </summary>
    public class AtmosphericRenderShader : ShaderProgram<AtmosphericRenderShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 버텍스 셰이더 유니폼
            model,
            view,
            proj,
            mvp,
            camPosMeter,

            // 프래그먼트 셰이더 유니폼
            R_e,                // 지구 반지름
            R_a,                // 대기 반지름
            beta_R,             // Rayleigh 산란 계수
            beta_M,             // Mie 산란 계수
            H_R,                // Rayleigh 스케일 높이
            H_M,                // Mie 스케일 높이
            g,                  // Mie 비대칭 계수
            sunPos,             // 태양 위치
            I_sun,              // 태양 강도
            viewSamples,        // 뷰 광선 샘플 수
            transmittanceLUT,   // 투과율 LUT 텍스처
            viewPos,            // 시점 위치 (camPos와 동일)
            toneMappingFactor,  // 톤 매핑 인자
                                
            sunDiskSize,        // 태양 디스크 크기 (각도)
            sunDiskColor,       // 태양 디스크 색상

            // 일출/일몰 효과 유니폼
            sunsetFactor,       // 일출/일몰 효과 강도
            sunsetColor,        // 일출/일몰 주 색상

            // 렌즈 플레어 유니폼
            enableLensFlare,
            lensFlareIntensity,
            lensFlareDispersal,
            lensFlareHaloWidth,
            lensFlareGhosts,

            // 글로우 효과 유니폼
            enableGlow,
            glowIntensity,
            glowSize,
            glowColor,

            // 구름 관련 유니폼
            cloudTexture,        // 3D 구름 텍스처
            cloudCoverage,       // 구름 커버리지 (0-1)
            cloudDensity,        // 구름 밀도
            cloudSpeed,          // 구름 이동 속도
            cloudHeight,         // 구름 높이 (km)
            cloudThickness,      // 구름 두께 (km)
            enableClouds,        // 구름 활성화 여부
            cloudBrightness,     // 구름 밝기
            cloudShadowStrength, // 구름 그림자 강도

            // 총 유니폼 개수
            Count
        }

        const string VERTEx_FILE = @"\Shader\AtmosphericScatteringLUT\atmosphere.vert";
        const string FRAGMENT_FILE = @"\Shader\AtmosphericScatteringLUT\atmosphere.frag";

        public AtmosphericRenderShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 버텍스/프래그먼트 셰이더만 설정
            VertFileName = projectPath + VERTEx_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }
    }
}