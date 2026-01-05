#version 450 core

in GS_OUT 
{
    vec2 texCoord;
    vec2 atlasOffset;
    vec3 viewPos;
    vec3 worldPos;  // ✅ 추가
} fs_in;

// ✅ G-Buffer MRT 출력 (GPUInstancedShader, CrossBillboard와 동일)
layout(location = 0) out vec4 gAlbedo;      // 알베도/컬러
layout(location = 1) out vec4 gPosition;    // 월드 위치
layout(location = 2) out vec4 gNormal;      // 법선 벡터
layout(location = 3) out float gDepth;      // 선형 깊이

uniform sampler2D impostorAtlas;
uniform float atlasSize;
uniform float individualSize;
uniform int enableEdgeLine;
uniform float gMaxDepthDistance = 10000.0;  // ✅ 동적 조정 가능

const float EDGE_THRESHOLD = 0.01;
const float ALPHA_THRESHOLD = 0.05;

void main()
{
    // 1. 디버그 모드 - 엣지 라인
    if (enableEdgeLine == 1)
    {
        bool isEdge = any(lessThan(fs_in.texCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fs_in.texCoord, vec2(1.0 - EDGE_THRESHOLD)));
        if (isEdge)
        {
            // ✅ G-Buffer 디버그 출력
            gAlbedo = vec4(1.0, 1.0, 0.0, 1.0);     // 노란색 엣지
            gPosition = vec4(fs_in.worldPos, 1.0);
            gNormal = vec4(0.0, 0.0, 1.0, 1.0);     // Z축 법선
            gDepth = length(fs_in.viewPos) / gMaxDepthDistance;
            return;
        }
    }
    
    // 2. 아틀라스 UV 계산
    float uvScale = individualSize / atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;
    
    // 3. 텍스처 샘플링 및 Alpha Test
    vec4 color = texture(impostorAtlas, finalUV);
    if (color.a < ALPHA_THRESHOLD) discard;
    
    // ✅ 4. G-Buffer 출력 (라이팅은 Deferred Pass에서 처리)
    // Impostor는 이미 라이팅이 베이크된 텍스처이므로 알베도를 그대로 출력
    gAlbedo = vec4(color.rgb, color.a);
    gPosition = vec4(fs_in.worldPos, 1.0);
    
    // 임포스터는 빌보드이므로 법선을 단순화 (CrossBillboard와 동일)
    // 방향광원이 Z축과의 각도(남중고도)만을 고려
    gNormal = vec4(0.0, 0.0, 1.0, 1.0);
    
    gDepth = length(fs_in.viewPos) / gMaxDepthDistance;  // 정규화된 선형 깊이
}