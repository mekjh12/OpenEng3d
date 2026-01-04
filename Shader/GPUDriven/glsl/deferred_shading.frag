//-----------------------------------------------------------------------------
// deferred_shading.frag - Deferred Rendering 라이팅 패스
//-----------------------------------------------------------------------------
#version 430 core

// G-Buffer 입력
uniform sampler2D gAlbedo;    // ColorAttachment0
uniform sampler2D gPosition;  // ColorAttachment1
uniform sampler2D gNormal;    // ColorAttachment2
uniform sampler2D gDepth;     // ColorAttachment3 (옵션)

in vec2 TexCoord;
out vec4 fragColor;

// UBO
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

void main()
{
    // ============================================
    // G-Buffer 샘플링
    // ============================================
    vec4 albedo = texture(gAlbedo, TexCoord);
    vec4 worldPos = texture(gPosition, TexCoord);
    vec4 normalData = texture(gNormal, TexCoord);
    float depth = texture(gDepth, TexCoord).r;
    
    // 법선 추출 (정규화 필요)
    vec3 normal = normalize(normalData.xyz);
    
    // ============================================
    // 배경 체크 (깊이가 0이면 배경)
    // ============================================
    if (depth > 0.9999) {
        // 배경색 (하늘색 또는 검은색)
        fragColor = albedo;
        return;
    }
    
    // ============================================
    // Ambient 라이팅
    // ============================================
    vec3 ambient = lighting.ambientColor * albedo.rgb;
    
    // ============================================
    // Diffuse 라이팅 (Lambert)
    // ============================================
    // 광원 방향 정규화 (태양광은 이미 정규화되어 있어야 함)
    vec3 lightDir = normalize(-lighting.lightDirection);
    
    // Lambert diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lighting.lightColor * albedo.rgb;
    
    // ============================================
    // 최종 색상 출력
    // ============================================
    vec3 finalColor = ambient + diffuse;
    
    fragColor = vec4(finalColor, albedo.a);
}