#version 430 core

// 버텍스 셰이더에서 받은 값
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;
in float vMaterialID;
in vec3 vViewPos;

// 라이팅 UBO
layout(std140, binding = 0) uniform LightingBlock
{
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용 등)

// ✅ Uniform sampler2D 배열 (최대 32개)
uniform sampler2D textures[32];
uniform int textureCount;  // 실제 텍스처 개수
uniform vec4 debugColor;
uniform bool enableDebug;

void main() 
{   
    int texIndex = int(vMaterialID);
    
    if (texIndex < 0 || texIndex >= textureCount)
    {
        fragColor = vec4(1.0, 0.0, 1.0, 1.0);
        fragDepth = 0.0;
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    if (texColor.a < 0.45) discard;
    
    // ✅ 라이팅 계산
    vec3 normal = normalize(vNormal);
    
    // 1. Ambient
    vec3 ambient = lighting.ambientColor;
    
    // 2. Directional Light
    float diff = max(dot(normal, -lighting.lightDirection), 0.0);
    
    // 양면 라이팅
    if (diff < 0.1) {
        diff = max(dot(-normal, -lighting.lightDirection), 0.0) * 0.5;
    }
    
    vec3 diffuse = diff * lighting.lightColor;
    
    // 3. 최종 라이팅
    vec3 finalLighting = ambient + diffuse;
    vec3 finalColor = texColor.rgb * finalLighting;
    
    if (enableDebug) {
        fragColor = vec4(finalColor, texColor.a) * debugColor;
    } else {
        fragColor = vec4(finalColor, texColor.a);
    }
    
    fragDepth = vViewPos.z / 10000.0;
}

