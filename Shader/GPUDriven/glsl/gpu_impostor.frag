#version 450 core
in GS_OUT {
    vec3 viewPos;
    vec3 worldPos;
    mat3 normalMatrix;  // ← normalMatrix 받기
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

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp;
    vec3 cameraPos;
} camera;

const float EDGE_THRESHOLD = 0.03;
const float ALPHA_THRESHOLD = 0.5;

void main()
{
    // 1. UV 계산
    float uvScale = fs_in.individualSize / fs_in.atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = fs_in.atlasOffset + localUV;
    
    // 2. 디버그 모드
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
    
    // 4. 노멀 처리 (일반 메시와 동일한 방식!)
    vec3 finalNormal;
    
    if (enableNormalMap == 1) 
    {
        // Normal Map 샘플링 및 디코딩 [0,1] -> [-1,1]
        vec3 rawNormal = texture(normalAtlas, finalUV).rgb;
        vec3 tangentNormal = rawNormal * 2.0 - 1.0;
        
        // normalMatrix로 변환 (일반 메시와 동일!)
        finalNormal = normalize(fs_in.normalMatrix * tangentNormal);
    } 
    else 
    {
        // Normal Map 비활성화 시 카메라 방향
        vec3 viewDir = normalize(fs_in.worldPos - camera.cameraPos);
        finalNormal = -viewDir;
    }
    
    // 5. 출력
    gAlbedo = vec4(color.rgb, 1.0);
    gPosition = vec4(fs_in.worldPos, 1.0);
    gNormal = vec4(finalNormal, 1.0);
    gDepth = length(fs_in.viewPos) / gMaxDepthDistance; 
}