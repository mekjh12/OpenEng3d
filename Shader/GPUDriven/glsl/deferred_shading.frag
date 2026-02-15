//-----------------------------------------------------------------------------
// deferred_shading.frag - 적응형 샘플링 PCF Shadow
//-----------------------------------------------------------------------------
#version 430 core

// G-Buffer 입력
uniform sampler2D gAlbedo;
uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gDepth;

// Shadow Map
uniform sampler2DShadow gTerrainShadowMap;
uniform sampler2DShadow gInstancesShadowMap;

uniform mat4 lightView;
uniform mat4 lightProj;
uniform mat4 lightView2;
uniform mat4 lightProj2;

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
// 적응형 샘플링 PCF Shadow
// - 가까운 거리: 많은 샘플 (16개) + 큰 반경 → 고품질 부드러운 그림자
// - 먼 거리: 적은 샘플 (4개) + 큰 반경 → 성능 향상
//-----------------------------------------------------------------------------
float CalculateShadowAdaptive(vec3 worldPos, sampler2DShadow shadowMap, 
                              mat4 viewMatrix, mat4 projMatrix, 
                              float bias, float baseRadius, float maxDistance)
{
    vec4 fragPosWorld = vec4(worldPos, 1.0);
    
    // Light Space로 변환
    vec4 lightSpacePos = viewMatrix * fragPosWorld;
    float currentDepth = -lightSpacePos.z / 10000.0;
    
    // NDC로 변환
    lightSpacePos = projMatrix * lightSpacePos;
    vec3 projCoords = lightSpacePos.xyz / lightSpacePos.w;
    vec2 shadowUV = projCoords.xy * 0.5 + 0.5;
    
    // 범위 체크
    if(shadowUV.x < 0.0 || shadowUV.x > 1.0 || 
       shadowUV.y < 0.0 || shadowUV.y > 1.0)
        return 0.0;
    
    // bias 적용
    currentDepth = clamp(currentDepth - bias, 0.0, 1.0);
    
    // ⭐ 카메라와의 거리 계산
    float distanceToCamera = length(camera.cameraPos.xyz - worldPos);
    float distanceFactor = clamp(distanceToCamera / maxDistance, 0.0, 1.0);
    
    // ⭐ 거리에 따른 샘플 수 결정
    // 가까움(0.0): 16개, 중간(0.5): 9개, 멀리(1.0): 4개
    int sampleCount;
    if (distanceFactor < 0.33) {
        sampleCount = 16;  // 가까운 거리 - 고품질
    } else if (distanceFactor < 0.66) {
        sampleCount = 9;   // 중간 거리
    } else {
        sampleCount = 4;   // 먼 거리 - 성능 우선
    }
    
    // Poisson Disk 샘플 (16개 준비, 필요한 만큼만 사용)
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
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    
    // ⭐ 반경은 크게 고정 (부드러운 그림자)
    float radius = baseRadius;
    
    // ⭐ 동적 샘플링 (거리에 따라 샘플 수만 조정)
    for(int i = 0; i < sampleCount; i++)
    {
        vec2 offset = poissonDisk[i] * texelSize * radius;
        vec3 shadowCoord = vec3(shadowUV + offset, currentDepth);
        
        float visibility = texture(shadowMap, shadowCoord);
        shadow += 1.0 - visibility;
    }
    shadow /= float(sampleCount);
    
    return shadow;
}

void main()
{
    // G-Buffer 샘플링
    vec4 albedo = texture(gAlbedo, TexCoord);
    vec4 worldPos = texture(gPosition, TexCoord);
    vec4 normalData = texture(gNormal, TexCoord);
    float depth = texture(gDepth, TexCoord).r;
    
    vec3 normal = normalize(normalData.xyz);
    
    // 배경 체크
    if (depth > 0.999) {
        fragColor = albedo;
        return;
    }
    
    // ⭐ 적응형 샘플링 그림자
    // Parameters: (worldPos, shadowMap, view, proj, bias, baseRadius, maxDistance)
    
    // Terrain Shadow
    float shadowTerrainFactor = CalculateShadowAdaptive(
        worldPos.xyz, 
        gTerrainShadowMap, 
        lightView, 
        lightProj, 
        0.0005,     // ⭐ 기존 bias 유지
        6.0,        // ⭐ 큰 반경 (부드러움)
        1500.0      // 이 거리까지 점진적 품질 변화
    );
    
    // Instances Shadow
    float shadowFactor = CalculateShadowAdaptive(
        worldPos.xyz, 
        gInstancesShadowMap, 
        lightView2, 
        lightProj2, 
        0.0001,     // ⭐ 기존 bias 유지
        5.0,        // ⭐ 큰 반경
        1200.0
    );
    
    // Diffuse 라이팅
    vec3 lightDir = normalize(-lighting.lightDirection);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lighting.lightColor * albedo.rgb;
    
    // 최종 라이팅
    vec3 ambient = lighting.ambientColor * albedo.rgb;
    //vec3 finalColor = ambient + (1.0 - shadowTerrainFactor) * (1.0 - shadowFactor) * diffuse;
    vec3 finalColor = ambient + diffuse;
    
    fragColor = vec4(finalColor, albedo.a);
}