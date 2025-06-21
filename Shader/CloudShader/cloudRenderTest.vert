#version 430 core

// 입력 버텍스 속성
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texCoord;

// 출력 변수
out vec3 worldPos;      // 월드 좌표
out vec2 texCoords;     // 텍스처 좌표
out vec3 viewRay;       // 뷰 레이

// 유니폼 변수
uniform mat4 model;         // 모델 행렬
uniform mat4 view;          // 뷰 행렬
uniform mat4 proj;          // 투영 행렬
uniform mat4 mvp;           // 모델-뷰-투영 행렬
uniform vec3 camPosMeter;   // 카메라 위치(미터)

// 버텍스 셰이더
void main() 
{
    // 월드 좌표 계산
    worldPos = vec3(model * vec4(position, 1.0));

    // 클립 좌표 계산
    gl_Position = mvp * vec4(position, 1.0);

    // 뷰 레이 계산
    viewRay = normalize(worldPos - camPosMeter);
    
    // 텍스처 좌표 전달
    texCoords = texCoord;
}
