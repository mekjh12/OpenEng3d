#version 450 core
in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;
in vec3 vViewPos;
in vec3 vNormal;
in vec3 vWorldPos;  // ✅ 추가

// MRT 출력
layout(location = 0) out vec4 fragColor;
layout(location = 1) out float fragDepth;

uniform sampler2D atlasTexture;
uniform sampler2D normalTexture;
uniform int useTexture;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp;
    vec3 cameraPos;
} camera;

// 라이팅 UBO
layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;
    vec3 lightDirection;
    vec3 lightColor;
} lighting;

void main() 
{
    vec4 texColor = texture(atlasTexture, fTexCoord);
    if (texColor.a < 0.45) discard;        

    if (useTexture == 1)
    {
        // ✅ Ambient 라이팅
        vec3 ambient = lighting.ambientColor;
    
        // ✅ 카메라 → 물체 방향 벡터 (inverse 계산 없이 바로 사용)
        vec3 viewDir = normalize(camera.cameraPos - vWorldPos);
        
        // ✅ 뷰 방향과 라이트 방향의 내적으로 diffuse 계산
        vec3 viewDirXY = normalize(vec3(viewDir.xy, 1.0));
        float diff = max(dot(viewDirXY, -lighting.lightDirection), 0.0);
        
        vec3 diffuse = diff * lighting.lightColor;
        vec3 finalLighting = ambient + diffuse;
    
        fragColor = vec4(texColor.rgb * finalLighting, texColor.a);
    }
    else
    {
        fragColor = vec4(texColor.rgb, 1);
    }
    
    fragDepth = vViewPos.z / 10000.0;
}