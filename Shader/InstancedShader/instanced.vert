#version 430 core

// 버텍스 속성
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texcoord;

// 인스턴스별 모델 행렬 (mat4는 4개의 vec4로 전달)
layout(location = 2) in vec4 instanceModel0;
layout(location = 3) in vec4 instanceModel1;
layout(location = 4) in vec4 instanceModel2;
layout(location = 5) in vec4 instanceModel3;

// 유니폼
uniform mat4 vp;

// 출력
out vec2 pass_texcoord;

void main()
{
    // 인스턴스별 모델 행렬 재구성
    mat4 instanceModel = mat4(
        instanceModel0*0.1f,
        instanceModel1*0.1f,
        instanceModel2*0.1f,
        instanceModel3
    );
    
    // 월드-뷰-투영 변환
    gl_Position = vp * instanceModel * vec4(position, 1.0);
    
    // 텍스처 좌표 전달
    pass_texcoord = texcoord;
}