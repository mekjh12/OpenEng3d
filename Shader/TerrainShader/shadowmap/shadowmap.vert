#version 430
in vec3 position;
in vec2 texCoord;

out vec2 Tex1;

void main()
{
    gl_Position = vec4(position, 1.0);
    Tex1 = texCoord;
}