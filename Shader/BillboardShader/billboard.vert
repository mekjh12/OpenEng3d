#version 420 core

layout (location = 0) in vec3 position;

uniform vec3 gCameraPos;

void main()
{
    float dist = length(gCameraPos - position);
    if (dist < 100.0f)
    {
         gl_Position = vec4(0.0);
    }
    else
    {
        gl_Position = vec4(position, 1.0);
    }
}