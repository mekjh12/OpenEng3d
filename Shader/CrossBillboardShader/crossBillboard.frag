#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;

out vec4 fragColor;

uniform sampler2D atlasTexture;
uniform bool enableEdgeLine;

const float EDGE_THRESHOLD = 0.01;

void main()
{
    if (enableEdgeLine)
    {
        bool isEdge = any(lessThan(fTexCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fTexCoord, vec2(1.0 - EDGE_THRESHOLD)));
                      
        if (isEdge)
        {
            fragColor = vec4(1.0, 1.0, 0.0, 1.0);
            return;
        }
    }

    vec4 texColor = texture(atlasTexture, fTexCoord);
    
    // 알파 테스트
    if (texColor.a < 0.1)
        discard;
    
    fragColor = texColor;
}