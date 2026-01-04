#version 430 core
#include "./../includes/lib_terrain_texturing.glsl"

// G-Buffer MRT 출력
layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;

// 입력 (Geometry Shader에서 전달)
in vec2 frag_Tex3;
in float frag_Height;
in vec4 frag_fragPos;
in vec4 frag_viewPos;
flat in vec3 frag_Normal;  // ⭐ flat: 보간 없음

// 지형 텍스처
uniform sampler2D gTextureHeight0;
uniform sampler2D gTextureHeight1;
uniform sampler2D gTextureHeight2;
uniform sampler2D gTextureHeight3;
uniform sampler2D gTextureHeight4;
uniform sampler2D gDetailMap;
uniform bool gIsDetailMap;

uniform float gHeight0 = 0.07f;
uniform float gHeight1 = 0.15f;
uniform float gHeight2 = 0.25f;
uniform float gHeight3 = 0.71f;
uniform float gHeight4 = 0.82f;
uniform float gColorTexcoordScaling = 800.0f;

void main()
{
    // 1. 지형 텍스처 블렌딩
    vec4 texColor = BlendTerrainTextures(
        frag_Height, frag_Tex3, gColorTexcoordScaling,
        gTextureHeight0, gTextureHeight1, gTextureHeight2, 
        gTextureHeight3, gTextureHeight4, gDetailMap,
        gIsDetailMap, gHeight0, gHeight1, gHeight2, gHeight3, gHeight4
    );
    
    // 2. 법선 사용 (Geometry Shader에서 계산됨)
    vec3 normal = frag_Normal;  // 정규화 이미 됨
    
    // 3. G-Buffer 출력
    gAlbedo = vec4(texColor.rgb, 1.0);
    gPosition = vec4(frag_fragPos.xyz, 1.0);
    gNormal = vec4(normal, 1.0);
    gDepth = frag_viewPos.z / 10000.0;
}