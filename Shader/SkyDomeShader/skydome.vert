#version 430 core

// 입력 버텍스 속성
layout(location = 0) in vec3 position; // 정점 위치
layout(location = 1) in vec2 texCoord; // 텍스처 좌표

// 유니폼 변수
uniform mat4 model; // 모델 변환 행렬
uniform mat4 view;  // 뷰 변환 행렬
uniform mat4 proj;  // 투영 변환 행렬

// 프래그먼트 셰이더로 전달할 출력 변수
out vec2 fragTexCoord;

void main()
{
    // 텍스처 좌표 전달
    fragTexCoord = texCoord;
    
    // 정점 변환
    gl_Position = proj * view * model * vec4(position, 1.0);
}