#version 450 core

layout(points) in;
layout(triangle_strip, max_vertices = 6) out;

uniform mat4 vp;
uniform float quadSize;

in VS_OUT {
    vec3 worldPos;
    vec3 color;
} gs_in[];

out vec3 vColor;

void main() 
{
    vec3 worldPos = gs_in[0].worldPos;
    vec3 color = gs_in[0].color;

    // 사각형 크기
    float size = quadSize;
    
    // 첫 번째 삼각형 (왼쪽 아래, 오른쪽 아래, 왼쪽 위)
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(-size, 0, -size), 1.0);
    EmitVertex();
    
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(size, 0, -size), 1.0);
    EmitVertex();
    
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(-size, 0, size), 1.0);
    EmitVertex();
    
    EndPrimitive();
    
    // 두 번째 삼각형 (오른쪽 아래, 오른쪽 위, 왼쪽 위)
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(size, 0, -size), 1.0);
    EmitVertex();
    
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(size, 0, size), 1.0);
    EmitVertex();
    
    vColor = color;
    gl_Position = vp * vec4(worldPos + vec3(-size, 0, size), 1.0);
    EmitVertex();
    
    EndPrimitive();
}