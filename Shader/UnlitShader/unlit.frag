#version 450 core
in vec2 vTexCoord;
in float vMaterialID;
in vec3 vViewPos;
in vec3 vNormal;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec4 fragNormal;
layout(location = 2) out float fragDepth;

uniform sampler2D textures[32];
uniform int textureCount;
uniform int enableLighting;

layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

void main()
{
    int texIndex = int(vMaterialID);
    
    if (texIndex < 0 || texIndex >= textureCount)
    {
        fragColor = vec4(1.0, 0.0, 1.0, 1.0);
        fragDepth = 0.0;
        fragNormal = vec4(0.5, 0.5, 1.0, 1.0);
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);    
    if (texColor.a < 0.45) discard;
    
    if (enableLighting == 1) {
        // 앞면/뒷면 구분하여 법선 조정
        vec3 normal = normalize(vNormal);
        if (!gl_FrontFacing) {
            normal = -normal;
        }
    
        // 1. Ambient
        vec3 ambient = lighting.ambientColor;
    
        // 2. Directional Light
        float diff = max(dot(normal, -lighting.lightDirection), 0.0);
    
        // 뒷면은 약간 어둡게
        if (!gl_FrontFacing) {
            diff *= 0.7;
        }
    
        vec3 diffuse = diff * lighting.lightColor;
    
        // 3. 최종 라이팅
        vec3 finalLighting = ambient + diffuse;
        vec3 finalColor = texColor.rgb * finalLighting;
    
        fragColor = vec4(finalColor, texColor.a);
    } else {
        fragColor = texColor;
    }
    
    fragDepth = vViewPos.z / 10000.0;
    
    // ✅ gl_FrontFacing 반영한 법선 인코딩
    vec3 encodedNormal = normalize(vNormal);
    if (!gl_FrontFacing) {
        encodedNormal = -encodedNormal;
    }
    fragNormal = vec4(encodedNormal * 0.5 + 0.5, 1.0);
}