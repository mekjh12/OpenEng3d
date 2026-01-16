#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

uniform mat4 vp;

in VS_OUT 
{
    vec3 worldPosition;
    float scale;
    vec3 cameraPos;
} gs_in[];

out vec2 pass_texCoord;
out vec3 pass_worldPosition;
out float pass_distanceToCamera;

void main() 
{
    vec3 center = gs_in[0].worldPosition;
    float scale = gs_in[0].scale;
    vec3 cameraPos = gs_in[0].cameraPos;
    
    // 스케일이 0이면 렌더링 안함 (무효한 인스턴스)
    scale = 10;
    if (scale <= 0.0) return;
    
    // 카메라 방향 벡터 계산 (Cylindrical Billboard - Z축 고정)
    vec3 toCameraDir = normalize(cameraPos - center);
    toCameraDir.z = 0.0; // Z축 성분 제거 (수평면에 투영)
    toCameraDir = normalize(toCameraDir);
    
    // 빌보드 좌표계 생성 (Z-up 좌표계)
    vec3 billboardUp = vec3(0, 0, 1);  // Z가 위
    vec3 billboardRight = normalize(cross(toCameraDir, billboardUp)); // 오른쪽 벡터
    
    // 빌보드 크기 (스케일 적용)
    float halfSize = scale * 0.5;
    
    // 카메라 거리
    float distance = length(cameraPos - center);
    
    // 4개의 정점 생성 (사각형)
    // 좌하 (0, 0)
    vec3 v0 = center - billboardRight * halfSize - billboardUp * halfSize;
    pass_texCoord = vec2(0.0, 0.0);
    pass_worldPosition = v0;
    pass_distanceToCamera = distance;
    gl_Position = vp * vec4(v0, 1.0);
    EmitVertex();
    
    // 우하 (1, 0)
    vec3 v1 = center + billboardRight * halfSize - billboardUp * halfSize;
    pass_texCoord = vec2(1.0, 0.0);
    pass_worldPosition = v1;
    pass_distanceToCamera = distance;
    gl_Position = vp * vec4(v1, 1.0);
    EmitVertex();
    
    // 좌상 (0, 1)
    vec3 v2 = center - billboardRight * halfSize + billboardUp * halfSize;
    pass_texCoord = vec2(0.0, 1.0);
    pass_worldPosition = v2;
    pass_distanceToCamera = distance;
    gl_Position = vp * vec4(v2, 1.0);
    EmitVertex();
    
    // 우상 (1, 1)
    vec3 v3 = center + billboardRight * halfSize + billboardUp * halfSize;
    pass_texCoord = vec2(1.0, 1.0);
    pass_worldPosition = v3;
    pass_distanceToCamera = distance;
    gl_Position = vp * vec4(v3, 1.0);
    EmitVertex();
    
    EndPrimitive();
}