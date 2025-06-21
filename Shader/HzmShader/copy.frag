#version 420 core

in vec2 TexCoord;
uniform sampler2D depthMap;

void main()
{
    float depthValue = texture(depthMap, TexCoord).r;
    if (depthValue > 1.0f) depthValue = 1.0f;
    if (depthValue < 0.0f) depthValue = 0.0f;
    gl_FragDepth = depthValue;
}