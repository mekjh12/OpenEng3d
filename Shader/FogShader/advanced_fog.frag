// /Shader/FogShader/advanced_fog.frag
#version 450 core

in vec2 vTexCoord;
out vec4 fragColor;

// ✅ G-Buffer 텍스처 (invViewProj 불필요!)
uniform sampler2D colorTexture;      // 라이팅 적용된 컬러
uniform sampler2D positionTexture;   // 월드 위치 (직접 읽기)
uniform sampler2D depthTexture;      // 선형 깊이

// 카메라 정보
uniform vec3 camPos;

// Distance Fog
uniform float distanceFogDensity;  // 기본값: 0.0003
uniform float distanceFogStart;    // 기본값: 0

// Height Fog
uniform vec3 heightFogColor;       // 안개 색상
uniform float heightFogDensity;    // 기본값: 0.5
uniform float heightFogFalloff;    // 기본값: 2.0
uniform float heightFogMin;        // 기본값: 0
uniform float heightFogMax;        // 기본값: 500

// Layered Fog
uniform bool enableLayeredFog;     // 기본값: false
uniform float layerHeight;         // 기본값: 200
uniform float layerThickness;      // 기본값: 50
uniform float layerDensity;        // 기본값: 0.3

// Fog Mode (0: Distance, 1: Height, 2: Combined)
uniform int fogMode;

//=============================================================================
// Exponential Squared Distance Fog
//=============================================================================
float CalcDistanceFog(float distance)
{
    float adjustedDist = max(0.0, distance - distanceFogStart);
    float exponent = distanceFogDensity * adjustedDist;
    return exp(-exponent * exponent);
}

//=============================================================================
// Height-based Fog
//=============================================================================
float CalcHeightFog(float worldHeight)
{
    // 높이 정규화
    float heightFactor = clamp(
        (worldHeight - heightFogMin) / (heightFogMax - heightFogMin),
        0.0, 1.0
    );
    
    // Exponential falloff
    float heightFog = exp(-heightFogFalloff * heightFactor);
    
    return heightFog * heightFogDensity;
}

//=============================================================================
// Layered Fog (구름층 효과)
//=============================================================================
float CalcLayeredFog(float worldHeight)
{
    if (!enableLayeredFog) return 0.0;
    
    float distFromLayer = abs(worldHeight - layerHeight);
    float layer = exp(-pow(distFromLayer / layerThickness, 2.0));
    
    return layer * layerDensity;
}

//=============================================================================
// 메인 함수
//=============================================================================
void main()
{
    // 원본 색상 (라이팅 적용된 결과)
    vec4 sceneColor = texture(colorTexture, vTexCoord);
    
    // 선형 깊이
    float linearDepth = texture(depthTexture, vTexCoord).r;
    
    // 하늘 처리 (깊이가 최대인 경우)
    if (linearDepth >= 0.9999)
    {
        fragColor = sceneColor;
        return;
    }
    
    // ✅ 월드 위치 직접 읽기 (재구성 불필요!)
    vec3 worldPos = texture(positionTexture, vTexCoord).xyz;
    
    // 거리 계산
    float distance = length(camPos - worldPos);
    
    // 안개 계산
    float fogFactor = 1.0; // 1.0 = 안개 없음, 0.0 = 완전 안개
    
    if (fogMode == 0)
    {
        // Distance Fog only
        fogFactor = CalcDistanceFog(distance);
    }
    else if (fogMode == 1)
    {
        // Height Fog only
        float heightFog = CalcHeightFog(worldPos.y);
        fogFactor = 1.0 - heightFog;
    }
    else // fogMode == 2
    {
        // ✅ Combined Fog (Distance + Height + Layered)
        
        // Distance fog
        float distanceFog = CalcDistanceFog(distance);
        
        // Height fog
        float heightFog = CalcHeightFog(worldPos.y);
        
        // Layered fog
        float layeredFog = CalcLayeredFog(worldPos.y);
        
        // 결합: distanceFog * (1 - heightFog - layeredFog)
        fogFactor = distanceFog * (1.0 - heightFog - layeredFog);
        fogFactor = clamp(fogFactor, 0.0, 1.0);
    }
    
    // 최종 색상 혼합
    vec3 finalColor = mix(heightFogColor, sceneColor.rgb, fogFactor);
    
    fragColor = vec4(finalColor, sceneColor.a);
}