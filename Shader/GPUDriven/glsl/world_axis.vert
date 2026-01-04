#version 450 core
layout(location = 0) in vec3 aPosition;

uniform mat4 vp;

void main() 
{
    // 원점 (0, 0, 0)에서 시작
    gl_Position = vec4(0.0, 0.0, 0.0, 1.0);
}