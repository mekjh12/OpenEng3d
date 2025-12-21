#version 450 core

in GS_OUT 
{
    vec2 texCoord;
    vec2 atlasOffset;
} fs_in;

out vec4 FragColor;

uniform sampler2D impostorAtlas;
uniform float atlasSize;
uniform float individualSize;
uniform bool enableEdgeLine;

const float EDGE_THRESHOLD = 0.01;
const float ALPHA_THRESHOLD = 0.05;

void main()
{
    // 디버깅용 엣지 라인
    if (enableEdgeLine)
    {
        bool isEdge = any(lessThan(fs_in.texCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fs_in.texCoord, vec2(1.0 - EDGE_THRESHOLD)));
                      
        if (isEdge)
        {
            FragColor = vec4(1.0, 1.0, 0.0, 1.0);
            return;
        }
    }
    
    // 아틀라스 UV 좌표 계산
    float uvScale = individualSize / atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;
    
    // UV 범위 체크 (약간의 여유 추가)
    vec2 uvMin = fs_in.atlasOffset - 0.001;
    vec2 uvMax = fs_in.atlasOffset + uvScale + 0.001;
    
    if (any(greaterThan(finalUV, uvMax)) || any(lessThan(finalUV, uvMin)))
    {
        discard;
    }
    
    // 텍스처 샘플링
    vec4 color = texture(impostorAtlas, finalUV);
    
    // 알파 테스트
    if (color.a < ALPHA_THRESHOLD)
    {
        discard;
    }
    
    FragColor = color;
}