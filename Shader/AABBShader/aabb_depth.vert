#version 430 core

// AABB 구조체 정의
struct AABB
{
    vec3 min;  // 최소 좌표
    float pad1;
    vec3 max;  // 최대 좌표
    float pad2;
};

// SSBO: AABB 배열
layout(std430, binding = 0) readonly buffer AABBBuffer
{
    AABB aabbs[];
};

// 출력: Geometry Shader로 전달
out int vInstanceID;

void main()
{
    // 인스턴스 ID를 Geometry Shader로 전달
    vInstanceID = gl_VertexID;
    
    // 위치는 Geometry Shader에서 계산하므로 여기서는 더미값
    gl_Position = vec4(0.0);
}