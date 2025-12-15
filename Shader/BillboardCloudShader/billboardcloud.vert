#version 450 core

// 입력: 인스턴스 데이터
layout(location = 0) in vec3 instancePosition;   // 나무 위치
layout(location = 1) in float instanceScale;     // 나무 크기

// Uniform
uniform mat4 vp;  // View-Projection 행렬

// Geometry Shader로 전달
out VS_OUT {
    vec3 worldPosition;
    float scale;
} vs_out;

void main()
{
    vs_out.worldPosition = instancePosition;
    vs_out.scale = instanceScale;
    
    // Point를 그대로 전달 (Geometry Shader에서 확장)
    gl_Position = vec4(instancePosition, 1.0);
}