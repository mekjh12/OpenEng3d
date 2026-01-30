#version 430 core

layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

out vec2 v_TexCoord;

void main()
{
    // 전체 화면을 덮는 쿼드 생성
    
    // 좌하단
    gl_Position = vec4(-1.0, -1.0, 0.0, 1.0);
    v_TexCoord = vec2(0.0, 0.0);
    EmitVertex();
    
    // 우하단
    gl_Position = vec4(1.0, -1.0, 0.0, 1.0);
    v_TexCoord = vec2(1.0, 0.0);
    EmitVertex();
    
    // 좌상단
    gl_Position = vec4(-1.0, 1.0, 0.0, 1.0);
    v_TexCoord = vec2(0.0, 1.0);
    EmitVertex();
    
    // 우상단
    gl_Position = vec4(1.0, 1.0, 0.0, 1.0);
    v_TexCoord = vec2(1.0, 1.0);
    EmitVertex();
    
    EndPrimitive();
}