//-----------------------------------------------------------------------------
// deferred_shading.frag - Deferred Rendering 라이팅 패스 + Shadow
//-----------------------------------------------------------------------------
#version 430 core

// G-Buffer 입력
uniform sampler2D gAlbedo;    // ColorAttachment0
uniform sampler2D gPosition;  // ColorAttachment1
uniform sampler2D gNormal;    // ColorAttachment2
uniform sampler2D gDepth;     // ColorAttachment3

// Shadow Map
uniform sampler2D gShadowMap; // Texture4
uniform mat4 lightView;
uniform mat4 lightProj;

in vec2 TexCoord;
out vec4 fragColor;

// UBO
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

//-----------------------------------------------------------------------------
// 그림자 계산 (Poisson Disk PCF)
// - 16개의 불규칙 샘플링으로 부드러운 그림자 경계 생성
// - 반환: 0.0(빛) ~ 1.0(그림자)
//-----------------------------------------------------------------------------
float CalculateShadow(vec3 worldPos)
{
    vec4 fragPosWorld = vec4(worldPos, 1.0);
    
    // Light Space로 변환
    vec4 lightSpacePos = lightView * fragPosWorld;
    
    // 현재 픽셀의 Light 뷰 깊이
    float currentDepth = -lightSpacePos.z / 10000.0;
    currentDepth = clamp(currentDepth, 0.0, 1.0);    
    
    // NDC로 변환해서 텍스처 좌표 얻기
    lightSpacePos = lightProj * lightSpacePos;
    vec3 projCoords = lightSpacePos.xyz / lightSpacePos.w;
    vec2 shadowUV = projCoords.xy * 0.5 + 0.5;
    
    // Shadow Map 범위 밖은 그림자 없음
    if(shadowUV.x < 0.0 || shadowUV.x > 1.0 || 
       shadowUV.y < 0.0 || shadowUV.y > 1.0)
        return 0.0;
    
    // Poisson Disk 샘플 배열
    vec2 poissonDisk[16] = vec2[](
        vec2(-0.94201624, -0.39906216),
        vec2(0.94558609, -0.76890725),
        vec2(-0.094184101, -0.92938870),
        vec2(0.34495938, 0.29387760),
        vec2(-0.91588581, 0.45771432),
        vec2(-0.81544232, -0.87912464),
        vec2(-0.38277543, 0.27676845),
        vec2(0.97484398, 0.75648379),
        vec2(0.44323325, -0.97511554),
        vec2(0.53742981, -0.47373420),
        vec2(-0.26496911, -0.41893023),
        vec2(0.79197514, 0.19090188),
        vec2(-0.24188840, 0.99706507),
        vec2(-0.81409955, 0.91437590),
        vec2(0.19984126, 0.78641367),
        vec2(0.14383161, -0.14100790)
    );
    
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(gShadowMap, 0);
    float bias = 0.0005;
    
    // 16개 샘플 포인트에서 그림자 판단
    for(int i = 0; i < 16; i++)
    {
        vec2 offset = poissonDisk[i] * texelSize * 2.0;
        float closestDepth = texture(gShadowMap, shadowUV + offset).r;
        shadow += (currentDepth - bias) > closestDepth ? 1.0 : 0.0;
    }
    shadow /= 16.0;
    
    return shadow;
}

void main()
{
    // ============================================
    // G-Buffer 샘플링
    // ============================================
    vec4 albedo = texture(gAlbedo, TexCoord);
    vec4 worldPos = texture(gPosition, TexCoord);
    vec4 normalData = texture(gNormal, TexCoord);
    float depth = texture(gDepth, TexCoord).r;
    
    // 법선 추출
    vec3 normal = normalize(normalData.xyz);
    
    // ============================================
    // 배경 체크 (깊이가 0이면 배경)
    // ============================================
    if (depth > 0.999) {
        fragColor = albedo;
        return;
    }
    
    // ============================================
    // 그림자 계산
    // ============================================
    float shadowFactor = CalculateShadow(worldPos.xyz);
    
    // ============================================
    // Diffuse 라이팅 (Lambert)
    // ============================================
    vec3 lightDir = normalize(-lighting.lightDirection);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lighting.lightColor * albedo.rgb;
    
    // ============================================
    // 최종 라이팅 (Ambient + Diffuse with Shadow)
    // ============================================
    vec3 ambient = lighting.ambientColor * albedo.rgb;

    vec3 finalColor = ambient + (1.0 - shadowFactor) * diffuse;
    
    fragColor = vec4(finalColor, albedo.a);
}