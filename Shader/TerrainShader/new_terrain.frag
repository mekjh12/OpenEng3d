#version 430 core
#include "./../includes/lib_terrain_texturing.glsl"

// G-Buffer MRT 출력
layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;

// 입력 (TES에서 전달)
in vec2 Tex3;
in vec4 fragPos;
in float viewDepth;

// 텍스처들
uniform sampler2D gHeightMap;
uniform sampler2D gNormalMap;
uniform sampler2D gTextureHeight0;
uniform sampler2D gTextureHeight1;
uniform sampler2D gTextureHeight2;
uniform sampler2D gTextureHeight3;
uniform sampler2D gTextureHeight4;
uniform sampler2D gDetailMap;

uniform bool gIsDetailMap;
uniform mat4 model;
uniform float gColorTexcoordScaling = 800.0f;

uniform float gHeight0 = 0.07f;
uniform float gHeight1 = 0.15f;
uniform float gHeight2 = 0.25f;
uniform float gHeight3 = 0.71f;
uniform float gHeight4 = 0.82f;

void main()
{
    // 1. 높이 샘플링
    float Height = texture(gHeightMap, Tex3).r;
    
    // 2. ⭐ Normal Map에서 법선 로드
    vec3 normalTangent = texture(gNormalMap, Tex3).rgb;
    normalTangent = normalTangent * 2.0 - 1.0;  // [0,1] → [-1,1]
    
    // 3. 월드 공간으로 변환
    mat3 normalMatrix = mat3(transpose(inverse(model)));
    vec3 normalWorld = normalize(normalMatrix * normalTangent);
    
    // 4. 지형 텍스처 블렌딩
    vec4 texColor = BlendTerrainTextures(
        Height, Tex3, gColorTexcoordScaling,
        gTextureHeight0, gTextureHeight1, gTextureHeight2, 
        gTextureHeight3, gTextureHeight4, gDetailMap,
        gIsDetailMap, gHeight0, gHeight1, gHeight2, gHeight3, gHeight4
    );
    
    // 5. G-Buffer 출력
    gAlbedo = vec4(texColor.rgb, 1.0);
    gPosition = vec4(fragPos.xyz, 1.0);
    gNormal = vec4(normalWorld, 1.0);
    gDepth = viewDepth / 10000.0;
}