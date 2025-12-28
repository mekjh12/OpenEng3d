#version 450 core
in GS_OUT 
{
    vec2 texCoord;
    vec2 atlasOffset;
    vec3 viewPos;  // ✅ 추가
} fs_in;

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용)

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
            fragColor = vec4(1.0, 1.0, 0.0, 1.0);
            fragDepth = fs_in.viewPos.z / 10000.0;  // ✅ 엣지라인에도 깊이 출력
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
    
    fragColor = color;
    
    // ✅ 선형 깊이 출력 (안개용)
    fragDepth = fs_in.viewPos.z / 10000.0;
}