#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;
in vec3 vViewPos;       // 뷰 공간 위치
in vec3 vNormal;

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용 등)

uniform sampler2D atlasTexture;
uniform bool useTexture;  // 텍스처 사용 여부

// 라이팅 UBO
layout(std140, binding = 0) uniform LightingBlock
{
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

void main() 
{
    if (useTexture)
    {
        vec4 texColor = texture(atlasTexture, fTexCoord);
        if (texColor.a < 0.1) discard;
        
        // ✅ 라이팅
        vec3 normal = normalize(vNormal);
        
        vec3 ambient = lighting.ambientColor;
        
        float diff = max(dot(normal, -lighting.lightDirection), 0.0);
        if (diff < 0.2) {
            diff = max(dot(-normal, -lighting.lightDirection), 0.0) * 0.6;
        }
        
        vec3 diffuse = diff * lighting.lightColor;
        vec3 finalLighting = ambient + diffuse;
        
        fragColor = vec4(texColor.rgb * finalLighting, texColor.a);
    }
    else
    {
        fragColor = vec4(vColor, 1.0);
    }
    
    fragDepth = vViewPos.z / 10000.0;
}