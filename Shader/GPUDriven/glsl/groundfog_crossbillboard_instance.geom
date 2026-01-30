#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 12) out;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

in VS_OUT 
{
    vec3 worldPosition;
    float scale;
    vec3 cameraPos;
} gs_in[];

out vec2 pass_texCoord;
out vec3 pass_worldPosition;
out float pass_distanceToCamera;

// ✅ 간단한 해시 함수로 랜덤 값 생성
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

void EmitQuad(vec3 center, vec3 right, vec3 up, float halfSize, float distance)
{
    // 좌하 (0, 0)
    vec3 v0 = center - right * halfSize - up * halfSize;
    pass_texCoord = vec2(0.0, 0.0);
    pass_worldPosition = v0;
    pass_distanceToCamera = distance;
    gl_Position = camera.vp * vec4(v0, 1.0);
    EmitVertex();
    
    // 우하 (1, 0)
    vec3 v1 = center + right * halfSize - up * halfSize;
    pass_texCoord = vec2(1.0, 0.0);
    pass_worldPosition = v1;
    pass_distanceToCamera = distance;
    gl_Position = camera.vp * vec4(v1, 1.0);
    EmitVertex();
    
    // 좌상 (0, 1)
    vec3 v2 = center - right * halfSize + up * halfSize;
    pass_texCoord = vec2(0.0, 1.0);
    pass_worldPosition = v2;
    pass_distanceToCamera = distance;
    gl_Position = camera.vp * vec4(v2, 1.0);
    EmitVertex();
    
    // 우상 (1, 1)
    vec3 v3 = center + right * halfSize + up * halfSize;
    pass_texCoord = vec2(1.0, 1.0);
    pass_worldPosition = v3;
    pass_distanceToCamera = distance;
    gl_Position = camera.vp * vec4(v3, 1.0);
    EmitVertex();
    
    EndPrimitive();
}

void main() 
{
    vec3 center = gs_in[0].worldPosition;
    float scale = gs_in[0].scale;
    
    if (scale <= 0.0) return;
    
    float halfSize = scale * 0.5;
    float distance = length(gs_in[0].cameraPos - center);
    
    // ✅ 기본 각도: 0°, 120°, 240°
    const float angleStep = radians(120.0);
    
    // ✅ 랜덤 시드 (위치 기반)
    float seed = hash(center);
    
    // ✅ 랜덤 오프셋 범위 조절
    const float maxOffsetDistance = 0.15; // scale의 15%
    const float maxAngleOffset = radians(20.0); // ±20도
    
    for (int i = 0; i < 3; i++)
    {
        // ✅ 각 평면마다 다른 랜덤 값 생성
        float randomSeed = hash(center + vec3(float(i) * 123.456, float(i) * 789.012, float(i) * 345.678));
        
        // ✅ 기본 회전 각도 + 랜덤 오프셋
        float baseAngle = float(i) * angleStep;
        float angleOffset = (randomSeed * 2.0 - 1.0) * maxAngleOffset;
        float angle = baseAngle + angleOffset;
        
        // ✅ 랜덤 위치 오프셋
        float offsetDist = hash(center + vec3(float(i) * 234.567, float(i) * 890.123, float(i) * 456.789));
        offsetDist = (offsetDist * 2.0 - 1.0) * scale * maxOffsetDistance;
        
        float offsetAngle = hash(center + vec3(float(i) * 567.890, float(i) * 123.456, float(i) * 678.901)) * 6.28318; // 0~2π
        vec3 offset = vec3(cos(offsetAngle), sin(offsetAngle), 0.0) * offsetDist;
        
        // ✅ 평면의 right 벡터 (수평 회전)
        vec3 right = vec3(cos(angle), sin(angle), 0.0);
        
        // ✅ 평면의 up 벡터 (수직)
        vec3 up = vec3(0, 0, 1);
        
        // ✅ 오프셋이 적용된 중심점
        vec3 planeCenter = center + offset;
        
        // 쿼드 생성
        EmitQuad(planeCenter, right, up, halfSize, distance);
    }
}