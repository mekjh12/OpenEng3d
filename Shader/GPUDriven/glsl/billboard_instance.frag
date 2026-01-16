#version 450 core

in vec2 pass_texCoord;
in vec3 pass_worldPosition;
in float pass_distanceToCamera;

uniform sampler2D fogTexture;
uniform vec3 fogColor;
uniform float fogDensity;
uniform float alphaThreshold;

out vec4 fragColor;

void main() 
{
    fragColor = vec4(1);
    return;

}