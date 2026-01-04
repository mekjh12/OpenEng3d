#version 450 core

in GS_OUT 
{
    vec2 texCoord;
    vec2 atlasOffset;
    vec3 viewPos;
} fs_in;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out float fragDepth;

uniform sampler2D impostorAtlas;
uniform float atlasSize;
uniform float individualSize;
uniform bool enableEdgeLine;

// UBO (Ambient만 사용)
layout(std140, binding = 1) uniform LightingBlock
{
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

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
            fragDepth = fs_in.viewPos.z / 10000.0;
            return;
        }
    }
    
    float uvScale = individualSize / atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;
    
    vec4 color = texture(impostorAtlas, finalUV);
    if (color.a < ALPHA_THRESHOLD) discard;
    
    // ✅ Ambient만 적용 (임포스터는 이미 라이팅 베이크됨)
    fragColor = vec4(color.rgb * lighting.ambientColor, color.a);
    
    fragDepth = fs_in.viewPos.z / 10000.0;
}