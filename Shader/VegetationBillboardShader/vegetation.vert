#version 420 core

layout(location = 0) in vec3 position; // Instanced Rendering: 나무의 월드 좌표

// 유니폼 변수
uniform mat4 proj;
uniform mat4 view;
uniform vec3 gCameraPos;

// 지오메트리 쉐이더로 전달할 출력 변수
out vec3 WorldPosition;

void main()
{
    WorldPosition = position; // 나무의 실제 월드 좌표 전달
    gl_Position = vec4(position, 1.0); // 지오메트리 쉐이더에서 최종 변환
}