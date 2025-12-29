#version 430 core

#include "./../includes/lib_terrain_texturing.glsl"

// ✅ MRT 출력
layout(location = 0) out vec4 fragColor;
layout(location = 1) out float fragDepth;

// 입력
in vec2 Tex3;         // 텍스처 좌표
in float Height;      // 높이값
in vec4 viewPos;      // 뷰 공간 위치
in vec4 fragPos;      // 월드 공간 위치

// ✅ 라이팅 UBO (binding = 0)
layout(std140, binding = 0) uniform LightingBlock
{
    vec3 ambientColor;      // offset 0
    vec3 lightDirection;    // offset 16
    vec3 lightColor;        // offset 32
} lighting;

// 높이별 지형 텍스처
uniform sampler2D gTextureHeight0;
uniform sampler2D gTextureHeight1;
uniform sampler2D gTextureHeight2;
uniform sampler2D gTextureHeight3;
uniform sampler2D gTextureHeight4;

// 지형 맵
uniform sampler2D gHeightMap;
uniform sampler2D gDetailMap;
uniform bool gIsDetailMap;

// 높이 구간 경계값
uniform float gHeight0 = 0.07f;
uniform float gHeight1 = 0.15f;
uniform float gHeight2 = 0.25f;
uniform float gHeight3 = 0.71f;
uniform float gHeight4 = 0.82f;

// 렌더링 파라미터
uniform float gColorTexcoordScaling = 800.0f;
uniform vec3 camPos;

// ✅ 간소화된 안개 시스템 (선택사항 - 필요 없으면 제거)
uniform bool isFogEnabled = false;
uniform vec3 fogColor = vec3(0.7, 0.8, 0.9);
uniform float fogDensity = 0.0001;

//-----------------------------------------------------------------------------
// 지형 노멀 계산
//-----------------------------------------------------------------------------
vec3 CalcTerrainNormal(sampler2D heightMap, vec2 texCoord)
{
    float left   = textureOffset(heightMap, texCoord, ivec2(-1, 0)).r;
    float right  = textureOffset(heightMap, texCoord, ivec2(1, 0)).r;
    float up     = textureOffset(heightMap, texCoord, ivec2(0, 1)).r;
    float down   = textureOffset(heightMap, texCoord, ivec2(0, -1)).r;

    return normalize(vec3(left - right, down - up, 0.1));
}

//-----------------------------------------------------------------------------
// 간단한 거리 기반 안개 (선택사항)
//-----------------------------------------------------------------------------
vec3 ApplySimpleFog(vec3 color, vec3 fragWorldPos, vec3 cameraPos)
{
    float distance = length(cameraPos - fragWorldPos);
    float fogFactor = exp(-fogDensity * distance);
    return mix(fogColor, color, fogFactor);
}

//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
    // 1. 지형 텍스처 블렌딩
    vec4 texColor = BlendTerrainTextures(
        Height, Tex3, gColorTexcoordScaling,
        gTextureHeight0, gTextureHeight1, gTextureHeight2, 
        gTextureHeight3, gTextureHeight4, gDetailMap,
        gIsDetailMap, gHeight0, gHeight1, gHeight2, gHeight3, gHeight4
    );
    
    // 2. 노멀 계산
    vec3 normal = CalcTerrainNormal(gHeightMap, Tex3);
    
    // 3. ✅ UBO 라이팅 적용
    // Ambient
    vec3 ambient = lighting.ambientColor;
    
    // Directional Light (Lambert Diffuse)
    float diff = max(dot(normal, -lighting.lightDirection), 0.0);
    vec3 diffuse = diff * lighting.lightColor;
    
    // 최종 라이팅
    vec3 finalLighting = ambient + diffuse;
    
    // 4. 텍스처에 라이팅 적용
    vec3 finalColor = texColor.rgb * finalLighting;
    
    // 5. 안개 적용 (선택사항)
    if (isFogEnabled)
    {
        finalColor = ApplySimpleFog(finalColor, fragPos.xyz, camPos);
    }
    
    // 6. 최종 출력
    fragColor = vec4(finalColor, 1.0);
    fragDepth = viewPos.z / 10000.0;
}