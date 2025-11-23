#version 430 core
// 버텍스 속성
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texcoord;

// ✅ SSBO 방식으로 변경
layout(std430, binding = 0) buffer InstanceBuffer
{
    mat4 modelMatrices[];
};

// 유니폼
uniform mat4 vp;

// 출력
out vec2 pass_texcoord;

void main()
{
    // ✅ SSBO에서 인스턴스 행렬 읽기
    mat4 instanceModel = modelMatrices[gl_InstanceID];
    
    // 월드-뷰-투영 변환
    gl_Position = vp * instanceModel * vec4(position, 1.0);
    
    // 텍스처 좌표 전달
    pass_texcoord = texcoord;
}