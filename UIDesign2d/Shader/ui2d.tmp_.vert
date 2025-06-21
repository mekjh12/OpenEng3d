#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoords;

uniform mat4 model;
uniform bool horzflip;

void main()
{
    gl_Position = model * vec4(aPos.x, aPos.y, 0, 1);

    if (horzflip)
    {
        TexCoords = vec2(1.0f - aTexCoord.x, aTexCoord.y);
    }
    else
    {
        TexCoords = aTexCoord;
    }
}
