#version 430 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texcoord;  // ✅ 텍스처 좌표 추가

// 인스턴스 변환 행렬 SSBO
layout(std430, binding = 0) buffer InstanceBuffer
{
    mat4 modelMatrices[];
};

uniform mat4 view;
uniform mat4 proj;

out vec4 gViewPos;
out vec2 pass_texcoord;  // ✅ Fragment Shader로 전달

void main()
{
    mat4 model = modelMatrices[gl_InstanceID];
    gViewPos = view * model * vec4(position, 1.0);
    gl_Position = proj * gViewPos;
    
    pass_texcoord = texcoord;  // ✅ 텍스처 좌표 전달
}