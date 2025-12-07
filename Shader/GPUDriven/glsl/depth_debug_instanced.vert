#version 430
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texCoord;
layout(location = 2) in vec3 normal;

// SSBO
layout(std430, binding = 0) readonly buffer Transforms {
    mat4 transforms[];
};
layout(std430, binding = 1) readonly buffer VisibleIndices {
    uint visibleIndices[];
};
layout(std430, binding = 8) readonly buffer DebugDepths {
    float debugDepths[];  // [0, 1] 범위
};

uniform mat4 vpMatrix;
uniform float nearPlane;
uniform float farPlane;

out vec3 debugColor;

// ============================================================================
// 깊이 → 색상 변환 (0-100-500-1000m 구간)
// ============================================================================
vec3 depthToColor(float depth)
{
    // [0, 1] → [0, 10000m] 변환
    float depthMeters = depth * 10000.0;
    
    vec3 color;
    
    if (depthMeters < 100.0)
    {
        // 0 ~ 100m: 파란색 → 청록색 (매우 가까움)
        float t = depthMeters / 100.0;
        color = vec3(0.0, t, 1.0);
    }
    else if (depthMeters < 500.0)
    {
        // 100 ~ 500m: 청록색 → 초록색 (가까움)
        float t = (depthMeters - 100.0) / 400.0;
        color = vec3(0.0, 1.0, 1.0 - t);
    }
    else if (depthMeters < 1000.0)
    {
        // 500 ~ 1000m: 초록색 → 노란색 (중간)
        float t = (depthMeters - 500.0) / 500.0;
        color = vec3(t, 1.0, 0.0);
    }
    else
    {
        // 1000m 이상: 노란색 → 빨간색 (멀음)
        float t = clamp((depthMeters - 1000.0) / 9000.0, 0.0, 1.0);
        color = vec3(1.0, 1.0 - t, 0.0);
    }
    
    return color;
}

void main()
{
    // 인스턴스 인덱스 가져오기
    uint instanceIndex = visibleIndices[gl_InstanceID];
    
    // Transform 적용
    mat4 modelMatrix = transforms[instanceIndex];
    vec4 worldPos = modelMatrix * vec4(position, 1.0);
    gl_Position = vpMatrix * worldPos;
    
    // 디버그 깊이 가져오기 ([0, 1] 범위)
    float depthNDC = debugDepths[instanceIndex];
    
    // 안전 클램핑
    depthNDC = clamp(depthNDC, 0.0, 1.0);
    
    // 색상 변환
    debugColor = depthToColor(depthNDC);
}