#version 450 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 textureCoords;
layout(location = 2) in vec3 normal;
layout(location = 3) in float materialID;

uniform mat4 mvp;
uniform mat4 mv;
uniform mat3 normalMatrix;

out VS_OUT {
    vec3 viewPos;
    vec3 viewNormal;
} vs_out;

void main()
{
    // 뷰 공간 위치 계산
    vs_out.viewPos = (mv * vec4(position, 1.0)).xyz;
    
    // 뷰 공간 노멀 계산
    vs_out.viewNormal = normalize(normalMatrix * normal);
    
    gl_Position = vec4(position, 1.0);  // Geometry Shader에서 변환
}