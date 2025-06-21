#version 430 core

// 입력 버텍스 속성
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texCoord;

// 출력 변수
out vec3 worldPos;
out vec2 texCoords;
out vec3 viewRay;

// 유니폼 변수
uniform mat4 model;
uniform mat4 view;
uniform mat4 proj;
uniform vec3 camPosMeter;   // 단위 미터
uniform float R_e;          // 지구 반지름(단위km)
uniform float R_a;          // 대기 반지름(단위km)

// 버텍스 셰이더
void main() 
{
    worldPos = vec3(model * vec4(position, 1.0));
    gl_Position = proj * view * vec4(worldPos, 1.0);
    viewRay = normalize(worldPos - camPosMeter);
}
