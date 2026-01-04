// grass.frag (또는 vegetation.frag)
#version 430 core

// 입력
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;
in float vMaterialID;
in vec3 vViewPos;

// 라이팅 UBO
layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

// ✅ G-Buffer MRT 출력 (지형과 동일)
layout(location = 0) out vec4 gAlbedo;      // 알베도/컬러
layout(location = 1) out vec4 gPosition;    // 월드 위치
layout(location = 2) out vec4 gNormal;      // 법선 벡터
layout(location = 3) out float gDepth;      // 선형 깊이

// Uniform
uniform sampler2D textures[32];
uniform int textureCount;
uniform vec4 debugColor;
uniform bool enableDebug;
uniform float gMaxDepthDistance = 10000.0;  // ✅ 깊이 정규화 거리

void main() 
{   
    // 1. 텍스처 인덱스 검증
    int texIndex = int(vMaterialID);
    
    if (texIndex < 0 || texIndex >= textureCount)
    {
        // ✅ G-Buffer 에러 출력
        gAlbedo = vec4(1.0, 0.0, 1.0, 1.0);    // 마젠타 (에러 색상)
        gPosition = vec4(vWorldPos, 1.0);
        gNormal = vec4(0.0, 1.0, 0.0, 1.0);    // 기본 법선 (위쪽)
        gDepth = 0.0;
        return;
    }
    
    // 2. 텍스처 샘플링
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    // ✅ Alpha test (투명한 부분 제거)
    if (texColor.a < 0.45) discard;
    
    // 3. 법선 처리 (앞면/뒷면)
    vec3 normal = normalize(vNormal);
    if (!gl_FrontFacing) {
        normal = -normal;  // 뒷면이면 법선 뒤집기
    }
    
    // 4. 디버그 색상 적용 (선택적)
    vec3 albedo = texColor.rgb;
    if (enableDebug) {
        albedo *= debugColor.rgb;
    }
    
    // ✅ 5. G-Buffer 출력  
    gAlbedo = vec4(albedo, texColor.a);         // 알베도 (라이팅 전)
    gPosition = vec4(vWorldPos, 1.0);           // 월드 위치
    gNormal = vec4(normal, 1.0);                // 법선 벡터
    gDepth = length(vViewPos) / gMaxDepthDistance;  // 정규화된 선형 깊이
}