// GLSL 버전 4.30을 사용
#version 430

// 버텍스 속성 입력 정의
layout(location = 0) in vec3 position;   // 정점 위치 (x, y, z)
//layout(location = 1) in vec3 normal;     // 정점 법선 벡터 (현재 미사용)
//layout(location = 2) in vec2 texCoord;   // 텍스처 좌표 (현재 미사용)

// 프래그먼트 셰이더로 전달할 출력 변수
out vec3 fsPosition;    // 월드 공간에서의 정점 위치

// 유니폼 변수 선언
uniform mat4 model;     // 모델 변환 행렬 (로컬 -> 월드 공간)
uniform mat4 mvp;       // 모델-뷰-프로젝션 결합 행렬 (로컬 -> 클립 공간)

void main()
{
    // 정점 위치를 4D 동차 좌표로 변환
    vec4 posVec4 = vec4(position, 1.0);
    
    // 정점의 월드 공간 위치를 계산하여 프래그먼트 셰이더로 전달
    fsPosition = vec3(model * posVec4);
    
    // 최종 클립 공간 위치 계산
    gl_Position = mvp * posVec4;
}