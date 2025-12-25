#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;
in vec3 vViewPos;       // 뷰 공간 위치

// MRT 출력
layout(location = 0) out vec4 fragColor;   // 컬러
layout(location = 1) out float fragDepth;  // 선형 깊이 (안개용 등)

uniform sampler2D atlasTexture;
uniform bool useTexture;  // 텍스처 사용 여부

void main() 
{
    if (useTexture)
    {
        // ✅ 텍스처 샘플링
        vec4 texColor = texture(atlasTexture, fTexCoord);
        
        // 알파 테스트
        if (texColor.a < 0.1)
            discard;
        
        fragColor = texColor;
    }
    else
    {
        // 디버그 모드: batchID 색상 표시
        fragColor = vec4(vColor, 1.0);
    }

    // ✅ 선형 깊이 출력
    fragDepth = vViewPos.z / 10000.0;
}