#version 450 core

in vec2 vTexCoord;
out vec4 fragColor;

// 텍스처
uniform sampler2D colorTexture;      // layout(location = 0) - 컬러
uniform sampler2D linearDepthTexture; // layout(location = 1) - 선형 깊이

// 안개 파라미터
uniform vec3 fogColor;           // 안개 색상
uniform float fogDensity;        // 안개 밀도 (Exponential용)
uniform float fogStart;          // 안개 시작 거리 (Linear용)
uniform float fogEnd;            // 안개 끝 거리 (Linear용)
uniform float maxDistance;       // 정규화에 사용된 최대 거리 (기본값: 10000.0)
uniform int fogType;             // 0: Linear, 1: Exp, 2: Exp2

/// <summary>
/// Linear Fog 계산
/// </summary>
float calcLinearFog(float distance)
{
    return clamp((fogEnd - distance) / (fogEnd - fogStart), 0.0, 1.0);
}

/// <summary>
/// Exponential Fog 계산
/// </summary>
float calcExpFog(float distance)
{
    return exp(-fogDensity * distance);
}

/// <summary>
/// Exponential Squared Fog 계산 (가장 자연스러움)
/// </summary>
float calcExp2Fog(float distance)
{
    float exponent = fogDensity * distance;
    return exp(-exponent * exponent);
}

void main()
{
    // 원본 색상 샘플링
    vec4 sceneColor = texture(colorTexture, vTexCoord);
    
    // 선형 깊이값 샘플링 (이미 정규화된 값: 0~1)
    float normalizedDepth = texture(linearDepthTexture, vTexCoord).r;
    
    // 실제 거리로 복원
    float distance = normalizedDepth * maxDistance;
    
    // 하늘 처리 (깊이가 매우 큰 경우)
    if (normalizedDepth >= 0.9999)
    {
        //fragColor = sceneColor;
        //return;
    }
    
    // 안개 밀도 계산
    float fogFactor = 1.0;  // 1.0 = 안개 없음, 0.0 = 완전 안개
    
    if (fogType == 0)
    {
        // Linear Fog
        fogFactor = calcLinearFog(distance);
    }
    else if (fogType == 1)
    {
        // Exponential Fog
        fogFactor = calcExpFog(distance);
    }
    else
    {
        // Exponential Squared Fog (기본값)
        fogFactor = calcExp2Fog(distance);
    }
    
    // 안개 색상과 씬 색상 혼합
    vec3 finalColor = mix(fogColor, sceneColor.rgb, fogFactor);
    
    fragColor = vec4(finalColor, sceneColor.a);
}