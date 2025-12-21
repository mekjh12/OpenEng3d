#version 430 core

// 버텍스 셰이더에서 받은 값
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;
in float vMaterialID;
in vec3 vViewPos;

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용)

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
        fragDepth = 0.0;
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    if (texColor.a < 0.05) discard;
    fragColor = texColor;

    // ✅ 선형 깊이 출력 (안개용)
    fragDepth = vViewPos.z / 10000.0;
    
    // ✅ 깊이 테스트용 (표준 깊이 버퍼)
    gl_FragDepth = vViewPos.z / 10000.0;
}

