// DepthDebugInstanced.frag
#version 430

in vec3 debugColor;
out vec4 fragColor;

void main()
{
    fragColor = vec4(debugColor, 1.0);
}