#version 450 core
in GS_OUT {
    vec3 viewPos;
    vec3 worldPos;
    vec3 tangent;
    vec3 bitangent;
    vec3 normal;
    flat float atlasSize;
    flat float individualSize;
    vec2 texCoord;
    vec2 atlasOffset;
} fs_in;

layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;

uniform sampler2D impostorAtlas;
uniform sampler2D normalAtlas;

uniform int enableEdgeLine = 0;
uniform int enableNormalMap = 1;
uniform float gMaxDepthDistance = 10000.0;

const float EDGE_THRESHOLD = 0.03;
const float ALPHA_THRESHOLD = 0.5; // Impostor는 보통 0.5 정도로 컷팅해야 윤곽이 깨끗함

void main()
{
    // 1. 아틀라스 UV 계산 (fs_in에서 받은 값 사용)
    float uvScale = fs_in.individualSize / fs_in.atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;

    // 2. 디버그 모드 (텍스처 샘플링 전 수행하여 성능 절약 가능, but discard 로직 고려 필요)
    if (enableEdgeLine == 1)
    {
        bool isEdge = any(lessThan(fs_in.texCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fs_in.texCoord, vec2(1.0 - EDGE_THRESHOLD)));
        if (isEdge)
        {
            gAlbedo = vec4(1.0, 1.0, 0.0, 1.0);
            gPosition = vec4(fs_in.worldPos, 1.0);
            gNormal = vec4(0.0, 0.0, 1.0, 1.0);
            gDepth = length(fs_in.viewPos) / gMaxDepthDistance;
            return;
        }
    }

    // 3. 알베도 샘플링
    vec4 color = texture(impostorAtlas, finalUV);
    if (color.a < ALPHA_THRESHOLD) discard;

    // 4. 노멀 맵 처리
    vec3 finalNormal;
    
    if (enableNormalMap == 1) {
        // Normal Map은 [0,1] 범위이므로 decoding 필요
        vec3 rawNormal = texture(normalAtlas, finalUV).rgb;
        vec3 tangentNormal = rawNormal * 2.0 - 1.0;

        // TBN 구성 (Column-Major: T가 1열, B가 2열, N이 3열)
        mat3 TBN = mat3(
            normalize(fs_in.tangent),
            normalize(fs_in.bitangent),
            normalize(fs_in.normal)
        );

        // Tangent Space -> World Space 변환
        finalNormal = -normalize(TBN * tangentNormal);
    } else {
        finalNormal = normalize(fs_in.normal);
    }

    // 5. 출력
    gAlbedo = vec4(color.rgb, 1.0); // Alpha Test 통과했으므로 1.0 (Blending 안 할 경우)
    gPosition = vec4(fs_in.worldPos, 1.0);
    gNormal = vec4(finalNormal, 1.0);
    
    // Linear Depth 계산
    gDepth = length(fs_in.viewPos) / gMaxDepthDistance; 
}