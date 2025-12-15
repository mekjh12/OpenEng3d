#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;

out vec4 fragColor;

uniform sampler2D atlasTexture;

void main()
{
    vec4 texColor = texture(atlasTexture, fTexCoord);
    
    // 알파 테스트
    if (texColor.a < 0.1)
        discard;
    
    fragColor = texColor;
}