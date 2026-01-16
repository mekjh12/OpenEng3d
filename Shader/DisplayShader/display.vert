#version 430 core
in vec2 position;
in vec2 texCoord;

out vec2 TexCoord;

void main(void)
{
    gl_Position = vec4(position, 0.0, 1.0);
    TexCoord = texCoord;
}