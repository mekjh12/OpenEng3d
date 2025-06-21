#version 430 core
//-----------------------------------------------------------------------------
// 지형 렌더링을 위한 프래그먼트 셰이더
// - 높이 기반 텍스처 블렌딩
// - 노멀 맵 기반 조명
// - 반평면 안개 효과
//-----------------------------------------------------------------------------

// 공통 헤더 포함
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
uniform sampler2D gDetailMap;         // 디테일 맵
uniform bool gIsDetailMap;            // 디테일 맵 사용 여부

// 지형 높이맵 텍스처
uniform sampler2D heightMapLowRes;  // 저해상도 높이맵
uniform sampler2D heightMapHighRes; // 고해상도 높이맵
uniform float blendFactor;          // 블렌딩 계수 (0.0-1.0)

// 높이 구간 경계값
uniform float gHeight0 = 0.07f;       // 최하단 경계
uniform float gHeight1 = 0.15f;       // 하단 경계
uniform float gHeight2 = 0.25f;       // 중단 경계
uniform float gHeight3 = 0.71f;       // 상단 경계
uniform float gHeight4 = 0.82f;       // 최상단 경계

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
vec3 CalcTerrainNormal(sampler2D lowResMap, sampler2D highResMap, vec2 texCoord, float blendFactor)
{
    // 저해상도 높이맵에서 샘플링
    float leftLow   = textureOffset(lowResMap, texCoord, ivec2(-1, 0)).r;
    float rightLow  = textureOffset(lowResMap, texCoord, ivec2(1, 0)).r;
    float upLow     = textureOffset(lowResMap, texCoord, ivec2(0, 1)).r;
    float downLow   = textureOffset(lowResMap, texCoord, ivec2(0, -1)).r;
    
    // 고해상도 높이맵에서 샘플링
    float leftHigh  = textureOffset(highResMap, texCoord, ivec2(-1, 0)).r;
    float rightHigh = textureOffset(highResMap, texCoord, ivec2(1, 0)).r;
    float upHigh    = textureOffset(highResMap, texCoord, ivec2(0, 1)).r;
    float downHigh  = textureOffset(highResMap, texCoord, ivec2(0, -1)).r;
    
    // 두 높이맵 사이 보간
    float left  = mix(leftLow, leftHigh, blendFactor);
    float right = mix(rightLow, rightHigh, blendFactor);
    float up    = mix(upLow, upHigh, blendFactor);
    float down  = mix(downLow, downHigh, blendFactor);
    
    // 법선 벡터 계산
    return normalize(vec3(left - right, up - down, 0.01f));
}

//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
    // 지형 텍스처 블렌딩
    vec4 TexColor = BlendTerrainTextures(
        Height, Tex3, gColorTexcoordScaling,
        gTextureHeight0, gTextureHeight1, gTextureHeight2, 
        gTextureHeight3, gTextureHeight4, gDetailMap,
        gIsDetailMap, gHeight0, gHeight1, gHeight2, gHeight3, gHeight4
    );
   
    // 노멀 계산 및 조명 적용 - 수정된 부분
    vec3 Normal = CalcTerrainNormal(heightMapLowRes, heightMapHighRes, Tex3, blendFactor);
    float Diffuse = max(0.5f, dot(Normal, normalize(gReversedLightDir)));
    vec3 shadedColor = Diffuse * TexColor.rgb;

    // 안개 효과가 비활성화된 경우
    if (isHalfFogEnabled == 0)
    {
        FragColor = vec4(shadedColor, 1.0f);
        return;
    }
    
    // 안개 효과 적용 (리팩토링됨: lib_fog_effect.glsl 사용)
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
    //if (Tex3.x > 0.999f) { FragColor = FragColor * vec4(1,0,0,1); }
    //if (Tex3.x < 0.001f) { FragColor = FragColor * vec4(0,1,0,1); }
    //if (Tex3.y < 0.001f) { FragColor = FragColor * vec4(1,1,0,1); }
    //if (Tex3.y > 0.999f) { FragColor = FragColor * vec4(1,0,1,1); }
}