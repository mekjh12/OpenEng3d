#version 450 core

// 입력: 인스턴스 데이터
layout(location = 0) in vec3 instancePosition;
layout(location = 1) in float instanceScale;

uniform vec3 aabbMin;
uniform vec3 aabbMax;
uniform mat4 model;

out VS_OUT {
    vec3 worldPos;
    vec3 color;
    float horizontalSize;
    float verticalSize;
    mat4 transform;
} vs_out;

void main()
{    
    vec3 extents = aabbMax - aabbMin;
    
    // 출력 변수 설정
    vs_out.worldPos = instancePosition;
    vs_out.horizontalSize = max(extents.x, extents.y) * 0.5;
    vs_out.verticalSize = extents.z * 0.5;
    vs_out.transform = model;

    // 최종 위치 계산
    gl_Position = vec4(vs_out.worldPos, 1.0);
}