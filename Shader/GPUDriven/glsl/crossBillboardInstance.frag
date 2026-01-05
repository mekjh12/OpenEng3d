#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;
in vec3 vViewPos;
in vec3 vNormal;
in vec3 vWorldPos;

// G-Buffer MRT 출력 (GPUInstancedShader와 동일)
layout(location = 0) out vec4 gAlbedo;      // 알베도/컬러
layout(location = 1) out vec4 gPosition;    // 월드 위치
layout(location = 2) out vec4 gNormal;      // 법선 벡터
layout(location = 3) out float gDepth;      // 선형 깊이

uniform sampler2D atlasTexture;
uniform int useTexture;
uniform float gMaxDepthDistance = 10000.0;  // 동적 조정 가능

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp;
    vec3 cameraPos;
} camera;

void main() 
{
    // 1. 텍스처 샘플링 및 Alpha Test
    vec4 texColor = texture(atlasTexture, fTexCoord);
    if (texColor.a < 0.45) discard;
    
    // 2. 법선 처리 (양면 렌더링)
    vec3 normal = normalize(vNormal);
    if (!gl_FrontFacing) {
        normal = -normal;  // 뒷면이면 법선 뒤집기
    }
    
    // 3. 알베도 색상 (디버그 모드 지원)
    vec3 albedo = texColor.rgb;
    if (useTexture == 0) {
        albedo = vColor;  // 디버그 색상
    }
    
    //4. G-Buffer 출력 (라이팅은 Deferred Pass에서 처리)
    gAlbedo = vec4(albedo, texColor.a);
    gPosition = vec4(vWorldPos, 1.0);

    // 크로스빌보드는 점마다 법선벡터를 적용할 수 없어서
    // 법선은 단순화하여 방향광원이 z축과의 각도(남중고도)만을 고려한다.
    gNormal = vec4(0, 0, 1.0, 1.0);                 
    gDepth = length(vViewPos) / gMaxDepthDistance;  // 정규화된 선형 깊이
}