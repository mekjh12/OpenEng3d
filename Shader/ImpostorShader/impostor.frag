#version 430

in GS_OUT {
    vec2 texCoord;
    vec2 atlasOffset;  // 인스턴스별로 전달받음
} fs_in;

out vec4 fragColor;

uniform sampler2D impostorAtlas;
uniform float atlasSize;
uniform float individualSize;
uniform bool enableEdgeLine;

const float EDGE_THRESHOLD = 0.01;
const float ALPHA_THRESHOLD = 0.05;

void main()
{
    if (enableEdgeLine)
    {
        bool isEdge = any(lessThan(fs_in.texCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fs_in.texCoord, vec2(1.0 - EDGE_THRESHOLD)));
                      
        if (isEdge)
        {
            fragColor = vec4(1.0, 1.0, 0.0, 1.0);
            return;
        }
    }
    
    float uvScale = individualSize / atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;  // varying에서 받은 offset 사용
    
    vec2 uvMin = fs_in.atlasOffset - 0.001;
    vec2 uvMax = fs_in.atlasOffset + uvScale + 0.001;
    
    if (any(greaterThan(finalUV, uvMax)) || any(lessThan(finalUV, uvMin)))
    {
        discard;
    }
    
    vec4 color = texture(impostorAtlas, finalUV);
    
    if (color.a < ALPHA_THRESHOLD)
    {
        discard;
    }
    
    fragColor = color;
}