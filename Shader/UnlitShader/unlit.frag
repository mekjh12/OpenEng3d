#version 450 core

in vec2 vTexCoord;
in float vMaterialID;
in vec3 vViewPos;       // 뷰 공간 위치

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용 등)

// ✅ Uniform sampler2D 배열 (최대 32개)
uniform sampler2D textures[32];
uniform int textureCount;  // 실제 텍스처 개수

void main()
{
    int texIndex = int(vMaterialID);
    
    // 범위 체크
    if (texIndex < 0 || texIndex >= textureCount)
    {
        fragColor = vec4(1.0, 0.0, 1.0, 1.0);  // 마젠타 = 에러
        fragDepth = 0.0;  // ✅ 에러 시에도 깊이 출력
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    if (texColor.a < 0.05) discard;
    
    fragColor = texColor;
    
    // ✅ 선형 깊이 출력
    fragDepth = vViewPos.z / 10000.0;
}