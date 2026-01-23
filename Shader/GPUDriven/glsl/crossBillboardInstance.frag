#version 450 core
in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;
in vec3 vViewPos;
in vec3 vNormal;
in vec3 vWorldPos;
in vec3 vTangent;      // ✅ 추가
in vec3 vBitangent;    // ✅ 추가

layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;

uniform sampler2D atlasTexture;
uniform sampler2D normalTexture;
uniform int useTexture;
uniform float gMaxDepthDistance = 10000.0;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

void main() 
{
    // 1. 텍스처 샘플링
    vec4 texColor = texture(atlasTexture, fTexCoord);
    if (texColor.a < 0.45) discard;
    
    // 1. TBN 구성 (GS에서 온 벡터 신뢰)
    vec3 N = normalize(vNormal);
    vec3 T = normalize(vTangent);
    vec3 B = normalize(vBitangent); 

    // Gram-Schmidt (정밀도 보정)
    T = normalize(T - dot(T, N) * N);
    // B는 다시 cross하지 않고 보정만 하거나, 
    // 정석대로 하려면: B = normalize(cross(N, T)); (N이 올바른 방향이라면)
    
    mat3 TBN = mat3(T, B, N);
    
   // 2. 노말 맵 적용
    vec3 texNormal = texture(normalTexture, fTexCoord).rgb;
    texNormal = texNormal * 2.0 - 1.0;
    vec3 worldNormal = normalize(TBN * texNormal);
    
    // 3. 양면 렌더링 처리 (표준 로직)
    if (!gl_FrontFacing) {
        worldNormal = -worldNormal;
    }
    
    // 5. 알베도
    vec3 albedo = (useTexture == 0) ? vColor : texColor.rgb;
    
    // 6. G-Buffer 출력
    gAlbedo = vec4(albedo, texColor.a);
    gPosition = vec4(vWorldPos, 1.0);    
    gNormal = vec4(worldNormal, 1.0);
    gDepth = length(vViewPos) / gMaxDepthDistance;
}