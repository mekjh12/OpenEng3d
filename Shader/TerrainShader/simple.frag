#version 430 core

#include "./../includes/lib_fog_effect.glsl"
#include "./../includes/lib_terrain_texturing.glsl"

// 출력 컬러 버퍼 설정
layout(location = 0) out vec4 FragColor;

// 버텍스 셰이더로부터의 입력
in vec2 Tex3;         // 텍스처 좌표
in float Height;      // 프래그먼트의 높이값
in vec4 viewPos;      // 뷰 공간 위치
in vec4 fragPos;      // 월드 공간 위치

// 높이별 지형 텍스처
uniform sampler2D gTextureHeight0;    // 최하단 높이 텍스처
uniform sampler2D gTextureHeight1;    // 하단 높이 텍스처
uniform sampler2D gTextureHeight2;    // 중단 높이 텍스처
uniform sampler2D gTextureHeight3;    // 상단 높이 텍스처
uniform sampler2D gTextureHeight4;    // 최상단 높이 텍스처

// 지형 맵 텍스처
uniform sampler2D gHeightMap;         // 높이 맵
uniform sampler2D gDetailMap;         // 디테일 맵
uniform bool gIsDetailMap;            // 디테일 맵 사용 여부
uniform sampler2D gVegetationMap;     // RGBA 채널 (R: 나무, G: 풀, B: 잡목, A: 예비)

// 높이 구간 경계값 (0.0 ~ 1.0 범위)
uniform float gHeight0 = 0.07f;       // 최하단 경계
uniform float gHeight1 = 0.15f;       // 하단 경계
uniform float gHeight2 = 0.25f;       // 중단 경계
uniform float gHeight3 = 0.71f;       // 상단 경계
uniform float gHeight4 = 0.82f;       // 최상단 경계
uniform float gHeight5 = 1.00f;       // 최대 높이

// 렌더링 파라미터
uniform vec3 gReversedLightDir;           // 반전된 빛의 방향
uniform float gColorTexcoordScaling = 800.0f;  // 텍스처 타일링 배율
uniform vec3 camPos;                      // 카메라 위치

// 안개 관련 uniform 블록
layout(std140) uniform HalfFogUniforms
{
    vec4  halfFogPlane;         // 0..15
    vec3  halfFogColor;         // 16..31 (실제로 16바이트)
    float padding1;             // 28-31 바이트 (vec3 패딩)
    float halfFogDensity;       // 32..35
    int   isHalfFogEnabled;     // 36..39
    float padding2;             // 4 바이트
    float padding3;             // 4 바이트 48바이트
};

// 두 높이맵을 블렌딩하여 지형의 법선 벡터를 계산하는 함수
vec3 CalcSimpleTerrainNormal(sampler2D lowResMap, vec2 texCoord)
{
    // 저해상도 높이맵에서 샘플링
    float left   = textureOffset(lowResMap, texCoord, ivec2(-1, 0)).r;
    float right  = textureOffset(lowResMap, texCoord, ivec2(1, 0)).r;
    float up     = textureOffset(lowResMap, texCoord, ivec2(0, 1)).r;
    float down   = textureOffset(lowResMap, texCoord, ivec2(0, -1)).r;
    
    // 법선 벡터 계산
    return normalize(vec3(left - right, up - down, 0.01f));
}


//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
    // 1. 지형 텍스처 블렌딩
    vec4 TexColor = BlendTerrainTextures(
        Height, Tex3, gColorTexcoordScaling,
        gTextureHeight0, gTextureHeight1, gTextureHeight2, 
        gTextureHeight3, gTextureHeight4, gDetailMap,
        gIsDetailMap, gHeight0, gHeight1, gHeight2, gHeight3, gHeight4
    );   
   
    // 현재 프래그먼트의 법선 벡터 계산
    vec3 Normal = CalcSimpleTerrainNormal(gHeightMap, Tex3);
    // 법선 벡터와 빛의 방향 벡터의 내적을 통해 디퓨즈 조명 강도 계산
    float Diffuse = dot(Normal, normalize(gReversedLightDir));
    // 완전한 어두움을 방지하기 위해 최소 조명값(0.5)을 설정
    // 이는 환경광(Ambient Light)처럼 작용하여 그림자 부분도 어느 정도 보이게 함
    Diffuse = max(0.5f, Diffuse);
    
    // 조명 적용된 색상 계산
    vec3 shadedColor = Diffuse * TexColor.rgb;
    
    // 안개 효과가 비활성화된 경우
    if (isHalfFogEnabled == 0)
    {
        FragColor = vec4(shadedColor, 1.0f);
        return;
    }
    
    // 안개 효과 적용 (lib_fog_effect.glsl의 함수 사용)
    vec3 finalWithFog = CalculateAndApplyFog(
        shadedColor,
        halfFogColor,
        halfFogDensity,
        camPos,
        fragPos.xyz,
        halfFogPlane
    );
    
    FragColor = vec4(finalWithFog, 1.0);

    // 디버깅용
    //if (Tex3.x > 0.9f) { FragColor = FragColor * vec4(1,0,0,1); }
    //if (Tex3.x < 0.1f) { FragColor = FragColor * vec4(0,1,0,1); }
    //if (Tex3.y < 0.1f) { FragColor = FragColor * vec4(1,1,0,1); }
    //if (Tex3.y > 0.9f) { FragColor = FragColor * vec4(1,0,1,1); }
}