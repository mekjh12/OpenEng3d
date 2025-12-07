#version 450 core

in vec2 vTexCoord;
in vec3 vWorldPos;

out vec4 fragColor;

uniform sampler2D uTexture;

void main() 
{   
    vec4 texColor = texture(uTexture, vTexCoord);
    fragColor = texColor;
    if (fragColor.a < 0.1) discard;
}